/*
┌────────────────────────────┐
│　Description: 查找, 获取选中
└────────────────────────────┘
*/
using UnityEditor;
using System.IO;
using Object = UnityEngine.Object;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Lin.Editor.Helper
{
    public static class PathHelper
    {
        /// <summary>
        /// 获取右键点击的文件夹位置
        /// </summary>
        /// <returns></returns>
        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }

        /// <summary>
        /// 根据名称查找Editor中的SO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T FindObject<T>(string name = null, bool includePackages = false) where T : ScriptableObject
        {
            var filter = string.IsNullOrEmpty(name) ? $"t:{typeof(T).Name}" : $"t:{typeof(T).Name} {name}";
            var guids = AssetDatabase.FindAssets(filter);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (includePackages || path.StartsWith("Assets/"))
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            throw new Exception($"Can't find {typeof(T).FullName}.");
        }

        public static void RunProcess(string path) => Task.Run(() => Process.Start("explorer.exe", path));

        /// <summary>
        /// 获取AssetDatabase中的脚本对象
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static MonoScript GetMonoScript(this Type self)
        {
            var guids = AssetDatabase.FindAssets($"t:MonoScript {self.Name}");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            }
            return null;
        }
    }
}