using System.Collections.Generic;
using AMU.Editor.Core.Schema;

namespace AMU.Editor.Setting
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
