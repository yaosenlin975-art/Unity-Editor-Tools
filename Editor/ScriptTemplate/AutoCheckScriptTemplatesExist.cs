/*
┌────────────────────────────┐
│　Description: 自动检测脚本模板是否存在, 不存在则复制
│　Author: 花球i
│　Remark: 模板随包分发, 源目录按本程序集所在包解析
└────────────────────────────┘
*/

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.ScriptTemplate
{
    public static class AutoCheckScriptTemplatesExist
    {
        // 模板住在包里, 宿主 Assets/ScriptTemplates 只是落地副本; 用 PackageInfo 而不是写死
        // "Packages/com.lin.editor-tools/..." —— 从 git/registry 装时包实际在 Library/PackageCache, 只有
        // embedded 包才会以路径形式出现在 Packages/ 下。
        private static string SourceDirectory
        {
            get
            {
                // 全限定：using UnityEditor 也带进来一个同名 PackageInfo
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AutoCheckScriptTemplatesExist).Assembly);
                if (package is null)
                    return null;

                return Path.Combine(package.resolvedPath, "Editor", "ScriptTemplate", "ScriptTemplates");
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptReloaded()
        {
            string directoryPath = Application.dataPath + "/ScriptTemplates";
            if (Directory.Exists(directoryPath))
                return;

            CopyTemplates();
        }

        [MenuItem("Lin/脚本模板")]
        static void CopyTemplates()
        {
            string sourceDirectory = SourceDirectory;
            if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                Debug.LogError($"[Lin Framework] 找不到脚本模板源目录: {sourceDirectory}");
                return;
            }

            string directoryPath = Application.dataPath + "/ScriptTemplates";
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string[] scriptTempaltes = Directory.GetFiles(sourceDirectory, "*.txt");
            foreach (var st in scriptTempaltes)
            {
                FileInfo fi = new FileInfo(st);
                string destination = $"{directoryPath}/{fi.Name}";
                if (File.Exists(destination))
                    continue;

                File.Copy(st, destination);
            }

            if (EditorUtility.DisplayDialog("脚本模板", "脚本复制完成, 重启编辑器后生效, 是否立即重启编辑器？", "立即重启", "稍后自行重启"))
                EditorApplication.OpenProject(Application.dataPath.Replace("Assets", string.Empty));
        }
    }
}
