using System;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Asset
{
    /// <summary>
    /// 一条自定义链接的输入窗口：标题 + URI，保存后回调宿主刷新，本窗口只负责收两个字符串。
    /// </summary>
    public class AssetMessageInputWindow : EditorWindow
    {
        private string key;
        private string path;
        private string input;
        private Action<string, string> save;
        private Action onWrited;

        public static void Show(string path, string key, string defaultMessage, Action<string, string> save, Action onWrited)
        {
            var window = GetWindow<AssetMessageInputWindow>("Uri");
            window.path = path;
            window.key = key;
            window.input = defaultMessage;
            window.save = save;
            window.onWrited = onWrited;
            window.minSize = window.maxSize = new Vector2(540, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label($"Path：{path}");
            GUILayout.Label("Title");
            key = EditorGUILayout.TextArea(key);
            GUILayout.Label("URI");
            input = EditorGUILayout.TextArea(input, GUILayout.Height(500));

            if (GUILayout.Button("Save"))
            {
                save?.Invoke(key, input);
                onWrited?.Invoke();
                AssetSummaryDrawer.Refresh(AssetDatabase.GUIDFromAssetPath(path).ToString());
                Close();
            }
        }
    }
}
