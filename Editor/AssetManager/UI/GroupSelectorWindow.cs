using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Data.Lang;

namespace AMU.AssetManager.UI
{
    public class GroupSelectorWindow : EditorWindow
    {
        private List<AssetInfo> _availableGroups;
        private AssetInfo _selectedGroup;
        private Vector2 _scrollPosition = Vector2.zero;
        private Action<AssetInfo> _onGroupSelected;
        private AssetDataManager _dataManager;

        public static void ShowWindow(AssetDataManager dataManager, Action<AssetInfo> onGroupSelected)
        {
            var window = GetWindow<GroupSelectorWindow>(true, LocalizationManager.GetText("GroupSelector_windowTitle"), true);
            window.minSize = new Vector2(400, 300);
            window.maxSize = new Vector2(400, 600);
            window._onGroupSelected = onGroupSelected;
            window._dataManager = dataManager;
            window.LoadAvailableGroups();
            window.ShowModal();
        }

        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);
        }

        private void LoadAvailableGroups()
        {
            if (_dataManager == null)
            {
                _availableGroups = new List<AssetInfo>();
                return;
            }

            // 利用可能なグループを取得
            _availableGroups = _dataManager.GetGroupAssets();
            _selectedGroup = _availableGroups.FirstOrDefault();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label(LocalizationManager.GetText("GroupSelector_selectGroup"), EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (_availableGroups == null || _availableGroups.Count == 0)
            {
                // グループがない場合
                GUILayout.Label(LocalizationManager.GetText("GroupSelector_noGroups"), EditorStyles.helpBox);
                GUILayout.FlexibleSpace();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(LocalizationManager.GetText("GroupSelector_cancel"), GUILayout.Height(30), GUILayout.Width(100)))
                    {
                        Close();
                    }
                }
                return;
            }

            // グループリスト表示
            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, false, false))
            {
                _scrollPosition = scrollView.scrollPosition;

                foreach (var group in _availableGroups)
                {
                    var isSelected = _selectedGroup == group;
                    var style = isSelected ? EditorStyles.helpBox : GUI.skin.button;

                    // グループの子アセット数を取得
                    var childCount = _dataManager.GetGroupChildren(group.uid).Count;
                    var displayText = $"{group.name} ({childCount}個のアセット)";

                    if (GUILayout.Toggle(isSelected, displayText, style, GUILayout.Height(40)))
                    {
                        if (!isSelected)
                        {
                            _selectedGroup = group;
                        }
                    }

                    GUILayout.Space(2);
                }
            }

            GUILayout.Space(10);

            // ボタン領域
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(LocalizationManager.GetText("GroupSelector_add"), GUILayout.Height(30)))
                {
                    if (_selectedGroup != null)
                    {
                        _onGroupSelected?.Invoke(_selectedGroup);
                        Close();
                    }
                }

                if (GUILayout.Button(LocalizationManager.GetText("GroupSelector_cancel"), GUILayout.Height(30)))
                {
                    Close();
                }
            }

            // Enterキーで確定
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (_selectedGroup != null)
                {
                    _onGroupSelected?.Invoke(_selectedGroup);
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
