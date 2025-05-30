using System.Collections.Generic;

namespace AMU.Data.Setting
{
    public enum SettingType { String, Int, Bool, Float, Choice, FilePath }

    public abstract class SettingItem
    {
        public string Name { get; }
        public SettingType Type { get; }
        protected SettingItem(string name, SettingType type)
        {
            Name = name;
            Type = type;
        }
    }

    public class StringSettingItem : SettingItem
    {
        public string DefaultValue { get; }
        public bool IsReadOnly { get; }
        public StringSettingItem(string name, string defaultValue = "", bool isReadOnly = false) : base(name, SettingType.String)
        {
            DefaultValue = defaultValue;
            IsReadOnly = isReadOnly;
        }
    }

    public class IntSettingItem : SettingItem
    {
        public int DefaultValue { get; }
        public int MinValue { get; }
        public int MaxValue { get; }
        public IntSettingItem(string name, int defaultValue = 0, int minValue = 0, int maxValue = 100) : base(name, SettingType.Int)
        {
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }

    public class BoolSettingItem : SettingItem
    {
        public bool DefaultValue { get; }
        public BoolSettingItem(string name, bool defaultValue = false) : base(name, SettingType.Bool)
        {
            DefaultValue = defaultValue;
        }
    }

    public class FloatSettingItem : SettingItem
    {
        public float DefaultValue { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public FloatSettingItem(string name, float defaultValue = 0f, float minValue = 0f, float maxValue = 1f) : base(name, SettingType.Float)
        {
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }

    public class ChoiceSettingItem : SettingItem
    {
        public Dictionary<string, string> Choices { get; }
        public string DefaultValue { get; }
        public ChoiceSettingItem(string name, Dictionary<string, string> choices, string defaultValue = "") : base(name, SettingType.Choice)
        {
            Choices = choices;
            DefaultValue = defaultValue;
        }
    }

    public class FilePathSettingItem : SettingItem
    {
        public string DefaultValue { get; }
        public string ExtensionFilter { get; }
        public bool IsDirectory { get; }
        public FilePathSettingItem(string name, string defaultValue = "", string extensionFilter = "*") : base(name, SettingType.FilePath)
        {
            DefaultValue = defaultValue;
            ExtensionFilter = extensionFilter;
            IsDirectory = false;
        }
        public FilePathSettingItem(string name, string defaultValue, bool isDirectory) : base(name, SettingType.FilePath)
        {
            DefaultValue = defaultValue;
            ExtensionFilter = "*";
            IsDirectory = isDirectory;
        }
    }
}
