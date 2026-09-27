using Lin.Editor.Settings;   // 形参类型：老计划写的是 AssetImportSettings，那个 SO 已被 M2 并进 AnnotationSettings

namespace Lin.Editor.Asset.Importer
{
    /// <summary>
    /// 模型导入里唯一两条还需要判定的规则。rig 类型（Human/Generic）与 generateSecondaryUV
    /// 已按 ADR-006 W5 整段删除：文档要求 Humanoid 只用于人形骸，而 Learn 也不烘焙光照。
    /// </summary>
    public static class ModelImportPolicy
    {
        public static bool ShouldSeedClips(int existingClipCount) => existingClipCount <= 0;

        internal static bool ShouldLoop(string clipName, ToolsSettings_SO settings)
        {
            if (string.IsNullOrEmpty(clipName) || string.IsNullOrEmpty(settings.loopSuffix))
                return false;

            return clipName.EndsWith(settings.loopSuffix, System.StringComparison.Ordinal);
        }
    }
}
