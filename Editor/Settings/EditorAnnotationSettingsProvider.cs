using Lin.Editor.Asset;
using Lin.Editor.Toolbar.Element;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Settings
{
    /// <summary>
    /// Project Settings 里的包配置页，分资源注释 / 场景物体注释 / 脚本注释 / 描述标识 / 导入策略 / SceneView 叠加六段。
    /// 原先注释三段住在项目私有的 EditorSettings_SO 上，整页搬过来会把 Learn 的框架配置一起带进包，
    /// 所以只保留与注释有关的三段；导入策略与 SceneView 叠加是后续并级进来的。
    /// </summary>
    internal static class EditorAnnotationSettingsProvider
    {
        public const string PagePath = "Project/Lin Editor Tools";
        private static SettingsProvider provider;
        private static GUIStyle sizePreviewStyle;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            provider = new SettingsProvider(PagePath, SettingsScope.Project)
            {
                label = EditorAnnotationLocalization.Text(EAnnotationText.SettingsPageLabel),
                keywords = new HashSet<string> { "Annotation", "Hierarchy", "Toolbar", "注释", "Description", "Summary", "脚本", "Import", "导入策略" },
                guiHandler = _ => DrawGUI()
            };
            EditorAnnotationLocalization.LanguageChanged -= OnLanguageChanged;
            EditorAnnotationLocalization.LanguageChanged += OnLanguageChanged;
            return provider;
        }

        [MenuItem("Lin/Editor Tools - 编辑器工具/Annotation Settings - 设置界面")]
        private static void OpenFromMenu() => OpenSettings();

        private static void OpenSettings() => SettingsService.OpenProjectSettings(PagePath);

        /// <summary>包自带的主工具栏入口：一个按钮直达本页。</summary>
        [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "注释", "打开注释设置")]
        private static void OpenFromToolbar() => SettingsService.OpenProjectSettings(PagePath);

        private static void OnLanguageChanged()
        {
            if (provider != null)
                provider.label = EditorAnnotationLocalization.Text(EAnnotationText.SettingsPageLabel);
        }

        private static void DrawGUI()
        {
            bool changed = false;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.SwitchLanguage), GUILayout.Width(90)))
                EditorAnnotationLocalization.ToggleLanguage();
            GUILayout.EndHorizontal();

            Section(EditorAnnotationLocalization.Text(EAnnotationText.AssetSection));
            changed |= IntSliderField(nameof(EditorAnnotationSettings.AssetSummaryTitleSize), EditorAnnotationLocalization.Text(EAnnotationText.TitleSize),
                EditorAnnotationSettings.AssetSummaryTitleSize, EditorAnnotationLocalization.Text(EAnnotationText.SizePreview),
                v => EditorAnnotationSettings.AssetSummaryTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.AssetSummaryEditWay), EditorAnnotationLocalization.Text(EAnnotationText.EditOnClick),
                EditorAnnotationSettings.AssetSummaryEditWay, v => EditorAnnotationSettings.AssetSummaryEditWay = v);

            Section(EditorAnnotationLocalization.Text(EAnnotationText.SceneSection));
            changed |= IntSliderField(nameof(EditorAnnotationSettings.SceneObjectDescriptionTitleSize), EditorAnnotationLocalization.Text(EAnnotationText.TitleSize),
                EditorAnnotationSettings.SceneObjectDescriptionTitleSize, EditorAnnotationLocalization.Text(EAnnotationText.SizePreview),
                v => EditorAnnotationSettings.SceneObjectDescriptionTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.SceneObjectDescriptionEditWay), EditorAnnotationLocalization.Text(EAnnotationText.EditOnClick),
                EditorAnnotationSettings.SceneObjectDescriptionEditWay, v => EditorAnnotationSettings.SceneObjectDescriptionEditWay = v);

            Section(EditorAnnotationLocalization.Text(EAnnotationText.ScriptSection));
            changed |= ColorField(nameof(EditorAnnotationSettings.ScriptDescriptionColor), EditorAnnotationLocalization.Text(EAnnotationText.Color),
                EditorAnnotationSettings.ScriptDescriptionColor, v => EditorAnnotationSettings.ScriptDescriptionColor = v);
            changed |= BoolField(nameof(EditorAnnotationSettings.ScriptDescriptionBold), EditorAnnotationLocalization.Text(EAnnotationText.Bold),
                EditorAnnotationSettings.ScriptDescriptionBold, v => EditorAnnotationSettings.ScriptDescriptionBold = v);
            changed |= BoolField(nameof(EditorAnnotationSettings.ScriptDescriptionItalic), EditorAnnotationLocalization.Text(EAnnotationText.Italic),
                EditorAnnotationSettings.ScriptDescriptionItalic, v => EditorAnnotationSettings.ScriptDescriptionItalic = v);
            changed |= IntField(nameof(EditorAnnotationSettings.ScriptDescriptionScanMaxLines), EditorAnnotationLocalization.Text(EAnnotationText.ScanMaxLines),
                EditorAnnotationSettings.ScriptDescriptionScanMaxLines, v => EditorAnnotationSettings.ScriptDescriptionScanMaxLines = v);

            changed |= DescriptionMarkersList();

            ImportPolicySection();
            SceneViewOverlaySection();

            // Project 窗口的绘制结果带缓存，样式或标识变了要重扫；Hierarchy 每帧现读，不需要
            if (changed)
                AssetSummaryDrawer.Refresh();
        }

        /// <summary>
        /// 导入策略段：字段从 com.lin.editor-asset-import 并进同一个 SO，仍住 ProjectSettings/LinEditorTools.asset。
        /// 这一段改动只影响导入器、不影响 Project 窗口的绘制，所以不进上面的 changed；
        /// 但 AssetDatabase 看不见这个对象，写完必须立刻 Save()。
        /// </summary>
        private static void ImportPolicySection()
        {
            var settings = ToolsSettings_SO.Instance;
            bool changed = false;

            Section(EditorAnnotationLocalization.Text(EAnnotationText.ImportSection));
            changed |= BoolField(nameof(ToolsSettings_SO.enabled), EditorAnnotationLocalization.Text(EAnnotationText.ImportEnabled),
                settings.enabled, v => settings.enabled = v, EditorAnnotationLocalization.Text(EAnnotationText.ImportEnabledTooltip));
            changed |= BoolField(nameof(ToolsSettings_SO.videoEnabled), EditorAnnotationLocalization.Text(EAnnotationText.ImportVideoEnabled),
                settings.videoEnabled, v => settings.videoEnabled = v, EditorAnnotationLocalization.Text(EAnnotationText.ImportVideoEnabledTooltip));

            Section(EditorAnnotationLocalization.Text(EAnnotationText.PlatformSection));
            changed |= StringListField(nameof(ToolsSettings_SO.overriddenPlatforms), EditorAnnotationLocalization.Text(EAnnotationText.OverriddenPlatforms),
                settings.overriddenPlatforms, v => settings.overriddenPlatforms = v, EditorAnnotationLocalization.Text(EAnnotationText.OverriddenPlatformsTooltip));
            changed |= StringListField(nameof(ToolsSettings_SO.followBuildDefaultPlatforms), EditorAnnotationLocalization.Text(EAnnotationText.FollowBuildDefaultPlatforms),
                settings.followBuildDefaultPlatforms, v => settings.followBuildDefaultPlatforms = v,
                EditorAnnotationLocalization.Text(EAnnotationText.FollowBuildDefaultPlatformsTooltip));
            changed |= IntField(nameof(ToolsSettings_SO.globalMaxTextureSize), EditorAnnotationLocalization.Text(EAnnotationText.GlobalMaxTextureSize),
                settings.globalMaxTextureSize, v => settings.globalMaxTextureSize = v, EditorAnnotationLocalization.Text(EAnnotationText.GlobalMaxTextureSizeTooltip));
            changed |= StringField(nameof(ToolsSettings_SO.pcPlatform), EditorAnnotationLocalization.Text(EAnnotationText.PcPlatform),
                settings.pcPlatform, v => settings.pcPlatform = v);

            Section(EditorAnnotationLocalization.Text(EAnnotationText.NamingSection));
            changed |= StringListField(nameof(ToolsSettings_SO.bgmKeywords), EditorAnnotationLocalization.Text(EAnnotationText.BgmKeywords),
                settings.bgmKeywords, v => settings.bgmKeywords = v);
            changed |= StringListField(nameof(ToolsSettings_SO.voiceKeywords), EditorAnnotationLocalization.Text(EAnnotationText.VoiceKeywords),
                settings.voiceKeywords, v => settings.voiceKeywords = v);
            changed |= StringField(nameof(ToolsSettings_SO.loopSuffix), EditorAnnotationLocalization.Text(EAnnotationText.LoopSuffix),
                settings.loopSuffix, v => settings.loopSuffix = v, EditorAnnotationLocalization.Text(EAnnotationText.LoopSuffixTooltip));

            if (changed)
                ToolsSettings_SO.Save();
        }

        /// <summary>
        /// SceneView 叠加段：SceneRendererInfoDrawer 的开关与颜色，随绘制器从宿主 EditorSettings_SO 移植而来。
        /// 与导入策略段同理：改动不进外层 changed（不影响 Project 窗口绘制），但必须立刻 Save()。
        /// </summary>
        private static void SceneViewOverlaySection()
        {
            var settings = ToolsSettings_SO.Instance;
            bool changed = false;

            Section(EditorAnnotationLocalization.Text(EAnnotationText.SceneViewSection));
            changed |= BoolField(nameof(ToolsSettings_SO.sceneRendererInfoEnabled), EditorAnnotationLocalization.Text(EAnnotationText.RendererInfoEnabled),
                settings.sceneRendererInfoEnabled, v => settings.sceneRendererInfoEnabled = v, EditorAnnotationLocalization.Text(EAnnotationText.RendererInfoEnabledTooltip));
            changed |= ColorField(nameof(ToolsSettings_SO.sceneRendererInfoColor), EditorAnnotationLocalization.Text(EAnnotationText.RendererInfoColor),
                settings.sceneRendererInfoColor, v => settings.sceneRendererInfoColor = v);

            if (changed)
                ToolsSettings_SO.Save();
        }

        private static void Section(string title)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static bool IntSliderField(string key, string label, int current, string preview, System.Action<int> apply)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(label, GetTooltip(key)), GUILayout.Width(EditorGUIUtility.labelWidth));
            int value = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(current, 8f, 32f)), 8, 32);
            EditorGUILayout.LabelField(value.ToString(), EditorStyles.miniLabel, GUILayout.Width(24f));

            if (sizePreviewStyle == null)
            {
                sizePreviewStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            sizePreviewStyle.fontSize = value;
            GUILayout.Label(preview, sizePreviewStyle, GUILayout.Width(90f), GUILayout.Height(Mathf.Max(EditorGUIUtility.singleLineHeight, value + 2f)));
            EditorGUILayout.EndHorizontal();

            if (value == current) return false;
            apply(value);
            return true;
        }

        /// <summary>字符串数组一行一格地编辑。null（老文件缺这个字段）先当空表，点一次"添加"即成表。</summary>
        private static bool StringListField(string key, string label, string[] current, System.Action<string[]> apply, string tooltip = null)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip ?? GetTooltip(key)));

            var items = new List<string>(current ?? new string[0]);
            bool changed = false;
            for (int i = 0; i < items.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string item = EditorGUILayout.TextField(items[i]);
                if (item != items[i])
                {
                    items[i] = item;
                    changed = true;
                }

                if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.RemoveMarker), GUILayout.Width(72f)))
                {
                    items.RemoveAt(i);
                    changed = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.AddItem)))
            {
                items.Add(string.Empty);
                changed = true;
            }

            if (changed)
                apply(items.ToArray());

            return changed;
        }

        private static bool DescriptionMarkersList()
        {
            EditorGUILayout.LabelField(new GUIContent(
                EditorAnnotationLocalization.Text(EAnnotationText.DescriptionMarkers),
                EditorAnnotationLocalization.Text(EAnnotationText.DescriptionMarkersTooltip)));

            var markers = new List<string>(EditorAnnotationSettings.DescriptionFiltersText.Split('\n'));
            bool changed = false;
            for (int i = 0; i < markers.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string marker = EditorGUILayout.TextField(markers[i]);
                if (marker != markers[i])
                {
                    markers[i] = marker;
                    changed = true;
                }

                if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.RemoveMarker), GUILayout.Width(72f)))
                {
                    markers.RemoveAt(i);
                    changed = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.AddMarker)))
            {
                markers.Add(string.Empty);
                changed = true;
            }

            if (changed)
                EditorAnnotationSettings.DescriptionFiltersText = string.Join("\n", markers);

            return changed;
        }

        private static bool BoolField(string key, string label, bool current, System.Action<bool> apply, string tooltip = null)
        {
            var value = EditorGUILayout.Toggle(new GUIContent(label, tooltip ?? GetTooltip(key)), current);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool StringField(string key, string label, string current, System.Action<string> apply, string tooltip = null)
        {
            var value = EditorGUILayout.TextField(new GUIContent(label, tooltip ?? GetTooltip(key)), current ?? string.Empty);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool IntField(string key, string label, int current, System.Action<int> apply, string tooltip = null)
        {
            var value = EditorGUILayout.IntField(new GUIContent(label, tooltip ?? GetTooltip(key)), current);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool ColorField(string key, string label, Color current, System.Action<Color> apply)
        {
            var value = EditorGUILayout.ColorField(new GUIContent(label, GetTooltip(key)), current);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool EnumField(string key, string label, EClickType current, System.Action<EClickType> apply)
        {
            var options = new[]
            {
                EditorAnnotationLocalization.Text(EAnnotationText.ClickNone),
                EditorAnnotationLocalization.Text(EAnnotationText.ClickSingle),
                EditorAnnotationLocalization.Text(EAnnotationText.ClickDouble)
            };
            int selected = (int)current;
            if (selected < 0 || selected >= options.Length)
                selected = 0;
            var value = (EClickType)EditorGUILayout.Popup(new GUIContent(label, GetTooltip(key)), selected, options);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static string GetTooltip(string key) =>
            EditorAnnotationLocalization.Text(EAnnotationText.PreferenceTooltip).Replace("{0}", key);
    }

    internal enum EAnnotationLanguage
    {
        Chinese,
        English
    }

    internal enum EAnnotationText
    {
        SettingsPageLabel,
        SwitchLanguage,
        AssetSection,
        TitleSize,
        EditOnClick,
        SceneSection,
        ScriptSection,
        Color,
        Bold,
        Italic,
        ScanMaxLines,
        DescriptionMarkers,
        DescriptionMarkersTooltip,
        AddMarker,
        RemoveMarker,
        SizePreview,
        PreferenceTooltip,
        ClickNone,
        ClickSingle,
        ClickDouble,
        AssetWindowTitle,
        SceneWindowTitle,
        Target,
        Title,
        Description,
        RichTextGuide,
        RichBoldExample,
        RichItalicExample,
        RichSizeExample,
        RichColorExample,
        Save,
        SaveAndClose,
        Cancel,
        Delete,
        Preview,
        EmptyPreview,
        ToolbarAnnotation,
        ToolbarAnnotationTooltip,
        TimeScaleLabel,
        TimeScaleTooltip,
        ImportSection,
        ImportEnabled,
        ImportEnabledTooltip,
        ImportVideoEnabled,
        ImportVideoEnabledTooltip,
        PlatformSection,
        OverriddenPlatforms,
        OverriddenPlatformsTooltip,
        FollowBuildDefaultPlatforms,
        FollowBuildDefaultPlatformsTooltip,
        GlobalMaxTextureSize,
        GlobalMaxTextureSizeTooltip,
        PcPlatform,
        NamingSection,
        BgmKeywords,
        VoiceKeywords,
        LoopSuffix,
        LoopSuffixTooltip,
        AddItem,
        SceneViewSection,
        RendererInfoEnabled,
        RendererInfoEnabledTooltip,
        RendererInfoColor
    }

    internal static class EditorAnnotationLocalization
    {
        private static readonly Dictionary<EAnnotationText, (string chinese, string english)> Texts =
            new Dictionary<EAnnotationText, (string chinese, string english)>
            {
                { EAnnotationText.SettingsPageLabel, ("Lin 编辑器工具", "Lin Editor Tools") },
                { EAnnotationText.SwitchLanguage, ("English", "中文") },
                { EAnnotationText.AssetSection, ("资源注释（Project 窗口）", "Asset annotations (Project window)") },
                { EAnnotationText.TitleSize, ("注释字体大小", "Annotation font size") },
                { EAnnotationText.EditOnClick, ("点击注释修改", "Edit annotation on click") },
                { EAnnotationText.SceneSection, ("场景物体注释（Hierarchy 行内）", "Scene object annotations (Hierarchy)") },
                { EAnnotationText.ScriptSection, ("脚本注释（取自 .cs 源码头部注释）", "Script annotations (read from .cs headers)") },
                { EAnnotationText.Color, ("颜色", "Color") },
                { EAnnotationText.Bold, ("粗体", "Bold") },
                { EAnnotationText.Italic, ("斜体", "Italic") },
                { EAnnotationText.ScanMaxLines, ("每个脚本扫描最大行数", "Max scanned lines per script") },
                { EAnnotationText.DescriptionMarkers, ("描述标识", "Description markers") },
                { EAnnotationText.DescriptionMarkersTooltip, ("在 .cs 文件里识别描述行的前缀，一行一个", "Prefixes used to identify description lines in .cs files, one per line") },
                { EAnnotationText.AddMarker, ("添加标识", "Add marker") },
                { EAnnotationText.RemoveMarker, ("移除", "Remove") },
                { EAnnotationText.SizePreview, ("注释", "Note") },
                { EAnnotationText.PreferenceTooltip, ("存储于 ProjectSettings/LinEditorTools.asset（设置项 {0}，随工程进版本库、团队共享）", "Stored in the {0} setting of ProjectSettings/LinEditorTools.asset (versioned with the project, shared across the team)") },
                { EAnnotationText.ClickNone, ("无响应", "None") },
                { EAnnotationText.ClickSingle, ("单击", "Single click") },
                { EAnnotationText.ClickDouble, ("双击", "Double click") },
                { EAnnotationText.AssetWindowTitle, ("文件注释", "Asset Annotation") },
                { EAnnotationText.SceneWindowTitle, ("场景物体注释", "Scene Object Annotation") },
                { EAnnotationText.Target, ("目标", "Target") },
                { EAnnotationText.Title, ("标题", "Title") },
                { EAnnotationText.Description, ("说明", "Description") },
                { EAnnotationText.RichTextGuide, ("富文本语法说明", "Rich text syntax") },
                { EAnnotationText.RichBoldExample, ("<b>文本</b> - 粗体", "<b>Text</b> - Bold") },
                { EAnnotationText.RichItalicExample, ("<i>文本</i> - 斜体", "<i>Text</i> - Italic") },
                { EAnnotationText.RichSizeExample, ("<size=14>文本</size> - 字体大小", "<size=14>Text</size> - Font size") },
                { EAnnotationText.RichColorExample, ("<color=#ff0000>文本</color> - 字体颜色", "<color=#ff0000>Text</color> - Font color") },
                { EAnnotationText.Save, ("保存", "Save") },
                { EAnnotationText.SaveAndClose, ("保存并关闭", "Save and Close") },
                { EAnnotationText.Cancel, ("取消", "Cancel") },
                { EAnnotationText.Delete, ("删除", "Delete") },
                { EAnnotationText.Preview, ("注释效果预览", "Annotation Preview") },
                { EAnnotationText.EmptyPreview, ("预览将在这里显示...", "Preview will appear here...") },
                { EAnnotationText.ToolbarAnnotation, ("注释", "Note") },
                { EAnnotationText.ToolbarAnnotationTooltip, ("打开注释设置", "Open annotation settings") },
                { EAnnotationText.TimeScaleLabel, ("时间倍率", "Time Scale") },
                { EAnnotationText.TimeScaleTooltip, ("TimeScale控制器", "Time Scale Controller") },
                { EAnnotationText.ImportSection, ("导入策略", "Import policy") },
                { EAnnotationText.ImportEnabled, ("启用导入自动处理", "Apply import settings automatically") },
                { EAnnotationText.ImportEnabledTooltip, ("关闭时三个导入回调直接返回，不动任何 importer。装包后默认关闭。", "When off, the import callbacks return immediately and touch no importer. Off by default after install.") },
                { EAnnotationText.ImportVideoEnabled, ("启用视频转码", "Enable video transcoding") },
                { EAnnotationText.ImportVideoEnabledTooltip, ("转码在导入期执行，高分辨率源可能耗时数小时。", "Transcoding runs during import; high-resolution sources can take hours.") },
                { EAnnotationText.PlatformSection, ("平台分组", "Platform groups") },
                { EAnnotationText.OverriddenPlatforms, ("覆盖平台", "Overridden platforms") },
                { EAnnotationText.OverriddenPlatformsTooltip, ("逐个写 SetPlatformTextureSettings 覆盖。Web 目标不要放进来：建了覆盖会让桌面浏览器拿不到 BC 系列。", "Writes a SetPlatformTextureSettings override for each entry. Keep web targets out: an override leaves desktop browsers without the BC family.") },
                { EAnnotationText.FollowBuildDefaultPlatforms, ("跟随构建默认", "Follow build defaults") },
                { EAnnotationText.FollowBuildDefaultPlatformsTooltip, ("不建平台覆盖，格式由引擎按构建目标选；尺寸走下面的全局上限。", "No platform override; the engine picks the format per build target, and size is capped by the global maximum below.") },
                { EAnnotationText.GlobalMaxTextureSize, ("全局纹理上限", "Global max texture size") },
                { EAnnotationText.GlobalMaxTextureSizeTooltip, ("作用于未覆盖的平台，也是 Web / 小游戏的尺寸闸门。", "Applies to platforms without an override, and is the size gate for web and mini-game targets.") },
                { EAnnotationText.PcPlatform, ("PC 平台名", "PC platform name") },
                { EAnnotationText.NamingSection, ("命名约定", "Naming conventions") },
                { EAnnotationText.BgmKeywords, ("BGM 关键字", "BGM keywords") },
                { EAnnotationText.VoiceKeywords, ("语音关键字", "Voice keywords") },
                { EAnnotationText.LoopSuffix, ("循环后缀", "Loop suffix") },
                { EAnnotationText.LoopSuffixTooltip, ("clip 名以此结尾则 loopTime = true。", "A clip whose name ends with this gets loopTime = true.") },
                { EAnnotationText.AddItem, ("添加", "Add") },
                { EAnnotationText.SceneViewSection, ("SceneView 叠加", "SceneView overlay") },
                { EAnnotationText.RendererInfoEnabled, ("显示选中物体信息", "Show selected object info") },
                { EAnnotationText.RendererInfoEnabledTooltip, ("在 SceneView 里绘制选中物体的包围盒，并统计顶点数与三角面数。", "Draws the selected object's bounds in the SceneView with vertex and triangle counts.") },
                { EAnnotationText.RendererInfoColor, ("信息颜色", "Info color") }
            };

        public static event System.Action LanguageChanged;

        public static bool IsEnglish => EditorAnnotationSettings.Language == EAnnotationLanguage.English;

        public static string Text(EAnnotationText key)
        {
            var pair = Texts[key];
            return IsEnglish ? pair.english : pair.chinese;
        }

        public static void ToggleLanguage()
        {
            EditorAnnotationSettings.Language = IsEnglish ? EAnnotationLanguage.Chinese : EAnnotationLanguage.English;
            LanguageChanged?.Invoke();
        }

        public static void SetLabel(VisualElement root, string name, EAnnotationText key)
        {
            var label = root.Q<Label>(name);
            if (label != null)
                label.text = Text(key);
        }
    }
}
