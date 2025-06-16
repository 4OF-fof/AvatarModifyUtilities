using System.Collections.Generic;
using AMU.Data.Setting;

namespace AMU.Data.Setting
{
    public static class AssetManagerSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "Asset Manager", new SettingItem[] {
                new BoolSettingItem("AssetManager_watchDownloadFolder", false),
                new TextAreaSettingItem("AssetManager_excludedImportExtensions", ".zip\n.psd", false, 3, 8)
            } },
        };
    }
}
