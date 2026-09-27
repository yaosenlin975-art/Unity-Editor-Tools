/*
┌────────────────────────────┐
│　Description: SO拓展
└────────────────────────────┘
*/
using System;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Helper
{
    public static class ScriptableObjectHelper
    {
        public static T Find<T>(bool includePackage = false)where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (includePackage || path.StartsWith("Assets/"))
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            throw new Exception($"Can't find {typeof(T).FullName}.");
        }
    }
}