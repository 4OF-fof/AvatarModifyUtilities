using System.Collections.Generic;
using System;
using Untitled.Data.Lang;
using Untitled.Data.Setting;

namespace Untitled.Data.Setting
{
    public static class AutoVariantSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            { "AutoVariant", new SettingItem[] {
                new BoolSettingItem("AutoVariant_enableAutoVariant", true),
                new BoolSettingItem("AutoVariant_enablePrebuild", true)
            } },
        };
    }
}
