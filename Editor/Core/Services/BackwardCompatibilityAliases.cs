using AMU.Editor.Core.API;

namespace AMU.Editor.Core.Helper
{
    /// <summary>
    /// ObjectCaptureHelperの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.API.ObjectCaptureAPI instead", false)]
    public static class ObjectCaptureHelper
    {
        [System.Obsolete("Use AMU.Editor.Core.API.ObjectCaptureAPI.CaptureObject instead", false)]
        public static UnityEngine.Texture2D CaptureObject(UnityEngine.GameObject targetObject, string savePath, int width = 512, int height = 512)
        {
            return ObjectCaptureAPI.CaptureObject(targetObject, savePath, width, height);
        }
    }

    /// <summary>
    /// PipelineManagerHelperの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.API.VRChatAPI instead", false)]
    public static class PipelineManagerHelper
    {
        [System.Obsolete("Use AMU.Editor.Core.API.VRChatAPI.GetBlueprintId instead", false)]
        public static string GetBlueprintId(UnityEngine.GameObject go)
        {
            return VRChatAPI.GetBlueprintId(go);
        }

        [System.Obsolete("Use AMU.Editor.Core.API.VRChatAPI.IsVRCAvatar instead", false)]
        public static bool isVRCAvatar(UnityEngine.GameObject obj)
        {
            return VRChatAPI.IsVRCAvatar(obj);
        }
    }
}

namespace AMU.Editor.Initializer
{
    /// <summary>
    /// AMUInitializerの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Services.InitializationService instead", false)]
    public static class AMUInitializer
    {
        [System.Obsolete("Initialization is now handled automatically by InitializationService", false)]
        public static void Initialize()
        {
            AMU.Editor.Core.Services.InitializationService.Initialize();
        }
    }
}
