using System;
using UnityEditor;

namespace Lin.Editor.Asset.Importer
{
    /// <summary>
    /// 导入策略里与具体工程无关的技术约定（原「平台分组」设置段，2026-09-28 改为固定默认）。
    /// 依据 Unity 文档：桌面走 BCn、移动走 ASTC；WebGL / 小游戏不建平台覆盖，让引擎按构建目标选格式
    /// （一旦给 WebGL 建覆盖，桌面浏览器拿不到 BC 系列）。
    /// </summary>
    internal static class ImportPolicyDefaults
    {
        /// <summary>写 SetPlatformTextureSettings 覆盖的平台组。</summary>
        internal static readonly string[] OverriddenPlatforms = { "Standalone", "Android", "iPhone" };

        /// <summary>不建覆盖的平台组：格式由引擎按构建目标决定，尺寸仍受 <see cref="GlobalMaxTextureSize"/> 约束。</summary>
        internal static readonly string[] FollowBuildDefaultPlatforms = { "WebGL", "WeixinMiniGame" };

        /// <summary>所有纹理的 maxTextureSize 上限，也是未覆盖平台与 Web / 小游戏的尺寸闸门。</summary>
        internal const int GlobalMaxTextureSize = 2048;

        internal static bool IsWebPlatform(string platform) => Contains(FollowBuildDefaultPlatforms, platform);

        /// <summary>
        /// 是否走桌面 BCn 档。按 BuildTargetGroup 判定，不把平台展示名当逻辑语义；
        /// 目前覆盖组里只有 Standalone 属于 PC 系。
        /// </summary>
        internal static bool IsPCPlatform(string platform)
        {
            if (string.IsNullOrEmpty(platform))
                return false;

            return Enum.TryParse<BuildTargetGroup>(platform, out var group)
                   && group == BuildTargetGroup.Standalone;
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
    }
}
