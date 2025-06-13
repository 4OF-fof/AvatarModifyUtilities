using UnityEngine;
using UnityEditor;

namespace AMU.Editor.AutoVariant.Watcher
{
    public static class PrebuildSettings
    {
        public static bool IsOptimizationEnabled =>
            EditorPrefs.GetBool("Setting.AutoVariant_enablePrebuild", true);

        public static bool IncludeAllAssets =>
            EditorPrefs.GetBool("Setting.AutoVariant_includeAllAssets", true);

        public static string CurrentLanguage =>
            EditorPrefs.GetString("Setting.Core_language", "en_us");

        public static string BaseDirectoryPath =>
            EditorPrefs.GetString("Setting.Core_dirPath",
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
    }
}
