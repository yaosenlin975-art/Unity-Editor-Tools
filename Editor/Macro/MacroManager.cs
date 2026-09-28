/*
┌────────────────────────────┐
│　Description: 宏管理窗口
│　Remark: 
└────────────────────────────┘
┌──────────────┐                                   
│　ClassName: MacroManager
└──────────────┘
*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace Lin.Editor.Macro
{
    class MacroManager : EditorWindow
    {
        private const string ADD_FIELD = "MacroToAdd";

        private static readonly string[] INTRINSIC_MACROS = new string[]
        {
            "TEST",
            "UNITY_2D"
        };

        private List<Macro> macros;
        private readonly HashSet<string> toRemove = new HashSet<string>();
        private string toAdd = string.Empty;
        private string search = string.Empty;
        private Vector2 scrollPosition;
        private BuildTargetGroup selectedBuildTargetGroup;

        [MenuItem("Lin/Macros")]
        private static void Init()
        {
            var window = GetWindow<MacroManager>("宏管理器");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        #region - 生命周期 -

        private void OnEnable()
        {
            selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            toRemove.Clear();
            ChangeTarget();
        }

        #endregion

        #region - 界面 -

        private void OnGUI()
        {
            bool shouldSave = false;

            // 切换平台立即生效，不再依赖单独的"切换"按钮
            GUILayout.BeginHorizontal();
            var newGroup = (BuildTargetGroup)EditorGUILayout.EnumPopup("目标平台", selectedBuildTargetGroup);
            if (newGroup != selectedBuildTargetGroup)
            {
                selectedBuildTargetGroup = newGroup;
                ChangeTarget();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(ADD_FIELD);
            toAdd = EditorGUILayout.TextField("宏", toAdd);
            bool clickAdd = GUILayout.Button("添加", GUILayout.Width(60));
            bool pressEnter = Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Return
                && GUI.GetNameOfFocusedControl() == ADD_FIELD;
            if (clickAdd || pressEnter)
            {
                shouldSave = TryAdd();
                if (pressEnter)
                    Event.current.Use();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            search = EditorGUILayout.TextField("搜索", search);
            int enabledCount = 0;
            for (int i = 0; i < macros.Count; i++)
                enabledCount += macros[i].enable ? 1 : 0;
            GUILayout.Label($"{enabledCount}/{macros.Count}", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < macros.Count; i++)
            {
                var macro = macros[i];
                if (!string.IsNullOrEmpty(search) && macro.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                GUILayout.BeginHorizontal();
                bool enable = EditorGUILayout.ToggleLeft(macro.name, macro.enable);
                if (enable != macro.enable)
                {
                    macro.enable = enable;
                    macros[i] = macro;
                    shouldSave = true;
                }
                if (GUILayout.Button("移除", GUILayout.Width(60)))
                {
                    toRemove.Add(macro.name);
                    shouldSave = true;
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (toRemove.Count > 0)
            {
                macros.RemoveAll(IsMarkedForRemove);
                toRemove.Clear();
            }

            if (shouldSave)
                Save();

            GUILayout.Space(10);
            // 显式应用：列表改动只写本地 JSON，改 ProjectSettings 必须点此按钮并确认
            if (GUILayout.Button("应用到 PlayerSettings", GUILayout.Height(28)))
                ApplyToPlayerSettings();
        }

        // 已存在的同名宏直接置为开启（幂等），返回是否需要保存
        private bool TryAdd()
        {
            string name = toAdd.Trim();
            if (string.IsNullOrEmpty(name))
                return false;

            bool needSave = true;
            int index = IndexOfName(name);
            if (index >= 0)
            {
                var macro = macros[index];
                needSave = !macro.enable;
                macro.enable = true;
                macros[index] = macro;
            }
            else
            {
                macros.Add(new Macro() { name = name, enable = true });
            }

            toAdd = string.Empty;
            GUI.FocusControl(null);
            return needSave;
        }

        #endregion

        #region - 数据持久化 -

        // 平台切换时以 PlayerSettings 实际 define 为准合并进列表；只写本地 JSON，不回写 ProjectSettings
        private void ChangeTarget()
        {
            toRemove.Clear();
            search = string.Empty;
            Load();

            string macroStr = GetScriptingDefineSymbolsForGroup(selectedBuildTargetGroup);
            if (!string.IsNullOrEmpty(macroStr))
            {
                foreach (var child in macroStr.Split(';'))
                {
                    if (string.IsNullOrEmpty(child))
                        continue;

                    int index = IndexOfName(child);
                    if (index == -1)
                        macros.Add(new Macro() { name = child, enable = true });
                    else
                    {
                        var macro = macros[index];
                        macro.enable = true;
                        macros[index] = macro;
                    }
                }
            }

            Save();
        }

        private string filePath => Path.Combine(Path.GetDirectoryName(Application.dataPath), $"{selectedBuildTargetGroup}-Macros.json");

        // 只持久化本地 JSON；写 PlayerSettings 走 ApplyToPlayerSettings（需显式确认）
        private void Save()
        {
            macros.RemoveAll(IsNameEmpty);
            IntrinsicMacro();

            string json = JsonConvert.SerializeObject(macros);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 把列表里的宏写入 PlayerSettings。变更前展示 diff 并确认，避免误点改 ProjectSettings。
        /// </summary>
        private void ApplyToPlayerSettings()
        {
            IntrinsicMacro();

            var sb = new System.Text.StringBuilder();
            bool first = true;
            for (int i = 0; i < macros.Count; i++)
            {
                if (!macros[i].enable)
                    continue;

                if (!first)
                    sb.Append(';');
                sb.Append(macros[i].name);
                first = false;
            }
            string newDefines = sb.ToString();

            string currentDefines = GetScriptingDefineSymbolsForGroup(selectedBuildTargetGroup);
            if (newDefines == currentDefines)
            {
                EditorUtility.DisplayDialog("宏管理器", "当前 PlayerSettings 宏与列表一致，无需应用。", "确定");
                return;
            }

            string diff = BuildDiff(currentDefines, newDefines);
            if (!EditorUtility.DisplayDialog("应用宏到 PlayerSettings？", diff, "应用", "取消"))
                return;

            SetScriptingDefineSymbolsForGroup(selectedBuildTargetGroup, newDefines);
        }

        // 组装人可读的 define 变更差异
        private static string BuildDiff(string current, string next)
        {
            var currentSet = new HashSet<string>(SplitDefines(current));
            var nextSet = new HashSet<string>(SplitDefines(next));

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("将修改 PlayerSettings 脚本宏：");
            sb.AppendLine();
            sb.Append("当前: ");
            sb.AppendLine(string.IsNullOrEmpty(current) ? "(空)" : current);
            sb.Append("应用后: ");
            sb.AppendLine(string.IsNullOrEmpty(next) ? "(空)" : next);
            sb.AppendLine();

            bool any = false;
            foreach (var name in nextSet)
            {
                if (currentSet.Contains(name))
                    continue;
                sb.Append("  + ");
                sb.AppendLine(name);
                any = true;
            }
            foreach (var name in currentSet)
            {
                if (nextSet.Contains(name))
                    continue;
                sb.Append("  - ");
                sb.AppendLine(name);
                any = true;
            }
            if (!any)
                sb.AppendLine("  (无净变更)");

            return sb.ToString();
        }

        private static IEnumerable<string> SplitDefines(string defines)
        {
            if (string.IsNullOrEmpty(defines))
                yield break;

            foreach (var item in defines.Split(';'))
            {
                if (!string.IsNullOrEmpty(item))
                    yield return item;
            }
        }

        private void Load()
        {
            if (!File.Exists(filePath))
            {
                macros = new List<Macro>();
                return;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                macros = JsonConvert.DeserializeObject<List<Macro>>(json) ?? new List<Macro>();
            }
            catch (Exception)
            {
                macros = new List<Macro>();
            }
            IntrinsicMacro();
        }

        private void IntrinsicMacro()
        {
            foreach (var macroName in INTRINSIC_MACROS)
            {
                if (IndexOfName(macroName) != -1)
                    continue;

                macros.Add(new Macro() { name = macroName, enable = false });
            }
        }

        #endregion

        #region - 集合辅助 -

        private static bool IsNameEmpty(Macro macro) => string.IsNullOrEmpty(macro.name);

        private bool IsMarkedForRemove(Macro macro) => toRemove.Contains(macro.name);

        private int IndexOfName(string name)
        {
            for (int i = 0; i < macros.Count; i++)
            {
                if (macros[i].name == name)
                    return i;
            }
            return -1;
        }

        #endregion

        [Serializable]
        private struct Macro
        {
            public string name;
            public bool enable;
        }

        // 包依赖宏（HybridCLR/Luban/YooAsset/UniTask 等）已改由各 asmdef 的 versionDefines
        // 随包自动增删，不再走全局 define；本窗口只负责用户自定义宏。
    }
}
