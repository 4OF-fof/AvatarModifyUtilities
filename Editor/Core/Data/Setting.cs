using System.Collections.Generic;
using System;
using AMU.Data.Lang;
using AMU.Data.Setting;

namespace AMU.Data.Setting
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
                new FilePathSettingItem(
                    "Core_dirPath",
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"),
                    true),
                new StringSettingItem("Core_versionInfo", "0.1.0-alpha", true),
                new StringSettingItem("Core_repositoryUrl", "https://github.com/4OF-fof/AvatarModifyUtilities", true),
            } },
        };
    }
}
