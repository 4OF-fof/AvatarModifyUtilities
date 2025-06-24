using System;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Setting;
using AMU.Editor.Core.Schema;

namespace AMU.Editor.Core.Controller
{
    public static class SettingsController
    {
        private static bool _isInitialized = false;

        public static void InitializeEditorPrefs()
        {
            if (_isInitialized) return;

            try
            {
                var allSettingItems = GetAllSettingItems();

                foreach (var category in allSettingItems)
                {
                    foreach (var item in category.Value)
                    {
                        string key = $"Setting.{item.Name}";

                        if (!EditorPrefs.HasKey(key))
                        {
                            SetDefaultValue(item, key);
                        }
                    }
                }
                Debug.Log(LocalizationController.GetText("message_success_settings_initialized"));
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("message_error_settings_failed"), ex.Message));
            }
        }

        public static T GetSetting<T>(string settingName)
        {
            if (!_isInitialized)
            {
                InitializeEditorPrefs();
            }

            string key = $"Setting.{settingName}";

            if (typeof(T) == typeof(string))
                return (T)(object)EditorPrefs.GetString(key, "");
            else if (typeof(T) == typeof(int))
                return (T)(object)EditorPrefs.GetInt(key, 0);
            else if (typeof(T) == typeof(bool))
                return (T)(object)EditorPrefs.GetBool(key, false);
            else if (typeof(T) == typeof(float))
                return (T)(object)EditorPrefs.GetFloat(key, 0f);

            return default(T);
        }

        public static void SetSetting<T>(string settingName, T value)
        {
            string key = $"Setting.{settingName}";

            if (typeof(T) == typeof(string))
                EditorPrefs.SetString(key, value?.ToString() ?? "");
            else if (typeof(T) == typeof(int))
                EditorPrefs.SetInt(key, value is int ? (int)(object)value : 0);
            else if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(key, value is bool ? (bool)(object)value : false);
            else if (typeof(T) == typeof(float))
                EditorPrefs.SetFloat(key, value is float ? (float)(object)value : 0f);
        }

        public static bool HasSetting(string settingName)
        {
            string key = $"Setting.{settingName}";
            return EditorPrefs.HasKey(key);
        }

        public static void DeleteSetting(string settingName)
        {
            string key = $"Setting.{settingName}";
            EditorPrefs.DeleteKey(key);
        }

        public static Dictionary<string, SettingItem[]> GetAllSettingItems()
        {
            var dictList = new List<Dictionary<string, SettingItem[]>>();
            var asm = typeof(SettingsController).Assembly;
            var types = asm.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Namespace == "AMU.Editor.Setting");

            foreach (var type in types)
            {
                var field = type.GetField("SettingItems", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && field.FieldType == typeof(Dictionary<string, SettingItem[]>))
                {
                    var dict = field.GetValue(null) as Dictionary<string, SettingItem[]>;
                    if (dict != null) dictList.Add(dict);
                }
            }

            return dictList.SelectMany(d => d).ToDictionary(x => x.Key, x => x.Value);
        }

        private static void SetDefaultValue(SettingItem item, string key)
        {
            switch (item.Type)
            {
                case SettingType.String:
                    var stringItem = (StringSettingItem)item;
                    EditorPrefs.SetString(key, stringItem.DefaultValue);
                    break;
                case SettingType.Int:
                    var intItem = (IntSettingItem)item;
                    EditorPrefs.SetInt(key, intItem.DefaultValue);
                    break;
                case SettingType.Bool:
                    var boolItem = (BoolSettingItem)item;
                    EditorPrefs.SetBool(key, boolItem.DefaultValue);
                    break;
                case SettingType.Float:
                    var floatItem = (FloatSettingItem)item;
                    EditorPrefs.SetFloat(key, floatItem.DefaultValue);
                    break;
                case SettingType.Choice:
                    var choiceItem = (ChoiceSettingItem)item;
                    EditorPrefs.SetString(key, choiceItem.DefaultValue);
                    break;
                case SettingType.FilePath:
                    var fileItem = (FilePathSettingItem)item;
                    EditorPrefs.SetString(key, fileItem.DefaultValue);
                    break;
                case SettingType.TextArea:
                    var textAreaItem = (TextAreaSettingItem)item;
                    EditorPrefs.SetString(key, textAreaItem.DefaultValue);
                    break;
            }
        }
    }
}
