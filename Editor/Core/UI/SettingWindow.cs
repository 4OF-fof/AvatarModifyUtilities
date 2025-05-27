using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using Untitled.Data;
using Untitled.Data.Setting;
using Untitled.Data.Lang;

public class SettingWindow : EditorWindow
{
    private const float MenuWidth = 240f;
    private const float MenuPadding = 8f;
    private const float MenuTopSpace = 12f;
    private const int TitleFontSize = 20;

    private string[] menuItems;
    private int selectedMenu = 0;
    private string menuSearch = "";
    private Dictionary<string, Untitled.Data.Setting.SettingItem[]> settingItems;

    [MenuItem("Untitled/Setting")]
    public static void ShowWindow()
    {
        string lang = EditorPrefs.GetString("Setting.language", "ja_jp");
        LocalizationManager.LoadLanguage(lang);
        var window = GetWindow<SettingWindow>(LocalizationManager.GetText("setting"));
        window.minSize = window.maxSize = new Vector2(960, 540);
        window.InitializeSettings();
    }

    private void InitializeSettings()
    {
        settingItems = SettingDataHelper.GetAllSettingItems();
        menuItems = settingItems.Keys.ToArray();
        selectedMenu = 0;
    }

    void OnEnable()
    {
        string lang = EditorPrefs.GetString("Setting.language", "ja_jp");
        LocalizationManager.LoadLanguage(lang);
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
            return Enumerable.Range(0, menuItems.Length).ToList();

        return menuItems
            .Select((menu, i) => new { menu, i })
            .Where(x => settingItems[x.menu].Any(item => LocalizationManager.GetText(item.Name).Contains(menuSearch)))
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
                buttonStyle.fontSize = 16;

                foreach (var i in filteredMenuIndices)
                {
                    bool isSelected = selectedMenu == i;
                    bool pressed = GUILayout.Toggle(isSelected, LocalizationManager.GetText(menuItems[i]), buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(36));
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
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = TitleFontSize };
            var filteredMenuIndices = GetFilteredMenuIndices();
            if (filteredMenuIndices.Count > 0)
            {
                GUILayout.Label($"{LocalizationManager.GetText(menuItems[selectedMenu])}", titleStyle);
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
                GUILayout.Label(LocalizationManager.GetText("empty_result"), titleStyle);
            }
        }
    }

    private void ResetMenuToDefault(string menu)
    {
        var items = settingItems[menu];
        foreach (var item in items)
        {
            string key = $"Setting.{item.Name}";
            switch (item.Type)
            {
                case Untitled.Data.Setting.SettingType.String:
                    EditorPrefs.SetString(key, ((StringSettingItem)item).DefaultValue);
                    break;
                case Untitled.Data.Setting.SettingType.Int:
                    EditorPrefs.SetInt(key, ((IntSettingItem)item).DefaultValue);
                    break;
                case Untitled.Data.Setting.SettingType.Bool:
                    EditorPrefs.SetBool(key, ((BoolSettingItem)item).DefaultValue);
                    break;
                case Untitled.Data.Setting.SettingType.Float:
                    EditorPrefs.SetFloat(key, ((FloatSettingItem)item).DefaultValue);
                    break;
            }
        }
    }

    private void DrawSettingItems(Untitled.Data.Setting.SettingItem[] items)
    {
        bool hasResult = false;
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(menuSearch) || LocalizationManager.GetText(item.Name).Contains(menuSearch))
            {
                string key = $"Setting.{item.Name}";
                switch (item.Type)
                {
                    case Untitled.Data.Setting.SettingType.String:
                        {
                            string value = EditorPrefs.GetString(key, ((StringSettingItem)item).DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            string newValue = EditorGUILayout.TextField(LocalizationManager.GetText(item.Name), value, GUILayout.Width(700));
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetString(key, newValue);
                            break;
                        }
                    case Untitled.Data.Setting.SettingType.Int:
                        {
                            var intItem = (IntSettingItem)item;
                            int value = EditorPrefs.GetInt(key, intItem.DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.BeginHorizontal();
                            int newValue = EditorGUILayout.IntField(LocalizationManager.GetText(item.Name), value, GUILayout.Width(300));
                            EditorGUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetInt(key, newValue);
                            break;
                        }
                    case Untitled.Data.Setting.SettingType.Bool:
                        {
                            bool value = EditorPrefs.GetBool(key, ((BoolSettingItem)item).DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            bool newValue = EditorGUILayout.Toggle(LocalizationManager.GetText(item.Name), value);
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetBool(key, newValue);
                            break;
                        }
                    case Untitled.Data.Setting.SettingType.Float:
                        {
                            var floatItem = (FloatSettingItem)item;
                            float value = EditorPrefs.GetFloat(key, floatItem.DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            float newValue = EditorGUILayout.Slider(LocalizationManager.GetText(item.Name), value, floatItem.MinValue, floatItem.MaxValue, GUILayout.Width(700));
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetFloat(key, newValue);
                            break;
                        }
                    case Untitled.Data.Setting.SettingType.Choice:
                        {
                            var choiceItem = (ChoiceSettingItem)item;
                            string value = EditorPrefs.GetString(key, choiceItem.DefaultValue);
                            var displayNames = choiceItem.Choices.Values.ToArray();
                            var values = choiceItem.Choices.Keys.ToArray();
                            int selectedIndex = System.Array.IndexOf(values, value);
                            if (selectedIndex < 0) selectedIndex = 0;
                            EditorGUI.BeginChangeCheck();
                            int newIndex = EditorGUILayout.Popup(LocalizationManager.GetText(item.Name), selectedIndex, displayNames, GUILayout.Width(700));
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetString(key, values[newIndex]);
                                if (key == "Setting.language")
                                {
                                    LocalizationManager.LoadLanguage(values[newIndex]);
                                    InitializeSettings();
                                    Repaint();
                                }
                            }
                            break;
                        }
                }
                hasResult = true;
            }
        }
        if (!hasResult)
            GUILayout.Label(LocalizationManager.GetText("empty_result"));
    }
}

public static class SettingDataHelper
{
    public static Dictionary<string, Untitled.Data.Setting.SettingItem[]> GetAllSettingItems()
    {
        var dictList = new List<Dictionary<string, Untitled.Data.Setting.SettingItem[]>>();
        var asm = typeof(SettingWindow).Assembly;
        var types = asm.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Namespace == "Untitled.Data.Setting");
        foreach (var type in types)
        {
            var field = type.GetField("SettingItems", BindingFlags.Public | BindingFlags.Static);
            if (field != null && field.FieldType == typeof(Dictionary<string, Untitled.Data.Setting.SettingItem[]>))
            {
                var dict = field.GetValue(null) as Dictionary<string, Untitled.Data.Setting.SettingItem[]>;
                if (dict != null) dictList.Add(dict);
            }
        }
        return dictList.SelectMany(d => d).ToDictionary(x => x.Key, x => x.Value);
    }
}
