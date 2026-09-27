/*
┌────────────────────────────┐
│　Description: 菜单拓展
│　Author: 花球i
└────────────────────────────┘
*/
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Helper
{
    public static class EditorGUIHelper
    {
        private static Texture2D MakeColoredTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static GUIStyle GetButtonStyle(Color color)
        {
            GUIStyle result = new GUIStyle(GUI.skin.button);
            result.normal.textColor = Color.white;
            result.hover.textColor = Color.white;
            result.active.textColor = Color.white;
            result.focused.textColor = Color.white;
            result.normal.background = MakeColoredTexture(color * 0.8f);
            result.hover.background = MakeColoredTexture(color * 0.6f); // 点击时颜色变暗
            result.active.background = MakeColoredTexture(color * 0.8f);
            result.focused.background = MakeColoredTexture(color);
            return result;
        }
    }
}