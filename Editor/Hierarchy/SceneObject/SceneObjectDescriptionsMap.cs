using Lin.Runtime;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_6000_4_OR_NEWER
using InstanceId = UnityEngine.EntityId;
#else
using InstanceId = System.Int32;
#endif

namespace Lin.Editor.Hierarchy.SceneObject
{
    /// <summary>
    /// 注释的内存缓存，真值存在场景物体的 Description 组件上。切场景即清空，故注释是 per-scene 的。
    /// </summary>
    public class SceneObjectDescriptionsMap
    {
        private static SceneObjectDescriptionsMap instance;

        private Dictionary<InstanceId, Description> descriptionMap;

        private SceneObjectDescriptionsMap()
        {
            descriptionMap = new Dictionary<InstanceId, Description>();

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        public static SceneObjectDescriptionsMap GetInstance()
        {
            if (instance == null)
                instance = new SceneObjectDescriptionsMap();
            return instance;
        }

        private void OnSceneChanged(Scene arg0, Scene arg1) => descriptionMap.Clear();

        public void SetDescription(InstanceId instanceId, string title, string description, Color titleColor)
        {
            var desCmp = GetOrAdd(instanceId);
            if (desCmp == null)
                return;

            desCmp.title = title;
            desCmp.description = description;
            desCmp.titleColor = titleColor;
            Save(desCmp);
        }

        public (string title, string description, Color color) GetDescription(InstanceId instanceId)
        {
            var desCmp = Get(instanceId);
            if (desCmp != null)
                return desCmp.Get();

            return (string.Empty, string.Empty, Color.white);
        }

        public void RemoveDescription(InstanceId instanceId)
        {
            var des = Get(instanceId);
            if (des != null)
                Destroy(des);
            descriptionMap.Remove(instanceId);
        }

        private Description GetOrAdd(InstanceId instanceId)
        {
            var result = Get(instanceId);
            if (result == null)
            {
#if UNITY_6000_4_OR_NEWER
                GameObject target = UnityEditor.EditorUtility.EntityIdToObject(instanceId) as GameObject;
#else
                GameObject target = UnityEditor.EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#endif
                // 物体已删除时 target 为空，早退避免 AddComponent 空引用
                if (target == null)
                {
                    UnityEngine.Debug.LogWarning($"[Lin Framework] 场景物体已删除，无法写入注释 (InstanceId: {instanceId})");
                    return null;
                }

                if (!target.TryGetComponent(out result))
                    result = target.AddComponent<Description>();
                descriptionMap[instanceId] = result;
            }
            return result;
        }

        private Description Get(InstanceId instanceId)
        {
            if (!descriptionMap.TryGetValue(instanceId, out var result))
            {
#if UNITY_6000_4_OR_NEWER
                GameObject target = UnityEditor.EditorUtility.EntityIdToObject(instanceId) as GameObject;
#else
                GameObject target = UnityEditor.EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#endif
                if (target == null)
                    return null;

                result = target.GetComponent<Description>();
                descriptionMap.Add(instanceId, result);
            }

            return result;
        }

        private static void Save(Object target)
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }

        private static void Destroy(Description description)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(description);
                return;
            }

            var owner = description.gameObject;
            // 走 Undo 以便误删注释后可恢复
            Undo.DestroyObjectImmediate(description);
            Save(owner);
        }
    }
}
