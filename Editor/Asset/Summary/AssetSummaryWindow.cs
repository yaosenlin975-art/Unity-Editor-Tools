using Lin.Editor.Settings;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Asset
{
    /// <summary>编辑某个资源/文件夹注释的窗口。</summary>
    public class AssetSummaryWindow : EditorWindow
    {
        private const string AssetFolder = EditorAnnotationSettings.PackagePath + "/Editor/Asset/Summary";

        private ObjectField currentObjectField;
        private ColorField titleColorField;
        private TextField titleField;
        private TextField descriptionField;
        private Label previewLabel;

        private string assetPath;

        public static void ShowAssetSummary(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || (!File.Exists(assetPath) && !Directory.Exists(assetPath)))
            {
                Debug.LogError($"[Lin Editor Tools] {assetPath} 资源不存在");
                return;
            }

            AssetSummaryWindow wnd = GetWindow<AssetSummaryWindow>(true,
                EditorAnnotationLocalization.Text(EAnnotationText.AssetWindowTitle), true);
            wnd.minSize = new Vector2(400, 500);
            wnd.maxSize = new Vector2(600, 700);
            wnd.assetPath = assetPath;
            wnd.InitializeComponents();
            wnd.Show();
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{AssetFolder}/AssetSummaryWindow.uxml");
            if (visualTree == null)
            {
                Debug.LogError($"[Lin Editor Tools] 找不到界面定义 {AssetFolder}/AssetSummaryWindow.uxml");
                return;
            }

            visualTree.CloneTree(rootVisualElement);
            EditorAnnotationSettings.ApplyWindowStyleSheet(rootVisualElement);

            // 查找获取UI组件
            currentObjectField = rootVisualElement.Q<ObjectField>("CurrentObjectField");
            titleColorField = rootVisualElement.Q<ColorField>("TitleColor");
            titleField = rootVisualElement.Q<TextField>("TitleField");
            descriptionField = rootVisualElement.Q<TextField>("ToolTipField");
            previewLabel = rootVisualElement.Q<Label>("previewLabel");

            ApplyLocalization();

            var tooltipField = descriptionField.Q("unity-text-input");
            // 设置描述字段的最小高度为100像素
            if (tooltipField != null)
            {
                tooltipField.style.minHeight = 100;
            }

            // 绑定事件
            BindEvents();

            // 首次打开时 ShowAssetSummary 那一次 InitializeComponents 跑在 CreateGUI 之前，字段全是 null
            // 而整段静默跳过，窗口显示空表单，点保存就把原注释写空；已建好 UI 的窗口仍由那一次负责
            InitializeComponents();
        }

        private void OnEnable()
        {
            EditorAnnotationLocalization.LanguageChanged -= OnLanguageChanged;
            EditorAnnotationLocalization.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            EditorAnnotationLocalization.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged() => ApplyLocalization();

        private void ApplyLocalization()
        {
            if (rootVisualElement == null || currentObjectField == null)
                return;

            titleContent = new GUIContent(EditorAnnotationLocalization.Text(EAnnotationText.AssetWindowTitle));
            currentObjectField.label = EditorAnnotationLocalization.Text(EAnnotationText.Target);
            titleColorField.label = EditorAnnotationLocalization.Text(EAnnotationText.Color);
            titleField.label = EditorAnnotationLocalization.Text(EAnnotationText.Title);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "WindowTitle", EAnnotationText.AssetWindowTitle);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "DescriptionLabel", EAnnotationText.Description);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichTextGuide", EAnnotationText.RichTextGuide);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichBoldExample", EAnnotationText.RichBoldExample);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichItalicExample", EAnnotationText.RichItalicExample);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichSizeExample", EAnnotationText.RichSizeExample);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichColorExample", EAnnotationText.RichColorExample);
            rootVisualElement.Q<Button>("SaveButton").text = EditorAnnotationLocalization.Text(EAnnotationText.Save);
            rootVisualElement.Q<Button>("SaveAndCloseBtn").text = EditorAnnotationLocalization.Text(EAnnotationText.SaveAndClose);
            rootVisualElement.Q<Button>("CancelButton").text = EditorAnnotationLocalization.Text(EAnnotationText.Cancel);
            rootVisualElement.Q<Button>("DeleteButton").text = EditorAnnotationLocalization.Text(EAnnotationText.Delete);
            rootVisualElement.Q<Foldout>("PreviewFoldout").text = EditorAnnotationLocalization.Text(EAnnotationText.Preview);
            UpdatePreview();
        }

        /// <summary>
        /// 初始化UI组件的默认值
        /// </summary>
        private void InitializeComponents()
        {
            // 加载当前资源
            if (currentObjectField != null && !string.IsNullOrEmpty(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                currentObjectField.value = asset;
                currentObjectField.SetEnabled(false); // 设置为只读
            }

            // 加载现有的注释信息
            LoadExistingComment();
        }

        private void BindEvents()
        {
            var saveButton = rootVisualElement.Q<Button>("SaveButton");
            var saveAndCloseButton = rootVisualElement.Q<Button>("SaveAndCloseBtn");
            var cancelButton = rootVisualElement.Q<Button>("CancelButton");
            var deleteButton = rootVisualElement.Q<Button>("DeleteButton");

            saveButton?.RegisterCallback<ClickEvent>(evt => SaveComment(false));
            saveAndCloseButton?.RegisterCallback<ClickEvent>(evt => SaveComment(true));
            cancelButton?.RegisterCallback<ClickEvent>(evt => Close());
            deleteButton?.RegisterCallback<ClickEvent>(evt => DeleteComment());

            // 标题和描述字段变化时更新预览
            titleField?.RegisterValueChangedCallback(evt => UpdatePreview());
            descriptionField?.RegisterValueChangedCallback(evt => UpdatePreview());
            titleColorField?.RegisterValueChangedCallback(evt => UpdatePreview());
        }

        private void LoadExistingComment()
        {
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null || titleColorField == null)
                return;

            var summary = importer.GetAnnotation();

            // 无注释时 titleColor 为 null，TryParse 失败不改 color，直接回写会把出厂默认标题色冲成全黑
            if (!ColorUtility.TryParseHtmlString($"#{summary.titleColor}", out var color))
                color = EditorAnnotationSettings.DescriptionTitleDefaultColor;

            titleColorField.value = color;
            titleField.value = RemoveColorTag(summary.title);
            descriptionField.value = summary.description;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (previewLabel == null) return;

            string title = titleField?.value ?? "";
            string description = descriptionField?.value ?? "";
            Color color = titleColorField?.value ?? EditorAnnotationSettings.DescriptionTitleDefaultColor;

            string colorHex = ColorUtility.ToHtmlStringRGBA(color);
            string previewText = "";

            if (!string.IsNullOrEmpty(title))
            {
                previewText += $"<color=#{colorHex}><b>{titleField.value}</b></color>\n";
            }

            if (!string.IsNullOrEmpty(description))
            {
                previewText += description;
            }

            previewLabel.text = string.IsNullOrEmpty(previewText)
                ? EditorAnnotationLocalization.Text(EAnnotationText.EmptyPreview)
                : previewText;
        }

        private void SaveComment(bool closeWindow)
        {
            AssetImporter.GetAtPath(assetPath).SetAnnotation(new AssetSummary
            {
                title = titleField.value,
                titleColor = ColorUtility.ToHtmlStringRGB(titleColorField.value),
                description = descriptionField.value,
            });
            if (closeWindow)
                Close();
        }

        private void DeleteComment()
        {
            AssetImporter.GetAtPath(assetPath).RemoveAnnotation();
            Close();
        }

        /// <summary>
        /// 移除颜色与其它富文本标签，保留内部文本，供标题输入框回显纯文本。
        /// </summary>
        private string RemoveColorTag(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return string.Empty;

            string result = Regex.Replace(comment, @"<color=#[0-9A-Fa-f]+>(.*?)</color>", "$1",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            result = Regex.Replace(result, @"</?[bi]>", "", RegexOptions.IgnoreCase);

            return result;
        }
    }
}
