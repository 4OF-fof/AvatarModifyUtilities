using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using AMU.Editor.Setting;
using AMU.Editor.Core.Schema;
using AMU.Editor.Core.Controller;
using AMU.Editor.Core.UI.Components;

namespace AMU.Editor.Core.UI
{
    /// <summary>
    /// 設定ウィンドウのUI管理クラス
    /// </summary>
    public class SettingWindow : EditorWindow
    {
        private Dictionary<string, AMU.Editor.Core.Schema.SettingItem[]> settingItems;
        private MenuComponent menuComponent;
        private SettingPanelComponent settingPanelComponent;

        [MenuItem("AMU/Setting", priority = 1000)]
        public static void ShowWindow()
        {
            string lang = SettingsController.GetSetting<string>("Core_language");
            LocalizationController.LoadLanguage(lang);
            var window = GetWindow<SettingWindow>(LocalizationController.GetText("Core_setting"));
            window.minSize = window.maxSize = new Vector2(960, 540);
            window.InitializeSettings();
        }

        private void InitializeSettings()
        {
            settingItems = SettingsController.GetAllSettingItems();

            // コンポーネントの初期化
            if (menuComponent == null)
                menuComponent = new MenuComponent();
            if (settingPanelComponent == null)
            {
                settingPanelComponent = new SettingPanelComponent();
                settingPanelComponent.OnLanguageChanged = () =>
                {
                    InitializeSettings();
                    Repaint();
                };
            }

            menuComponent.Initialize(settingItems);
            settingPanelComponent.Initialize(settingItems);

            // 設定の初期化はSettingsControllerに委譲
            SettingsController.InitializeEditorPrefs();
        }

        void OnEnable()
        {
            string lang = SettingsController.GetSetting<string>("Core_language");
            LocalizationController.LoadLanguage(lang);
            InitializeSettings();
        }

        void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                menuComponent?.Draw(position.size);
                settingPanelComponent?.Draw(position.size, menuComponent);
            }
        }
    }
}