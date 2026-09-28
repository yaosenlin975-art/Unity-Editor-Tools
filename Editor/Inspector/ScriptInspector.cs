using UnityEditor;
using UnityEngine;
using Lin.Runtime.Attribute;

namespace Lin.Editor.Inspector
{
    public static class ScriptInspector
    {
        [MenuItem("CONTEXT/MonoBehaviour/Rename GameObject")]
        private static void RenameGameObject(MenuCommand command) => RenameGameObjectInternal(command);

        private static void RenameGameObjectInternal(MenuCommand command)
        {
            MonoBehaviour monoBehaviour = command.context as MonoBehaviour;
            if (monoBehaviour != null)
            {
                var nameAtt = monoBehaviour.GetType().GetCustomAttributes(typeof(NameAttribute), false);
                if (nameAtt is { Length: > 0 } && nameAtt[0] is NameAttribute nameAttr)
                    monoBehaviour.name = nameAttr.Name;
                else
                    monoBehaviour.name = monoBehaviour.GetType().Name;
            }
        }
    }
}
