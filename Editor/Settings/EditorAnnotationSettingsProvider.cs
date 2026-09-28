using Lin.Editor.Asset;
using Lin.Editor.Toolbar.Element;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Settings
{
    /// <summary>
    /// Project Settings 里的包配置。主页 <see cref="PagePath"/>（Lin Framework）保持原样画全部分段；
    /// 另注册若干子页作快速跳转，点进去只画对应那一段：三块注释设置先归到「注释」父节点下
    /// （注释 / 资源注释 / 场景物体注释 / 脚本注释），再加导入策略 / SceneView 叠加 / TODO 收集。
    /// 子页靠路径前缀挂到主页节点下。
    /// </summary>
    internal static class EditorAnnotationSettingsProvider
    {
        public const string PagePath = "Project/Lin Framework";

        private const string SubPageRoot = PagePath + "/";

        private static readonly HashSet<string> SettingsKeywords = new HashSet<string>
        {
            "Annotation", "Hierarchy", "Toolbar", "注释", "Description", "Summary", "脚本", "Import", "导入策略", "TODO", "Todo"
        };

        // 语言切换要重设每个 provider 的 label，所以建好后都留着；key=path，重注册（Notify 会重扫）时覆盖
        private static readonly Dictionary<string, (SettingsProvider provider, EAnnotationText label)> Providers =
            new Dictionary<string, (SettingsProvider, EAnnotationText)>();

        private static GUIStyle sizePreviewStyle;

        [SettingsProvider]
        public static SettingsProvider CreateHomeProvider() =>
            Register(PagePath, EAnnotationText.SettingsPageLabel, DrawHomeGui);

        [SettingsProvider]
        public static SettingsProvider CreateAnnotationsProvider() =>
            Register(SubPageRoot + "Annotations", EAnnotationText.SubAnnotations, DrawAnnotationsSection);

        [SettingsProvider]
        public static SettingsProvider CreateAssetProvider() =>
            Register(SubPageRoot + "Annotations/Asset", EAnnotationText.SubAsset, DrawAssetSection);

        [SettingsProvider]
        public static SettingsProvider CreateSceneProvider() =>
            Register(SubPageRoot + "Annotations/Scene", EAnnotationText.SubScene, DrawSceneSection);

        [SettingsProvider]
        public static SettingsProvider CreateScriptProvider() =>
            Register(SubPageRoot + "Annotations/Script", EAnnotationText.SubScript, DrawScriptSection);

        [SettingsProvider]
        public static SettingsProvider CreateImportProvider() =>
            Register(SubPageRoot + "Import", EAnnotationText.SubImport, ImportPolicySection);

        [SettingsProvider]
        public static SettingsProvider CreateSceneViewProvider() =>
            Register(SubPageRoot + "SceneView", EAnnotationText.SubSceneView, SceneViewOverlaySection);

        [SettingsProvider]
        public static SettingsProvider CreateTodoProvider() =>
            Register(SubPageRoot + "Todo", EAnnotationText.SubTodo, TodoSection);

        private static SettingsProvider Register(string path, EAnnotationText label, System.Action gui)
        {
            var created = new SettingsProvider(path, SettingsScope.Project)
            {
                label = EditorAnnotationLocalization.Text(label),
                keywords = SettingsKeywords,
                guiHandler = _ => gui()
            };

            Providers[path] = (created, label);
            EditorAnnotationLocalization.LanguageChanged -= OnLanguageChanged;
            EditorAnnotationLocalization.LanguageChanged += OnLanguageChanged;
            return created;
        }

        [MenuItem("Lin/Lin Framework - 设置界面")]
        private static void OpenFromMenu() => OpenSettings();

        private static void OpenSettings() => SettingsService.OpenProjectSettings(PagePath);

        /// <summary>包自带的主工具栏入口：一个按钮直达本页。</summary>
        [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "注释", "打开注释设置")]
        private static void OpenFromToolbar() => SettingsService.OpenProjectSettings(PagePath);

        private static void OnLanguageChanged()
        {
            foreach (var entry in Providers.Values)
                entry.provider.label = EditorAnnotationLocalization.Text(entry.label);

            // 侧栏树的标题在建树时缓存，只改 label 不生效；通知 SettingsService 重建树
            SettingsService.NotifySettingsProviderChanged();
        }

        /// <summary>主页：语言开关 + 全部分段（与改造前一致）。子页各自只画对应一段。</summary>
        private static void DrawHomeGui()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.SwitchLanguage), GUILayout.Width(90)))
                EditorAnnotationLocalization.ToggleLanguage();
            GUILayout.EndHorizontal();

            DrawAssetSection();
            DrawSceneSection();
            DrawScriptSection();
            ImportPolicySection();
            SceneViewOverlaySection();
            TodoSection();
        }

        private static void DrawAssetSection()
        {
            bool changed = false;

            Section(EditorAnnotationLocalization.Text(EAnnotationText.AssetSection));
            changed |= IntSliderField(nameof(EditorAnnotationSettings.AssetSummaryTitleSize), EditorAnnotationLocalization.Text(EAnnotationText.TitleSize),
                EditorAnnotationSettings.AssetSummaryTitleSize, EditorAnnotationLocalization.Text(EAnnotationText.SizePreview),
                v => EditorAnnotationSettings.AssetSummaryTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.AssetSummaryEditWay), EditorAnnotationLocalization.Text(EAnnotationText.EditOnClick),
                EditorAnnotationSettings.AssetSummaryEditWay, v => EditorAnnotationSettings.AssetSummaryEditWay = v);

            // Project 窗口的绘制结果带缓存，样式或标识变了要重扫；Hierarchy 每帧现读，不需要
            if (changed)
                AssetSummaryDrawer.Refresh();
        }

        private static void DrawSceneSection()
        {
            bool changed = false;

            Section(EditorAnnotationLocalization.Text(EAnnotationText.SceneSection));
            changed |= IntSliderField(nameof(EditorAnnotationSettings.SceneObjectDescriptionTitleSize), EditorAnnotationLocalization.Text(EAnnotationText.TitleSize),
                EditorAnnotationSettings.SceneObjectDescriptionTitleSize, EditorAnnotationLocalization.Text(EAnnotationText.SizePreview),
                v => EditorAnnotationSettings.SceneObjectDescriptionTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.SceneObjectDescriptionEditWay), EditorAnnotationLocalization.Text(EAnnotationText.EditOnClick),
                EditorAnnotationSettings.SceneObjectDescriptionEditWay, v => EditorAnnotationSettings.SceneObjectDescriptionEditWay = v);

            if (changed)
                AssetSummaryDrawer.Refresh();
        }

        private static void DrawScriptSection()
        {
            bool changed = false;

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

            if (changed)
                AssetSummaryDrawer.Refresh();
        }

        /// <summary>「注释」类目页：三块注释设置合并显示，供父节点展开时先看一眼。</summary>
        private static void DrawAnnotationsSection()
        {
            DrawAssetSection();
            DrawSceneSection();
            DrawScriptSection();
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

            // 平台分组（覆盖平台 / 跟随构建默认 / 全局纹理上限 / PC 平台名）已改为包内固定默认，不再暴露设置。
            Section(EditorAnnotationLocalization.Text(EAnnotationText.NamingSection));
            changed |= FolderListField(nameof(ToolsSettings_SO.bgmFolders), EditorAnnotationLocalization.Text(EAnnotationText.BgmFolders),
                settings.bgmFolders, v => settings.bgmFolders = v, EditorAnnotationLocalization.Text(EAnnotationText.BgmFoldersTooltip));
            changed |= FolderListField(nameof(ToolsSettings_SO.voiceFolders), EditorAnnotationLocalization.Text(EAnnotationText.VoiceFolders),
                settings.voiceFolders, v => settings.voiceFolders = v, EditorAnnotationLocalization.Text(EAnnotationText.VoiceFoldersTooltip));
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

        /// <summary>
        /// TODO 收集段：TodoCollector 与 Learn 的 ScriptTodoWindow 共用的输出目录与两个忽略表，
        /// 随 TodoCollector 从宿主 EditorSettings_SO 并级。与导入策略段同理：改动不进外层 changed，
        /// 但 AssetDatabase 看不见这个 SO，写完必须立刻 Save()。
        /// </summary>
        private static void TodoSection()
        {
            var settings = ToolsSettings_SO.Instance;
            bool changed = false;

            Section(EditorAnnotationLocalization.Text(EAnnotationText.TodoSection));
            changed |= StringField(nameof(ToolsSettings_SO.docsOutputFolderName), EditorAnnotationLocalization.Text(EAnnotationText.TodoDocsFolder),
                settings.docsOutputFolderName, v => settings.docsOutputFolderName = v, EditorAnnotationLocalization.Text(EAnnotationText.TodoDocsFolderTooltip));
            settings.todoIgnoreFolders ??= new List<string>();
            settings.todoIgnoreAssemblies ??= new List<string>();
            changed |= FolderListField(nameof(ToolsSettings_SO.todoIgnoreFolders), EditorAnnotationLocalization.Text(EAnnotationText.TodoIgnoreFolders),
                settings.todoIgnoreFolders.ToArray(), v => settings.todoIgnoreFolders = new List<string>(v), null);
            AssemblyListField(EditorAnnotationLocalization.Text(EAnnotationText.TodoIgnoreAssemblies), null, settings.todoIgnoreAssemblies);

            if (changed)
                ToolsSettings_SO.Save();
        }

        /// <summary>
        /// 文件夹列表：添加时弹系统文件夹选择窗，只收工程内目录并转成 "Assets/..." 相对路径。
        /// 行内可编辑/移除，返回是否有改动，由调用方统一落盘。
        /// </summary>
        private static bool FolderListField(string key, string label, string[] current, System.Action<string[]> apply, string tooltip)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip ?? GetTooltip(key)));
            var items = new List<string>(current ?? new string[0]);
            bool changed = DrawEditableRows(items);

            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.AddItem)))
            {
                string picked = EditorUtility.OpenFolderPanel(label, ProjectRoot, string.Empty);
                if (!string.IsNullOrEmpty(picked))
                {
                    if (!TryToProjectRelativePath(picked, out string relative))
                        EditorUtility.DisplayDialog("无法忽略", "只能选择工程目录内的文件夹。", "好");
                    else if (!items.Contains(relative))
                    {
                        items.Add(relative);
                        changed = true;
                    }
                }
            }

            if (changed)
                apply(items.ToArray());

            return changed;
        }

        /// <summary>忽略程序集：添加时弹出选择窗，列出 Assets 下参与编译的程序集名（与忽略判定口径一致）。</summary>
        private static void AssemblyListField(string label, string tooltip, List<string> items)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip));
            bool changed = DrawEditableRows(items);

            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.AddItem)))
            {
                var rect = GUILayoutUtility.GetLastRect();
                ListPickerWindow.Show(rect, CollectAssetAssemblyNames(), picked =>
                {
                    if (!items.Contains(picked))
                    {
                        items.Add(picked);
                        ToolsSettings_SO.Save();
                    }
                });
            }

            if (changed)
                ToolsSettings_SO.Save();
        }

        /// <summary>一行一格地编辑字符串列表，返回是否有改动；行尾是移除按钮。</summary>
        private static bool DrawEditableRows(List<string> items)
        {
            bool changed = false;
            for (int i = 0; i < items.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string edited = EditorGUILayout.TextField(items[i]);
                if (edited != items[i])
                {
                    items[i] = edited;
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

            return changed;
        }

        private static string projectRoot;

        private static string ProjectRoot
        {
            get
            {
                if (string.IsNullOrEmpty(projectRoot))
                    projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');

                return projectRoot;
            }
        }

        /// <summary>绝对路径转工程相对路径（正斜杠）；不在工程内返回 false。</summary>
        private static bool TryToProjectRelativePath(string absolutePath, out string relative)
        {
            relative = null;
            if (string.IsNullOrEmpty(absolutePath))
                return false;

            string normalized = absolutePath.Replace('\\', '/').TrimEnd('/');
            string root = ProjectRoot.TrimEnd('/');
            if (!normalized.StartsWith(root + "/", System.StringComparison.OrdinalIgnoreCase))
                return false;

            relative = normalized.Substring(root.Length + 1);
            return !string.IsNullOrEmpty(relative);
        }

        /// <summary>Assets 下参与编译的程序集名（asmdef 名），供忽略程序集选择窗使用。</summary>
        private static List<string> CollectAssetAssemblyNames()
        {
            string assetsRoot = ProjectRoot.TrimEnd('/') + "/Assets/";
            var names = new List<string>();
            foreach (var assembly in CompilationPipeline.GetAssemblies())
            {
                if (string.IsNullOrEmpty(assembly.name) || names.Contains(assembly.name))
                    continue;

                var files = assembly.sourceFiles;
                if (files == null)
                    continue;

                for (int i = 0; i < files.Length; i++)
                {
                    string full = files[i].Replace('\\', '/');
                    if (!Path.IsPathRooted(full))
                        full = ProjectRoot.TrimEnd('/') + "/" + full;

                    if (full.StartsWith(assetsRoot, System.StringComparison.OrdinalIgnoreCase))
                    {
                        names.Add(assembly.name);
                        break;
                    }
                }
            }

            names.Sort(System.StringComparer.Ordinal);
            return names;
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
        NamingSection,
        BgmFolders,
        BgmFoldersTooltip,
        VoiceFolders,
        VoiceFoldersTooltip,
        LoopSuffix,
        LoopSuffixTooltip,
        AddItem,
        SceneViewSection,
        RendererInfoEnabled,
        RendererInfoEnabledTooltip,
        RendererInfoColor,
        TodoSection,
        TodoDocsFolder,
        TodoDocsFolderTooltip,
        TodoIgnoreFolders,
        TodoIgnoreAssemblies,
        SubAnnotations,
        SubAsset,
        SubScene,
        SubScript,
        SubImport,
        SubSceneView,
        SubTodo
    }

    internal static class EditorAnnotationLocalization
    {
        private static readonly Dictionary<EAnnotationText, (string chinese, string english)> Texts =
            new Dictionary<EAnnotationText, (string chinese, string english)>
            {
                { EAnnotationText.SettingsPageLabel, ("Lin Framework", "Lin Framework") },
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
                { EAnnotationText.NamingSection, ("文件夹识别", "Folder matching") },
                { EAnnotationText.BgmFolders, ("BGM 文件夹", "BGM folders") },
                { EAnnotationText.BgmFoldersTooltip, ("资源路径落在其中任一目录下即判为 BGM，一行一个。", "An asset whose path is under any of these folders is treated as BGM, one per line.") },
                { EAnnotationText.VoiceFolders, ("语音文件夹", "Voice folders") },
                { EAnnotationText.VoiceFoldersTooltip, ("资源路径落在其中任一目录下即判为语音，一行一个。", "An asset whose path is under any of these folders is treated as voice, one per line.") },
                { EAnnotationText.LoopSuffix, ("循环后缀", "Loop suffix") },
                { EAnnotationText.LoopSuffixTooltip, ("clip 名以此结尾则 loopTime = true。", "A clip whose name ends with this gets loopTime = true.") },
                { EAnnotationText.AddItem, ("添加", "Add") },
                { EAnnotationText.SceneViewSection, ("SceneView 叠加", "SceneView overlay") },
                { EAnnotationText.RendererInfoEnabled, ("显示选中物体信息", "Show selected object info") },
                { EAnnotationText.RendererInfoEnabledTooltip, ("在 SceneView 里绘制选中物体的包围盒，并统计顶点数与三角面数。", "Draws the selected object's bounds in the SceneView with vertex and triangle counts.") },
                { EAnnotationText.RendererInfoColor, ("信息颜色", "Info color") },
                { EAnnotationText.TodoSection, ("TODO 收集", "TODO collection") },
                { EAnnotationText.TodoDocsFolder, ("输出目录名", "Output folder name") },
                { EAnnotationText.TodoDocsFolderTooltip, ("Todos.md 写到项目根下的这个目录。", "Todos.md is written to this folder under the project root.") },
                { EAnnotationText.TodoIgnoreFolders, ("忽略的文件夹", "Ignored folders") },
                { EAnnotationText.TodoIgnoreAssemblies, ("忽略的程序集", "Ignored assemblies") },
                { EAnnotationText.SubAnnotations, ("注释", "Annotations") },
                { EAnnotationText.SubAsset, ("资源注释", "Asset annotations") },
                { EAnnotationText.SubScene, ("场景物体注释", "Scene object annotations") },
                { EAnnotationText.SubScript, ("脚本注释", "Script annotations") },
                { EAnnotationText.SubImport, ("导入策略", "Import policy") },
                { EAnnotationText.SubSceneView, ("SceneView 叠加", "SceneView overlay") },
                { EAnnotationText.SubTodo, ("TODO 收集", "TODO collection") }
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
