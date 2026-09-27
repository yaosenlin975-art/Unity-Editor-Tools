/*
┌────────────────────────────┐
│　Description: 在资源导入后，检测 .cs / .txt / .lua / .json 文本文件编码，非 UTF-8 的转为 UTF-8
│　Remark: .bytes 是二进制容器, 不做编码转换, 否则内容会损坏
└────────────────────────────┘
*/
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Asset
{
    public class ChineseTextAssetImporter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                           string[] movedAssets, string[] movedFromAssetPaths)
        {
            // 遍历本次导入的所有资源
            for (int i = 0; i < importedAssets.Length; i++)
            {
                var assetPath = importedAssets[i];
                if (!IsTextCandidate(assetPath))
                    continue;

                // 绝对路径
                string fullPath = Path.GetFullPath(assetPath);
                if (!File.Exists(fullPath))
                    continue;

                // 检测当前编码
                Encoding encoding = TextAssetEditor.DetectFileEncodingStatic(fullPath);
                if (encoding.Equals(Encoding.UTF8))
                    continue; // 已是 UTF-8，无需转换

                // 执行转换为 UTF-8（带 BOM），并刷新资源
                bool ok = TextAssetEditor.ConvertToUTF8Static(fullPath);
                if (ok)
                {
                    // 再导入一次以更新 Unity 的资源数据库
                    AssetDatabase.ImportAsset(assetPath);
                    Debug.Log($"已将包含中文的文件转为 UTF-8 编码: {assetPath}", AssetDatabase.LoadAssetAtPath<Object>(assetPath));
                }
                else
                {
                    Debug.LogError($"转换为 UTF-8 失败: {assetPath}");
                }
            }
        }

        // 判断是否为需要检测的文本类型（.cs / .txt / .lua / .json）
        private static bool IsTextCandidate(string assetPath)
        {
            return assetPath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase)
                   || assetPath.EndsWith(".txt", System.StringComparison.OrdinalIgnoreCase)
                   || assetPath.EndsWith(".lua", System.StringComparison.OrdinalIgnoreCase)
                   || assetPath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
