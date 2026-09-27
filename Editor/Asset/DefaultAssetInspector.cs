using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Lin.Editor.Asset
{
    /// <summary>
    /// DefaultAsset（文件夹与没有专用导入器的资产）的 Inspector：头部显示注释与自定义链接，
    /// 选中文件夹时额外递归列出整棵子树，点一条就在 Project 窗口 ping 它，再点同一条打开它。
    /// </summary>
    [CustomEditor(typeof(DefaultAsset), true)]
    public class DefaultAssetInspector : UnityEditor.Editor
    {
        // 树节点的池。Unity 自带 ObjectPool 只活在 CoreModule 里，本包用它换掉框架的 Factory<T>，零新增依赖。
        private static readonly ObjectPool<Data> DataPool =
            new ObjectPool<Data>(() => new Data(), OnDataGet, OnDataRelease);

        // isSelected 不进池就得自己清：归还时它可能还停在 true，下个 Inspector 拿到同一实例就画出一条无关的灰底
        private static void OnDataGet(Data data)
        {
            data.childs.Clear();
            data.isSelected = false;
        }

        private static void OnDataRelease(Data data) => data.childs.Clear();

        private Dictionary<string, string> customUris;
        private AssetSummary assetSummary;
        private string path;
        private AssetImporter importer;

        private void OnEnable()
        {
            path = AssetDatabase.GetAssetPath(target);
            importer = AssetImporter.GetAtPath(path);

            Refresh();

            if (Directory.Exists(path))
            {
                data = DataPool.Get();
                LoadFiles(data, path);
            }
        }

        private void OnDisable()
        {
            RecycleDatas(data);

            void RecycleDatas(Data data)
            {
                if (data is null)
                    return;

                if (data.childs is not null)
                {
                    foreach (var child in data.childs)
                        RecycleDatas(child);

                    data.childs.Clear();
                }

                DataPool.Release(data);
            }
        }

        protected override void OnHeaderGUI()
        {
            if (!string.IsNullOrEmpty(assetSummary.title))
            {
                GUILayout.Space(10);
                GUILayout.Label($"路径: {path}", EditorStyles.boldLabel);
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                var richTextStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true
                };
                GUILayout.Label(assetSummary.GetRichTitle(), richTextStyle);
                GUILayout.Label(assetSummary.description, richTextStyle);
                GUILayout.Space(10);

                GUILayout.Label($"<i>更新时间: {assetSummary.updateTime}</i>", richTextStyle);
                GUILayout.Label($"<i>创建时间: {assetSummary.createTime}</i>", richTextStyle);

                GUILayout.EndVertical();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("修改备注"))
                AssetSummaryWindow.ShowAssetSummary(path);
            if (GUILayout.Button("添加uri"))
                AssetMessageInputWindow.Show(path, string.Empty, string.Empty, SaveUri, Refresh);
            GUILayout.EndHorizontal();

            if (customUris.Count > 0)
            {
                GUILayout.Space(10);
                foreach (var pair in customUris)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(pair.Key);
                    GUILayout.Label(pair.Value);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("修改"))
                    {
                        string oldKey = pair.Key;
                        AssetMessageInputWindow.Show(path,
                            key: pair.Key,
                            defaultMessage: pair.Value,
                            save: (key, uri) =>
                            {
                                importer.RemoveUri(oldKey);
                                importer.SetUri(key, uri);
                            },
                            onWrited: Refresh);
                    }
                    if (GUILayout.Button("访问"))
                        Application.OpenURL(pair.Value);
                    if (GUILayout.Button("移除"))
                    {
                        importer.RemoveUri(pair.Key);
                        Refresh();
                    }
                    GUILayout.EndHorizontal();
                }
            }

            base.OnHeaderGUI();
        }

        public override void OnInspectorGUI()
        {
            if (Directory.Exists(path))
            {
                GUI.enabled = true;
                EditorGUIUtility.SetIconSize(Vector2.one * 16);
                DrawData(data);
            }
        }

        private void SaveUri(string key, string uri) => importer.SetUri(key, uri);

        private void Refresh()
        {
            assetSummary = importer.GetAnnotation();
            customUris = importer.GetUris();
        }

        #region - 文件夹拓展 -

        private Data data;
        private Data selectData;

        private void LoadFiles(Data data, string currentPath, int indent = 0)
        {
            if (string.IsNullOrEmpty(currentPath))
                return;

            GUIContent content = GetGUIContent(currentPath);

            if (content != null)
            {
                data.indent = indent;
                data.content = content;
                data.assetPath = currentPath;
            }

            var files = Directory.GetFiles(currentPath);
            var directories = Directory.GetDirectories(currentPath);
            data.childs = data.childs ?? new List<Data>(files.Length + directories.Length);

            foreach (var file in files)
            {
                string assetPath = ToAssetPath(file);
                content = GetGUIContent(assetPath);
                if (content == null)
                    continue;

                Data child = DataPool.Get();
                child.indent = indent + 1;
                child.content = content;
                child.assetPath = assetPath;
                data.childs.Add(child);
            }

            foreach (var directory in directories)
            {
                Data childDir = DataPool.Get();
                data.childs.Add(childDir);
                LoadFiles(childDir, ToAssetPath(directory), indent + 1);
            }
        }

        // Directory 系返回的是 Windows 反斜杠路径，而下面的 GetAnnotation/GetCachedIcon/PingObject 吃的是 AssetDatabase 路径
        private static string ToAssetPath(string path) => path.Replace('\\', '/');

        private void DrawData(Data data)
        {
            if (data.content != null)
            {
                EditorGUI.indentLevel = data.indent;
                DrawGUIData(data);
            }

            for (int i = 0; i < data.childs.Count; i++)
            {
                Data child = data.childs[i];
                if (child.content != null)
                {
                    EditorGUI.indentLevel = child.indent;
                    if (child.childs != null && child.childs.Count > 0)
                        DrawData(child);
                    else
                        DrawGUIData(child);
                }
            }
        }

        private void DrawGUIData(Data data)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleLeft
            };

            Rect rt = GUILayoutUtility.GetRect(data.content, style);
            if (data.isSelected)
                EditorGUI.DrawRect(rt, Color.gray);

            rt.x += 16 * EditorGUI.indentLevel;
            if (GUI.Button(rt, data.content, style))
            {
                if (selectData != null)
                    selectData.isSelected = false;
                data.isSelected = true;

                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(data.assetPath));

                if (selectData == data)
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(selectData.assetPath);
                    if (obj != null)
                        AssetDatabase.OpenAsset(obj);
                }
                else
                    selectData = data;
            }
        }

        private GUIContent GetGUIContent(string path)
        {
            if (path.EndsWith(".meta"))
                return null;

            var summary = AssetImporter.GetAtPath(path).GetAnnotation();
            var text = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(summary.title))
                text = $"{text}    {summary.GetRichTitle()}";

            return new GUIContent(text, AssetDatabase.GetCachedIcon(path));
        }

        private class Data
        {
            public bool isSelected;
            public int indent;
            public GUIContent content;
            public string assetPath;
            public List<Data> childs;

            public Data() => childs = new List<Data>();
        }

        #endregion
    }
}
