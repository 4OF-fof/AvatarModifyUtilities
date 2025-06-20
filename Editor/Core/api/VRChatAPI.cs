using System.Reflection;

using UnityEngine;

namespace AMU.Editor.Core.Api
{
    public static class VRChatAPI
    {
        public static string GetBlueprintId(GameObject go)
        {
            if (go == null) return null;
            var pipelineManager = go.GetComponent("PipelineManager");
            if (pipelineManager == null) return null;

            System.Type type = pipelineManager.GetType();

            string blueprintId = null;
            FieldInfo field = type.GetField("blueprintId", BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(pipelineManager);
                blueprintId = value != null ? value.ToString() : null;
            }
            else
            {
                PropertyInfo prop = type.GetProperty("blueprintId", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanRead)
                {
                    try
                    {
                        object value = prop.GetValue(pipelineManager, null);
                        blueprintId = value != null ? value.ToString() : null;
                    }
                    catch
                    {
                        blueprintId = null;
                    }
                }
            }

            if (!string.IsNullOrEmpty(blueprintId) && blueprintId.StartsWith("avtr"))
                return blueprintId;

            return null;
        }

        public static bool IsVRCAvatar(GameObject obj)
        {
            if (obj == null) return false;
            var pipelineManager = obj.GetComponent("PipelineManager");
            return pipelineManager != null;
        }
    }
}
