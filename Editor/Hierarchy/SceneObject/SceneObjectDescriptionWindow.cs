using Lin.Editor.Settings;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_6000_4_OR_NEWER
using InstanceId = UnityEngine.EntityId;
#else
using InstanceId = System.Int32;
#endif

namespace Lin.Editor.Hierarchy.SceneObject
{
    public class SceneObjectDescriptionWindow : EditorWindow
    {
        // 走包路径而非 project:// GUID 引用：包内资产被重新导入时 GUID 会变，写死的引用会静默失效。
        private const string AssetFolder = EditorAnnotationSettings.PackagePath + "/Editor/Hierarchy/SceneObject";

        private ColorField titleColorField;
        private TextField titleField;
        private TextField descriptionField;
        private Label previewLabel;
        private ObjectField targetField;

        private InstanceId instanceId;

        /// <summary>
        /// 根据当前选中的GameObject显示描述窗口
        /// </summary>
        [MenuItem("GameObject/Edit Annotation - 修改注释", false, 48)]
        private static void ShowBySelect()
        {
            GameObject selectedObject = Selection.activeGameObject;
#if UNITY_6000_4_OR_NEWER
            InstanceId instanceId = selectedObject.GetEntityId();
#else
            int instanceId = selectedObject.GetInstanceID();
#endif
            ShowDescription(instanceId);
        }

        public static void ShowDescription(InstanceId instanceId)
        {
            SceneObjectDescriptionWindow wnd = GetWindow<SceneObjectDescriptionWindow>(true,
                EditorAnnotationLocalization.Text(EAnnotationText.SceneWindowTitle), true);
            wnd.minSize = new Vector2(400, 500);
            wnd.maxSize = new Vector2(600, 700);
            wnd.instanceId = instanceId;
            wnd.Show();
            // 首次打开时 CreateGUI 还没跑完，这一次会被 LoadExistingComment 的判空挡掉，由 CreateGUI 末尾补做；
            // 已建好 UI 的窗口（再次打开）由这里立即刷新
            wnd.LoadExistingComment();
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{AssetFolder}/SceneObjectDescriptionWindow.uxml");
            if (visualTree == null)
            {
                Debug.LogError($"[Lin Editor Tools] 找不到界面定义 {AssetFolder}/SceneObjectDescriptionWindow.uxml");
                return;
            }

            visualTree.CloneTree(rootVisualElement);
            EditorAnnotationSettings.ApplyWindowStyleSheet(rootVisualElement);

            // 查找获取UI组件
            titleColorField = rootVisualElement.Q<ColorField>("TitleColor");
            titleField = rootVisualElement.Q<TextField>("TitleField");
            descriptionField = rootVisualElement.Q<TextField>("ToolTipField");
            previewLabel = rootVisualElement.Q<Label>("previewLabel");
            targetField = rootVisualElement.Q<ObjectField>("CurrentObjectField");

            ApplyLocalization();

            var tooltipField = descriptionField.Q("unity-text-input");
            // 设置描述字段的最小高度为100像素
            if (tooltipField != null)
            {
                tooltipField.style.minHeight = 100;
            }

            // 获取按钮组件
            var saveButton = rootVisualElement.Q<Button>("SaveButton");
            var cancelButton = rootVisualElement.Q<Button>("CancelButton");
            var deleteButton = rootVisualElement.Q<Button>("DeleteButton");

            // 绑定事件
            BindEvents(saveButton, cancelButton, deleteButton);

            LoadExistingComment();
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
            if (rootVisualElement == null || titleColorField == null)
                return;

            titleContent = new GUIContent(EditorAnnotationLocalization.Text(EAnnotationText.SceneWindowTitle));
            targetField.label = EditorAnnotationLocalization.Text(EAnnotationText.Target);
            titleColorField.label = EditorAnnotationLocalization.Text(EAnnotationText.Color);
            titleField.label = EditorAnnotationLocalization.Text(EAnnotationText.Title);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "WindowTitle", EAnnotationText.SceneWindowTitle);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "DescriptionLabel", EAnnotationText.Description);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichTextGuide", EAnnotationText.RichTextGuide);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichBoldExample", EAnnotationText.RichBoldExample);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichItalicExample", EAnnotationText.RichItalicExample);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichSizeExample", EAnnotationText.RichSizeExample);
            EditorAnnotationLocalization.SetLabel(rootVisualElement, "RichColorExample", EAnnotationText.RichColorExample);
            rootVisualElement.Q<Button>("SaveButton").text = EditorAnnotationLocalization.Text(EAnnotationText.Save);
            rootVisualElement.Q<Button>("CancelButton").text = EditorAnnotationLocalization.Text(EAnnotationText.Cancel);
            rootVisualElement.Q<Button>("DeleteButton").text = EditorAnnotationLocalization.Text(EAnnotationText.Delete);
            rootVisualElement.Q<Foldout>("PreviewFoldout").text = EditorAnnotationLocalization.Text(EAnnotationText.Preview);
            UpdatePreview();
        }

        /// <summary>
        /// 绑定UI事件
        /// </summary>
        /// <param name="saveButton">保存按钮</param>
        /// <param name="cancelButton">取消按钮</param>
        /// <param name="deleteButton">删除按钮</param>
        private void BindEvents(Button saveButton, Button cancelButton, Button deleteButton)
        {
            // 保存按钮事件
            saveButton?.RegisterCallback<ClickEvent>(evt => SaveComment());

            // 取消按钮事件
            cancelButton?.RegisterCallback<ClickEvent>(evt => Close());

            // 删除按钮事件
            deleteButton?.RegisterCallback<ClickEvent>(evt => DeleteComment());

            // 标题和描述字段变化时更新预览
            titleField?.RegisterValueChangedCallback(evt => UpdatePreview());
            descriptionField?.RegisterValueChangedCallback(evt => UpdatePreview());
            titleColorField?.RegisterValueChangedCallback(evt => UpdatePreview());
        }

        /// <summary>
        /// 加载现有的注释信息。UI 未建好时（CreateGUI 之前被调用）直接跳过，等 CreateGUI 末尾再刷一次
        /// </summary>
        private void LoadExistingComment()
        {
            if (titleColorField == null || targetField == null)
                return;

            var description = SceneObjectDescriptionsMap.GetInstance().GetDescription(instanceId);
            titleColorField.value = description.color;
            titleField.value = description.title;
            descriptionField.value = description.description;
#if UNITY_6000_4_OR_NEWER
            targetField.value = UnityEditor.EditorUtility.EntityIdToObject(instanceId);
#else
            targetField.value = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
#endif

            UpdatePreview();
        }

        /// <summary>
        /// 更新预览显示
        /// </summary>
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

        /// <summary>
        /// 保存注释
        /// </summary>
        private void SaveComment()
        {
            SceneObjectDescriptionsMap.GetInstance().SetDescription(instanceId,
                titleField.value,
                descriptionField.value,
                titleColorField.value);

            Close();
        }

        /// <summary>
        /// 删除注释
        /// </summary>
        private void DeleteComment()
        {
            SceneObjectDescriptionsMap.GetInstance().RemoveDescription(instanceId);
            Close();
        }
    }
}
