using System.Collections.Generic;

using AMU.Editor.Core.Schema;

namespace AMU.Editor.Setting
{
    public static class VrcAssetManagerSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "VrcAssetManager_category", new SettingItem[] {
                new TextAreaSettingItem("AssetManager_excludedImportExtensions", ".zip\n.psd", false, 3, 8)
            } },
        };
    }
}
