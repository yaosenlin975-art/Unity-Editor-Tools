using Lin.Editor.Settings;
using Lin.Editor.Toolbar.Element;
using UnityEditor;

namespace Lin.Editor
{
    /// <summary>标记一个签名为 static void(bool) 的方法，使其出现在主工具栏上成为开关。开关状态按 key 持久化。</summary>
    public class ToolbarToggleAttribute : ToolbarElementAttribute
    {
        public bool isOn { get; }
        public string key { get; }

        public ToolbarToggleAttribute(EAlign align, EVisibleMode visibleMode, string iconPathOrLabel, string tooltip, string key)
            : base(align, visibleMode, iconPathOrLabel, tooltip)
        {
            this.key = key;
            isOn = EditorPrefs.GetBool(EditorAnnotationSettings.PrefKey(key), false);
        }
    }
}
