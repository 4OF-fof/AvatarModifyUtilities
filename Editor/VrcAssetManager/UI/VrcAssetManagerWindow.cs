using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Services;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.UI.Components;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class VrcAssetManagerWindow : EditorWindow
    {
        [MenuItem("AMU/VRC Asset Manager", priority = 10000)]
        public static void ShowWindow()
        {
            string lang = SettingAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(lang);
            var window = GetWindow<VrcAssetManagerWindow>(LocalizationAPI.GetText("VrcAssetManager_title"));
            window.minSize = window.maxSize = new Vector2(1200, 800);
            window.maximized = false;
            window.Show();
        }

        private static DownloadFolderWatcherService _downloadWatcher;

        void OnEnable()
        {
            string lang = SettingAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(lang);
            AssetLibraryController.Instance.InitializeLibrary();
            if (_downloadWatcher == null)
            {
                _downloadWatcher = new DownloadFolderWatcherService();
                Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_vrcAssetManagerWindow_downloadWatcherStarted"));
            }
        }

        private void OnGUI()
        {
            SkinUtility.ApplySkin();

            ToolbarComponent.Draw();
            using (new EditorGUILayout.HorizontalScope())
            {
                AssetTypePanelComponent.Draw();
                MainGridComponent.Draw();
            }
        }

        private void OnDestroy()
        {
            ToolbarComponent.DestroyWindow();
            _downloadWatcher?.Dispose();
            _downloadWatcher = null;
        }
    }
}