/*
┌────────────────────────────┐
│　Description: SceneView Renderer 信息绘制
│　Remark: 与 Scene GUI Attribute 绘制器分离，保留 Renderer 统计叠加
└────────────────────────────┘
┌────────────────────────────┐
│　ClassName: SceneRendererInfoDrawer
└────────────────────────────┘
*/

using Lin.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.SceneGUI
{
    internal static class SceneRendererInfoDrawer
    {
        private sealed class RendererInfos
        {
            public Bounds bounds;
            public int vertexsCount;
            public int trianglesCount;
        }

        private static RendererInfos infos;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            OnSelectionChanged();
        }

        private static void OnSelectionChanged()
        {
            CalculateBounds();
            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            DrawObjectBounds();
        }

        private static void CalculateBounds()
        {
            Object target = Selection.activeObject;
            if (target == null)
            {
                infos = null;
                return;
            }

            if (target is not GameObject gameObject || !gameObject.scene.IsValid())
            {
                infos = null;
                return;
            }

            if (gameObject.GetComponentInChildren<Renderer>() == null)
            {
                infos = null;
                return;
            }

            infos = new RendererInfos
            {
                bounds = CalculateObjectBounds(gameObject, false)
            };

            void AddMeshInfo(Mesh mesh)
            {
                if (mesh == null)
                    return;

                infos.vertexsCount += mesh.vertexCount;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    if (mesh.GetTopology(i) == MeshTopology.Triangles)
                        infos.trianglesCount += (int)mesh.GetIndexCount(i) / 3;
                }
            }

            MeshFilter[] filters = gameObject.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < filters.Length; i++)
                AddMeshInfo(filters[i].sharedMesh);

            SkinnedMeshRenderer[] skinners = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < skinners.Length; i++)
                AddMeshInfo(skinners[i].sharedMesh);
        }

        private static void DrawObjectBounds()
        {
            if (infos == null)
                return;

            ToolsSettings_SO settings = ToolsSettings_SO.Instance;
            if (!settings.sceneRendererInfoEnabled)
                return;

            Handles.color = settings.sceneRendererInfoColor;
            Handles.DrawWireCube(infos.bounds.center, infos.bounds.size);

            var style = new GUIStyle
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = settings.sceneRendererInfoColor;

            SceneView sceneView = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
            Camera camera = sceneView != null ? sceneView.camera : null;
            Vector3 center = infos.bounds.center;
            Vector3 extents = infos.bounds.extents;
            var corners = new Vector3[8];
            corners[0] = center + new Vector3(extents.x, extents.y, extents.z);
            corners[1] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[2] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[3] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[4] = center + new Vector3(-extents.x, extents.y, extents.z);
            corners[5] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[6] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[7] = center + new Vector3(-extents.x, -extents.y, -extents.z);

            Vector3 bestWorld = corners[0];
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 gui = HandleUtility.WorldToGUIPoint(corners[i]);
                float score = gui.x + gui.y;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestWorld = corners[i];
                }
            }

            if (camera != null)
            {
                float size = HandleUtility.GetHandleSize(bestWorld) * 0.2f;
                bestWorld += camera.transform.right * (size * 0.3f) - camera.transform.up * (size * 0.3f);
            }

            Handles.Label(bestWorld, $"顶点数: {infos.vertexsCount}\n面数: {infos.trianglesCount}", style);
        }

        /// <summary>
        /// 包内自含的包围盒并集，语义对应宿主 Lin.Runtime 的 BoundsExtension.CalculateObjectBounds(includeInactive)：
        /// Renderer 与 Terrain 两路各取 min/max 后合并。包程序集不引用宿主运行时，扩展方法搬不进来，只能落一份私有实现。
        /// </summary>
        private static Bounds CalculateObjectBounds(GameObject root, bool includeInactive)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                min = Vector3.Min(min, renderers[i].bounds.min);
                max = Vector3.Max(max, renderers[i].bounds.max);
            }

            Terrain[] terrains = root.GetComponentsInChildren<Terrain>(includeInactive);
            for (int i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] == null || terrains[i].terrainData == null)
                    continue;

                Bounds bounds = terrains[i].terrainData.bounds;
                bounds.center = terrains[i].transform.position + new Vector3(bounds.size.x, 0f, bounds.size.z) / 2f;

                min = Vector3.Min(min, bounds.min);
                max = Vector3.Max(max, bounds.max);
            }

            return new Bounds((min + max) / 2f, max - min);
        }
    }
}
