using System;
using UnityEditor;

namespace Lin.Editor.Asset.Importer
{
    /// <summary>纹理用途，决定压缩格式档位。</summary>
    public enum TextureUsage
    {
        /// <summary>UI/Sprite 图，无 mipmap，要求清晰度</summary>
        UI,
        /// <summary>法线贴图，要求精度</summary>
        NormalMap,
        /// <summary>光照贴图，允许高压缩比</summary>
        Lightmap,
        /// <summary>普通纹理，alpha 感知</summary>
        Default,
        /// <summary>图集</summary>
        Atlas,
    }

    /// <summary>纹理平台压缩格式的唯一事实来源。</summary>
    public static class PlatformTextureFormat
    {
        /// <summary>
        /// 按用途给覆盖组内的某个平台取格式与尺寸。
        /// 只对 ImportPolicyDefaults.OverriddenPlatforms 调用；Web/小游戏走 SetPlatformTextureSettings 会让
        /// 桌面浏览器拿不到 DXT（见 ADR-006 W1），那边由 TextureImporter.maxTextureSize 管尺寸。
        /// </summary>
        internal static (TextureImporterFormat format, int maxSize) Get(
            TextureUsage usage, string platform, bool hasAlpha = true)
        {
            bool isPC = ImportPolicyDefaults.IsPCPlatform(platform);

            return usage switch
            {
                TextureUsage.UI => isPC
                    ? (TextureImporterFormat.BC7, 2048)
                    : (TextureImporterFormat.ASTC_4x4, 2048),

                TextureUsage.NormalMap => isPC
                    ? (TextureImporterFormat.BC5, 2048)
                    : (TextureImporterFormat.ASTC_4x4, 2048),

                TextureUsage.Lightmap => isPC
                    ? (TextureImporterFormat.BC6H, 4096)
                    : (TextureImporterFormat.ASTC_8x8, 2048),

                TextureUsage.Default => isPC
                    ? (TextureImporterFormat.BC7, 4096)
                    : (hasAlpha ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6, 2048),

                TextureUsage.Atlas => isPC
                    ? (TextureImporterFormat.BC7, 2048)
                    : (TextureImporterFormat.ASTC_6x6, 2048),

                _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null),
            };
        }

        /// <summary>平台名是否被当前编辑器支持（主线 Unity 没有 WeixinMiniGame 时为 false）。</summary>
        public static bool IsPlatformAvailable(string platformName)
            => Enum.IsDefined(typeof(BuildTargetGroup), platformName);
    }
}
