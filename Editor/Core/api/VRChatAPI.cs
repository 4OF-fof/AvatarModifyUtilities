using UnityEngine;
using System.Reflection;

namespace AMU.Editor.Core.API
{
    /// <summary>
    /// VRChat関連の機能を提供するAPI
    /// </summary>
    public static class VRChatAPI
    {
        /// <summary>
        /// GameObjectからBlueprint IDを取得します
        /// </summary>
        /// <param name="go">対象のGameObject</param>
        /// <returns>Blueprint ID（avtrで始まる場合のみ）、取得できない場合はnull</returns>
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

        /// <summary>
        /// 指定されたGameObjectがVRCアバターかどうかを判定します
        /// </summary>
        /// <param name="obj">判定対象のGameObject</param>
        /// <returns>VRCアバターの場合true、そうでなければfalse</returns>
        public static bool IsVRCAvatar(GameObject obj)
        {
            if (obj == null) return false;
            var pipelineManager = obj.GetComponent("PipelineManager");
            return pipelineManager != null;
        }
    }
}
