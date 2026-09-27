using System;
using System.Reflection;
using Lin.Runtime.Attribute;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Lin.Editor
{
    //Description: 脚本重载时自动对场景中的脚本进行对象获取
    public static class AutoGetEditor
    {
        [DidReloadScripts]
        [MenuItem("Lin/获取组件 #g")]
        private static void OnScriptsReloaded()
        {
            // 全限定：using System 带进来 System.Object，与 UnityEngine.Object 同名
            var monoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in monoBehaviours)
            {
                if (mb == null) 
                    continue;
                
                var fields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields) 
                {
                    var getAttr = field.GetCustomAttribute<GetAttribute>();
                    var getInChildAttr = field.GetCustomAttribute<GetInChildAttribute>();
                    var getInParentAttr = field.GetCustomAttribute<GetInParentAttribute>();

                    if (getAttr is null && getInParentAttr is null && getInChildAttr is null || (field.GetValue(mb) as MonoBehaviour) != null)
                        continue;

                    if (!field.IsPublic)
                    {
                        var serializeField = field.GetCustomAttribute<SerializeField>();
                        if (serializeField == null)
                        {
                            Debug.LogError($"{mb.GetType().Name}.{field.Name} 必须带有 [SerializeField]", mb);
                            continue;
                        }
                    }

                    bool got = false;

                    if (getAttr != null)
                    {
                        var component = GetOrAddComponent(mb, field.FieldType);
                        field.SetValue(mb, component);
                        EditorUtility.SetDirty(mb);
                        EditorUtility.SetDirty(mb.gameObject);
                        got = true;
                    }
                    else if (getInChildAttr != null) 
                    {
                        Component component = null;
                        if (string.IsNullOrEmpty(getInChildAttr.Path))
                            component = mb.GetComponentInChildren(field.FieldType, true);
                        else
                        {
                            var child = mb.transform.Find(getInChildAttr.Path);
                            if (child != null)
                                component = child.GetComponent(field.FieldType);
                        }

                        if (component != null)
                        {
                            field.SetValue(mb, component);
                            EditorUtility.SetDirty(mb.gameObject);
                            got = true;
                        }
                    }
                    //Parent
                    else
                    {
                        Component component = mb.GetComponentInParent(field.FieldType);

                        if (component != null)
                        {
                            field.SetValue(mb, component);
                            EditorUtility.SetDirty(mb.gameObject);
                            got = true;
                        }
                    }

                    if (!got)
                    {
                        Debug.LogError($"自动获取 {mb.GetType().Name}.{field.Name} 失败, 请检查组件是否存在.", mb);
                        continue;
                    }
                }
            }
        }

        // 宿主框架的 ComponentExtensions.GetOrAddComponent(Type) 不进包, 就地内联等价实现
        private static Component GetOrAddComponent(Component owner, Type type)
        {
            if (!owner.TryGetComponent(type, out var attached))
                attached = owner.gameObject.AddComponent(type);

            return attached;
        }
    }
}