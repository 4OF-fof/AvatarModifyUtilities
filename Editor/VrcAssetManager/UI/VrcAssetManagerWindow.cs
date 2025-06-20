using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class VrcAssetManagerWindow : EditorWindow
    {
        private AssetLibraryController _controller = new AssetLibraryController();
        private IReadOnlyList<AssetSchema> _assets;
        private IReadOnlyList<string> _tags;
        private IReadOnlyList<string> _assetTypes;

        [MenuItem("AMU/VRC Asset Manager", priority = 10000)]
        public static void ShowWindow()
        {
            string lang = SettingsAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(lang);
            var window = GetWindow<VrcAssetManagerWindow>(LocalizationAPI.GetText("VrcAssetManager_title"));
            window.minSize = window.maxSize = new Vector2(1200, 800);
            window.Show();
        }

        void OnEnable()
        {
            string lang = SettingsAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(lang);
            _controller.InitializeLibrary();
            _assets = _controller.GetAllAssets();
            _tags = _controller.GetAllTags();
            _assetTypes = _controller.GetAllAssetTypes();
        }

        private void OnGUI()
        {
            GUILayout.Label($"Total Assets: {_assets.Count}", EditorStyles.boldLabel);
            GUILayout.Label($"Total Tags: {_tags.Count}", EditorStyles.boldLabel);
            GUILayout.Label($"Total Asset Types: {_assetTypes.Count}", EditorStyles.boldLabel);
        }
    }
}