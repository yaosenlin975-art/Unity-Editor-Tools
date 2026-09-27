using UnityEditor;
using Lin.Editor.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Toolbar.Element
{
    /// <summary>包自带的示例元素：播放期间在工具栏中部调节 TimeScale。</summary>
    public class TimeScaleSlider : IToolbarElement
    {
        public EAlign align => EAlign.Middle;

        public EVisibleMode visibleMode => EVisibleMode.Runtime;

        public float width => 300;

        public VisualElement Create()
        {
            var imgui = new IMGUIContainer(OnGUI);
            imgui.tooltip = EditorAnnotationLocalization.Text(EAnnotationText.TimeScaleTooltip);
            return imgui;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(5);
                ToolbarElementDrawer.Button(OnResetBtnClick, "R");
                GUILayout.Label(EditorAnnotationLocalization.Text(EAnnotationText.TimeScaleLabel));
                Rect sliderRect = EditorGUILayout.GetControlRect(GUILayout.Width(200), GUILayout.Height(20));
                Time.timeScale = EditorGUI.Slider(sliderRect, Time.timeScale, 0.2f, 5f);
            }
            GUILayout.EndHorizontal();
        }

        private void OnResetBtnClick() => Time.timeScale = 1;
    }
}
