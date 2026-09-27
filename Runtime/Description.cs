using UnityEngine;

namespace Lin.Runtime
{
    /// <summary>
    /// 挂在场景物体上的注释数据。构建时由 hideFlags 剥离，故仅编辑器下有序列化字段。
    /// </summary>
    public class Description : MonoBehaviour
    {
#if UNITY_EDITOR
        public string title;
        public string description;
        public Color titleColor;

        public (string title, string description, Color color) Get() => (title, description, titleColor);
#endif

        private void OnValidate()
        {
            hideFlags = HideFlags.DontSaveInBuild | HideFlags.HideInInspector;
        }
    }
}
