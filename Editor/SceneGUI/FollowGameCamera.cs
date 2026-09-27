using Lin.Editor.Settings;
using Lin.Editor.Toolbar.Element;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.SceneGUI
{
    /// <summary>开关控制 SceneView 跟随 GameView：播放中把 SceneView 相机对齐 GameView 相机。开关状态按 key 走包内 EditorPrefs 前缀持久化。</summary>
    [InitializeOnLoad]
    internal static class FollowGameCamera
    {
        private const string FOLLOW_GAME_VIEW_KEY = nameof(FOLLOW_GAME_VIEW_KEY);
        private static bool followGameView;

        static FollowGameCamera()
        {
            followGameView = EditorPrefs.GetBool(EditorAnnotationSettings.PrefKey(FOLLOW_GAME_VIEW_KEY), false);
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying || !followGameView)
                return;

            Camera camera = Camera.current;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.cameraSettings.nearClip = camera.nearClipPlane;
                sceneView.cameraSettings.fieldOfView = camera.fieldOfView;
                sceneView.pivot = camera.transform.position +
                    camera.transform.forward * sceneView.cameraDistance;
                sceneView.rotation = camera.transform.rotation;
            }
        }

        [ToolbarToggle(EAlign.Middle, EVisibleMode.Runtime, "Camera Icon", "SceneView 跟随 GameView", nameof(FollowGameView))]
        private static void FollowGameView(bool isOn)
        {
            EditorPrefs.SetBool(EditorAnnotationSettings.PrefKey(FOLLOW_GAME_VIEW_KEY), isOn);
            followGameView = isOn;
        }
    }
}
