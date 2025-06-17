using System.Collections.Generic;
using System;
using AMU.Data.Lang;
using AMU.Editor.Core.Schema;

namespace AMU.Editor.Setting
{
    public static class AutoVariantSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "AutoVariant", new SettingItem[] {
                new BoolSettingItem("AutoVariant_enableAutoVariant", false),
                new BoolSettingItem("AutoVariant_enablePrebuild", true),
                new BoolSettingItem("AutoVariant_includeAllAssets", true)
            } },
        };
    }
}
