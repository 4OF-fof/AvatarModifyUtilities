using System;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public enum AssetFilterType
    {
        All,
        Favorites,
        ArchivedOnly
    }

    public static class ToolbarComponent
    {
        // UI State variables (temporary - should be moved to a proper state manager)
        private static string _searchText = "";
        private static AssetFilterType _currentFilter = AssetFilterType.All;
        private static int _selectedSortOption = 1; // 0: Name, 1: Date, 2: Size
        private static bool _sortDescending = true;
        private static bool _isUsingAdvancedSearch = false;
        private static bool _isGroupFilterActive = false;

        public static void Draw(AssetLibraryController controller)
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Search field area
                using (new GUILayout.HorizontalScope(GUILayout.Width(240f)))
                {
                    if (_isGroupFilterActive)
                    {
                        // Group filter status display
                        var groupStatusStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.2f, 0.7f, 0.2f) },
                            fontSize = 11,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleLeft
                        };

                        var rect = GUILayoutUtility.GetRect(GUIContent.none, groupStatusStyle, GUILayout.ExpandWidth(true));
                        GUI.Label(rect, "グループ: [グループ名]", groupStatusStyle);

                        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
                        {
                            _isGroupFilterActive = false;
                        }
                    }
                    else if (_isUsingAdvancedSearch)
                    {
                        // Advanced search status display
                        var statusStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.3f, 0.6f, 1f) },
                            fontSize = 11,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleLeft
                        };

                        var rect = GUILayoutUtility.GetRect(GUIContent.none, statusStyle, GUILayout.ExpandWidth(true));
                        GUI.Label(rect, "詳細検索中", statusStyle);

                        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
                        {
                            _isUsingAdvancedSearch = false;
                        }
                    }
                    else
                    {
                        // Normal search field
                        var newSearchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
                        if (newSearchText != _searchText)
                        {
                            _searchText = newSearchText;
                            _isUsingAdvancedSearch = false;
                        }

                        // Advanced search button
                        if (GUILayout.Button(LocalizationAPI.GetText("AssetManager_advancedSearch"), EditorStyles.toolbarButton, GUILayout.Width(40)))
                        {
                            _isUsingAdvancedSearch = true;
                            Debug.Log("Advanced search requested");
                        }
                    }
                }

                // Filter buttons
                using (new GUILayout.HorizontalScope())
                {
                    GUI.enabled = !_isGroupFilterActive;

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.All, LocalizationAPI.GetText("AssetManager_filterAll"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.All;
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.Favorites, LocalizationAPI.GetText("AssetManager_filterFavorite"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.Favorites;
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.ArchivedOnly, LocalizationAPI.GetText("AssetManager_filterArchived"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.ArchivedOnly;
                    }

                    GUI.enabled = true;
                    GUILayout.Space(10);

                    GUILayout.FlexibleSpace();

                    // Sort options
                    string[] sortOptions = {
                        LocalizationAPI.GetText("AssetManager_sortName"),
                        LocalizationAPI.GetText("AssetManager_sortDate"),
                        LocalizationAPI.GetText("AssetManager_sortSize")
                    };

                    var newSortOption = EditorGUILayout.Popup(_selectedSortOption, sortOptions, EditorStyles.toolbarPopup, GUILayout.Width(100));
                    if (newSortOption != _selectedSortOption)
                    {
                        _selectedSortOption = newSortOption;
                    }

                    string sortArrow = _sortDescending ? "↓" : "↑";
                    var newSortDescending = GUILayout.Toggle(_sortDescending, sortArrow, EditorStyles.toolbarButton, GUILayout.Width(25));
                    if (newSortDescending != _sortDescending)
                    {
                        _sortDescending = newSortDescending;
                    }

                    GUILayout.Space(10);

                    // Action buttons
                    if (GUILayout.Button(LocalizationAPI.GetText("AssetManager_addAsset"), EditorStyles.toolbarButton))
                    {
                        Debug.Log("Add asset requested");
                    }

                    if (GUILayout.Button(LocalizationAPI.GetText("Common_refresh"), EditorStyles.toolbarButton))
                    {
                        controller.SyncAssetLibrary();
                        Debug.Log("Refresh requested");
                    }

                    // Selected assets count (placeholder)
                    // if (selectedAssetsCount > 1)
                    // {
                    //     GUILayout.Space(10);
                    //     GUILayout.Label($"選択中: {selectedAssetsCount}", EditorStyles.toolbarButton);
                    // }
                }
            }
        }

        // Public accessors for state (to be used by the main window)
        public static string SearchText => _searchText;
        public static AssetFilterType CurrentFilter => _currentFilter;
        public static int SelectedSortOption => _selectedSortOption;
        public static bool SortDescending => _sortDescending;
        public static bool IsUsingAdvancedSearch => _isUsingAdvancedSearch;
        public static bool IsGroupFilterActive => _isGroupFilterActive;

        // Public setters for external state management
        public static void SetSearchText(string text) => _searchText = text;
        public static void SetFilter(AssetFilterType filter) => _currentFilter = filter;
        public static void SetSortOption(int option) => _selectedSortOption = option;
        public static void SetSortDescending(bool descending) => _sortDescending = descending;
        public static void SetAdvancedSearch(bool active) => _isUsingAdvancedSearch = active;
        public static void SetGroupFilter(bool active) => _isGroupFilterActive = active;
    }
}
