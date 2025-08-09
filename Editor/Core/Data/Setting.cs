using System;
using System.Collections.Generic;

using AMU.Editor.Core.Schema;

namespace AMU.Editor.Setting
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
                    }, "en_us"),
                new FilePathSettingItem(
                    "Core_dirPath",
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"),
                    true),
                new StringSettingItem("Core_versionInfo", "0.4.0", true),
                new StringSettingItem("Core_repositoryUrl", "https://github.com/4OF-fof/AvatarModifyUtilities", true),
            } },
        };
    }
}
