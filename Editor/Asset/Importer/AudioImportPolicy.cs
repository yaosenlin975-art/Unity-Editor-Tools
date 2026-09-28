using System;
using UnityEditor;
using UnityEngine;
using Lin.Editor.Settings;   // Classify 形参：BGM / 语音文件夹列表住 ToolsSettings_SO

namespace Lin.Editor.Asset.Importer
{
    /// <summary>音频资源的角色，由资源所在文件夹判定。</summary>
    public enum AudioRole { Bgm, Voice, Sfx }

    internal readonly struct AudioSampleDecision
    {
        public readonly AudioClipLoadType loadType;
        public readonly AudioCompressionFormat format;
        public readonly float quality;

        public AudioSampleDecision(AudioClipLoadType loadType, AudioCompressionFormat format, float quality)
        {
            this.loadType = loadType;
            this.format = format;
            this.quality = quality;
        }
    }

    /// <summary>
    /// 音频导入策略。原生组与 Web 组的分歧来自两处文档约束：
    /// WebGL 只支持 CompressedInMemory / DecompressOnLoad，且 Vorbis 解压加载吃约 10 倍内存；
    /// 而 PCM 是文档给短音效的推荐，代价是包体，所以只在原生组用。
    /// </summary>
    public static class AudioImportPolicy
    {
        /// <summary>按资源路径落在哪个文件夹下判定角色；两个文件夹都不命中即音效。</summary>
        internal static AudioRole Classify(string assetPath, ToolsSettings_SO settings)
        {
            if (IsUnderAnyFolder(assetPath, settings.bgmFolders))
                return AudioRole.Bgm;
            if (IsUnderAnyFolder(assetPath, settings.voiceFolders))
                return AudioRole.Voice;
            return AudioRole.Sfx;
        }

        internal static AudioSampleDecision Resolve(AudioRole role, string platform)
        {
            float quality = role switch
            {
                AudioRole.Bgm => 0.8f,
                AudioRole.Voice => 0.6f,
                _ => 0.5f,
            };

            // Web / 小游戏：流送不可用，大文件不能解压加载 → 三类统一 CompressedInMemory
            if (ImportPolicyDefaults.IsWebPlatform(platform))
                return new AudioSampleDecision(AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, quality);

            return role switch
            {
                AudioRole.Bgm => new AudioSampleDecision(AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, quality),
                AudioRole.Voice => new AudioSampleDecision(AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, quality),
                // PCM 下 quality 被忽略（文档："Doesn't apply to PCM/ADPCM/HEVAG formats"），照写不误
                _ => new AudioSampleDecision(AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, quality),
            };
        }

        /// <summary>
        /// 跟随构建组的值该按**哪个平台**算 —— 判据是组归属，不是数组位置（`AudioImportPolicy` 与调用方都不取 [0]）。
        /// 跟随组那份落在 <c>AudioImporter.defaultSampleSettings</c>，它是**资产级默认**：
        /// 对"任何没有覆盖行的平台"生效（Web 组就在其中），所以喂进去的必须是 Web 的决策；
        /// 按数组位置取就会把非 Web 平台的 DecompressOnLoad（W2 明令禁止）静默变成 Web 的实际行为。
        /// 一个 Web 名字都没有时返回 null —— 调用方整段跳过，宁可不写也不猜。
        /// 纯函数：只读设置，绝不碰 AssetImporter。
        /// </summary>
        internal static AudioSampleDecision? WebDefault(AudioRole role)
        {
            foreach (var name in ImportPolicyDefaults.FollowBuildDefaultPlatforms)
                if (ImportPolicyDefaults.IsWebPlatform(name))
                    return Resolve(role, name);

            return null;
        }

        /// <summary>资源路径是否落在其中任一文件夹下。文件夹按前缀匹配，需带目录分隔符，避免 "BGMX" 误命中 "BGM"。</summary>
        private static bool IsUnderAnyFolder(string assetPath, string[] folders)
        {
            if (string.IsNullOrEmpty(assetPath) || folders == null)
                return false;

            var normalized = assetPath.Replace('\\', '/');
            for (int i = 0; i < folders.Length; i++)
            {
                var folder = folders[i];
                if (string.IsNullOrEmpty(folder))
                    continue;

                folder = folder.Replace('\\', '/').TrimEnd('/');
                if (normalized.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
