/*
┌────────────────────────────┐
│　Description: SceneView Attribute 成员显示与绘制调度
│　Remark: 缓存反射结果并保留 IMGUI/Handles 事件顺序
└────────────────────────────┘
┌────────────────────────────┐
│　ClassName: SceneGUIAttributeDrawer
└────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using Lin.Editor.Settings;
using Lin.Runtime.Attribute;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.SceneGUI
{
    internal static class SceneGUIAttributeDrawer
    {
        private const BindingFlags STATIC_FLAGS = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags INSTANCE_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, SceneGUITypeInfo> instanceTypeInfos = new Dictionary<Type, SceneGUITypeInfo>();
        private static readonly Dictionary<MonoBehaviour, SceneGUIClassInfo> selectedComponents = new Dictionary<MonoBehaviour, SceneGUIClassInfo>();
        private static readonly List<SceneGUIClassInfo> staticTypes = new List<SceneGUIClassInfo>();
        private static string staticFoldoutLabel;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.duringSceneGui -= OnSceneGUI;

            instanceTypeInfos.Clear();
            selectedComponents.Clear();
            staticTypes.Clear();
            CollectStaticTypes();

            EditorAnnotationLocalization.LanguageChanged -= OnLanguageChanged;
            EditorAnnotationLocalization.LanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            OnSelectionChanged();
        }

        private static void OnLanguageChanged()
        {
            staticFoldoutLabel = EditorAnnotationLocalization.IsEnglish ? "Static" : "静态";
            SceneView.RepaintAll();
        }

        private static void CollectStaticTypes()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Type[] types;
                try
                {
                    types = assemblies[assemblyIndex].GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    Type type = types[typeIndex];
                    if (type == null)
                        continue;

                    SceneGUITypeInfo typeInfo = CreateTypeInfo(type, STATIC_FLAGS);
                    if (!typeInfo.IsEmpty)
                        staticTypes.Add(new SceneGUIClassInfo(typeInfo, null));
                }
            }
        }

        private static SceneGUITypeInfo GetInstanceTypeInfo(Type type)
        {
            if (instanceTypeInfos.TryGetValue(type, out SceneGUITypeInfo typeInfo))
                return typeInfo;

            typeInfo = CreateTypeInfo(type, INSTANCE_FLAGS);
            instanceTypeInfos.Add(type, typeInfo);
            return typeInfo;
        }

        private static SceneGUITypeInfo CreateTypeInfo(Type type, BindingFlags bindingFlags)
        {
            List<MethodInfo> showMethods = null;
            List<MethodInfo> drawMethods = null;
            MethodInfo[] methods = type.GetMethods(bindingFlags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                bool showInScene = System.Attribute.IsDefined(method, typeof(ShowInSceneGUIAttribute), false);
                bool drawInScene = System.Attribute.IsDefined(method, typeof(DrawInSceneGUIAttribute), false);
                if (!showInScene && !drawInScene || method.GetParameters().Length != 0)
                    continue;

                if (showInScene)
                {
                    if (showMethods == null)
                        showMethods = new List<MethodInfo>();
                    showMethods.Add(method);
                }
                if (drawInScene)
                {
                    if (drawMethods == null)
                        drawMethods = new List<MethodInfo>();
                    drawMethods.Add(method);
                }
            }

            List<FieldInfo> fields = null;
            FieldInfo[] members = type.GetFields(bindingFlags);
            for (int i = 0; i < members.Length; i++)
            {
                if (System.Attribute.IsDefined(members[i], typeof(ShowInSceneGUIAttribute), false))
                {
                    if (fields == null)
                        fields = new List<FieldInfo>();
                    fields.Add(members[i]);
                }
            }

            List<PropertyInfo> properties = null;
            PropertyInfo[] propertyMembers = type.GetProperties(bindingFlags);
            for (int i = 0; i < propertyMembers.Length; i++)
            {
                if (System.Attribute.IsDefined(propertyMembers[i], typeof(ShowInSceneGUIAttribute), false))
                {
                    if (properties == null)
                        properties = new List<PropertyInfo>();
                    properties.Add(propertyMembers[i]);
                }
            }

            return new SceneGUITypeInfo(
                showMethods != null ? showMethods.ToArray() : Array.Empty<MethodInfo>(),
                drawMethods != null ? drawMethods.ToArray() : Array.Empty<MethodInfo>(),
                fields != null ? fields.ToArray() : Array.Empty<FieldInfo>(),
                properties != null ? properties.ToArray() : Array.Empty<PropertyInfo>());
        }

        private static void OnSelectionChanged()
        {
            selectedComponents.Clear();
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                MonoBehaviour[] components = selectedObject.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    MonoBehaviour component = components[i];
                    if (component == null)
                        continue;

                    SceneGUITypeInfo typeInfo = GetInstanceTypeInfo(component.GetType());
                    if (!typeInfo.IsEmpty)
                        selectedComponents.Add(component, new SceneGUIClassInfo(typeInfo, component));
                }
            }

            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            DrawStaticTypes();
            DrawSelectedObject();
        }

        private static void DrawStaticTypes()
        {
            if (staticTypes.Count == 0)
                return;

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 200, 500));
            for (int i = 0; i < staticTypes.Count; i++)
            {
                SceneGUIClassInfo classInfo = staticTypes[i];
                classInfo.isExpanded = EditorGUILayout.Foldout(classInfo.isExpanded, staticFoldoutLabel);
                if (!classInfo.isExpanded)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical("box");
                DrawMembers(classInfo);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void DrawSelectedObject()
        {
            if (selectedComponents.Count == 0)
                return;

            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
                return;

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(50, 10, 200, 500));
            GUILayout.Label(selectedObject.name);
            foreach (KeyValuePair<MonoBehaviour, SceneGUIClassInfo> pair in selectedComponents)
            {
                MonoBehaviour component = pair.Key;
                if (component == null)
                    continue;

                SceneGUIClassInfo classInfo = pair.Value;
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();
                classInfo.isExpanded = EditorGUILayout.Foldout(classInfo.isExpanded, component.GetType().Name);
                if (classInfo.isExpanded)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.BeginVertical("box");
                    DrawMembers(classInfo);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void DrawMembers(SceneGUIClassInfo classInfo)
        {
            SceneGUITypeInfo typeInfo = classInfo.typeInfo;
            for (int i = 0; i < typeInfo.showMethods.Length; i++)
            {
                MethodInfo method = typeInfo.showMethods[i];
                if (GUILayout.Button(method.Name))
                    Invoke(method, classInfo.showActions[i], classInfo.target);
            }

            for (int i = 0; i < typeInfo.drawMethods.Length; i++)
                InvokeDrawMethod(typeInfo.drawMethods[i], classInfo.drawActions[i], classInfo.target);

            for (int i = 0; i < typeInfo.fields.Length; i++)
            {
                FieldInfo field = typeInfo.fields[i];
                EditorGUILayout.LabelField(field.Name, ReadFieldValue(field, classInfo.target));
            }

            for (int i = 0; i < typeInfo.properties.Length; i++)
            {
                PropertyInfo property = typeInfo.properties[i];
                EditorGUILayout.LabelField(property.Name, ReadPropertyValue(property, classInfo.target));
            }
        }

        // 成员读取与调用随时可能抛（属性 getter 里带逻辑、目标已被销毁）。异常从 Begin/End 之间穿出去
        // 会把 SceneView 的 IMGUI 配对打断，之后每帧都布局错乱，所以每个读取点各自兜住
        private static string ReadFieldValue(FieldInfo field, object target)
        {
            try
            {
                return GetDisplayValue(field.GetValue(target));
            }
            catch (Exception ex)
            {
                return ReadMemberFailed(field.Name, ex);
            }
        }

        private static string ReadPropertyValue(PropertyInfo property, object target)
        {
            try
            {
                return GetDisplayValue(property.GetValue(target));
            }
            catch (Exception ex)
            {
                return ReadMemberFailed(property.Name, ex);
            }
        }

        private static string ReadMemberFailed(string memberName, Exception ex)
        {
            UnityEngine.Debug.LogError($"[Lin Framework] 读取 {memberName} 失败: {ex.GetType().Name}: {ex.Message}");
            return "读取失败";
        }

        private static string GetDisplayValue(object value)
        {
            return value != null ? value.ToString() : string.Empty;
        }

        private static void Invoke(MethodInfo method, Action action, object target)
        {
            try
            {
                if (action != null)
                {
                    action();
                    return;
                }

                method.Invoke(target, null);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Lin Framework] 调用 {method.Name} 失败: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void InvokeDrawMethod(MethodInfo method, Action action, object target)
        {
            UnityEngine.Profiling.Profiler.BeginSample(method.Name);
            try
            {
                Invoke(method, action, target);
            }
            finally
            {
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        private static Action[] BindMethods(MethodInfo[] methods, object target)
        {
            var actions = new Action[methods.Length];
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.ReturnType != typeof(void))
                    continue;

                try
                {
                    actions[i] = target == null
                        ? (Action)method.CreateDelegate(typeof(Action))
                        : (Action)method.CreateDelegate(typeof(Action), target);
                }
                catch (Exception)
                {
                    // 绑定失败时由绘制路径回退到 MethodInfo.Invoke。
                }
            }

            return actions;
        }

        private sealed class SceneGUITypeInfo
        {
            public readonly MethodInfo[] showMethods;
            public readonly MethodInfo[] drawMethods;
            public readonly FieldInfo[] fields;
            public readonly PropertyInfo[] properties;

            public bool IsEmpty => showMethods.Length == 0 && drawMethods.Length == 0 && fields.Length == 0 && properties.Length == 0;

            public SceneGUITypeInfo(MethodInfo[] showMethods, MethodInfo[] drawMethods, FieldInfo[] fields, PropertyInfo[] properties)
            {
                this.showMethods = showMethods;
                this.drawMethods = drawMethods;
                this.fields = fields;
                this.properties = properties;
            }
        }

        private sealed class SceneGUIClassInfo
        {
            public readonly SceneGUITypeInfo typeInfo;
            public readonly object target;
            public readonly Action[] showActions;
            public readonly Action[] drawActions;
            public bool isExpanded = true;

            public SceneGUIClassInfo(SceneGUITypeInfo typeInfo, object target)
            {
                this.typeInfo = typeInfo;
                this.target = target;
                showActions = BindMethods(typeInfo.showMethods, target);
                drawActions = BindMethods(typeInfo.drawMethods, target);
            }
        }
    }
}
