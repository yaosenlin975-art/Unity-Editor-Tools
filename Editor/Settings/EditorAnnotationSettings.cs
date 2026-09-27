using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Settings
{
    /// <summary>
    /// 本包的设置门面。9 个注释显示设置存在 <see cref="ToolsSettings_SO"/>（宿主工程 ProjectSettings/ 下的
    /// SO，随工程进版本库）；界面语言与主工具栏开关态是本机本用户偏好，仍存 EditorPrefs。
    /// 属性名与签名保持不变，调用点无需感知后端换过。
    /// </summary>
    public static class EditorAnnotationSettings
    {
        /// <summary>包内资产的加载路径前缀，与目录名绑定。</summary>
        public const string PackagePath = "Packages/com.lin.editor-tools";

        /// <summary>两个注释窗口共用的样式表（两份原文件逐字节相同，故合一份）。</summary>
        public const string WindowStyleSheetPath = PackagePath + "/Editor/UI/AnnotationWindow.uss";

        /// <summary>给使用方提供的 EditorPrefs key 前缀入口，避免与其它包撞 key。</summary>
        public static string PrefKey(string name) => "com.lin.editor-annotation/" + name;

        /// <summary>注释标题未指定颜色时的兜底值，沿用原 EditorConst 的取值。</summary>
        public static readonly Color DescriptionTitleDefaultColor = new Color(0.267f, 2 / 3f, 1, 1);

        internal static EAnnotationLanguage Language
        {
            get => EditorPrefs.GetInt(PrefKey(nameof(Language)), (int)EAnnotationLanguage.Chinese) == (int)EAnnotationLanguage.English
                ? EAnnotationLanguage.English
                : EAnnotationLanguage.Chinese;
            set => EditorPrefs.SetInt(PrefKey(nameof(Language)), (int)value);
        }

        // ----------------- Project 窗口资源注释 -----------------

        public static int AssetSummaryTitleSize
        {
            get => ToolsSettings_SO.Instance.assetSummaryTitleSize;
            set { ToolsSettings_SO.Instance.assetSummaryTitleSize = value; ToolsSettings_SO.Save(); }
        }

        public static EClickType AssetSummaryEditWay
        {
            get => ToolsSettings_SO.Instance.assetSummaryEditWay;
            set { ToolsSettings_SO.Instance.assetSummaryEditWay = value; ToolsSettings_SO.Save(); }
        }

        // ----------------- Hierarchy 场景物体注释 -----------------

        public static int SceneObjectDescriptionTitleSize
        {
            get => ToolsSettings_SO.Instance.sceneObjectDescriptionTitleSize;
            set { ToolsSettings_SO.Instance.sceneObjectDescriptionTitleSize = value; ToolsSettings_SO.Save(); }
        }

        public static EClickType SceneObjectDescriptionEditWay
        {
            get => ToolsSettings_SO.Instance.sceneObjectDescriptionEditWay;
            set { ToolsSettings_SO.Instance.sceneObjectDescriptionEditWay = value; ToolsSettings_SO.Save(); }
        }

        // ----------------- 脚本注释（从 .cs 源码头部注释里提取） -----------------

        public static Color ScriptDescriptionColor
        {
            get => ToolsSettings_SO.Instance.scriptDescriptionColor;
            set { ToolsSettings_SO.Instance.scriptDescriptionColor = value; ToolsSettings_SO.Save(); }
        }

        public static bool ScriptDescriptionBold
        {
            get => ToolsSettings_SO.Instance.scriptDescriptionBold;
            set { ToolsSettings_SO.Instance.scriptDescriptionBold = value; ToolsSettings_SO.Save(); }
        }

        public static bool ScriptDescriptionItalic
        {
            get => ToolsSettings_SO.Instance.scriptDescriptionItalic;
            set { ToolsSettings_SO.Instance.scriptDescriptionItalic = value; ToolsSettings_SO.Save(); }
        }

        /// <summary>描述标识的原始存储：换行分隔，一行一个。</summary>
        public static string DescriptionFiltersText
        {
            get => ToolsSettings_SO.Instance.descriptionFiltersText;
            set { ToolsSettings_SO.Instance.descriptionFiltersText = value; ToolsSettings_SO.Save(); }
        }

        /// <summary>
        /// 供绘制使用的标识列表。空行必须滤掉——空标识会让 Contains("") 恒真，把源码首行当成注释。
        /// </summary>
        public static List<string> DescriptionFilters
        {
            get
            {
                var result = new List<string>();
                foreach (var line in DescriptionFiltersText.Split('\n'))
                    if (!string.IsNullOrEmpty(line))
                        result.Add(line);
                return result;
            }
        }

        /// <summary>
        /// 单个脚本最多扫描多少行。描述是头部注释；实测本仓库自有脚本 99% 的命中在第 98 行以内，
        /// 上限设 100 既不漏，又能挡住第三方包里几千行的脚本被整份读进 Project 绘制循环。
        /// 存取两边都夹到 >=1，否则存 0 读 1，设置框里的数字会被顶回去。
        /// </summary>
        public static int ScriptDescriptionScanMaxLines
        {
            get => Mathf.Max(1, ToolsSettings_SO.Instance.scriptDescriptionScanMaxLines);
            set { ToolsSettings_SO.Instance.scriptDescriptionScanMaxLines = Mathf.Max(1, value); ToolsSettings_SO.Save(); }
        }

        // ----------------- TODO 收集（原宿主 EditorSettings_SO 的 docsOutputFolderName / todoIgnore*） -----------------

        /// <summary>Todos.md 的输出目录名，相对项目根。</summary>
        public static string DocsOutputFolderName
        {
            get => ToolsSettings_SO.Instance.docsOutputFolderName;
            set { ToolsSettings_SO.Instance.docsOutputFolderName = value; ToolsSettings_SO.Save(); }
        }

        /// <summary>忽略的文件夹前缀表。返回 SO 里的活对象，就地增删改后调 <see cref="SaveTodoSettings"/> 落盘。</summary>
        public static List<string> TodoIgnoreFolders => ToolsSettings_SO.Instance.todoIgnoreFolders;

        /// <summary>忽略的程序集名表，用法同 <see cref="TodoIgnoreFolders"/>。</summary>
        public static List<string> TodoIgnoreAssemblies => ToolsSettings_SO.Instance.todoIgnoreAssemblies;

        /// <summary>把两个忽略表的就地修改落盘（SO 不是 AssetDatabase 资产，SetDirty/SaveAssets 对它无效）。</summary>
        public static void SaveTodoSettings() => ToolsSettings_SO.Save();

        /// <summary>
        /// 只读脚本头部若干行，两处 .cs 注释扫描（Project 绘制、Markdown 汇总）共用。行与行之间补回 \n，
        /// 使既有的 Contains + 正则逐行提取逻辑不变。maxLines 由调用方传入 <see cref="ScriptDescriptionScanMaxLines"/>：
        /// 汇总扫描在后台线程跑，那里读不了 SO。
        /// </summary>
        public static string ReadScriptHead(string assetPath, int maxLines)
        {
            var lines = new List<string>();
            foreach (var line in File.ReadLines(assetPath))
            {
                lines.Add(line);
                if (lines.Count >= maxLines)
                    break;
            }

            return string.Join("\n", lines);
        }

        /// <summary>把共用样式表挂到窗口根节点上，两个注释窗口都用它。</summary>
        public static void ApplyWindowStyleSheet(VisualElement root)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(WindowStyleSheetPath);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
        }
    }
}
