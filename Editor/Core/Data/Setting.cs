using System.Collections.Generic;
using Untitled.Data.Lang;
using Untitled.Data.Setting;

namespace Untitled.Data.Setting
{
    public static class SettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "Core_general", new SettingItem[] {
                new ChoiceSettingItem("Core_language",
                    new Dictionary<string, string>
                    {
                        { "ja_jp", "日本語" },
                        { "en_us", "English" },
                    }, "ja_jp"),
                    new FilePathSettingItem("Core_savePath", "", "cs", true),
            } },
        };
    }
}
