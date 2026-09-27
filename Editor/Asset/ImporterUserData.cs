using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Asset
{
    /// <summary>
    /// AssetImporter.userData 的读写容器。userData 是宿主资产上的**共享**字符串，
    /// 所以规矩是：只动自己的 key、未知 key 逐字保留、读路径绝不写盘。
    /// </summary>
    public static class ImporterUserData
    {
        internal const string AnnotationKey = "lin.annotation";
        internal const string UrisKey = "lin.uris";

        #region - 纯变换（string→string，给单测直打，不碰 AssetImporter） -

        /// <summary>取原生值。字符串标量解包成它自己的内容，其余给紧凑 JSON；无值/非 JSON 一律 null。</summary>
        internal static string TryGetValue(string userData, string key)
        {
            if (string.IsNullOrEmpty(userData))
                return null;

            try
            {
                var token = JObject.Parse(userData)[key];
                if (token == null || token.Type == JTokenType.Null)
                    return null;

                return token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Formatting.None);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>把 JSON 片段作为**原生值**写入（一层编码，.meta 里可读可 diff）。</summary>
        internal static string SetNativeKey(string userData, string key, string jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue))
                return RemoveKey(userData, key);

            var obj = ParseOrEmpty(userData);
            obj[key] = JToken.Parse(jsonValue);
            return Write(obj);
        }

        /// <summary>
        /// 把内容作为**字符串标量**写入，即 ADR-004 冻结的双层编码：值先序列化，整串再当 string 塞进去。
        /// tag 那条线要用它，否则 Learn 存量的 TAGS_KEY 会被判成未标记。
        /// </summary>
        internal static string SetStringKey(string userData, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return RemoveKey(userData, key);

            var obj = ParseOrEmpty(userData);
            obj[key] = value;
            return Write(obj);
        }

        internal static string RemoveKey(string userData, string key)
        {
            var obj = ParseOrEmpty(userData);
            obj.Remove(key);
            return obj.Count == 0 ? string.Empty : Write(obj);
        }

        private static JObject ParseOrEmpty(string userData)
        {
            if (string.IsNullOrEmpty(userData))
                return new JObject();

            try
            {
                return JObject.Parse(userData);
            }
            catch (JsonException)
            {
                // 非 JSON：无法与自有内容共存于同一个字符串，只能当空档。
                // 覆盖动作的调用方（importer 层）负责留下那条日志，那里才知道 assetPath。
                return new JObject();
            }
        }

        // Formatting.None 是 Learn 里 CleanJson 字符串替换的等价物，也是 ADR-004 冻结格式的来源
        private static string Write(JObject obj) => obj.ToString(Formatting.None);

        #endregion

        #region - AssetImporter 层 -

        public static T GetUserData<T>(this AssetImporter self, string key)
        {
            if (self == null)
                return default;

            return ReadValue<T>(self.userData, key);
        }

        internal static T ReadValue<T>(string userData, string key)
        {
            var json = TryGetValue(userData, key);
            if (string.IsNullOrEmpty(json))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static void SetUserData<T>(this AssetImporter self, string key, T value, bool saveAndReimport = false)
        {
            Apply(self, SetNativeKey(self.userData, key, value == null ? null : JsonConvert.SerializeObject(value)), saveAndReimport);
        }

        public static void RemoveUserData(this AssetImporter self, string key, bool saveAndReimport = false)
        {
            Apply(self, RemoveKey(self.userData, key), saveAndReimport);
        }

        /// <summary>
        /// 唯一的写出口。先比对：没有实际变化就不写、不重导入——删一条不存在的注释不该让资产变脏。
        /// before 必须在赋值前取。
        /// </summary>
        private static void Apply(AssetImporter self, string after, bool saveAndReimport)
        {
            var before = self.userData;
            if (before == after)
                return;

            if (!string.IsNullOrEmpty(before))
            {
                try
                {
                    JObject.Parse(before);
                }
                catch (JsonException)
                {
                    // 非 JSON 的 userData 保不住（无法与自有内容同串共存），至少留一条日志
                    Debug.LogWarning($"[Lin Editor Tools] {self.assetPath} 原有的 userData 不是 JSON，无法与注释共存，已覆盖");
                }
            }

            self.userData = after;
            if (saveAndReimport)
                self.SaveAndReimport();
        }

        public static string GetAssetGUID(this AssetImporter self) => AssetDatabase.AssetPathToGUID(self.assetPath);

        #endregion

        #region - 注释层 -

        /// <summary>
        /// 编解码单独露出来是为了能测：AssetSummary 全靠 public 字段，
        /// "Newtonsoft 默认序列化 public 字段"这条假设得有断言兜着（C6/C7），
        /// 而测试程序集引用不到 Newtonsoft，所以测试只喂 string 和结构体。
        /// </summary>
        internal static string AnnotationToJson(AssetSummary summary) => JsonConvert.SerializeObject(summary);

        internal static AssetSummary AnnotationFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<AssetSummary>(json);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static AssetSummary GetAnnotation(this AssetImporter self) =>
            self == null ? default : AnnotationFromJson(TryGetValue(self.userData, AnnotationKey));

        public static void SetAnnotation(this AssetImporter self, AssetSummary summary)
        {
            // 标题空 = 注释没了，别在 .meta 里留一条空记录
            if (string.IsNullOrEmpty(summary.title))
            {
                self.RemoveAnnotation();
                return;
            }

            var now = System.DateTime.Now.ToString("o");
            var old = self.GetAnnotation();
            summary.createTime = string.IsNullOrEmpty(old.createTime) ? now : old.createTime;
            summary.updateTime = now;

            Apply(self, SetNativeKey(self.userData, AnnotationKey, AnnotationToJson(summary)), true);
            AssetSummaryDrawer.Refresh(GetAssetGUID(self));
        }

        public static void RemoveAnnotation(this AssetImporter self)
        {
            Apply(self, RemoveKey(self.userData, AnnotationKey), true);
            AssetSummaryDrawer.Refresh(GetAssetGUID(self));
        }

        #endregion

        #region - URI 层 -

        /// <summary>资产上的自定义链接表（标题 → uri）。无记录时给空表，绝不返回 null。</summary>
        public static Dictionary<string, string> GetUris(this AssetImporter self)
        {
            if (self == null)
                return new Dictionary<string, string>();

            return ReadValue<Dictionary<string, string>>(self.userData, UrisKey) ?? new Dictionary<string, string>();
        }

        public static void SetUri(this AssetImporter self, string key, string uri)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var uris = self.GetUris();
            uris[key] = uri;
            Apply(self, WriteUris(self.userData, uris), true);
        }

        public static void RemoveUri(this AssetImporter self, string key)
        {
            var uris = self.GetUris();
            if (uris.Remove(key))
                Apply(self, WriteUris(self.userData, uris), true);
        }

        /// <summary>表删空时连 key 一起摘掉，不留 "lin.uris":{} 这种空壳。</summary>
        private static string WriteUris(string userData, Dictionary<string, string> uris) =>
            uris.Count == 0
                ? RemoveKey(userData, UrisKey)
                : SetNativeKey(userData, UrisKey, JsonConvert.SerializeObject(uris));

        #endregion
    }
}
