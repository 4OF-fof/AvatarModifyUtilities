using UnityEngine;
using UnityEditor;
using AMU.Data.Lang;

namespace AMU.VrcAssetKeeper.UI
{
    public class VrcAssetKeeperWindow : EditorWindow
    {
        [MenuItem("AMU/VRC Asset Keeper", priority = 0)]
        public static void ShowWindow()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            var window = GetWindow<VrcAssetKeeperWindow>("VRC Asset Keeper");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        void OnGUI()
        {
        }
    }
}
