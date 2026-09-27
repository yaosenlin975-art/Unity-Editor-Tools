using Lin.Editor.Settings;
using System;

namespace Lin.Editor.Asset
{
    /// <summary>
    /// Project 窗口里一条资源/文件夹注释。titleColor 是不带 # 的 RGB 十六进制串。
    /// </summary>
    [Serializable]
    public struct AssetSummary
    {
        public string title;
        public string titleColor;

        public string description;
        public string createTime;
        public string updateTime;

        public string GetRichTitle() =>
            $"<size={EditorAnnotationSettings.AssetSummaryTitleSize}><color=#{titleColor}><b>{title}</b></color></size>";
    }
}
