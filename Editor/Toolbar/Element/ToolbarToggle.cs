using Lin.Editor;
using System;
using UnityEngine;

namespace Lin.Editor.Toolbar.Element
{
    public class ToolbarToggle : ToolbarElementBase
    {
        private readonly Action<bool> onValueChanged;
        private bool isOn;

        public ToolbarToggle(ToolbarToggleAttribute toolbarToggleAttribute, Action<bool> onValueChanged) : base(toolbarToggleAttribute)
        {
            this.onValueChanged = onValueChanged;
            isOn = toolbarToggleAttribute.isOn;
        }

        public override float width => 34;

        protected override void OnGUI()
        {
            bool currentValue = isOn;
            if (icon != null)
                currentValue = ToolbarElementDrawer.Toggle(isOn, icon);
            else
                currentValue = ToolbarElementDrawer.Toggle(isOn, label);

            if (currentValue ^ isOn)
            {
                isOn = currentValue;
                onValueChanged(isOn);
            }
        }
    }
}
