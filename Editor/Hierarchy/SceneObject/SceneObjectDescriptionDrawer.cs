using Lin.Editor.Settings;
using UnityEditor;
using UnityEngine;

#if UNITY_6000_4_OR_NEWER
using InstanceId = UnityEngine.EntityId;
#else
using InstanceId = System.Int32;
#endif

namespace Lin.Editor.Hierarchy.SceneObject
{
    /// <summary>把 Description 组件的内容绘制在 Hierarchy 行尾。</summary>
    public class SceneObjectDescriptionDrawer : IHierarchyDrawable
    {
        // 双击检测相关字段
        private static float lastClickTime = 0f;
        private static Vector2 lastClickPosition = Vector2.zero;
#if UNITY_6000_4_OR_NEWER
        private static EntityId lastClickedInstanceId = EntityId.None;
#else
        private static int lastClickedInstanceId = -1;
#endif
        private const float DOUBLE_CLICK_TIME = 0.3f; // 双击时间间隔
        private const float DOUBLE_CLICK_DISTANCE = 5f; // 双击位置容差

        public int drawPriority => 0;

        public float DrawInHierarchy(InstanceId instanceId, Rect drawablePoint)
        {
            var description = SceneObjectDescriptionsMap.GetInstance().GetDescription(instanceId);
            Rect labelRect = new Rect(drawablePoint.xMax - 60, drawablePoint.y, 60, 12);
            if (string.IsNullOrEmpty(description.title))
                return 0;

            // 创建样式并启用富文本
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.richText = true;
            style.fontSize = EditorAnnotationSettings.SceneObjectDescriptionTitleSize;
            style.alignment = TextAnchor.MiddleRight;

            // 对象Tooltip
            var content = new GUIContent($"<color=#{ColorUtility.ToHtmlStringRGB(description.color)}>{description.title}</color>");
            content.tooltip = description.description;

            var size = style.CalcSize(content);
            labelRect.width = size.x;
            labelRect.x = drawablePoint.xMax - size.x - 5;

            // 绘制注释
            GUI.Label(labelRect, content, style);

            // 处理鼠标点击事件
            Event currentEvent = Event.current;

            // 检查鼠标是否在标签区域内
            if (labelRect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    switch (EditorAnnotationSettings.SceneObjectDescriptionEditWay)
                    {
                        case EClickType.无响应:
                            break;

                        case EClickType.单击:
                            SceneObjectDescriptionWindow.ShowDescription(instanceId);
                            Event.current.Use();
                            break;

                        case EClickType.双击:
                        default:
                            // 实现双击检测逻辑
                            float currentTime = (float)EditorApplication.timeSinceStartup;
                            Vector2 currentPosition = currentEvent.mousePosition;

                            // 检查是否为双击
                            bool isDoubleClick = (currentTime - lastClickTime) <= DOUBLE_CLICK_TIME &&
                                                 Vector2.Distance(currentPosition, lastClickPosition) <= DOUBLE_CLICK_DISTANCE &&
                                                 lastClickedInstanceId == instanceId;

                            if (isDoubleClick)
                            {
                                // 双击事件：显示描述窗口
                                SceneObjectDescriptionWindow.ShowDescription(instanceId);

                                // 重置双击检测状态
                                lastClickTime = 0f;
#if UNITY_6000_4_OR_NEWER
                                lastClickedInstanceId = EntityId.None;
#else
                                lastClickedInstanceId = -1;
#endif
                                Event.current.Use();
                            }
                            else
                            {
                                // 记录当前点击信息，用于下次双击检测
                                lastClickTime = currentTime;
                                lastClickPosition = currentPosition;
                                lastClickedInstanceId = instanceId;
                            }
                            break;
                    }
                }
            }

            return labelRect.width;
        }
    }
}
