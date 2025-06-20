using AMU.Editor.Core.Api;

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
        public static string CurrentLanguage => AMU.Editor.Core.Controller.LocalizationController.CurrentLanguage;

        /// <summary>
        /// 指定された言語コードの言語ファイルを読み込みます
        /// </summary>
        public static void LoadLanguage(string languageCode)
        {
            AMU.Editor.Core.Controller.LocalizationController.LoadLanguage(languageCode);
        }

        /// <summary>
        /// 指定されたキーのローカライズされたテキストを取得します
        /// </summary>
        public static string GetText(string key)
        {
            return AMU.Editor.Core.Controller.LocalizationController.GetText(key);
        }
    }
}

// Data namespace backward compatibility aliases
namespace AMU.Data.Setting
{
    /// <summary>
    /// SettingDataの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Setting.SettingData instead", false)]
    public static class SettingData
    {
        public static System.Collections.Generic.Dictionary<string, AMU.Editor.Core.Schema.SettingItem[]> SettingItems => AMU.Editor.Setting.SettingData.SettingItems;
    }

    // Schema types backward compatibility aliases

    /// <summary>
    /// SettingTypeの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.SettingType instead", false)]
    public enum SettingType { String, Int, Bool, Float, Choice, FilePath, TextArea }

    /// <summary>
    /// SettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.SettingItem instead", false)]
    public abstract class SettingItem
    {
        public string Name => _item.Name;
        public SettingType Type => (SettingType)(int)_item.Type;

        private readonly AMU.Editor.Core.Schema.SettingItem _item;

        protected SettingItem(AMU.Editor.Core.Schema.SettingItem item)
        {
            _item = item;
        }
    }

    /// <summary>
    /// StringSettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.StringSettingItem instead", false)]
    public class StringSettingItem : SettingItem
    {
        public string DefaultValue { get; }
        public bool IsReadOnly { get; }

        public StringSettingItem(string name, string defaultValue = "", bool isReadOnly = false)
            : base(new AMU.Editor.Core.Schema.StringSettingItem(name, defaultValue, isReadOnly))
        {
            DefaultValue = defaultValue;
            IsReadOnly = isReadOnly;
        }
    }

    /// <summary>
    /// IntSettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.IntSettingItem instead", false)]
    public class IntSettingItem : SettingItem
    {
        public int DefaultValue { get; }
        public int MinValue { get; }
        public int MaxValue { get; }

        public IntSettingItem(string name, int defaultValue = 0, int minValue = 0, int maxValue = 100)
            : base(new AMU.Editor.Core.Schema.IntSettingItem(name, defaultValue, minValue, maxValue))
        {
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }

    /// <summary>
    /// BoolSettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.BoolSettingItem instead", false)]
    public class BoolSettingItem : SettingItem
    {
        public bool DefaultValue { get; }

        public BoolSettingItem(string name, bool defaultValue = false)
            : base(new AMU.Editor.Core.Schema.BoolSettingItem(name, defaultValue))
        {
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// FloatSettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.FloatSettingItem instead", false)]
    public class FloatSettingItem : SettingItem
    {
        public float DefaultValue { get; }
        public float MinValue { get; }
        public float MaxValue { get; }

        public FloatSettingItem(string name, float defaultValue = 0f, float minValue = 0f, float maxValue = 1f)
            : base(new AMU.Editor.Core.Schema.FloatSettingItem(name, defaultValue, minValue, maxValue))
        {
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }

    /// <summary>
    /// ChoiceSettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.ChoiceSettingItem instead", false)]
    public class ChoiceSettingItem : SettingItem
    {
        public System.Collections.Generic.Dictionary<string, string> Choices { get; }
        public string DefaultValue { get; }

        public ChoiceSettingItem(string name, System.Collections.Generic.Dictionary<string, string> choices, string defaultValue = "")
            : base(new AMU.Editor.Core.Schema.ChoiceSettingItem(name, choices, defaultValue))
        {
            Choices = choices;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// FilePathSettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.FilePathSettingItem instead", false)]
    public class FilePathSettingItem : SettingItem
    {
        public string DefaultValue { get; }
        public string ExtensionFilter { get; }
        public bool IsDirectory { get; }

        public FilePathSettingItem(string name, string defaultValue = "", string extensionFilter = "*")
            : base(new AMU.Editor.Core.Schema.FilePathSettingItem(name, defaultValue, extensionFilter))
        {
            DefaultValue = defaultValue;
            ExtensionFilter = extensionFilter;
            IsDirectory = false;
        }

        public FilePathSettingItem(string name, string defaultValue, bool isDirectory)
            : base(new AMU.Editor.Core.Schema.FilePathSettingItem(name, defaultValue, isDirectory))
        {
            DefaultValue = defaultValue;
            ExtensionFilter = "*";
            IsDirectory = isDirectory;
        }
    }

    /// <summary>
    /// TextAreaSettingItemの後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use AMU.Editor.Core.Schema.TextAreaSettingItem instead", false)]
    public class TextAreaSettingItem : SettingItem
    {
        public string DefaultValue { get; }
        public bool IsReadOnly { get; }
        public int MinLines { get; }
        public int MaxLines { get; }

        public TextAreaSettingItem(string name, string defaultValue = "", bool isReadOnly = false, int minLines = 3, int maxLines = 10)
            : base(new AMU.Editor.Core.Schema.TextAreaSettingItem(name, defaultValue, isReadOnly, minLines, maxLines))
        {
            DefaultValue = defaultValue;
            IsReadOnly = isReadOnly;
            MinLines = minLines;
            MaxLines = maxLines;
        }
    }
}
