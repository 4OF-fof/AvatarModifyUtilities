using System;
using System.Linq;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;

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
        private static string _searchText = "";
        private static AssetFilterType _currentFilter = AssetFilterType.All;
        private static int _selectedSortOption = 0; // 0: Name, 1: Date
        private static bool _sortDescending = true;
        private static bool _isUsingAdvancedSearch = false;

        public static void Draw(AssetLibraryController controller)
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new GUILayout.HorizontalScope(GUILayout.Width(240f)))
                {
                    if (_isUsingAdvancedSearch)
                    {
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
                        var newSearchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
                        if (newSearchText != _searchText)
                        {
                            _searchText = newSearchText;
                            controller.filterOptions.name = controller.filterOptions.authorName = controller.filterOptions.description =  _searchText;
                        }
                        if (GUILayout.Button("検索", EditorStyles.toolbarButton, GUILayout.Width(40)))
                        {
                            _isUsingAdvancedSearch = true;
                            Debug.Log("Advanced search requested");
                        }
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUI.enabled = !_isUsingAdvancedSearch;

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.All, LocalizationAPI.GetText("AssetManager_filterAll"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.All;
                        controller.filterOptions.ClearFilter();
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.Favorites, LocalizationAPI.GetText("AssetManager_filterFavorite"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.Favorites;
                        controller.filterOptions.isFavorite = true;
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.ArchivedOnly, LocalizationAPI.GetText("AssetManager_filterArchived"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.ArchivedOnly;
                        controller.filterOptions.isArchived = true;
                    }

                    GUI.enabled = true;
                    GUILayout.Space(10);

                    GUILayout.FlexibleSpace();

                    string[] sortOptions = Enum.GetNames(typeof(SortOptionsEnum))
                        .Select(name => 
                        {
                            switch (name)
                            {
                                case "Name": return "名前";
                                case "Date": return "日付";
                                default: return name;
                            }
                        })
                        .ToArray();

                    var newSortOption = EditorGUILayout.Popup(_selectedSortOption, sortOptions, EditorStyles.toolbarPopup, GUILayout.Width(100));
                    if (newSortOption != _selectedSortOption)
                    {
                        _selectedSortOption = newSortOption;
                        controller.sortOptions.sortBy = (SortOptionsEnum)_selectedSortOption;
                    }

                    string sortArrow = _sortDescending ? "↓" : "↑";
                    var newSortDescending = GUILayout.Toggle(_sortDescending, sortArrow, EditorStyles.toolbarButton, GUILayout.Width(25));
                    if (newSortDescending != _sortDescending)
                    {
                        _sortDescending = newSortDescending;
                        controller.sortOptions.isDescending = _sortDescending;
                    }

                    GUILayout.Space(10);


                    // TODO: Select item counter
                    if (GUILayout.Button(LocalizationAPI.GetText("AssetManager_addAsset"), EditorStyles.toolbarButton))
                    {
                        Debug.Log("Add asset requested");
                    }

                    if (GUILayout.Button(LocalizationAPI.GetText("Common_refresh"), EditorStyles.toolbarButton))
                    {
                        controller.SyncAssetLibrary();
                        Debug.Log("Refresh requested");
                    }
                }
            }
        }

        // Public accessors for state (to be used by the main window)
        public static string SearchText => _searchText;
        public static AssetFilterType CurrentFilter => _currentFilter;
        public static int SelectedSortOption => _selectedSortOption;
        public static bool SortDescending => _sortDescending;
        public static bool IsUsingAdvancedSearch => _isUsingAdvancedSearch;

        // Public setters for external state management
        public static void SetSearchText(string text) => _searchText = text;
        public static void SetFilter(AssetFilterType filter) => _currentFilter = filter;
        public static void SetSortOption(int option) => _selectedSortOption = option;
        public static void SetSortDescending(bool descending) => _sortDescending = descending;
        public static void SetAdvancedSearch(bool active) => _isUsingAdvancedSearch = active;
    }
}
