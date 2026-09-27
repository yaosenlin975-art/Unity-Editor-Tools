using UnityEditor;
using UnityEngine;

#if UNITY_6000_4_OR_NEWER
using InstanceId = UnityEngine.EntityId;
#else
using InstanceId = System.Int32;
#endif

namespace Lin.Editor
{
    /// <summary>
    /// 实现此接口的类会被 HierarchyDrawer 自动发现并绘制在 Hierarchy 行内。
    /// </summary>
    public interface IHierarchyDrawable
    {
        int drawPriority { get; }

        /// <param name="instanceId">SceneObject Id</param>
        /// <param name="drawablePoint">最右侧可以绘制的位置</param>
        /// <returns>使用掉的宽度</returns>
        float DrawInHierarchy(InstanceId instanceId, Rect drawablePoint);
    }
}
