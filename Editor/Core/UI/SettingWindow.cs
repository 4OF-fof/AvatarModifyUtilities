using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using AMU.Data;
using AMU.Data.Setting;
using AMU.Data.Lang;

public class SettingWindow : EditorWindow
{
    private const float MenuWidth = 240f;
    private const float MenuPadding = 8f;
    private const float MenuTopSpace = 12f;
    private const int TitleFontSize = 20;

    private string[] menuItems;
    private int selectedMenu = 0;
    private string menuSearch = "";
    private Dictionary<string, AMU.Data.Setting.SettingItem[]> settingItems;

    [MenuItem("AMU/Setting", priority = 100)]
    public static void ShowWindow()
    {
        string lang = EditorPrefs.GetString("Setting.Core_language", "en_us");
        LocalizationManager.LoadLanguage(lang);
        var window = GetWindow<SettingWindow>(LocalizationManager.GetText("Core_setting"));
        window.minSize = window.maxSize = new Vector2(960, 540);
        window.InitializeSettings();
    }

    private void InitializeSettings()
    {
        settingItems = SettingDataHelper.GetAllSettingItems();
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

        // EditorPrefsが設定されていない場合に初期値を設定
        InitializeDefaultValues();
    }

    void OnEnable()
    {
        string lang = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
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

    private void DrawSettingItems(AMU.Data.Setting.SettingItem[] items)
    {
        bool hasResult = false;
        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 260f;

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(menuSearch) || LocalizationManager.GetText(item.Name).Contains(menuSearch))
            {
                string key = $"Setting.{item.Name}";
                var labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.fontSize = (int)(EditorStyles.label.fontSize * 1.2f);
                switch (item.Type)
                {
                    case AMU.Data.Setting.SettingType.String:
                        {
                            var stringItem = (StringSettingItem)item;
                            string value = EditorPrefs.GetString(key, stringItem.DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(LocalizationManager.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                            if (stringItem.IsReadOnly)
                            {
                                EditorGUILayout.TextField(value, GUI.skin.textField, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                            }
                            else
                            {
                                EditorGUI.BeginChangeCheck();
                                string newValue = EditorGUILayout.TextField(value, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                                if (EditorGUI.EndChangeCheck())
                                    EditorPrefs.SetString(key, newValue);
                            }
                            GUILayout.EndHorizontal();
                            break;
                        }
                    case AMU.Data.Setting.SettingType.Int:
                        {
                            var intItem = (IntSettingItem)item;
                            int value = EditorPrefs.GetInt(key, intItem.DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(LocalizationManager.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                            int newValue = EditorGUILayout.IntField(value, GUILayout.Width(300));
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetInt(key, newValue);
                            break;
                        }
                    case AMU.Data.Setting.SettingType.Bool:
                        {
                            bool value = EditorPrefs.GetBool(key, ((BoolSettingItem)item).DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(LocalizationManager.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                            bool newValue = EditorGUILayout.Toggle(value);
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetBool(key, newValue);
                            break;
                        }
                    case AMU.Data.Setting.SettingType.Float:
                        {
                            var floatItem = (FloatSettingItem)item;
                            float value = EditorPrefs.GetFloat(key, floatItem.DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(LocalizationManager.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                            float newValue = EditorGUILayout.Slider(value, floatItem.MinValue, floatItem.MaxValue, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetFloat(key, newValue);
                            break;
                        }
                    case AMU.Data.Setting.SettingType.Choice:
                        {
                            var choiceItem = (ChoiceSettingItem)item;
                            string value = EditorPrefs.GetString(key, choiceItem.DefaultValue);
                            var displayNames = choiceItem.Choices.Values.ToArray();
                            var values = choiceItem.Choices.Keys.ToArray();
                            int selectedIndex = System.Array.IndexOf(values, value);
                            if (selectedIndex < 0) selectedIndex = 0;
                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(LocalizationManager.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                            int newIndex = EditorGUILayout.Popup(selectedIndex, displayNames, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                            GUILayout.EndHorizontal();
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorPrefs.SetString(key, values[newIndex]);
                                if (key == "Setting.Core_language")
                                {
                                    LocalizationManager.LoadLanguage(values[newIndex]);
                                    InitializeSettings();
                                    Repaint();
                                }
                            }
                            break;
                        }
                    case AMU.Data.Setting.SettingType.FilePath:
                        {
                            var fileItem = (FilePathSettingItem)item;
                            string value = EditorPrefs.GetString(key, fileItem.DefaultValue);
                            string extension = fileItem.ExtensionFilter;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(LocalizationManager.GetText(item.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
                            EditorGUI.BeginChangeCheck();
                            string newValue = EditorGUILayout.TextField(value, GUILayout.Width(700 - EditorGUIUtility.labelWidth - 48));
                            if (GUILayout.Button("...", GUILayout.Width(40)))
                            {
                                string path = fileItem.IsDirectory
                                    ? EditorUtility.OpenFolderPanel(LocalizationManager.GetText(item.Name), value, "")
                                    : EditorUtility.OpenFilePanel(LocalizationManager.GetText(item.Name), value, extension);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    newValue = path;
                                }
                            }
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetString(key, newValue); GUILayout.EndHorizontal();
                            break;
                        }
                    case AMU.Data.Setting.SettingType.TextArea:
                        {
                            var textAreaItem = (TextAreaSettingItem)item;
                            string value = EditorPrefs.GetString(key, textAreaItem.DefaultValue);
                            EditorGUI.BeginChangeCheck();
                            GUILayout.BeginVertical();
                            GUILayout.Label(LocalizationManager.GetText(item.Name), labelStyle);
                            if (textAreaItem.IsReadOnly)
                            {
                                EditorGUILayout.TextArea(value, GUI.skin.textArea, GUILayout.Width(700), GUILayout.MinHeight(textAreaItem.MinLines * 16), GUILayout.MaxHeight(textAreaItem.MaxLines * 16));
                            }
                            else
                            {
                                string newValue = EditorGUILayout.TextArea(value, GUILayout.Width(700), GUILayout.MinHeight(textAreaItem.MinLines * 16), GUILayout.MaxHeight(textAreaItem.MaxLines * 16));
                                if (EditorGUI.EndChangeCheck())
                                    EditorPrefs.SetString(key, newValue);
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
            GUILayout.Label(LocalizationManager.GetText("empty_result"));
    }

    /// <summary>
    /// EditorPrefsが設定されていない場合に初期値を設定します
    /// </summary>
    private void InitializeDefaultValues()
    {
        foreach (var category in settingItems)
        {
            foreach (var item in category.Value)
            {
                string key = $"Setting.{item.Name}";

                // EditorPrefsに値が既に存在するかチェック
                bool hasValue = false;
                switch (item.Type)
                {
                    case AMU.Data.Setting.SettingType.String:
                    case AMU.Data.Setting.SettingType.Choice:
                    case AMU.Data.Setting.SettingType.FilePath:
                    case AMU.Data.Setting.SettingType.TextArea:
                        hasValue = EditorPrefs.HasKey(key);
                        break;
                    case AMU.Data.Setting.SettingType.Int:
                        hasValue = EditorPrefs.HasKey(key);
                        break;
                    case AMU.Data.Setting.SettingType.Bool:
                        hasValue = EditorPrefs.HasKey(key);
                        break;
                    case AMU.Data.Setting.SettingType.Float:
                        hasValue = EditorPrefs.HasKey(key);
                        break;
                }

                // 値が存在しない場合のみ初期値を設定
                if (!hasValue)
                {
                    SetDefaultValue(item, key);
                }
            }
        }
    }

    /// <summary>
    /// 指定された設定項目の初期値をEditorPrefsに設定します
    /// </summary>
    private void SetDefaultValue(AMU.Data.Setting.SettingItem item, string key)
    {
        switch (item.Type)
        {
            case AMU.Data.Setting.SettingType.String:
                var stringItem = (StringSettingItem)item;
                EditorPrefs.SetString(key, stringItem.DefaultValue);
                break;
            case AMU.Data.Setting.SettingType.Int:
                var intItem = (IntSettingItem)item;
                EditorPrefs.SetInt(key, intItem.DefaultValue);
                break;
            case AMU.Data.Setting.SettingType.Bool:
                var boolItem = (BoolSettingItem)item;
                EditorPrefs.SetBool(key, boolItem.DefaultValue);
                break;
            case AMU.Data.Setting.SettingType.Float:
                var floatItem = (FloatSettingItem)item;
                EditorPrefs.SetFloat(key, floatItem.DefaultValue);
                break;
            case AMU.Data.Setting.SettingType.Choice:
                var choiceItem = (ChoiceSettingItem)item;
                EditorPrefs.SetString(key, choiceItem.DefaultValue);
                break;
            case AMU.Data.Setting.SettingType.FilePath:
                var fileItem = (FilePathSettingItem)item;
                EditorPrefs.SetString(key, fileItem.DefaultValue);
                break;
            case AMU.Data.Setting.SettingType.TextArea:
                var textAreaItem = (TextAreaSettingItem)item;
                EditorPrefs.SetString(key, textAreaItem.DefaultValue);
                break;
        }
    }
}

public static class SettingDataHelper
{
    public static Dictionary<string, AMU.Data.Setting.SettingItem[]> GetAllSettingItems()
    {
        var dictList = new List<Dictionary<string, AMU.Data.Setting.SettingItem[]>>();
        var asm = typeof(SettingWindow).Assembly;
        var types = asm.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Namespace == "AMU.Data.Setting");
        foreach (var type in types)
        {
            var field = type.GetField("SettingItems", BindingFlags.Public | BindingFlags.Static);
            if (field != null && field.FieldType == typeof(Dictionary<string, AMU.Data.Setting.SettingItem[]>))
            {
                var dict = field.GetValue(null) as Dictionary<string, AMU.Data.Setting.SettingItem[]>;
                if (dict != null) dictList.Add(dict);
            }
        }
        return dictList.SelectMany(d => d).ToDictionary(x => x.Key, x => x.Value);
    }
}
