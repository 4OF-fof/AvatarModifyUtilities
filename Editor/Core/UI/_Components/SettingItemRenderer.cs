using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.Editor.Setting;
using AMU.Editor.Core.Schema;
using AMU.Editor.Core.Controller;

namespace AMU.Editor.Core.UI.Components
{
    /// <summary>
    /// 各設定項目の描画を担当するレンダラー
    /// </summary>
    public static class SettingItemRenderer
    {
        private const float MenuPadding = 8f;

        public static void DrawSettingItem(SettingItem item, string menuSearch)
        {
            if (!string.IsNullOrEmpty(menuSearch) && !LocalizationController.GetText(item.Name).Contains(menuSearch))
                return;

            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontSize = (int)(EditorStyles.label.fontSize * 1.2f);

            switch (item.Type)
            {
                case SettingType.String:
                    DrawStringItem((StringSettingItem)item, labelStyle);
                    break;
                case SettingType.Int:
                    DrawIntItem((IntSettingItem)item, labelStyle);
                    break;
                case SettingType.Bool:
                    DrawBoolItem((BoolSettingItem)item, labelStyle);
                    break;
                case SettingType.Float:
                    DrawFloatItem((FloatSettingItem)item, labelStyle);
                    break;
                case SettingType.Choice:
                    DrawChoiceItem((ChoiceSettingItem)item, labelStyle);
                    break;
                case SettingType.FilePath:
                    DrawFilePathItem((FilePathSettingItem)item, labelStyle);
                    break;
                case SettingType.TextArea:
                    DrawTextAreaItem((TextAreaSettingItem)item, labelStyle);
                    break;
            }
        }

        private static void DrawStringItem(StringSettingItem stringItem, GUIStyle labelStyle)
        {
            string value = SettingsController.GetSetting<string>(stringItem.Name);
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationController.GetText(stringItem.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));

            if (stringItem.IsReadOnly)
            {
                EditorGUILayout.TextField(value, GUI.skin.textField, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUILayout.TextField(value, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
                if (EditorGUI.EndChangeCheck())
                    SettingsController.SetSetting(stringItem.Name, newValue);
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawIntItem(IntSettingItem intItem, GUIStyle labelStyle)
        {
            int value = SettingsController.GetSetting<int>(intItem.Name);
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationController.GetText(intItem.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
            int newValue = EditorGUILayout.IntField(value, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                SettingsController.SetSetting(intItem.Name, newValue);
        }

        private static void DrawBoolItem(BoolSettingItem boolItem, GUIStyle labelStyle)
        {
            bool value = SettingsController.GetSetting<bool>(boolItem.Name);
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationController.GetText(boolItem.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
            bool newValue = EditorGUILayout.Toggle(value);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                SettingsController.SetSetting(boolItem.Name, newValue);
        }

        private static void DrawFloatItem(FloatSettingItem floatItem, GUIStyle labelStyle)
        {
            float value = SettingsController.GetSetting<float>(floatItem.Name);
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationController.GetText(floatItem.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
            float newValue = EditorGUILayout.Slider(value, floatItem.MinValue, floatItem.MaxValue, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                SettingsController.SetSetting(floatItem.Name, newValue);
        }

        private static void DrawChoiceItem(ChoiceSettingItem choiceItem, GUIStyle labelStyle)
        {
            string value = SettingsController.GetSetting<string>(choiceItem.Name);
            var displayNames = choiceItem.Choices.Values.ToArray();
            var values = choiceItem.Choices.Keys.ToArray();
            int selectedIndex = System.Array.IndexOf(values, value);
            if (selectedIndex < 0) selectedIndex = 0;

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationController.GetText(choiceItem.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
            int newIndex = EditorGUILayout.Popup(selectedIndex, displayNames, GUILayout.Width(700 - EditorGUIUtility.labelWidth));
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                SettingsController.SetSetting(choiceItem.Name, values[newIndex]);
                if (choiceItem.Name == "Core_language")
                {
                    LocalizationController.LoadLanguage(values[newIndex]);
                    // 言語変更時の処理は呼び出し元で処理
                }
            }
        }

        private static void DrawFilePathItem(FilePathSettingItem fileItem, GUIStyle labelStyle)
        {
            string value = SettingsController.GetSetting<string>(fileItem.Name);
            string extension = fileItem.ExtensionFilter;

            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationController.GetText(fileItem.Name), labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUI.BeginChangeCheck();
            string newValue = EditorGUILayout.TextField(value, GUILayout.Width(700 - EditorGUIUtility.labelWidth - 48));

            if (GUILayout.Button("...", GUILayout.Width(40)))
            {
                string path = fileItem.IsDirectory
                    ? EditorUtility.OpenFolderPanel(LocalizationController.GetText(fileItem.Name), value, "")
                    : EditorUtility.OpenFilePanel(LocalizationController.GetText(fileItem.Name), value, extension);
                if (!string.IsNullOrEmpty(path))
                {
                    newValue = path;
                }
            }

            if (EditorGUI.EndChangeCheck())
                SettingsController.SetSetting(fileItem.Name, newValue);
            GUILayout.EndHorizontal();
        }

        private static void DrawTextAreaItem(TextAreaSettingItem textAreaItem, GUIStyle labelStyle)
        {
            string value = SettingsController.GetSetting<string>(textAreaItem.Name);
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginVertical();
            GUILayout.Label(LocalizationController.GetText(textAreaItem.Name), labelStyle);

            if (textAreaItem.IsReadOnly)
            {
                EditorGUILayout.TextArea(value, GUI.skin.textArea,
                    GUILayout.Width(700),
                    GUILayout.MinHeight(textAreaItem.MinLines * 16),
                    GUILayout.MaxHeight(textAreaItem.MaxLines * 16));
            }
            else
            {
                string newValue = EditorGUILayout.TextArea(value,
                    GUILayout.Width(700),
                    GUILayout.MinHeight(textAreaItem.MinLines * 16),
                    GUILayout.MaxHeight(textAreaItem.MaxLines * 16));
                if (EditorGUI.EndChangeCheck())
                    SettingsController.SetSetting(textAreaItem.Name, newValue);
            }
            GUILayout.EndVertical();
        }
    }
}
