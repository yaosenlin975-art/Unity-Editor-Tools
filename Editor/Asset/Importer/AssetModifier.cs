using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Video;
using Lin.Editor.Settings;   // 策略来源：老计划写的是 AssetImportSettings，那个 SO 已被 M2 并进 AnnotationSettings

namespace Lin.Editor.Asset.Importer
{
    /// <summary>按 AnnotationSettings 把导入设置写到 importer 上。</summary>
    public static class AssetModifier
    {
        private const string ModifiedTag = "ImporterModified";

        #region - 菜单 -

        [MenuItem("Assets/优化资源设置", false, 80)]
        private static void Postprocess() => Run(false);

        [MenuItem("Assets/优化资源设置(强制)", false, 81)]
        private static void PostprocessForce() => Run(true);

        // [W1 of ADR-003] enabled 为 false 时灰化。2021.3 没有 4 参 MenuItem 重载，
        // 校验函数用 MenuItem(path, true) 标在另一个静态方法上（docs 已核实）。
        [MenuItem("Assets/优化资源设置", true)]
        private static bool ValidatePostprocess() => ToolsSettings_SO.Instance.enabled;

        [MenuItem("Assets/优化资源设置(强制)", true)]
        private static bool ValidatePostprocessForce() => ToolsSettings_SO.Instance.enabled;

        private static void Run(bool force)
        {
            var folders = ResolveFolders();
            if (folders == null)
                return; // 用户在整库确认框里选了取消

            foreach (var guid in AssetDatabase.FindAssets("t:Texture", folders))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is TextureImporter importer)
                {
                    if (force) importer.RemoveUserTag(ModifiedTag);
                    ApplyTexture(importer);
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", folders))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is AudioImporter importer)
                {
                    if (force) importer.RemoveUserTag(ModifiedTag);
                    ApplyAudio(importer);
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Model", folders))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is ModelImporter importer)
                {
                    if (force) importer.RemoveUserTag(ModifiedTag);
                    ApplyModelBase(importer);
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas", folders))
                if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid)) is SpriteAtlas atlas)
                    ApplySpriteAtlas(atlas);

            // AssetPostprocessor 没有视频预处理回调，视频只能走这里
            foreach (var guid in AssetDatabase.FindAssets("t:VideoClip", folders))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is VideoClipImporter importer)
                {
                    if (force) importer.RemoveUserTag(ModifiedTag);
                    ApplyVideo(importer);
                }
            }
        }

        /// <summary>
        /// [W1 of ADR-003] 未选中文件夹时 Learn 的写法是静默把作用域扩到整个 Assets。
        /// 那等于一次点击改写全库 .meta，必须显式同意。
        /// </summary>
        private static string[] ResolveFolders()
        {
            var active = Selection.activeObject;
            var path = active == null ? null : AssetDatabase.GetAssetPath(active);

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                return new[] { path };

            // 不用 "t:Texture t:AudioClip" 这种多类型过滤 —— 多个 t: 之间是 AND，会返回 0 条。
            // 语法未经实测，所以直接分四次数。
            var count = AssetDatabase.FindAssets("t:Texture").Length
                      + AssetDatabase.FindAssets("t:AudioClip").Length
                      + AssetDatabase.FindAssets("t:Model").Length
                      + AssetDatabase.FindAssets("t:VideoClip").Length;
            var go = EditorUtility.DisplayDialog(
                "作用域：整个 Assets",
                $"未选中文件夹，将处理整个 Assets 目录（约 {count} 个资源），会改写这些资源的 .meta 导入设置。",
                "继续", "取消");

            return go ? new[] { "Assets" } : null;
        }

        #endregion

        #region - 平台可用性 -

        /// <summary>
        /// 覆盖组平台名逐个过 IsPlatformAvailable：当前编辑器不支持的 Warn 后跳过，可用的才交给下游写 importer。
        /// 纹理与音频两处共用这一处判断（原先是两份内联 if）；skipMessage 由调用方拼好资产路径，这里只补平台名。
        /// </summary>
        private static IEnumerable<string> Available(IEnumerable<string> platformNames, string skipMessage, Object context)
        {
            foreach (var platform in platformNames)
            {
                if (PlatformTextureFormat.IsPlatformAvailable(platform))
                    yield return platform;
                else
                    Warn($"当前编辑器不支持平台 {platform}, {skipMessage}", context);
            }
        }

        #endregion

        #region - 纹理 -

        /// <summary>
        /// userData 的写入只发生在 AddUserTag / RemoveUserTag 里，M1 的扩展方法在"算出来确实要写"之后、
        /// 赋值之前调 WarnIfTagsKeyUnreadable()，所以本文件不存在绕过守卫的写入点。
        /// </summary>
        public static void ApplyTexture(TextureImporter importer)
        {
            if (importer == null || importer.ContainsUserTag(ModifiedTag))
                return;

            TextureUsage usage;

            switch (importer.textureType)
            {
                case TextureImporterType.Sprite:
                case TextureImporterType.GUI:
                    usage = TextureUsage.UI;
                    importer.mipmapEnabled = false;
                    break;

                case TextureImporterType.NormalMap:
                    usage = TextureUsage.NormalMap;
                    importer.mipmapEnabled = true;
                    importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
                    importer.streamingMipmaps = false; // 法线图流送是误配
                    importer.sRGBTexture = false;
                    break;

                case TextureImporterType.Lightmap:
                    usage = TextureUsage.Lightmap;
                    importer.mipmapEnabled = false;
                    importer.sRGBTexture = true;
                    break;

                case TextureImporterType.Default:
                    usage = TextureUsage.Default;
                    importer.mipmapEnabled = true;
                    importer.streamingMipmaps = true;
                    break;

                default:
                    return; // 不认识的类型不打 tag，策略升级后还能再处理
            }

            importer.isReadable = false;

            // [W1] 只有覆盖组建平台覆盖；跟随组的尺寸走下面的全局上限
            foreach (var platform in Available(ImportPolicyDefaults.OverriddenPlatforms, $"跳过 {importer.assetPath}", importer))
            {
                var (format, maxSize) = PlatformTextureFormat.Get(
                    usage, platform, importer.DoesSourceTextureHaveAlpha());
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    format = format,
                    maxTextureSize = maxSize,
                });
            }

            importer.maxTextureSize = ImportPolicyDefaults.GlobalMaxTextureSize;

            // 不变量：tag 必须在 SaveAndReimport 之前，且 AddUserTag 不自己触发重导入
            importer.AddUserTag(ModifiedTag);
            importer.SaveAndReimport();
            Log($"已设置 {importer.assetPath} 纹理压缩", importer);
        }

        public static (int, int) GetTextureImporterSize(TextureImporter importer)
        {
            if (importer == null)
                return (0, 0);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            return (width, height);
        }

        #endregion

        #region - 音频 -

        public static void ApplyAudio(AudioImporter importer)
        {
            if (importer == null || importer.ContainsUserTag(ModifiedTag))
                return;

            var settings = ToolsSettings_SO.Instance;
            var role = AudioImportPolicy.Classify(importer.assetPath, settings);

            // [W4] 覆盖组：SetOverrideSampleSettings 只对这三个名字落 platformSettingOverrides 行（实测键 1/4/7）。
            // 注意可用性判据挡不住 WebGL：它在 BuildTargetGroup 里是定义的（且模块已装），
            // 但对音频它照样不落盘 —— 承载 API 由分组决定，不由 IsPlatformAvailable 决定。
            foreach (var platform in Available(ImportPolicyDefaults.OverriddenPlatforms, $"跳过 {importer.assetPath}", importer))
            {
                var d = AudioImportPolicy.Resolve(role, platform);
                importer.SetOverrideSampleSettings(platform, new AudioImporterSampleSettings
                {
                    compressionFormat = d.format,
                    loadType = d.loadType,
                    quality = d.quality,
                });
            }

            // [W2] 跟随构建组（WebGL / WeixinMiniGame）的唯一承载点是 defaultSampleSettings。
            // 实测（audio-probe.md Q1/Q2）：SetOverrideSampleSettings 对这两个名字是静默 no-op ——
            // 不抛异常、不落 .meta 行、ContainsSampleSettingsOverride 照样 false；
            // 而 defaultSettings: 块会落盘，两个 Web 平台的读回随之变成 CompressedInMemory + 写进去的 quality。
            // 传得过去的只有 loadType 与 quality：WebGL 的 compressionFormat 恒读回 AAC(7)、
            // sampleRateSetting 恒 OverrideSampleRate，引擎钉死，所以这里不宣称给 WebGL 任何压缩格式
            // （Vorbis 只是这条 asset 级默认的字面值，对没有覆盖行的平台才生效；要钉 WebGL 的格式得走 2021.2+ 的 Format Importer）。
            // 覆盖组三行都在，asset 级默认值才敢当 Web 组的载体 —— 只有没有行的平台才吃它。
            var web = ImportPolicyDefaults.FollowBuildDefaultPlatforms;
            if (web != null && web.Length > 0)
            {
                var d = AudioImportPolicy.Resolve(role, web[0]);
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    compressionFormat = d.format,
                    loadType = d.loadType,
                    quality = d.quality,
                };
            }

            importer.AddUserTag(ModifiedTag);
            importer.SaveAndReimport();
            Log($"已设置 {importer.assetPath} 音频导入（角色 {role}）", importer);
        }

        #endregion

        #region - 模型 -

        public static void ApplyModelBase(ModelImporter importer)
        {
            if (importer == null || importer.ContainsUserTag(ModifiedTag))
                return;

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importBlendShapes = true;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;

            // [W5] 不设 animationType / avatarSetup / generateSecondaryUV —— 见 ADR-006

            importer.AddUserTag(ModifiedTag);
            importer.SaveAndReimport();
            Log($"已设置 {importer.assetPath} 模型配置", importer);
        }

        /// <summary>[W3] 由 OnPreprocessAnimation 调用；已有 clip 配置时一个字都不写。</summary>
        public static void ApplyClips(ModelImporter importer)
        {
            if (importer == null)
                return;

            var settings = ToolsSettings_SO.Instance;
            if (!settings.enabled)
                return;

            var existing = importer.clipAnimations;
            if (!ModelImportPolicy.ShouldSeedClips(existing == null ? -1 : existing.Length))
                return;

            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
                clip.loopTime = ModelImportPolicy.ShouldLoop(clip.name, settings);
            importer.clipAnimations = clips;
        }

        #endregion

        #region - 图集 -

        /// <summary>
        /// 图集不设 tag：探针没验过 .spriteatlas 的 userData 能否落盘，
        /// 而"值相同就不写"是更强的幂等条件（顺带省掉一次 SetDirty + SaveAssets）。
        /// </summary>
        public static void ApplySpriteAtlas(SpriteAtlas atlas)
        {
            if (atlas == null)
                return;

            bool changed = false;

            foreach (var platform in ImportPolicyDefaults.OverriddenPlatforms)
            {
                if (!PlatformTextureFormat.IsPlatformAvailable(platform))
                {
                    Warn($"当前编辑器不支持平台 {platform}, 跳过图集 {atlas.name}");
                    continue;
                }

                var (format, maxSize) = PlatformTextureFormat.Get(TextureUsage.Atlas, platform);
                var current = atlas.GetPlatformSettings(platform);
                if (current.overridden && current.format == format && current.maxTextureSize == maxSize)
                    continue;

                atlas.SetPlatformSettings(new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    textureCompression = TextureImporterCompression.Compressed,
                    format = format,
                    maxTextureSize = maxSize,
                });
                changed = true;
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            Log($"已设置图集 {atlas.name} 平台压缩", atlas);
        }

        #endregion

        #region - 视频 -

        public static void ApplyVideo(VideoClipImporter importer)
        {
            if (importer == null)
                return;

            // [W6] videoEnabled 与 enabled 互不隶属
            if (!ToolsSettings_SO.Instance.videoEnabled || importer.ContainsUserTag(ModifiedTag))
                return;

            // 读-改-写：直接 new 会重置已有设置，导致 resizeMode 静默失效
            var target = importer.defaultTargetSettings;
            target.enableTranscoding = true;
            target.resizeMode = VideoResizeMode.ThreeQuarterRes;
            importer.defaultTargetSettings = target;

            importer.AddUserTag(ModifiedTag);
            importer.SaveAndReimport();
            Log($"已设置 {importer.assetPath} 视频导入", importer);
        }

        #endregion

        #region - 日志 -

        private const string Prefix = "[Lin Framework] ";

        private static void Log(string message, Object context = null) => Debug.Log(Prefix + message, context);

        private static void Warn(string message, Object context = null) => Debug.LogWarning(Prefix + message, context);

        #endregion
    }
}
