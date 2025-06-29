using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class TagSelectorWindow : EditorWindow
    {
        private bool _allowMultipleSelection;
        private bool _allowTagCreation;
        private Action<List<string>> _onTagsSelected;
        private List<string> _availableTags;
        private List<string> _filteredTags;
        private List<string> _selectedTags = new List<string>();
        private Vector2 _scrollPosition = Vector2.zero;
        private string _searchText = "";
        private string _newTagInput = "";

        public static void ShowWindow(Action<List<string>> onTagsSelected, List<string> initialSelectedTags = null, bool allowMultipleSelection = false, bool allowTagCreation = false)
        {
            var window = GetWindow<TagSelectorWindow>(LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_title"));
            window.minSize = window.maxSize = new Vector2(300, 400);
            window._allowMultipleSelection = allowMultipleSelection;
            window._onTagsSelected = onTagsSelected;
            window._allowTagCreation = allowTagCreation;

            try
            {
                AssetLibraryController.Instance.LoadAssetLibrary();
                window._availableTags = AssetLibraryController.Instance.GetAllTags().ToList();
                window._availableTags.Sort();
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_tagSelector_loadTagsFailed"), ex.Message));
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
            var language = SettingAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(language);
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                GUILayout.Space(10);

                string headerText = _allowMultipleSelection
                    ? LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_selectMultipleTags")
                    : LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_selectSingleTag");

                GUILayout.Label(headerText, EditorStyles.boldLabel);
                GUILayout.Space(5);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_search"), GUILayout.Width(50));
                    GUI.SetNextControlName("SearchField");
                    var newSearchText = GUILayout.TextField(_searchText);
                    if (newSearchText != _searchText)
                    {
                        _searchText = newSearchText;
                        FilterTags();
                    }
                    GUILayout.Space(25);
                }

                if (_allowTagCreation)
                {
                    GUILayout.Space(5);
                    using (new GUILayout.HorizontalScope())
                    {
                        _newTagInput = GUILayout.TextField(_newTagInput, GUILayout.ExpandWidth(true));
                        GUI.enabled = !string.IsNullOrWhiteSpace(_newTagInput) && !_availableTags.Contains(_newTagInput.Trim());
                        var plusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
                        if (GUILayout.Button(plusIcon, GUILayout.Width(30), GUILayout.Height(18)))
                        {
                            var tagName = _newTagInput.Trim();
                            if (!string.IsNullOrEmpty(tagName) && !_availableTags.Contains(tagName))
                            {
                                AssetLibraryController.Instance.AddTag(tagName);
                                _availableTags.Add(tagName);
                                _availableTags.Sort();
                                if (!_allowMultipleSelection)
                                {
                                    _selectedTags.Clear();
                                }
                                _selectedTags.Add(tagName);
                                FilterTags();
                            }
                            else
                            {
                                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_common_error"), LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_tagAlreadyExists"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                            }
                        }
                        GUI.enabled = true;
                    }
                }

                GUILayout.Space(10);

                if (_filteredTags == null || _filteredTags.Count == 0)
                {
                    var message = string.IsNullOrEmpty(_searchText)
                        ? LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_noTags")
                        : LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_noSearchResults");

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
                    ? $"{LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_selectedCount")}: {_selectedTags.Count}"
                    : _selectedTags.Count > 0
                        ? $"{LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_selected")}: {_selectedTags.First()}"
                        : LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_noSelection");

                    GUILayout.Label(selectionInfo, EditorStyles.miniLabel);

                    GUILayout.Space(5);
                }

                if (_allowMultipleSelection)
                {
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_tagSelector_clearAll")))
                    {
                        _selectedTags.Clear();
                    }
                    GUILayout.Space(3);
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_common_cancel")))
                    {
                        Close();
                    }

                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_common_ok")))
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
    
    public class AssetSelectorWindow : EditorWindow
    {
        private bool _allowMultipleSelection;
        private int _filterMode = 0;
        private Action<List<string>> _onAssetsSelected;
        private List<AssetSchema> _availableAssets;
        private List<AssetSchema> _filteredAssets;
        private List<string> _selectedAssets = new List<string>();
        private Vector2 _scrollPosition = Vector2.zero;
        private string _searchText = "";

        // filterMode, 0: All, 1: noChild + noParent, 2: noChild
        public static void ShowWindow(Action<List<string>> onAssetsSelected, List<string> initialSelectedAssets = null, bool allowMultipleSelection = false, int filterMode = 0)
        {
            var window = GetWindow<AssetSelectorWindow>(LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_title"));
            window.minSize = window.maxSize = new Vector2(400, 500);
            window._allowMultipleSelection = allowMultipleSelection;
            window._onAssetsSelected = onAssetsSelected;
            window._filterMode = filterMode;

            try
            {
                AssetLibraryController.Instance.OptimizeAssetLibrary();
                AssetLibraryController.Instance.LoadAssetLibrary();
                window._availableAssets = AssetLibraryController.Instance.GetAllAssets().ToList();
                window._availableAssets.Sort((a, b) => string.Compare(a.metadata.name, b.metadata.name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_assetSelector_loadAssetsFailed"), ex.Message));
                window._availableAssets = new List<AssetSchema>();
            }

            window._selectedAssets.Clear();
            if (initialSelectedAssets != null && initialSelectedAssets.Count > 0)
            {
                foreach (var assetId in initialSelectedAssets)
                {
                    if (Guid.TryParse(assetId, out var guid) && window._availableAssets.Any(a => a.assetId == guid))
                    {
                        if (window._allowMultipleSelection)
                        {
                            window._selectedAssets.Add(assetId);
                        }
                        else
                        {
                            window._selectedAssets.Add(assetId);
                            break;
                        }
                    }
                }
            }
            window.FilterAssets();
            window.Show();
        }

        private void OnEnable()
        {
            var language = SettingAPI.GetSetting<string>("Core_language");
            LocalizationAPI.LoadLanguage(language);
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                GUILayout.Space(10);

                string headerText = _allowMultipleSelection
                    ? LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_selectMultipleAssets")
                    : LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_selectSingleAsset");

                GUILayout.Label(headerText, EditorStyles.boldLabel);
                GUILayout.Space(5);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_search"), GUILayout.Width(50));
                    GUI.SetNextControlName("SearchField");
                    var newSearchText = GUILayout.TextField(_searchText);
                    if (newSearchText != _searchText)
                    {
                        _searchText = newSearchText;
                        FilterAssets();
                    }
                    GUILayout.Space(25);
                }

                GUILayout.Space(10);

                if (_filteredAssets == null || _filteredAssets.Count == 0)
                {
                    var message = string.IsNullOrEmpty(_searchText)
                        ? LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_noAssets")
                        : LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_noSearchResults");

                    GUILayout.Label(message, EditorStyles.helpBox);
                    return;
                }

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                    {
                        _scrollPosition = scrollView.scrollPosition;
                        using (new GUILayout.VerticalScope())
                        {                            foreach (var asset in _filteredAssets)
                            {
                                bool isSelected = _selectedAssets.Contains(asset.assetId.ToString());

                                if (isSelected)
                                {
                                    var originalColor = GUI.backgroundColor;
                                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 1f);

                                    if (GUILayout.Button(asset.metadata.name, GUI.skin.button, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                                    {
                                        if (_allowMultipleSelection)
                                        {
                                            if (_selectedAssets.Contains(asset.assetId.ToString()))
                                            {
                                                _selectedAssets.Remove(asset.assetId.ToString());
                                            }
                                            else
                                            {
                                                _selectedAssets.Add(asset.assetId.ToString());
                                            }
                                        }
                                        else
                                        {
                                            _selectedAssets.Clear();
                                            _selectedAssets.Add(asset.assetId.ToString());

                                            CompleteSelection();
                                        }
                                    }

                                    GUI.backgroundColor = originalColor;
                                }
                                else
                                {
                                    if (GUILayout.Button(asset.metadata.name, GUI.skin.button, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                                    {
                                        if (_allowMultipleSelection)
                                        {
                                            if (_selectedAssets.Contains(asset.assetId.ToString()))
                                            {
                                                _selectedAssets.Remove(asset.assetId.ToString());
                                            }
                                            else
                                            {
                                                _selectedAssets.Add(asset.assetId.ToString());
                                            }
                                        }
                                        else
                                        {
                                            _selectedAssets.Clear();
                                            _selectedAssets.Add(asset.assetId.ToString());

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
                {                    string selectionInfo = _allowMultipleSelection
                    ? $"{LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_selectedCount")}: {_selectedAssets.Count}"
                    : _selectedAssets.Count > 0
                        ? $"{LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_selected")}: {_availableAssets.FirstOrDefault(a => a.assetId.ToString() == _selectedAssets.First())?.metadata.name}"
                        : LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_noSelection");

                    GUILayout.Label(selectionInfo, EditorStyles.miniLabel);

                    GUILayout.Space(5);
                }

                if (_allowMultipleSelection)
                {
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_assetSelector_clearAll")))
                    {
                        _selectedAssets.Clear();
                    }
                    GUILayout.Space(3);
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_common_cancel")))
                    {
                        Close();
                    }

                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_common_ok")))
                    {
                        CompleteSelection();
                    }
                }

                GUILayout.Space(10);
            }
        }

        private void CompleteSelection()
        {
            var selectedAssetsList = _selectedAssets.ToList();
            _onAssetsSelected?.Invoke(selectedAssetsList);
            Close();
        }

        private void OnDestroy()
        {
            _onAssetsSelected = null;
        }

        private void FilterAssets()
        {
            IEnumerable<AssetSchema> assets = _availableAssets;
            if (_filterMode == 1)
            {
                assets = assets.Where(asset => string.IsNullOrEmpty(asset.parentGroupId) && (asset.childAssetIds == null || asset.childAssetIds.Count == 0));
            }
            else if (_filterMode == 2)
            {
                assets = assets.Where(asset => asset.childAssetIds == null || asset.childAssetIds.Count == 0);
            }
            if (string.IsNullOrEmpty(_searchText))
            {
                _filteredAssets = assets.ToList();
            }
            else
            {
                _filteredAssets = assets
                    .Where(asset => asset.metadata.name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
        }
    }
}
