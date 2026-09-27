using Lin.Editor.Toolbar.Element;

namespace Lin.Editor
{
    /// <summary>标记一个静态方法，使其出现在主工具栏上成为按钮。</summary>
    public class ToolbarButtonAttribute : ToolbarElementAttribute
    {
        public ToolbarButtonAttribute(EAlign align, EVisibleMode visibleMode, string iconPathOrLabel, string tooltip)
            : base(align, visibleMode, iconPathOrLabel, tooltip) { }
    }
}
