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

        public static int SelectedAssetCount => _selectedAssets?.Count ?? 0;

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
                                    .OrderBy(asset => asset.Metadata.Name)
                                    .ToList();
                            }
                            else
                            {
                                sortedAssets = sortedAssets
                                    .OrderByDescending(asset => asset.Metadata.Name)
                                    .ToList();
                            }
                            break;
                        case SortOptionsEnum.Date:
                            if (controller.sortOptions.isDescending)
                            {
                                sortedAssets = sortedAssets
                                    .OrderByDescending(asset => asset.Metadata.ModifiedDate)
                                    .ToList();
                            }
                            else
                            {
                                sortedAssets = sortedAssets
                                    .OrderBy(asset => asset.Metadata.ModifiedDate)
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
                                    HandleAssetLeftClick,
                                    HandleAssetRightClick,
                                    a => HandleAssetDoubleClick(a, controller)
                                );
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
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
                _selectedAsset = asset;
                _selectedAssets.Clear();
                _selectedAssets.Add(asset);
            }
        }

        private static void HandleAssetRightClick(AssetSchema asset)
        {
            if (_selectedAssets.Count > 1)
            {
                Debug.LogWarning("Right-clicking on multiple selected.");
            }
            else
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Open Asset"), false, () => Debug.Log($"Opening asset: {asset.Metadata.Name}"));
                menu.AddItem(new GUIContent("Delete Asset"), false, () => Debug.Log($"Deleting asset: {asset.Metadata.Name}"));
                menu.ShowAsContext();
            }
        }

        private static void HandleAssetDoubleClick(AssetSchema asset, AssetLibraryController controller)
        {
            AssetDetailWindow.ShowWindow(asset, controller);
        }
    }
}