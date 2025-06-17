using UnityEditor;
using AMU.Data.Setting;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.Core.Controllers
{
    /// <summary>
    /// 設定データの永続化を管理するコントローラ
    /// </summary>
    public static class SettingsController
    {
        /// <summary>
        /// 全ての設定項目のEditorPrefsを初期化します
        /// </summary>
        public static void InitializeEditorPrefs()
        {
            try
            {
                // 全ての設定項目を取得
                var allSettingItems = GetAllSettingItems();

                foreach (var category in allSettingItems)
                {
                    foreach (var item in category.Value)
                    {
                        string key = $"Setting.{item.Name}";

                        // EditorPrefsに値が存在しない場合のみ初期値を設定
                        if (!EditorPrefs.HasKey(key))
                        {
                            SetDefaultValue(item, key);
                        }
                    }
                }

                Debug.Log("[SettingsController] EditorPrefs initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SettingsController] EditorPrefs initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定された設定項目の値をEditorPrefsから取得します
        /// </summary>
        /// <param name="settingName">設定名</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>設定値</returns>
        public static T GetSetting<T>(string settingName, T defaultValue = default(T))
        {
            string key = $"Setting.{settingName}";

            if (typeof(T) == typeof(string))
                return (T)(object)EditorPrefs.GetString(key, defaultValue?.ToString() ?? "");
            else if (typeof(T) == typeof(int))
                return (T)(object)EditorPrefs.GetInt(key, defaultValue is int ? (int)(object)defaultValue : 0);
            else if (typeof(T) == typeof(bool))
                return (T)(object)EditorPrefs.GetBool(key, defaultValue is bool ? (bool)(object)defaultValue : false);
            else if (typeof(T) == typeof(float))
                return (T)(object)EditorPrefs.GetFloat(key, defaultValue is float ? (float)(object)defaultValue : 0f);

            return defaultValue;
        }

        /// <summary>
        /// 指定された設定項目の値をEditorPrefsに保存します
        /// </summary>
        /// <param name="settingName">設定名</param>
        /// <param name="value">保存する値</param>
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

        /// <summary>
        /// 指定された設定項目がEditorPrefsに存在するかどうかを確認します
        /// </summary>
        /// <param name="settingName">設定名</param>
        /// <returns>存在する場合true</returns>
        public static bool HasSetting(string settingName)
        {
            string key = $"Setting.{settingName}";
            return EditorPrefs.HasKey(key);
        }

        /// <summary>
        /// 指定された設定項目をEditorPrefsから削除します
        /// </summary>
        /// <param name="settingName">設定名</param>
        public static void DeleteSetting(string settingName)
        {
            string key = $"Setting.{settingName}";
            EditorPrefs.DeleteKey(key);
        }

        /// <summary>
        /// 全ての設定項目を取得します
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, SettingItem[]> GetAllSettingItems()
        {
            var dictList = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, SettingItem[]>>();
            var asm = typeof(SettingsController).Assembly;
            var types = asm.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Namespace == "AMU.Data.Setting");

            foreach (var type in types)
            {
                var field = type.GetField("SettingItems", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && field.FieldType == typeof(System.Collections.Generic.Dictionary<string, SettingItem[]>))
                {
                    var dict = field.GetValue(null) as System.Collections.Generic.Dictionary<string, SettingItem[]>;
                    if (dict != null) dictList.Add(dict);
                }
            }

            return dictList.SelectMany(d => d).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// 指定された設定項目の初期値をEditorPrefsに設定します
        /// </summary>
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
