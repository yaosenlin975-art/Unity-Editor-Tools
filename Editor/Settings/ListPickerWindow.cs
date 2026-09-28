using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Settings
{
    /// <summary>
    /// 在按钮下方弹出的可搜索选择列表。程序集这类可能上百项的选择用它，避免手打字符串。
    /// </summary>
    internal class ListPickerWindow : EditorWindow
    {
        private List<string> items;
        private Action<string> onPick;
        private string search = string.Empty;
        private Vector2 scroll;

        internal static void Show(Rect activatorRect, List<string> items, Action<string> onPick)
        {
            var window = CreateInstance<ListPickerWindow>();
            window.items = items;
            window.onPick = onPick;
            window.titleContent = new GUIContent("选择");
            window.minSize = new Vector2(280f, 240f);

            // 用可移动/可缩放的工具窗，而不是 ShowAsDropDown：后者是屏幕坐标的固定弹层，
            // 从设置页 IMGUI 的局部 Rect 传进去会落到左上角且不能拖动。
            window.ShowUtility();
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(activatorRect.position);
            window.position = new Rect(
                screenPoint.x, screenPoint.y + activatorRect.height,
                Mathf.Max(280f, activatorRect.width), 360f);
            window.Focus();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                search = GUILayout.TextField(search ?? string.Empty, EditorStyles.toolbarSearchField);

            if (items == null)
                return;

            using (var scope = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = scope.scrollPosition;
                bool any = false;
                for (int i = 0; i < items.Count; i++)
                {
                    string item = items[i];
                    if (!string.IsNullOrEmpty(search) &&
                        item.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    any = true;
                    if (GUILayout.Button(item, EditorStyles.label))
                    {
                        onPick?.Invoke(item);
                        Close();
                    }
                }

                if (!any)
                    EditorGUILayout.LabelField("无匹配项", EditorStyles.miniLabel);
            }
        }
    }
}
