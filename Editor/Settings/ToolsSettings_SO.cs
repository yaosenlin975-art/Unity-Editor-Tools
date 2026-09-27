using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace Lin.Editor.Settings
{
    /// <summary>
    /// 包设置，落在宿主工程 ProjectSettings/ 下、随工程进版本库。
    /// AssetDatabase 看不见这个对象，所以 SetDirty 无效——改动必须显式走 Save()。
    /// 类名与实际承载范围（注释设置 + 导入策略 + SceneView 叠加）暂时不符：合并只改入口名的那一步之后一起处理。
    /// </summary>
    internal class ToolsSettings_SO : ScriptableObject
    {
        internal const string FilePath = "ProjectSettings/LinEditorTools.asset";

        /// <summary>v1：只有九项注释设置的那一版，也是对方已经发布出去的那一版。</summary>
        internal const int AnnotationOnlyVersion = 1;

        /// <summary>v2：导入策略的十个字段并进同一个 SO。文件路径与类名都不变（用户裁定：入口名不换）。</summary>
        internal const int ImportMergeVersion = 2;

        /// <summary>v3：SceneView 叠加的两个字段（开关 + 颜色）随 SceneRendererInfoDrawer 从宿主 EditorSettings_SO 移植并级。</summary>
        internal const int SceneOverlayVersion = 3;

        /// <summary>v4：TODO 收集的三个字段（输出目录名 + 两个忽略表）随 TodoCollector 从宿主 EditorSettings_SO 移植并级。</summary>
        internal const int CurrentVersion = 4;

        public int settingsVersion;

        public int assetSummaryTitleSize = 14;
        public EClickType assetSummaryEditWay = EClickType.单击;
        public int sceneObjectDescriptionTitleSize = 14;
        public EClickType sceneObjectDescriptionEditWay = EClickType.单击;

        // 门面原先存的是字符串 "#808080"，SO 直接存 Color
        public Color scriptDescriptionColor = new Color(128 / 255f, 128 / 255f, 128 / 255f, 1f);
        public bool scriptDescriptionBold;
        public bool scriptDescriptionItalic;
        public int scriptDescriptionScanMaxLines = 100;
        public string descriptionFiltersText = "Description: \nDescription：\n功能说明: ";

        // ----------------- 导入策略（原 com.lin.editor-asset-import 的 AssetImportSettings 并进来） -----------------

        public bool enabled;
        public bool videoEnabled;

        /// <summary>只有这一组写 SetPlatformTextureSettings 覆盖。</summary>
        public string[] overriddenPlatforms = { "Standalone", "Android", "iPhone" };

        /// <summary>这一组不建覆盖，格式由引擎按构建目标决定；尺寸走 globalMaxTextureSize。</summary>
        public string[] followBuildDefaultPlatforms = { "WebGL", "WeixinMiniGame" };

        public int globalMaxTextureSize = 2048;
        public string pcPlatform = "Standalone";

        public string[] bgmKeywords = { "BGM", "背景" };
        public string[] voiceKeywords = { "Voice" };
        public string loopSuffix = "_Loop";

        // ----------------- SceneView 叠加（原宿主 EditorSettings_SO 的 drawRendererInfos_SV / infosColor_SV，随 SceneRendererInfoDrawer 移植并级） -----------------

        /// <summary>SceneView 里绘制选中物体包围盒与顶点/面数统计的总开关，默认关。</summary>
        public bool sceneRendererInfoEnabled;

        /// <summary>叠加的线框与文字颜色；默认取不透明灰——alpha 0 画出来是隐形的，不能当默认。</summary>
        public Color sceneRendererInfoColor = Color.gray;

        // ----------------- TODO 收集（原宿主 EditorSettings_SO 的 docsOutputFolderName / todoIgnore*，随 TodoCollector 移植并级） -----------------

        /// <summary>Todos.md 的输出目录名，相对项目根。</summary>
        public string docsOutputFolderName = "Markdown";

        public List<string> todoIgnoreFolders = new List<string>();
        public List<string> todoIgnoreAssemblies = new List<string>();

        // 版本号只有 settingsVersion 一个（原 importPolicyVersion 已删：第二个版本字段没人读、只会撒谎，
        // 导入策略那一段的形状由 Migrate 的 v1→v2 级负责）。

        // 新建对象先落在 v1：导入策略那一段是本轮才并进同一个 SO 的，由 Migrate 的 v1→v2 级补默认并抬到 CurrentVersion，
        // 于是"老文件升级"和"新工程首建"共用同一条补字段的道路（S1 锁的就是"新实例即 v1"）。
        private ToolsSettings_SO() => settingsVersion = AnnotationOnlyVersion;

        private static ToolsSettings_SO instance;

        internal static ToolsSettings_SO Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                // 文件不存在时这个重载返回 null 还是空数组本地无证据（URP 那份把返回值丢了，不算证据），先判空
                var loaded = InternalEditorUtility.LoadSerializedFileAndForget(FilePath);
                if (loaded != null)
                {
                    foreach (var obj in loaded)
                    {
                        if (obj is ToolsSettings_SO found)
                        {
                            instance = found;
                            break;
                        }
                    }
                }

                if (instance == null)
                    instance = CreateInstance<ToolsSettings_SO>();

                instance.hideFlags = HideFlags.HideAndDontSave;

                // 收敛要在同一次访问里做完：只升一级会把 v0 对象留在"版本号已抬到 v1、导入字段还全是 null/0"的半成品形状，
                // 该状态下导入回调会去迭代 null 平台表、把 maxTextureSize 赋成 0，要等下一次启动才自愈。
                // 分级语义留在 Migrate() 内部（一次调用只升一级），逐级调到位是这里的事。
                while (instance.settingsVersion < CurrentVersion)
                    instance.Migrate();

                // 读路径不写盘（ADR-001 落盘语义 / ADR-003"装包零副作用"）：Instance 是纯 getter，
                // 任何一次导入回调（MediaAssetPostprocessor.ShouldHandle）都会读它；关着闸门也不许改宿主的设置文件。
                // 迁移只在内存里收敛，下一次真实写入（设置页改动、写注释）自然把补齐的形状落盘；
                // 磁盘上短暂留着旧版本号是无害的。唯一还要在这里落盘的场景是文件根本不存在。
                if (!File.Exists(FilePath))
                    Save();

                return instance;
            }
        }

        internal bool IsOverridden(string platform) => Contains(overriddenPlatforms, platform);

        internal bool IsWebPlatform(string platform) => Contains(followBuildDefaultPlatforms, platform);

        /// <summary>
        /// 覆盖组的空安全视图：数组为 null（老文件缺字段、或 `settingsVersion` 高于 CurrentVersion 而没被 Migrate 补过）
        /// 就给空序列，串为空/空的元素一律跳过 —— 平台名空着传给 importer API 是一次静默的错配置。
        /// 生产侧的三处循环（纹理/音频/图集）与音频跟随组的载体选择都读这两个访问器，不再 foreach 裸数组。
        /// </summary>
        internal IEnumerable<string> OverriddenPlatformNames => Named(overriddenPlatforms);

        /// <summary>跟随构建组的空安全视图，语义同 <see cref="OverriddenPlatformNames"/>。</summary>
        internal IEnumerable<string> FollowBuildDefaultPlatformNames => Named(followBuildDefaultPlatforms);

        private static IEnumerable<string> Named(string[] list)
        {
            if (list == null)
                yield break;

            for (int i = 0; i < list.Length; i++)
                if (!string.IsNullOrEmpty(list[i]))
                    yield return list[i];
        }

        private static bool Contains(string[] list, string platform)
        {
            if (list == null || string.IsNullOrEmpty(platform))
                return false;
            for (int i = 0; i < list.Length; i++)
                if (list[i] == platform)
                    return true;
            return false;
        }

        /// <summary>
        /// 分级迁移，一次调用只升一级（升几级由调用方决定：Instance 会 while 到 CurrentVersion 才 Save()，
        /// 所以磁盘上不会留下"版本号抬了、新字段还空着"的半成品）。
        ///
        /// v0 → v1：settingsVersion 为 0 说明这份文件早于该字段：缺的字段反序列化填 0/false/空，而不是 C# 初始化值。
        /// 无法区分"用户真选了 无响应(0)"和"字段当时不存在"，所以整份回默认，认这个代价（ADR-001）。
        /// 这是注释设置自己那一版的既定取舍——那时代码里还没有真数据可丢，本轮原样保留。
        ///
        /// v1 → v2：这一级多出来的只有导入策略那十个字段，而 v1 里的九项注释设置全是用户真调过的值。
        /// 再走一次整份回默认就会把它们静默抹掉，所以这一级只补新字段（null/0/空 → 各自默认）。
        ///
        /// v2 → v3：SceneView 叠加两个字段（sceneRendererInfoEnabled / sceneRendererInfoColor）随绘制器移植并级，
        /// 同样只补新字段。开关默认关等于不动；颜色以 alpha 0 为"未设置"判据补默认（不透明灰），真调过的颜色原样保留。
        ///
        /// v3 → v4：TODO 收集三个字段随 TodoCollector 移植并级，只补新字段。目录名空回默认 "Markdown"，
        /// 两个忽略表 null 回空表（老文件缺字段反序列化成 null）。
        /// </summary>
        internal void Migrate()
        {
            var fresh = CreateInstance<ToolsSettings_SO>();

            if (settingsVersion < AnnotationOnlyVersion)
            {
                assetSummaryTitleSize = fresh.assetSummaryTitleSize;
                assetSummaryEditWay = fresh.assetSummaryEditWay;
                sceneObjectDescriptionTitleSize = fresh.sceneObjectDescriptionTitleSize;
                sceneObjectDescriptionEditWay = fresh.sceneObjectDescriptionEditWay;
                scriptDescriptionColor = fresh.scriptDescriptionColor;
                scriptDescriptionBold = fresh.scriptDescriptionBold;
                scriptDescriptionItalic = fresh.scriptDescriptionItalic;
                scriptDescriptionScanMaxLines = fresh.scriptDescriptionScanMaxLines;
                descriptionFiltersText = fresh.descriptionFiltersText;
                settingsVersion = AnnotationOnlyVersion;
                return;
            }

            if (settingsVersion >= CurrentVersion)
                return;

            if (settingsVersion < ImportMergeVersion)
            {
                // enabled / videoEnabled 不碰：两个开关的默认本就是 false，回默认等于不动
                if (overriddenPlatforms == null || overriddenPlatforms.Length == 0)
                    overriddenPlatforms = fresh.overriddenPlatforms;
                if (followBuildDefaultPlatforms == null || followBuildDefaultPlatforms.Length == 0)
                    followBuildDefaultPlatforms = fresh.followBuildDefaultPlatforms;
                if (bgmKeywords == null || bgmKeywords.Length == 0)
                    bgmKeywords = fresh.bgmKeywords;
                if (voiceKeywords == null || voiceKeywords.Length == 0)
                    voiceKeywords = fresh.voiceKeywords;
                globalMaxTextureSize = globalMaxTextureSize > 0 ? globalMaxTextureSize : fresh.globalMaxTextureSize;
                pcPlatform = string.IsNullOrEmpty(pcPlatform) ? fresh.pcPlatform : pcPlatform;
                loopSuffix = string.IsNullOrEmpty(loopSuffix) ? fresh.loopSuffix : loopSuffix;

                settingsVersion = ImportMergeVersion;
                return;
            }

            if (settingsVersion < SceneOverlayVersion)
            {
                // v2 → v3：sceneRendererInfoEnabled 不碰（默认关即不动）
                sceneRendererInfoColor = sceneRendererInfoColor.a > 0f ? sceneRendererInfoColor : fresh.sceneRendererInfoColor;
                settingsVersion = SceneOverlayVersion;
                return;
            }

            // v3 → v4：TODO 段三个字段；目录名空则回默认，忽略表 null 回空表（老文件缺字段会被反序列化成 null）
            docsOutputFolderName = string.IsNullOrEmpty(docsOutputFolderName) ? fresh.docsOutputFolderName : docsOutputFolderName;
            todoIgnoreFolders = todoIgnoreFolders ?? new List<string>();
            todoIgnoreAssemblies = todoIgnoreAssemblies ?? new List<string>();

            settingsVersion = CurrentVersion;
        }

        internal static void Save()
        {
            if (instance == null)
                return;

            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 不设 allowTextSerialization 会得到一个不可 diff、不可 merge 的二进制 .asset
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { instance }, FilePath, true);
        }
    }
}
