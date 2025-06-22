using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Core.Api;
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
            string lang = SettingAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(lang);
            var window = GetWindow<VrcAssetManagerWindow>(LocalizationAPI.GetText("VrcAssetManager_title"));
            window.minSize = window.maxSize = new Vector2(1200, 800);
            window.Show();
        }

        void OnEnable()
        {
            string lang = SettingAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(lang);
            _controller.InitializeLibrary();
        }

        private void OnGUI()
        {
            ToolbarComponent.Draw(_controller);
            using (new EditorGUILayout.HorizontalScope())
            {
                AssetTypePanelComponent.Draw(_controller);
                MainGridComponent.Draw(_controller);
            }
        }

        private void OnDestroy()
        {
            ToolbarComponent.DestroyWindow();
        }
    }
}