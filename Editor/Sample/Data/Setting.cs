using System.Collections.Generic;
using Untitled.Data.Lang;
using Untitled.Data.Setting;

namespace Untitled.Data.Setting
{
    public static class SampleSettingData
    {
        public static readonly Dictionary<string, SettingItem[]> SettingItems = new Dictionary<string, SettingItem[]>
        {
            {
                "Sample_hoge", new SettingItem[] { new StringSettingItem("Sample_fuga", "foobar") }
            }
        };
    }
}
