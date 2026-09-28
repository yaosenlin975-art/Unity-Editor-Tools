using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_6000_4_OR_NEWER
using InstanceId = UnityEngine.EntityId;
#else
using InstanceId = System.Int32;
#endif

namespace Lin.Editor.Hierarchy
{
    [InitializeOnLoad]
    static class HierarchyDrawer
    {
        private static List<IHierarchyDrawable> drawers;

        static HierarchyDrawer()
        {
            InitializeDrawers();
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
#endif
        }

        /// <summary>
        /// 扫描所有程序集，实例化实现了 IHierarchyDrawable 的类
        /// </summary>
        private static void InitializeDrawers()
        {
            drawers = new List<IHierarchyDrawable>();
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (!type.IsClass || type.IsAbstract || !typeof(IHierarchyDrawable).IsAssignableFrom(type))
                                continue;

                            try
                            {
                                var instance = Activator.CreateInstance(type) as IHierarchyDrawable;
                                if (instance != null)
                                    drawers.Add(instance);
                            }
                            catch (Exception ex)
                            {
                                Error($"实例化 {type.Name} 时出错: {ex.Message}");
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // 某些程序集可能无法加载所有类型，跳过这些程序集
                        Error($"跳过程序集 {assembly.FullName}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Error($"处理程序集 {assembly.FullName} 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"初始化绘制器时出错: {ex.Message}");
            }

            drawers.Sort((a, b) => b.drawPriority - a.drawPriority);
        }

        private static void OnHierarchyGUI(InstanceId instanceID, Rect selectionRect)
        {
            var currentRect = selectionRect;
            currentRect.x += 5;    // 紧贴预制体的跳转按钮
            foreach (var drawer in drawers)
            {
                try
                {
                    var usedWidth = drawer.DrawInHierarchy(instanceID, currentRect);
                    if (usedWidth != 0)
                        usedWidth += 5;
                    currentRect.x -= usedWidth;
                }
                catch (Exception ex)
                {
                    Error($"绘制器 {drawer.GetType().Name} 执行Draw方法时出错: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private static void Error(string message) => UnityEngine.Debug.LogError($"[Lin Framework] {message}");
    }
}
