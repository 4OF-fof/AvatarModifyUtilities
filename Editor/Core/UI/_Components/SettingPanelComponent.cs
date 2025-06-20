using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AMU.Editor.Core.Schema;
using AMU.Editor.Core.Controller;

namespace AMU.Editor.Core.UI.Components
{
    public class SettingPanelComponent
    {
        private const float MenuWidth = 240f;
        private const float MenuPadding = 8f;
        private const float MenuTopSpace = 12f;
        private const int TitleFontSize = 20;

        private Dictionary<string, SettingItem[]> settingItems;

        public void Initialize(Dictionary<string, SettingItem[]> settingItems)
        {
            this.settingItems = settingItems;
        }

        public void Draw(Vector2 windowPosition, MenuComponent menuComponent)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(windowPosition.x - MenuWidth)))
            {
                GUILayout.Space(MenuTopSpace);
                var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = TitleFontSize };
                var filteredMenuIndices = menuComponent.GetFilteredMenuIndices();

                if (filteredMenuIndices.Count > 0)
                {
                    string selectedMenuKey = menuComponent.MenuItems[menuComponent.SelectedMenu];
                    GUILayout.Label($"{LocalizationController.GetText(selectedMenuKey)}", titleStyle);

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(MenuPadding);
                        using (new GUILayout.VerticalScope())
                        {
                            DrawSettingItems(settingItems[selectedMenuKey], menuComponent.GetMenuSearch());
                        }
                    }
                }
                else
                {
                    GUILayout.Label(LocalizationController.GetText("ui_empty_result"), titleStyle);
                }
            }
        }

        private void DrawSettingItems(SettingItem[] items, string menuSearch)
        {
            bool hasResult = false;
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 260f;

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(menuSearch) || LocalizationController.GetText(item.Name).Contains(menuSearch))
                {
                    // 言語変更の検出
                    bool wasLanguageItem = item.Name == "Core_language";
                    string previousLanguage = wasLanguageItem
                        ? SettingsController.GetSetting<string>("Core_language")
                        : null;

                    SettingItemRenderer.DrawSettingItem(item, menuSearch);

                    if (wasLanguageItem)
                    {
                        string currentLanguage = SettingsController.GetSetting<string>("Core_language");
                        if (previousLanguage != currentLanguage)
                        {
                            LocalizationController.LoadLanguage(currentLanguage);
                            OnLanguageChanged?.Invoke();
                        }
                    }

                    hasResult = true;
                }
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;

            if (!hasResult)
                GUILayout.Label(LocalizationController.GetText("ui_empty_result"));
        }

        public System.Action OnLanguageChanged { get; set; }
    }
}
