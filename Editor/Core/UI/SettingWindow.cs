using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using AMU.Editor.Setting;
using AMU.Editor.Core.Schema;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.Core.UI
{
    /// <summary>
    /// 設定ウィンドウのUI管理クラス
    /// </summary>
    public class SettingWindow : EditorWindow
    {
        private const float MenuWidth = 240f;
        private const float MenuPadding = 8f;
        private const float MenuTopSpace = 12f;
        private const int TitleFontSize = 20;

        private string[] menuItems;
        private int selectedMenu = 0;
        private string menuSearch = "";
        private Dictionary<string, AMU.Editor.Core.Schema.SettingItem[]> settingItems;

        [MenuItem("AMU/Setting", priority = 1000)]
        public static void ShowWindow()
        {
            string lang = SettingsController.GetSetting("Core_language", "en_us");
            LocalizationController.LoadLanguage(lang);
            var window = GetWindow<SettingWindow>(LocalizationController.GetText("Core_setting"));
            window.minSize = window.maxSize = new Vector2(960, 540);
            window.InitializeSettings();
        }
        private void InitializeSettings()
        {
            settingItems = SettingsController.GetAllSettingItems();
            var keys = settingItems.Keys.ToList();
            if (keys.Contains("Core_general"))
            {
                keys.Remove("Core_general");
                menuItems = (new[] { "Core_general" }).Concat(keys).ToArray();
            }
            else
            {
                menuItems = keys.ToArray();
            }
            selectedMenu = 0;

            // 設定の初期化はSettingsControllerに委譲
            SettingsController.InitializeEditorPrefs();
        }
        void OnEnable()
        {
            string lang = SettingsController.GetSetting("Core_language", "en_us");
            LocalizationController.LoadLanguage(lang);
            InitializeSettings();
        }

        void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                DrawMenu();
                DrawSettingPanel();
            }
        }

        private void DrawMenu()
        {
            DrawMenuBackground();
            using (new GUILayout.VerticalScope(GUILayout.Width(MenuWidth)))
            {
                GUILayout.Space(MenuTopSpace);
                GUILayout.Space(MenuPadding);
                DrawMenuSearchBar();
                GUILayout.Space(MenuPadding);
                var filteredMenuIndices = GetFilteredMenuIndices();
                UpdateSelectedMenu(filteredMenuIndices);
                DrawMenuItems(filteredMenuIndices);
            }
        }

        private void DrawMenuBackground()
        {
            var menuRect = new Rect(0, 0, MenuWidth, position.height);
            var menuBgColor = new Color(0.19f, 0.19f, 0.19f, 1f);
            var borderColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            EditorGUI.DrawRect(menuRect, borderColor);
            EditorGUI.DrawRect(new Rect(0, 0, MenuWidth, position.height), menuBgColor);
        }

        private void DrawMenuSearchBar()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(MenuPadding);
                menuSearch = GUILayout.TextField(menuSearch, GUI.skin.FindStyle("SearchTextField"), GUILayout.Width(MenuWidth - MenuPadding * 2));
                GUILayout.Space(MenuPadding);
            }
        }

        private List<int> GetFilteredMenuIndices()
        {
            if (string.IsNullOrEmpty(menuSearch))
                return Enumerable.Range(0, menuItems.Length).ToList(); return menuItems
         .Select((menu, i) => new { menu, i })
         .Where(x => settingItems[x.menu].Any(item => LocalizationController.GetText(item.Name).Contains(menuSearch)))
         .Select(x => x.i)
         .ToList();
        }

        private void UpdateSelectedMenu(List<int> filteredMenuIndices)
        {
            if (!filteredMenuIndices.Contains(selectedMenu) && filteredMenuIndices.Count > 0)
                selectedMenu = filteredMenuIndices[0];
        }

        private void DrawMenuItems(List<int> filteredMenuIndices)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(MenuPadding);
                using (new GUILayout.VerticalScope())
                {
                    var buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.fontSize = 16; foreach (var i in filteredMenuIndices)
                    {
                        bool isSelected = selectedMenu == i;
                        bool pressed = GUILayout.Toggle(isSelected, LocalizationController.GetText(menuItems[i]), buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(36));
                        if (pressed && !isSelected)
                        {
                            selectedMenu = i;
                            GUI.FocusControl(null);
                        }
                    }
                }
                GUILayout.Space(MenuPadding);
            }
        }

        private void DrawSettingPanel()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(position.width - MenuWidth)))
            {
                GUILayout.Space(MenuTopSpace);
                var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = TitleFontSize }; var filteredMenuIndices = GetFilteredMenuIndices();
                if (filteredMenuIndices.Count > 0)
                {
                    GUILayout.Label($"{LocalizationController.GetText(menuItems[selectedMenu])}", titleStyle);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(MenuPadding);
                        using (new GUILayout.VerticalScope())
                        {
                            DrawSettingItems(settingItems[menuItems[selectedMenu]]);
                        }
                    }
                }
                else
                {
                    GUILayout.Label(LocalizationController.GetText("empty_result"), titleStyle);
                }
            }
        }

        private void DrawSettingItems(AMU.Editor.Core.Schema.SettingItem[] items)
        {
            bool hasResult = false;
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 260f; foreach (var item in items)
            {
                if (string.IsNullOrEmpty(menuSearch) || LocalizationController.GetText(item.Name).Contains(menuSearch))
                {
                    var labelStyle = new GUIStyle(EditorStyles.label);
                    labelStyle.fontSize = (int)(EditorStyles.label.fontSize * 1.2f);
                    switch (item.Type)
                    {
                        case AMU.Editor.Core.Schema.SettingType.String:
                            {
                                var stringItem = (StringSettingItem)item;
                                string value = SettingsController.GetSetting(item.Name, stringItem.DefaultValue);
                                EditorGUI.BeginChangeCheck();
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(LocalizationController.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                                if (stringItem.IsReadOnly)
                                {
                                    EditorGUILayout.TextField(value, GUI.skin.textField, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                                }
                                else
                                {
                                    EditorGUI.BeginChangeCheck();
                                    string newValue = EditorGUILayout.TextField(value, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                                    if (EditorGUI.EndChangeCheck())
                                        SettingsController.SetSetting(item.Name, newValue);
                                }
                                GUILayout.EndHorizontal();
                                break;
                            }
                        case AMU.Editor.Core.Schema.SettingType.Int:
                            {
                                var intItem = (IntSettingItem)item;
                                int value = SettingsController.GetSetting(item.Name, intItem.DefaultValue);
                                EditorGUI.BeginChangeCheck();
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(LocalizationController.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                                int newValue = EditorGUILayout.IntField(value, GUILayout.Width(300));
                                GUILayout.EndHorizontal();
                                if (EditorGUI.EndChangeCheck())
                                    SettingsController.SetSetting(item.Name, newValue);
                                break;
                            }
                        case AMU.Editor.Core.Schema.SettingType.Bool:
                            {
                                bool value = SettingsController.GetSetting(item.Name, ((BoolSettingItem)item).DefaultValue);
                                EditorGUI.BeginChangeCheck();
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(LocalizationController.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                                bool newValue = EditorGUILayout.Toggle(value);
                                GUILayout.EndHorizontal();
                                if (EditorGUI.EndChangeCheck())
                                    SettingsController.SetSetting(item.Name, newValue);
                                break;
                            }
                        case AMU.Editor.Core.Schema.SettingType.Float:
                            {
                                var floatItem = (FloatSettingItem)item;
                                float value = SettingsController.GetSetting(item.Name, floatItem.DefaultValue);
                                EditorGUI.BeginChangeCheck();
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(LocalizationController.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                                float newValue = EditorGUILayout.Slider(value, floatItem.MinValue, floatItem.MaxValue, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                                GUILayout.EndHorizontal();
                                if (EditorGUI.EndChangeCheck())
                                    SettingsController.SetSetting(item.Name, newValue);
                                break;
                            }
                        case AMU.Editor.Core.Schema.SettingType.Choice:
                            {
                                var choiceItem = (ChoiceSettingItem)item;
                                string value = SettingsController.GetSetting(item.Name, choiceItem.DefaultValue);
                                var displayNames = choiceItem.Choices.Values.ToArray();
                                var values = choiceItem.Choices.Keys.ToArray();
                                int selectedIndex = System.Array.IndexOf(values, value);
                                if (selectedIndex < 0) selectedIndex = 0;
                                EditorGUI.BeginChangeCheck();
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(LocalizationController.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                                int newIndex = EditorGUILayout.Popup(selectedIndex, displayNames, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                                GUILayout.EndHorizontal();
                                if (EditorGUI.EndChangeCheck())
                                {
                                    SettingsController.SetSetting(item.Name, values[newIndex]);
                                    if (item.Name == "Core_language")
                                    {
                                        LocalizationController.LoadLanguage(values[newIndex]);
                                        InitializeSettings();
                                        Repaint();
                                    }
                                }
                                break;
                            }
                        case AMU.Editor.Core.Schema.SettingType.FilePath:
                            {
                                var fileItem = (FilePathSettingItem)item;
                                string value = SettingsController.GetSetting(item.Name, fileItem.DefaultValue);
                                string extension = fileItem.ExtensionFilter;
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(LocalizationController.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                                EditorGUI.BeginChangeCheck();
                                string newValue = EditorGUILayout.TextField(value, GUILayout.Width(700 - EditorGUIUtility.labelWidth - 48));
                                if (GUILayout.Button("...", GUILayout.Width(40)))
                                {
                                    string path = fileItem.IsDirectory
                                        ? EditorUtility.OpenFolderPanel(LocalizationController.GetText(item.Name), value, "")
                                        : EditorUtility.OpenFilePanel(LocalizationController.GetText(item.Name), value, extension);
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        newValue = path;
                                    }
                                }
                                if (EditorGUI.EndChangeCheck())
                                    SettingsController.SetSetting(item.Name, newValue);
                                GUILayout.EndHorizontal();
                                break;
                            }
                        case AMU.Editor.Core.Schema.SettingType.TextArea:
                            {
                                var textAreaItem = (TextAreaSettingItem)item;
                                string value = SettingsController.GetSetting(item.Name, textAreaItem.DefaultValue);
                                EditorGUI.BeginChangeCheck();
                                GUILayout.BeginVertical();
                                GUILayout.Label(LocalizationController.GetText(item.Name), labelStyle);
                                if (textAreaItem.IsReadOnly)
                                {
                                    EditorGUILayout.TextArea(value, GUI.skin.textArea, GUILayout.Width(700), GUILayout.MinHeight(textAreaItem.MinLines * 16), GUILayout.MaxHeight(textAreaItem.MaxLines * 16));
                                }
                                else
                                {
                                    string newValue = EditorGUILayout.TextArea(value, GUILayout.Width(700), GUILayout.MinHeight(textAreaItem.MinLines * 16), GUILayout.MaxHeight(textAreaItem.MaxLines * 16));
                                    if (EditorGUI.EndChangeCheck())
                                        SettingsController.SetSetting(item.Name, newValue);
                                }
                                GUILayout.EndVertical();
                                break;
                            }
                    }
                    hasResult = true;
                }
            }
            EditorGUIUtility.labelWidth = prevLabelWidth;
            if (!hasResult)
                GUILayout.Label(LocalizationController.GetText("empty_result"));
        }
    }
}
