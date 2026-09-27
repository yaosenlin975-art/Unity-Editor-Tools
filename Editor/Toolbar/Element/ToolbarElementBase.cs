using Lin.Editor;
using UnityEditor;
using Lin.Editor.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Toolbar.Element
{
    public abstract class ToolbarElementBase : IToolbarElement
    {
        protected readonly Texture2D icon;
        protected readonly string label;
        protected readonly string tooltip;

        public EAlign align { get; }
        public EVisibleMode visibleMode { get; }
        public abstract float width { get; }

        public ToolbarElementBase(ToolbarElementAttribute toolbarElementAttribute)
        {
            align = toolbarElementAttribute.align;
            visibleMode = toolbarElementAttribute.visibleMode;
            icon = LoadIcon(toolbarElementAttribute.iconPathOrLabel);
            label = toolbarElementAttribute.iconPathOrLabel;
            tooltip = toolbarElementAttribute.tooltip;
            if (label == "注释" && tooltip == "打开注释设置")
            {
                label = EditorAnnotationLocalization.Text(EAnnotationText.ToolbarAnnotation);
                tooltip = EditorAnnotationLocalization.Text(EAnnotationText.ToolbarAnnotationTooltip);
            }
        }

        /// <summary>
        /// 取值为资产路径或内置图标名时解析成图标，否则返回 null 由子类退化为文字按钮。
        /// EditorGUIUtility.IconContent 对不存在的名字会打一条 "Unable to load the icon" 警告，
        /// 而内置图标名全是 ASCII，所以含非 ASCII 字符（中文按钮文字）直接跳过，不刷警告。
        /// </summary>
        private static Texture2D LoadIcon(string iconPathOrLabel)
        {
            if (string.IsNullOrEmpty(iconPathOrLabel))
                return null;

            var byPath = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPathOrLabel);
            if (byPath != null)
                return byPath;

            if (!IsAscii(iconPathOrLabel))
                return null;

            try
            {
                return EditorGUIUtility.IconContent(iconPathOrLabel).image as Texture2D;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        // Encoding.ASCII.GetByteCount 会把非 ASCII 字符按 '?' 计数，长度对得上，用它判 ASCII 是错的
        private static bool IsAscii(string value)
        {
            foreach (var c in value)
                if (c > 127)
                    return false;
            return true;
        }

        public VisualElement Create()
        {
            var imgui = new IMGUIContainer(OnGUI);
            imgui.tooltip = tooltip;
            return imgui;
        }

        protected abstract void OnGUI();
    }
}
