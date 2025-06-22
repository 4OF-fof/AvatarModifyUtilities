using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class TagSelectorWindow : EditorWindow
    {
        private bool _allowMultipleSelection;
        private Action<List<string>> _onTagsSelected;

        private List<string> _availableTags;
        private List<string> _filteredTags;
        private List<string> _selectedTags = new List<string>();

        private Vector2 _scrollPosition = Vector2.zero;
        private string _searchText = "";

        public static void ShowWindow(bool allowMultipleSelection, Action<List<string>> onTagsSelected, List<string> initialSelectedTags = null)
        {
            var window = GetWindow<TagSelectorWindow>("Tag Selector");
            window.minSize = window.maxSize = new Vector2(300, 400);
            window._allowMultipleSelection = allowMultipleSelection;
            window._onTagsSelected = onTagsSelected;

            try
            {
                var controller = new AssetLibraryController();
                controller.LoadAssetLibrary();
                window._availableTags = controller.GetAllTags().ToList();
                window._availableTags.Sort();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TagSelectorWindow] Failed to load tags: {ex.Message}");
                window._availableTags = new List<string>();
            }

            window._selectedTags.Clear();

            if (initialSelectedTags != null && initialSelectedTags.Count > 0)
            {
                foreach (var tag in initialSelectedTags)
                {
                    if (window._availableTags.Contains(tag))
                    {
                        if (window._allowMultipleSelection)
                        {
                            window._selectedTags.Add(tag);
                        }
                        else
                        {
                            window._selectedTags.Add(tag);
                            break;
                        }
                    }
                }
            }
            window.FilterTags();
            window.Show();
        }

        private void OnEnable()
        {
            var language = SettingsAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(language);
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                GUILayout.Space(10);

                string headerText = _allowMultipleSelection
                    ? LocalizationAPI.GetText("TagSelector_selectMultipleTags")
                    : LocalizationAPI.GetText("TagSelector_selectSingleTag");

                GUILayout.Label(headerText, EditorStyles.boldLabel);
                GUILayout.Space(5);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("TagSelector_search"), GUILayout.Width(50));

                    GUI.SetNextControlName("SearchField");
                    var newSearchText = GUILayout.TextField(_searchText);

                    if (newSearchText != _searchText)
                    {
                        _searchText = newSearchText;
                        FilterTags();
                    }

                    GUILayout.Space(25);
                }

                GUILayout.Space(10);

                if (_filteredTags == null || _filteredTags.Count == 0)
                {
                    var message = string.IsNullOrEmpty(_searchText)
                        ? LocalizationAPI.GetText("TagSelector_noTags")
                        : LocalizationAPI.GetText("TagSelector_noSearchResults");

                    GUILayout.Label(message, EditorStyles.helpBox);
                    return;
                }

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                    {
                        _scrollPosition = scrollView.scrollPosition;
                        using (new GUILayout.VerticalScope())
                        {
                            foreach (var tag in _filteredTags)
                            {
                                bool isSelected = _selectedTags.Contains(tag);

                                if (isSelected)
                                {
                                    var originalColor = GUI.backgroundColor;
                                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 1f);

                                    if (GUILayout.Button(tag, GUI.skin.button, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                                    {
                                        if (_allowMultipleSelection)
                                        {
                                            if (_selectedTags.Contains(tag))
                                            {
                                                _selectedTags.Remove(tag);
                                            }
                                            else
                                            {
                                                _selectedTags.Add(tag);
                                            }
                                        }
                                        else
                                        {
                                            _selectedTags.Clear();
                                            _selectedTags.Add(tag);

                                            CompleteSelection();
                                        }
                                    }

                                    GUI.backgroundColor = originalColor;
                                }
                                else
                                {
                                    if (GUILayout.Button(tag, GUI.skin.button, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                                    {
                                        if (_allowMultipleSelection)
                                        {
                                            if (_selectedTags.Contains(tag))
                                            {
                                                _selectedTags.Remove(tag);
                                            }
                                            else
                                            {
                                                _selectedTags.Add(tag);
                                            }
                                        }
                                        else
                                        {
                                            _selectedTags.Clear();
                                            _selectedTags.Add(tag);

                                            CompleteSelection();
                                        }
                                    }
                                }
                                GUILayout.Space(2);
                            }
                        }
                    }
                }

                GUILayout.Space(10);

                if (_allowMultipleSelection)
                {
                    string selectionInfo = _allowMultipleSelection
                    ? $"{LocalizationAPI.GetText("TagSelector_selectedCount")}: {_selectedTags.Count}"
                    : _selectedTags.Count > 0
                        ? $"{LocalizationAPI.GetText("TagSelector_selected")}: {_selectedTags.First()}"
                        : LocalizationAPI.GetText("TagSelector_noSelection");

                    GUILayout.Label(selectionInfo, EditorStyles.miniLabel);

                    GUILayout.Space(5);
                }

                if (_allowMultipleSelection)
                {
                    if (GUILayout.Button(LocalizationAPI.GetText("TagSelector_clearAll")))
                    {
                        _selectedTags.Clear();
                    }
                    GUILayout.Space(3);
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(LocalizationAPI.GetText("Common_cancel")))
                    {
                        Close();
                    }

                    if (GUILayout.Button(LocalizationAPI.GetText("Common_ok")))
                    {
                        CompleteSelection();
                    }
                }

                GUILayout.Space(10);
            }
        }

        private void CompleteSelection()
        {
            var selectedTagsList = _selectedTags.ToList();
            _onTagsSelected?.Invoke(selectedTagsList);
            Close();
        }

        private void OnDestroy()
        {
            _onTagsSelected = null;
        }

        private void FilterTags()
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredTags = new List<string>(_availableTags);
            }
            else
            {
                _filteredTags = _availableTags
                    .Where(tag => tag.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
        }
    }
}
