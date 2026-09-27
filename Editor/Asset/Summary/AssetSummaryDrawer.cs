using Lin.Editor.Settings;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Asset
{
    /// <summary>
    /// 在 Project 窗口绘制资源/文件夹注释；.cs 文件额外从源码头部注释里提取一行作为脚本注释。
    /// </summary>
    [InitializeOnLoad]
    public static class AssetSummaryDrawer
    {
        private const float DISPLAY_OFFSET = 25;
        private const int MIN_FONT_SIZE = 8;

        private static Dictionary<string, (string description, string tooltip)> descriptionMap;
        private static HashSet<string> readedList;

        // 双击检测相关字段
        private static float lastClickTime = 0f;
        private static Vector2 lastClickPosition = Vector2.zero;
        private static string lastClickedGuid = string.Empty;
        private const float DOUBLE_CLICK_TIME = 0.3f;
        private const float DOUBLE_CLICK_DISTANCE = 5f;

        static AssetSummaryDrawer()
        {
            readedList = new HashSet<string>();
            descriptionMap = new Dictionary<string, (string description, string tooltip)>();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
        }

        [MenuItem("Assets/Edit Annotation - 修改注释")]
        private static void SetAssetDescription()
        {
            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            AssetSummaryWindow.ShowAssetSummary(assetPath);
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            if (descriptionMap.ContainsKey(guid))
            {
                var labelRect = new Rect(selectionRect);

                // 创建样式并启用富文本
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                style.richText = true;
                style.fontSize = EditorAnnotationSettings.AssetSummaryTitleSize;

                // 计算注释文本
                string description = descriptionMap[guid].description;

                // 对象Tooltip
                var content = new GUIContent(description);
                content.tooltip = descriptionMap[guid].tooltip;

                // 网格格子竖长（宽 gridSize，高 gridSize + 14 的名字行），列表行横扁，按形状分模式
                bool isGridView = selectionRect.width <= selectionRect.height;
                if (isGridView)
                {
                    // 名字行下面只剩 15px 行间距，注释画在这一条里
                    // ponytail: 字高超过行距时会压到下一格图标顶部几像素，点击热区同样多出这几像素；要干净就给高度封顶
                    style.alignment = TextAnchor.MiddleCenter;
                    labelRect = new Rect(selectionRect.x + 1f, selectionRect.yMax + 1f, Mathf.Max(0f, selectionRect.width - 2f), 0f);
                    content = FitToWidth(style, content, labelRect.width);
                    labelRect.height = style.CalcHeight(content, labelRect.width);
                }
                else
                {
                    style.alignment = TextAnchor.MiddleRight;
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileNameWithoutExtension(assetPath);
                    float nameEndX = selectionRect.x + EditorStyles.label.CalcSize(new GUIContent(fileName)).x + DISPLAY_OFFSET;
                    float availableWidth = Mathf.Max(0f, selectionRect.xMax - nameEndX - 5f);
                    labelRect.width = Mathf.Min(style.CalcSize(content).x, availableWidth);
                    labelRect.x = selectionRect.xMax - labelRect.width - 5f;
                }

                // 绘制注释
                GUI.Label(labelRect, content, style);

                // 点击修改
                var currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (labelRect.Contains(currentEvent.mousePosition))
                    {
                        switch (EditorAnnotationSettings.AssetSummaryEditWay)
                        {
                            case EClickType.无响应:
                                break;

                            case EClickType.单击:
                                AssetSummaryWindow.ShowAssetSummary(AssetDatabase.GUIDToAssetPath(guid));
                                currentEvent.Use();
                                break;

                            case EClickType.双击:
                            default:
                                // 双击检测逻辑
                                float currentTime = (float)EditorApplication.timeSinceStartup;
                                Vector2 currentPosition = currentEvent.mousePosition;

                                // 检查是否为双击
                                if (currentTime - lastClickTime <= DOUBLE_CLICK_TIME &&
                                    Vector2.Distance(currentPosition, lastClickPosition) <= DOUBLE_CLICK_DISTANCE &&
                                    lastClickedGuid == guid)
                                {
                                    // 双击事件
                                    AssetSummaryWindow.ShowAssetSummary(AssetDatabase.GUIDToAssetPath(guid));

                                    // 重置双击检测状态
                                    lastClickTime = 0f;
                                    lastClickPosition = Vector2.zero;
                                    lastClickedGuid = string.Empty;
                                    currentEvent.Use();
                                }
                                else
                                {
                                    // 记录当前点击信息
                                    lastClickTime = currentTime;
                                    lastClickPosition = currentPosition;
                                    lastClickedGuid = guid;
                                }
                                break;
                        }
                    }
                }
            }
            else if (!readedList.Contains(guid))
            {
                readedList.Add(guid);

                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (importer == null)
                    return;

                var summary = importer.GetAnnotation();
                string title = summary.title;
                if (!string.IsNullOrEmpty(title))
                    title = summary.GetRichTitle();

                if (importer.assetPath.EndsWith(".cs"))
                {
                    // 读取.cs头部内容（行数上限见设置页），按每个标识各提取一次（多个标识同时命中时会各加一段前缀，与原实现一致）
                    string fileContent = EditorAnnotationSettings.ReadScriptHead(importer.assetPath,
                        EditorAnnotationSettings.ScriptDescriptionScanMaxLines);

                    foreach (var filter in EditorAnnotationSettings.DescriptionFilters)
                        FindDescriptions(filter);

                    void FindDescriptions(string filter)
                    {
                        if (!fileContent.Contains(filter))
                            return;

                        // filter 来自设置页可编辑文本，不转义时一个 "(" 就抛 ArgumentException 打断整轮 Project 绘制，
                        // 而 guid 在上面已进 readedList，该资源的注释要到下次域重载才显示
                        var descMatch = System.Text.RegularExpressions.Regex.Match(
                            fileContent,
                            @$"{System.Text.RegularExpressions.Regex.Escape(filter)}([^\n]+)");

                        if (!descMatch.Success)
                            return;

                        string descValue = descMatch.Groups[1].Value.Trim();
                        if (string.IsNullOrWhiteSpace(descValue))
                            return;

                        descValue = $"<size={EditorAnnotationSettings.AssetSummaryTitleSize}>" +
                                    $"<color=#{ColorUtility.ToHtmlStringRGB(EditorAnnotationSettings.ScriptDescriptionColor)}>{descValue}</color>" +
                                    $"</size>";
                        if (EditorAnnotationSettings.ScriptDescriptionBold)
                            descValue = $"<b>{descValue}</b>";

                        if (EditorAnnotationSettings.ScriptDescriptionItalic)
                            descValue = $"<i>{descValue}</i>";

                        title = $"{descValue} {title}";
                    }
                }

                if (!string.IsNullOrEmpty(title))
                    descriptionMap.Add(guid, (title, summary.description));
            }
        }

        /// <summary>
        /// 注释字号写在 &lt;size=N&gt; 标签里（文件夹标题与脚本头注释两条路径都写），改 style.fontSize 压不住，
        /// 所以格子放不下时按比例把标签里的字号一起改小。
        /// </summary>
        private static GUIContent FitToWidth(GUIStyle style, GUIContent content, float maxWidth)
        {
            float textWidth = style.CalcSize(content).x;
            if (maxWidth <= 0f || textWidth <= maxWidth || textWidth <= 0f)
                return content;

            int fitted = Mathf.Max(MIN_FONT_SIZE, Mathf.FloorToInt(style.fontSize * maxWidth / textWidth) - 1);
            if (fitted >= style.fontSize)
                return content;

            style.fontSize = fitted;
            return new GUIContent(System.Text.RegularExpressions.Regex.Replace(content.text, @"<size=\d+>", $"<size={fitted}>"),
                content.image, content.tooltip);
        }

        public static void Refresh(string guid)
        {
            descriptionMap.Remove(guid);
            readedList.Remove(guid);
        }

        public static void Refresh()
        {
            descriptionMap.Clear();
            readedList.Clear();
        }
    }
}
