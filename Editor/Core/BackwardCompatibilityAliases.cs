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

namespace AMU.Data.Lang
{
    /// <summary>
    /// ローカライゼーション機能を管理するマネージャー（後方互換性のため）
    /// </summary>
    public static partial class LocalizationManager
    {
        /// <summary>
        /// 現在の言語コード
        /// </summary>
        public static string CurrentLanguage => AMU.Editor.Core.Controllers.LocalizationController.CurrentLanguage;

        /// <summary>
        /// 指定された言語コードの言語ファイルを読み込みます
        /// </summary>
        public static void LoadLanguage(string languageCode)
        {
            AMU.Editor.Core.Controllers.LocalizationController.LoadLanguage(languageCode);
        }

        /// <summary>
        /// 指定されたキーのローカライズされたテキストを取得します
        /// </summary>
        public static string GetText(string key)
        {
            return AMU.Editor.Core.Controllers.LocalizationController.GetText(key);
        }
    }
}