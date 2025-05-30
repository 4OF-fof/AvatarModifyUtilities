using System.Collections.Generic;
using System;
using AMU.Data.Lang;
using AMU.Data.Setting;

namespace AMU.Data.Setting
{
    public static class AutoVariantSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "AutoVariant", new SettingItem[] {
                new BoolSettingItem("AutoVariant_enableAutoVariant", true),
                new BoolSettingItem("AutoVariant_enablePrebuild", true),
                new BoolSettingItem("AutoVariant_includeAllAssets", true)
            } },
        };
    }
}
