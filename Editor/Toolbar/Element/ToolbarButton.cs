using Lin.Editor;
using System;
using UnityEngine;

namespace Lin.Editor.Toolbar.Element
{
    public class ToolbarButton : ToolbarElementBase
    {
        private readonly Action onClick;

        public ToolbarButton(ToolbarButtonAttribute toolbarButtonAttribute, Action onClick) : base(toolbarButtonAttribute)
        {
            this.onClick = onClick;
        }

        public override float width => 34;

        protected override void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);

            if (icon != null)
                ToolbarElementDrawer.Button(onClick, icon);
            else
                ToolbarElementDrawer.Button(onClick, label);

            GUILayout.EndHorizontal();
        }
    }
}
