using AMU.Editor.Core.Controller;
using System.Collections.Generic;

namespace AMU.Editor.Core.Api
{
    public static class SettingAPI
    {
        public static T GetSetting<T>(string settingName) => SettingsController.GetSetting<T>(settingName);
    }
}
