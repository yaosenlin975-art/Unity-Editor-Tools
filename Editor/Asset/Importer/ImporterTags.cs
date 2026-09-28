using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;   // 只给下面那条只读守卫用（见 HasNonJsonUserData 的注释），编解码仍只在容器里
using UnityEditor;
using Lin.Editor.Asset;

namespace Lin.Editor.Asset.Importer
{
    /// <summary>
    /// 导入策略用的用户标记。容器是 ImporterUserData（同一个 .meta 上还住着 lin.annotation），
    /// 这里只负责 Learn 冻结的那套双层编码：值先序列化，再作为字符串标量写进外层对象。
    /// </summary>
    internal static class ImporterTags
    {
        internal const string TagsKey = "TAGS_KEY";

        internal static bool Contains(string userData, string tag)
        {
            var tags = Read(userData);
            return tags != null && tags.Contains(tag);
        }

        internal static string Add(string userData, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return userData ?? string.Empty;

            var tags = Read(userData) ?? new List<string>();
            if (!tags.Contains(tag))
                tags.Add(tag);

            return ImporterUserData.SetStringKey(userData, TagsKey, Serialize(tags));
        }

        internal static string Remove(string userData, string tag)
        {
            var tags = Read(userData);
            if (tags == null || !tags.Remove(tag))
                return userData ?? string.Empty;

            return tags.Count == 0
                ? ImporterUserData.RemoveKey(userData, TagsKey)
                : ImporterUserData.SetStringKey(userData, TagsKey, Serialize(tags));
        }

        /// <summary>
        /// TAGS_KEY 在、但读不出字符串列表 —— 与"没有这个 key"是两件事。
        /// 混为一谈会把资产当成未标记，重导一遍并覆盖原值，正是冻结格式要防的事故。
        /// </summary>
        internal static bool HasUnreadableTagsKey(string userData)
        {
            if (string.IsNullOrEmpty(userData))
                return false;
            if (Read(userData) != null)
                return false;
            return ImporterUserData.TryGetValue(userData, TagsKey) != null
                   || ContainsTagsKeyLiterally(userData);
        }

        /// <summary>
        /// userData 非空、且整体不是一个 JSON 对象 —— 容器重建时只能把它当空档丢掉（见 ImporterUserData.ParseOrEmpty），
        /// 所以本层任何一次 tag 写入都会把原值整体覆盖。与"有 TAGS_KEY 却读不出"是两件事，两条都得报。
        /// 这里是守卫专用的**只读** JObject.Parse 例外：本层不编码不解码，只看一眼"能不能接进去"。
        /// </summary>
        internal static bool HasNonJsonUserData(string userData)
        {
            if (string.IsNullOrEmpty(userData))
                return false;

            try
            {
                JObject.Parse(userData);
                return false;
            }
            catch (JsonException)
            {
                return true;
            }
        }

        private static string Serialize(List<string> tags) => JsonConvert.SerializeObject(tags);

        private static List<string> Read(string userData)
        {
            var raw = ImporterUserData.TryGetValue(userData, TagsKey);   // 字符串标量已被解包成 ["a","b"]
            if (string.IsNullOrEmpty(raw))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<List<string>>(raw);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool ContainsTagsKeyLiterally(string userData)
            => userData.IndexOf("\"" + TagsKey + "\"", System.StringComparison.Ordinal) >= 0;
    }

    public static class ImporterTagExtensions
    {
        public static void AddUserTag(this AssetImporter self, string tag)
        {
            var after = ImporterTags.Add(self.userData, tag);
            if (after == self.userData)
                return;

            // 本层是直接赋 self.userData 的，绕开了容器私有 Apply 里那条"非 JSON 会被覆盖"的告警，
            // 所以约束 6 的两条警告只能在这里发。位置在"算出来确实要写"之后、赋值之前：
            // 守卫读的还是写前的 userData，而没写就没有覆盖可言（MM-1：RemoveUserTag 撞非 JSON 时
            // Remove 原样返回，挂在开头的守卫会宣称一次根本没发生的覆盖）。
            WarnIfTagsKeyUnreadable(self);
            self.userData = after;
        }

        public static void RemoveUserTag(this AssetImporter self, string tag)
        {
            var after = ImporterTags.Remove(self.userData, tag);
            if (after == self.userData)
                return;

            // 同 AddUserTag：只在真要写之前告警。
            WarnIfTagsKeyUnreadable(self);
            self.userData = after;
        }

        /// <summary>只读：绝不写盘、绝不 SaveAndReimport。</summary>
        public static bool ContainsUserTag(this AssetImporter self, string tag)
            => self != null && ImporterTags.Contains(self.userData, tag);

        /// <summary>
        /// 该导入器上的全部用户标记。返回空表而不是 null —— 调用方一律直接 foreach，
        /// Learn 的旧实现（ImporterHelper.GetUserTags）在无标记时返回 null，是个潜在空引用。
        /// </summary>
        public static List<string> GetUserTags(this AssetImporter self)
            => self == null
                ? new List<string>()
                : ImporterUserData.GetUserData<List<string>>(self, ImporterTags.TagsKey) ?? new List<string>();

        /// <summary>
        /// 写盘前调一次，给一条带 assetPath 的警告。两种丢失形状分开报：
        /// TAGS_KEY 在但读不出（会被当成未标记后覆盖原值）、整串不是 JSON（容器当空档重建）。
        /// 守卫只读：不写 self.userData、不 SaveAndReimport，一次 JObject.Parse 只为判断形状。
        /// </summary>
        internal static void WarnIfTagsKeyUnreadable(this AssetImporter self)
        {
            if (self == null)
                return;

            if (ImporterTags.HasUnreadableTagsKey(self.userData))
            {
                UnityEngine.Debug.LogWarning(
                    $"[Lin Framework] {self.assetPath} 的 userData 里 {ImporterTags.TagsKey} 存在但读不出，" +
                    "将按未标记处理并覆盖原值 —— 若那是别家工具的格式，请关掉导入自动处理");
                return;
            }

            if (ImporterTags.HasNonJsonUserData(self.userData))
                UnityEngine.Debug.LogWarning(
                    $"[Lin Framework] {self.assetPath} 原有的 userData 不是 JSON，无法与 {ImporterTags.TagsKey} 同串共存，" +
                    "tag 写入会整体覆盖原值 —— 若那是别家工具的格式，请关掉导入自动处理");
        }
    }
}
