using AMU.Editor.Core.Controller;

namespace AMU.Editor.Core.Api
{
    public static class LocalizationAPI
    {
        public static string GetText(string key) => LocalizationController.GetText(key);
        public static string CurrentLanguage => LocalizationController.CurrentLanguage;
    }
}
