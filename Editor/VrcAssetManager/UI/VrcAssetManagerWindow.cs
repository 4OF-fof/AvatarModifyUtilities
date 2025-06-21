using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.UI.Components;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class VrcAssetManagerWindow : EditorWindow
    {
        private AssetLibraryController _controller = new AssetLibraryController();

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
        }

        private void OnGUI()
        {
            var _assets = _controller.GetAllAssets();
            var _tags = _controller.GetAllTags();
            var _assetTypes = _controller.GetAllAssetTypes();
            GUILayout.Label($"Total Assets: {_assets.Count}", EditorStyles.boldLabel);
            GUILayout.Label($"Total Tags: {_tags.Count}", EditorStyles.boldLabel);
            GUILayout.Label($"Total Asset Types: {_assetTypes.Count}", EditorStyles.boldLabel);
            foreach (var asset in _assets)
            {
                GUILayout.Label($"Asset ID: {asset.AssetId}", EditorStyles.label);
                GUILayout.Label($"Name: {asset.Metadata.Name}", EditorStyles.label);
                GUILayout.Space(10);
            }
            GUILayout.Space(10);
            ToolbarComponent toolbar = new ToolbarComponent();
            toolbar.Draw(_controller);
        }

    }
}