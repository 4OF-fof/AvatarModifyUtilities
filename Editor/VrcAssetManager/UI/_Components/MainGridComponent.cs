using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class MainGridComponent
    {
        private static Vector2 _scrollPosition = Vector2.zero;
        private static AssetSchema _selectedAsset;
        private static List<AssetSchema> _selectedAssets = new List<AssetSchema>();
        private static float _thumbnailSize = 120f;
        private static AssetItemComponent _assetItemComponent = new AssetItemComponent();

        public static int selectedAssetCount => _selectedAssets?.Count ?? 0;

        public static void Draw(AssetLibraryController controller)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = originalColor;
                if (controller?.library == null)
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(LocalizationAPI.GetText("AssetManager_libraryNotInitialized"), EditorStyles.largeLabel);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.FlexibleSpace();
                    return;
                }

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

                if (assets == null || assets.Count == 0)
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(LocalizationAPI.GetText("AssetManager_noAssets"), EditorStyles.largeLabel);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.FlexibleSpace();
                    return;
                }

                using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scrollView.scrollPosition;

                    float availableWidth = 920;
                    int columnsPerRow = controller.columnsPerRow;
                    float calculatedThumbnailSize = (availableWidth - (columnsPerRow - 1) * 10) / columnsPerRow;
                    _thumbnailSize = Mathf.Max(60f, calculatedThumbnailSize);

                    for (int i = 0; i < assets.Count; i += columnsPerRow)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            for (int j = 0; j < columnsPerRow && i + j < assets.Count; j++)
                            {
                                var asset = assets[i + j];
                                bool isSelected = _selectedAsset == asset;
                                bool isMultiSelected = _selectedAssets.Contains(asset);
                                _assetItemComponent.Draw(
                                    asset,
                                    _thumbnailSize,
                                    isSelected,
                                    isMultiSelected && _selectedAssets.Count > 1,
                                    a => HandleAssetLeftClick(a, controller),
                                    a => HandleAssetRightClick(a, controller),
                                    a => HandleAssetDoubleClick(a, controller)
                                );
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
            }
        }

        private static void HandleAssetLeftClick(AssetSchema asset, AssetLibraryController controller)
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

        private static void HandleAssetRightClick(AssetSchema asset, AssetLibraryController controller)
        {
            if (_selectedAssets.Count > 1)
            {
                var menu = new GenericMenu();

                bool allFavorite = _selectedAssets.All(a => a.state.isFavorite);
                bool noneFavorite = _selectedAssets.All(a => !a.state.isFavorite);
                if (!allFavorite) {
                    menu.AddItem(new GUIContent("お気に入り登録"), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => !a.state.isFavorite))
                        {
                            selectedAsset.state.SetFavorite(true);
                            controller.UpdateAsset(selectedAsset);
                        }
                    });
                }
                if (!noneFavorite) {
                    menu.AddItem(new GUIContent("お気に入り解除"), false, () => {
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
                    menu.AddItem(new GUIContent("アーカイブ"), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => !a.state.isArchived))
                        {
                            selectedAsset.state.SetArchived(true);
                            controller.UpdateAsset(selectedAsset);
                        }
                    });
                }
                if (!noneArchived) {
                    menu.AddItem(new GUIContent("アーカイブ解除"), false, () => {
                        foreach (var selectedAsset in _selectedAssets.Where(a => a.state.isArchived))
                        {
                            selectedAsset.state.SetArchived(false);
                            controller.UpdateAsset(selectedAsset);
                        }
                    });
                }

                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Delete Selected Assets"), false, () => 
                {
                    if (EditorUtility.DisplayDialog(
                        LocalizationAPI.GetText("AssetManager_deleteAssetsTitle"),
                        LocalizationAPI.GetText(asset.hasChildAssets ? "AssetManager_deleteGroupAssetMessage" : "AssetManager_deleteAssetMessage"),
                        LocalizationAPI.GetText("Yes"),
                        LocalizationAPI.GetText("No")))
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

                if (!asset.state.isFavorite) {
                    menu.AddItem(new GUIContent("お気に入り登録"), false, () => {
                        asset.state.SetFavorite(true);
                        controller.UpdateAsset(asset);
                    });
                }
                else
                {
                    menu.AddItem(new GUIContent("お気に入り解除"), false, () => {
                        asset.state.SetFavorite(false);
                        controller.UpdateAsset(asset);
                    });
                }

                if (!asset.state.isArchived) {
                    menu.AddItem(new GUIContent("アーカイブ"), false, () => {
                        asset.state.SetArchived(true);
                        controller.UpdateAsset(asset);
                    });
                }
                else
                {
                    menu.AddItem(new GUIContent("アーカイブ解除"), false, () => {
                        asset.state.SetArchived(false);
                        controller.UpdateAsset(asset);
                    });
                }

                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Delete Asset"), false, () => 
                {
                    if (EditorUtility.DisplayDialog(
                        LocalizationAPI.GetText("AssetManager_deleteAssetTitle"),
                        LocalizationAPI.GetText(asset.hasChildAssets ? "AssetManager_deleteGroupAssetMessage" : "AssetManager_deleteAssetMessage"),
                        LocalizationAPI.GetText("Yes"),
                        LocalizationAPI.GetText("No")))
                    {
                        controller.RemoveAsset(asset.assetId);
                    }
                });
                
                menu.ShowAsContext();
            }
        }

        private static void HandleAssetDoubleClick(AssetSchema asset, AssetLibraryController controller)
        {
            AssetDetailWindow.ShowWindow(asset, controller);
            AssetDetailWindow.history.Clear();
        }
    }
}