using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.Helper;
using AMU.Editor.VrcAssetManager.UI;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class MainGridComponent
    {
        private static Vector2 _scrollPosition = Vector2.zero;
        private static AssetSchema _selectedAsset;
        private static List<AssetSchema> _selectedAssets = new List<AssetSchema>();
        private static AssetItemComponent _assetItemComponent = new AssetItemComponent();
        private static SpecialAssetItemComponent _specialAssetItemComponent = new SpecialAssetItemComponent();
        private static string nowAssetType = "";
        private static int _currentPage = 1;
        private static int _maxPage = 1;

        public static int selectedAssetCount => _selectedAssets?.Count ?? 0;

        public static void Draw()
        {
            var controller = AssetLibraryController.Instance;
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);

            var assets = controller.GetFilteredAssets();

            if (controller.sortOptions != null)
            {
                var sortedAssets = new List<AssetSchema>(assets);

                switch (controller.sortOptions.sortBy)
                {
                    case SortOptionsEnum.Name:
                        // Although the definition is the opposite, this is a better experience.
                        if (controller.sortOptions.isDescending)
                        {
                            sortedAssets = sortedAssets
                                .OrderBy(asset => asset.metadata.name)
                                .ToList();
                        }
                        else
                        {
                            sortedAssets = sortedAssets
                                .OrderByDescending(asset => asset.metadata.name)
                                .ToList();
                        }
                        break;
                    case SortOptionsEnum.Date:
                        if (controller.sortOptions.isDescending)
                        {
                            sortedAssets = sortedAssets
                                .OrderByDescending(asset => asset.metadata.modifiedDate)
                                .ToList();
                        }
                        else
                        {
                            sortedAssets = sortedAssets
                                .OrderBy(asset => asset.metadata.modifiedDate)
                                .ToList();
                        }
                        break;
                }

                assets = sortedAssets;
            }
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(750)))
                {
                    GUI.backgroundColor = originalColor;
                    if (controller?.library == null)
                    {
                        GUILayout.FlexibleSpace();
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetManager_libraryNotInitialized"), EditorStyles.largeLabel);
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.FlexibleSpace();
                        return;
                    }

                    if (assets == null || assets.Count == 0)
                    {
                        GUILayout.FlexibleSpace();
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetManager_noAssets"), EditorStyles.largeLabel);
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.FlexibleSpace();
                        return;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Space(5);

                            bool isGroupFiltered = !string.IsNullOrEmpty(controller.filterOptions.parentGroupId);
                            int specialItemsCount = isGroupFiltered ? 2 : 0;
                            int totalItems = assets.Count + specialItemsCount;
                            
                            _maxPage = Mathf.Max(1, Mathf.CeilToInt((float)totalItems / 35));
                            _currentPage = Mathf.Clamp(_currentPage, 1, _maxPage);
                            int _startIndex = (_currentPage - 1) * 35;
                            int _endIndex = Mathf.Min(_startIndex + 35, totalItems);

                            int currentIndex = _startIndex;
                            
                            for (int row = 0; currentIndex < _endIndex; row++)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    for (int col = 0; col < 7 && currentIndex < _endIndex; col++, currentIndex++)
                                    {
                                        if (isGroupFiltered && currentIndex == 0)
                                        {
                                            _specialAssetItemComponent.DrawBackButton(HandleBackClick);
                                        }
                                        else if (isGroupFiltered && currentIndex == totalItems - 1)
                                        {
                                            _specialAssetItemComponent.DrawAddButton(HandleAddClick);
                                        }
                                        else
                                        {
                                            int assetIndex = currentIndex - (isGroupFiltered && currentIndex > 0 ? 1 : 0);
                                            if (assetIndex < assets.Count)
                                            {
                                                var asset = assets[assetIndex];
                                                bool isSelected = _selectedAsset == asset;
                                                bool isMultiSelected = _selectedAssets.Contains(asset);
                                                _assetItemComponent.Draw(
                                                    asset,
                                                    isSelected,
                                                    isMultiSelected && _selectedAssets.Count > 1,
                                                    HandleAssetLeftClick,
                                                    HandleAssetRightClick,
                                                    HandleAssetDoubleClick
                                                );
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
                using (new GUILayout.HorizontalScope(GUILayout.Height(30)))
                {
                    GUILayout.FlexibleSpace();

                    GUI.enabled = _currentPage != 1;
                    if (GUILayout.Button("<<", GUILayout.Width(30)))
                    {
                        _currentPage = 1;
                    }

                    if (GUILayout.Button("<", GUILayout.Width(30)))
                    {
                        _currentPage = Mathf.Max(1, _currentPage - 1);
                    }

                    GUI.enabled = true;
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{_currentPage} / {_maxPage}", EditorStyles.label);
                        GUILayout.Space(8);
                        GUILayout.FlexibleSpace();
                    }

                    GUI.enabled = _currentPage != _maxPage;
                    if (GUILayout.Button(">", GUILayout.Width(30)))
                    {
                        _currentPage = Mathf.Min(_maxPage, _currentPage + 1);
                    }

                    if (GUILayout.Button(">>", GUILayout.Width(30)))
                    {
                        _currentPage = _maxPage;
                    }

                    GUI.enabled = true;

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private static void HandleAssetLeftClick(AssetSchema asset)
        {
            if (Event.current.control || Event.current.command)
            {
                if (_selectedAssets.Contains(asset))
                {
                    _selectedAssets.Remove(asset);
                    if (_selectedAsset == asset)
                    {
                        _selectedAsset = _selectedAssets.Count > 0 ? _selectedAssets[0] : null;
                    }
                }
                else
                {
                    _selectedAssets.Add(asset);
                    _selectedAsset = asset;
                }
            }
            else
            {
                _selectedAsset = null;
                _selectedAssets.Clear();
            }
        }

        private static void HandleAssetRightClick(AssetSchema asset)
        {
            var controller = AssetLibraryController.Instance;
            if (_selectedAssets.Count > 1)
            {
                var menu = new GenericMenu();
                
                bool allImportable = _selectedAssets.All(a => AssetImportUtility.IsImportable(a.fileInfo.filePath) || asset.fileInfo.importFiles.Count > 0);
                if (allImportable && _selectedAssets.Count > 1)
                {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_importSelectedAssets")), false, () => 
                    {
                        AssetImportUtility.ImportAsset(_selectedAssets);
                    });

                    menu.AddSeparator("");
                }

                bool allFavorite = _selectedAssets.All(a => a.state.isFavorite);
                bool noneFavorite = _selectedAssets.All(a => !a.state.isFavorite);
                if (!allFavorite) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_favoriteAdd")), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => !a.state.isFavorite))
                        {
                            selectedAsset.state.SetFavorite(true);
                            controller.UpdateAsset(selectedAsset);
                        }
                    });
                }
                if (!noneFavorite) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_favoriteRemove")), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => a.state.isFavorite))
                        {
                            selectedAsset.state.SetFavorite(false);
                            controller.UpdateAsset(selectedAsset);
                        }
                    });
                }

                bool allArchived = _selectedAssets.All(a => a.state.isArchived);
                bool noneArchived = _selectedAssets.All(a => !a.state.isArchived);
                if (!allArchived) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_archive")), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => !a.state.isArchived))
                        {
                            selectedAsset.state.SetArchived(true);
                            controller.UpdateAsset(selectedAsset);
                        }
                    });
                }
                if (!noneArchived) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_archiveRemove")), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => a.state.isArchived))
                        {
                            selectedAsset.state.SetArchived(false);
                            controller.UpdateAsset(selectedAsset);
                        }
                    });
                }

                menu.AddSeparator("");

                bool allNotParentOrChild = !_selectedAssets.Any(a => a.hasChildAssets || a.hasParentGroup);
                if (allNotParentOrChild && _selectedAssets.Count > 1) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_createGroup")), false, () => {
                        var groupId = controller.CreateGroupAsset(_selectedAssets);
                        AssetDetailWindow.ShowWindow(controller.GetAsset(groupId));
                    });
                    menu.AddSeparator("");
                }

                bool anyHasParentGroup = _selectedAssets.Any(a => a.hasParentGroup);
                if (anyHasParentGroup) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_removeFromGroup")), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => a.hasParentGroup))
                        {
                            if (Guid.TryParse(selectedAsset.parentGroupId, out var parentGuid))
                            {
                                controller.RemoveChildFromParent(parentGuid, selectedAsset.assetId);
                            }
                        }
                    });
                    menu.AddSeparator("");
                }

                menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_deleteSelectedAssets")), false, () => 
                {
                    if (EditorUtility.DisplayDialog(
                        LocalizationAPI.GetText("VrcAssetManager_ui_assetManager_deleteAssetsTitle"),
                        LocalizationAPI.GetText(asset.hasChildAssets ? "VrcAssetManager_ui_assetManager_deleteGroupAssetMessage" : "VrcAssetManager_ui_assetManager_deleteAssetMessage"),
                        LocalizationAPI.GetText("VrcAssetManager_common_yes"),
                        LocalizationAPI.GetText("VrcAssetManager_common_no")))
                    {
                        foreach (var selectedAsset in _selectedAssets)
                        {
                            controller.RemoveAsset(selectedAsset.assetId);
                        }
                        _selectedAssets.Clear();
                        _selectedAsset = null;
                    }
                });

                menu.ShowAsContext();
            }
            else
            {
                var menu = new GenericMenu();

                if (AssetImportUtility.IsImportable(asset.fileInfo.filePath) || asset.fileInfo.importFiles.Count > 0)
                {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_importAsset")), false, () => 
                    {
                        AssetImportUtility.ImportAsset(asset);
                    });

                    menu.AddSeparator("");
                }

                menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_showAssetDetail")), false, () =>
                {
                    AssetDetailWindow.ShowWindow(asset);
                    AssetDetailWindow.history.Clear();
                });

                menu.AddSeparator("");

                if (!asset.state.isFavorite) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_favoriteAdd")), false, () => {
                        asset.state.SetFavorite(true);
                        controller.UpdateAsset(asset);
                    });
                }
                else
                {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_favoriteRemove")), false, () => {
                        asset.state.SetFavorite(false);
                        controller.UpdateAsset(asset);
                    });
                }

                if (!asset.state.isArchived) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_archive")), false, () => {
                        asset.state.SetArchived(true);
                        controller.UpdateAsset(asset);
                    });
                }
                else
                {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_archiveRemove")), false, () => {
                        asset.state.SetArchived(false);
                        controller.UpdateAsset(asset);
                    });
                }

                menu.AddSeparator("");

                if (asset.hasParentGroup) {
                    menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_removeFromGroup")), false, () => {
                        if (Guid.TryParse(asset.parentGroupId, out var parentGuid))
                        {
                            controller.RemoveChildFromParent(parentGuid, asset.assetId);
                        }
                    });
                    menu.AddSeparator("");
                }

                menu.AddItem(new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_mainGrid_deleteAsset")), false, () => 
                {
                    if (EditorUtility.DisplayDialog(
                        LocalizationAPI.GetText("VrcAssetManager_ui_assetManager_deleteAssetTitle"),
                        LocalizationAPI.GetText(asset.hasChildAssets ? "VrcAssetManager_ui_assetManager_deleteGroupAssetMessage" : "VrcAssetManager_ui_assetManager_deleteAssetMessage"),
                        LocalizationAPI.GetText("VrcAssetManager_common_yes"),
                        LocalizationAPI.GetText("VrcAssetManager_common_no")))
                    {
                        controller.RemoveAsset(asset.assetId);
                    }
                });
                
                menu.ShowAsContext();
            }
        }

        private static void HandleAssetDoubleClick(AssetSchema asset)
        {
            var controller = AssetLibraryController.Instance;
            
            if (asset.hasChildAssets)
            {
                nowAssetType = controller.filterOptions.assetType;
                controller.filterOptions.ClearFilter();
                controller.filterOptions.parentGroupId = asset.assetId.ToString();
                controller.filterOptions.isChildItem = true;
                ToolbarComponent.isUsingAdvancedSearch = true;
            }
            else
            {
                AssetDetailWindow.ShowWindow(asset);
                AssetDetailWindow.history.Clear();
            }
        }

        private static void HandleBackClick()
        {
            var controller = AssetLibraryController.Instance;
            controller.filterOptions.assetType = nowAssetType;
            controller.filterOptions.parentGroupId = string.Empty;
            controller.filterOptions.isChildItem = false;
            ToolbarComponent.isUsingAdvancedSearch = false;
        }

        private static void HandleAddClick()
        {
            var controller = AssetLibraryController.Instance;
            var currentGroupId = controller.filterOptions.parentGroupId;
            
            if (!string.IsNullOrEmpty(currentGroupId))
            {
                AssetSelectorWindow.ShowWindow(
                    (selectedAssetIds) =>
                    {
                        foreach (var assetId in selectedAssetIds)
                        {
                            if (Guid.TryParse(assetId, out var guid))
                            {
                                var asset = controller.GetAsset(guid);
                                if (asset != null)
                                {
                                    asset.SetParentGroupId(currentGroupId);
                                    controller.UpdateAsset(asset);
                                }
                            }
                        }
                    },
                    null,
                    true,
                    1
                );
            }
        }
    }
}