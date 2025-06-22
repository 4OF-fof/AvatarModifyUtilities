using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.UI;

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
        private static bool _isChildItem = false;
        private static AssetLibraryController _controller;

        public static bool isUsingAdvancedSearch
        {
            get => _isUsingAdvancedSearch;
            set => _isUsingAdvancedSearch = value;
        }

        public static void Draw(AssetLibraryController controller)
        {
            _controller = controller;
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

                        var closeIcon = EditorGUIUtility.IconContent("winbtn_win_close");
                        if (GUILayout.Button(closeIcon, EditorStyles.toolbarButton, GUILayout.Width(20)))
                        {
                            _isUsingAdvancedSearch = false;
                            var assetType = _controller.filterOptions.assetType;
                            _controller.filterOptions.ClearFilter();
                            _controller.filterOptions.assetType = assetType;
                        }
                    }
                    else
                    {
                        var newSearchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
                        if (newSearchText != _searchText)
                        {
                            _searchText = newSearchText;
                            _controller.filterOptions.name = _controller.filterOptions.authorName = _controller.filterOptions.description = _searchText;
                        }
                        var searchIcon = EditorGUIUtility.IconContent("Search Icon");
                        if (GUILayout.Button(searchIcon, EditorStyles.toolbarButton, GUILayout.Width(40)))
                        {
                            AdvancedSearchWindow.ShowWindow(_controller, (closedBySearch) => { _isUsingAdvancedSearch = closedBySearch; });
                        }
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUI.enabled = !_isUsingAdvancedSearch;

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.All, LocalizationAPI.GetText("AssetManager_filterAll"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.All;
                        _controller.filterOptions.isFavorite = null;
                        _controller.filterOptions.isArchived = false;
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.Favorites, LocalizationAPI.GetText("AssetManager_filterFavorite"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.Favorites;
                        _controller.filterOptions.isFavorite = true;
                        _controller.filterOptions.isArchived = false;
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.ArchivedOnly, LocalizationAPI.GetText("AssetManager_filterArchived"), EditorStyles.toolbarButton))
                    {
                        _currentFilter = AssetFilterType.ArchivedOnly;
                        _controller.filterOptions.isFavorite = null;
                        _controller.filterOptions.isArchived = true;
                    }

                    GUI.enabled = true;
                    GUILayout.Space(10);

                    GUILayout.FlexibleSpace();

                    var folderIcon = _isChildItem ? EditorGUIUtility.IconContent("FolderOpened Icon") : EditorGUIUtility.IconContent("Folder Icon");
                    var folderContent = new GUIContent(folderIcon.image);

                    var newIsChildItem = GUILayout.Toggle(_isChildItem, folderContent, EditorStyles.toolbarButton, GUILayout.Width(25));
                    if (newIsChildItem != _isChildItem)
                    {
                        _isChildItem = newIsChildItem;
                        _controller.filterOptions.isChildItem = _isChildItem;
                    }

                    GUILayout.Space(5);

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
                        _controller.sortOptions.sortBy = (SortOptionsEnum)_selectedSortOption;
                    }

                    var sortArrowIcon = _sortDescending ? EditorGUIUtility.IconContent("d_scrolldown") : EditorGUIUtility.IconContent("d_scrollup");
                    var newSortDescending = GUILayout.Toggle(_sortDescending, sortArrowIcon, EditorStyles.toolbarButton, GUILayout.Width(25));
                    if (newSortDescending != _sortDescending)
                    {
                        _sortDescending = newSortDescending;
                        _controller.sortOptions.isDescending = _sortDescending;
                    }

                    GUILayout.Space(10);

                    if (MainGridComponent.selectedAssetCount > 1)
                    {
                        var selectedCountStyle = new GUIStyle(EditorStyles.toolbarButton)
                        {
                            normal = { textColor = new Color(0.3f, 0.6f, 1f) },
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter
                        };
                        string selectedText = $"{MainGridComponent.selectedAssetCount} 件選択中";
                        GUILayout.Label(selectedText, selectedCountStyle, GUILayout.MinWidth(80));
                    }
                    else
                    {
                        var addIcon = EditorGUIUtility.IconContent("Toolbar Plus");
                        if (GUILayout.Button(addIcon, EditorStyles.toolbarButton, GUILayout.Width(40)))
                        {
                            OpenDownloadFolderAndSelectFile();
                        }
                        var refreshIcon = EditorGUIUtility.IconContent("d_Refresh");
                        if (GUILayout.Button(refreshIcon, EditorStyles.toolbarButton, GUILayout.Width(40)))
                        {
                            _controller.SyncAssetLibrary();
                            Debug.Log("Refresh requested");
                        }
                    }
                }
            }
        }

        private static void OpenDownloadFolderAndSelectFile()
        {
            string downloadPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                                               "Downloads");

            string selectedFile = EditorUtility.OpenFilePanel("ファイルを選択", downloadPath, "json,unitypackage,zip,*");

            if (!string.IsNullOrEmpty(selectedFile))
            {
                string fileName = Path.GetFileName(selectedFile);

                if (fileName.Equals("AMU_BoothItem.json", StringComparison.OrdinalIgnoreCase))
                {
                    BoothItemImportWindow.ShowWindowWithFile(_controller, selectedFile);
                }
                else
                {
                    Debug.Log($"Selected file for asset import: {selectedFile}");
                }
            }
        }

        public static void DestroyWindow()
        {
            _controller.filterOptions.ClearFilter();
            _isUsingAdvancedSearch = false;
            var advWindows = Resources.FindObjectsOfTypeAll<AMU.Editor.VrcAssetManager.UI.AdvancedSearchWindow>();
            if (advWindows != null && advWindows.Length > 0)
            {
                foreach (var win in advWindows)
                {
                    win.Close();
                }
            }
        }
    }
}
