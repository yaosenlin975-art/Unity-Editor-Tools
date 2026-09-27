/*
┌────────────────────────────┐
│　Description：将 .log 文件导入为 TextAsset
│　Remark：用于轨迹等文本日志在编辑器中的统一读取
└────────────────────────────┘
*/
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Lin.Editor.Asset.Importer
{
    [ScriptedImporter(1, new[] { "log" })]
    public class LogTextImporter : ScriptedImporter
    {
        /// <summary>
        /// 将 .log 文件作为 TextAsset 导入。适用于需要以文本读取的日志文件。
        /// 用法：把扩展名为 .log 的文件放入 Assets 下，导入器会自动生成一个 TextAsset。
        /// </summary>
        /// <param name="ctx">导入上下文</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string content = File.ReadAllText(ctx.assetPath);
            var text = new TextAsset(content);
            ctx.AddObjectToAsset("Text", text);
            ctx.SetMainObject(text);
        }
    }
}
