using Lin.Editor.Toolbar.Element;

namespace Lin.Editor
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public abstract class ToolbarElementAttribute : System.Attribute
    {
        public EAlign align { get; }
        public EVisibleMode visibleMode { get; }
        public string iconPathOrLabel { get; }
        public string tooltip { get; }

        protected ToolbarElementAttribute(EAlign align, EVisibleMode visibleMode, string tooltip)
        {
            this.align = align;
            this.visibleMode = visibleMode;
            this.tooltip = tooltip;
        }

        protected ToolbarElementAttribute(EAlign align, EVisibleMode visibleMode, string iconPathOrLabel, string tooltip)
            : this(align, visibleMode, tooltip)
        {
            this.iconPathOrLabel = iconPathOrLabel;
        }
    }
}
