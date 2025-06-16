using System;
using UnityEngine;
using UnityEditor;
using AMU.Data.Lang;

namespace AMU.AssetManager.UI
{
    public class GroupNameInputWindow : EditorWindow
    {
        private string _groupName = "";
        private Action<string> _onConfirm;

        public static void ShowWindow(Action<string> onConfirm)
        {
            var window = GetWindow<GroupNameInputWindow>(true, LocalizationManager.GetText("GroupNameInput_windowTitle"), true);
            window.minSize = new Vector2(300, 120);
            window.maxSize = new Vector2(300, 120);
            window._onConfirm = onConfirm;
            window._groupName = LocalizationManager.GetText("GroupNameInput_defaultName");
            window.ShowModal();
        }

        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label(LocalizationManager.GetText("GroupNameInput_enterGroupName"), EditorStyles.boldLabel);
            GUILayout.Space(5);

            GUI.SetNextControlName("GroupNameField");
            _groupName = EditorGUILayout.TextField(_groupName);

            GUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(LocalizationManager.GetText("GroupNameInput_create"), GUILayout.Height(30)))
                {
                    if (!string.IsNullOrWhiteSpace(_groupName))
                    {
                        _onConfirm?.Invoke(_groupName.Trim());
                        Close();
                    }
                }

                if (GUILayout.Button(LocalizationManager.GetText("GroupNameInput_cancel"), GUILayout.Height(30)))
                {
                    Close();
                }
            }

            // フォーカスをテキストフィールドに設定
            if (Event.current.type == EventType.Layout)
            {
                EditorGUI.FocusTextInControl("GroupNameField");
            }

            // Enterキーで確定
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (!string.IsNullOrWhiteSpace(_groupName))
                {
                    _onConfirm?.Invoke(_groupName.Trim());
                    Close();
                }
                Event.current.Use();
            }

            // Escapeキーでキャンセル
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
        }
    }
}
