using System.Collections.Generic;
using AMU.Data.Setting;

namespace AMU.Data.Setting
{
    public static class BoothPackageManagerSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "Booth Package Manager", new SettingItem[] {
                new BoolSettingItem("BPM_searchDownloadFolder", false)
            } },
        };
    }
}
