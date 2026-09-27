using System.IO;
using UnityEditor;
using UnityEngine;
using Lin.Editor.Settings;   // 策略来源：老计划写的是 AssetImportSettings，那个 SO 已被 M2 并进 AnnotationSettings

namespace Lin.Editor.Asset.Importer
{
    /// <summary>导入时自动套用 AnnotationSettings。全部回调受 enabled 闸门控制。</summary>
    public class MediaAssetPostprocessor : AssetPostprocessor
    {
        private bool ShouldHandle =>
            assetImporter != null &&
            assetImporter.assetPath.StartsWith("Assets") &&
            ToolsSettings_SO.Instance.enabled;

        private void OnPreprocessTexture()
        {
            if (!ShouldHandle)
                return;
            if (assetImporter is TextureImporter importer && IsFirstImport(importer))
                AssetModifier.ApplyTexture(importer);
        }

        // 贴图不存在、meta 不存在、或源图尺寸变了 → 视为首次导入
        private static bool IsFirstImport(TextureImporter importer)
        {
            var (width, height) = AssetModifier.GetTextureImporterSize(importer);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(importer.assetPath);
            return tex == null || !File.Exists(importer.assetPath + ".meta") || tex.width != width || tex.height != height;
        }

        private void OnPreprocessAudio()
        {
            if (!ShouldHandle)
                return;
            if (assetImporter is AudioImporter importer &&
                (AssetDatabase.LoadAssetAtPath<AudioClip>(importer.assetPath) == null ||
                 !File.Exists(importer.assetPath + ".meta")))
                AssetModifier.ApplyAudio(importer);
        }

        private void OnPreprocessModel()
        {
            if (!ShouldHandle)
                return;
            if (assetImporter is ModelImporter importer &&
                (AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath) == null ||
                 !File.Exists(importer.assetPath + ".meta")))
                AssetModifier.ApplyModelBase(importer);
        }

        /// <summary>
        /// 注意：AssetPostprocessor 的 OnPreprocess* 是按名字反射调用的，基类并未声明它们
        /// （实测 2021.3/2022.3 的 UnityEditor 程序集里搜不到这些方法名）。写错名字照样编译通过、
        /// 只是永不被调用。M4 的端到端探针就是为了证明这个回调真的会跑。
        /// </summary>
        private void OnPreprocessAnimation()
        {
            if (!ShouldHandle)
                return;
            if (assetImporter is ModelImporter importer)
                AssetModifier.ApplyClips(importer);
        }
    }
}
