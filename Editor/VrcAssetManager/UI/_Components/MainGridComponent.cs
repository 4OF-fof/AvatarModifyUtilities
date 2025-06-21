using System;
using System.Collections.Generic;
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
        private static float _thumbnailSize = 110f;
        private static float _leftPanelWidth = 240f;
        private static AssetItemComponent _assetItemComponent = new AssetItemComponent();

        public static void Draw(AssetLibraryController controller)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = originalColor;
                DrawAssetGrid(controller);
            }
        }

        private static void DrawAssetGrid(AssetLibraryController controller)
        {
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

            var assets = controller.GetFilteredAssets(controller.filterOptions);
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

                // Calculate grid layout
                float availableWidth = Screen.width - _leftPanelWidth - 40; // Approximate available width
                int columnsPerRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (_thumbnailSize + 10)));

                // Update asset item component thumbnail size
                _assetItemComponent.SetThumbnailSize(_thumbnailSize);

                // Draw assets in grid
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
                                isSelected, 
                                isMultiSelected && _selectedAssets.Count > 1,
                                HandleAssetLeftClick,
                                HandleAssetRightClick
                            );
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        /// <summary>
        /// Handle left click on asset item
        /// </summary>
        private static void HandleAssetLeftClick(AssetSchema asset)
        {
            if (Event.current.control || Event.current.command)
            {
                // Multi-select toggle
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
                // Single select
                _selectedAsset = asset;
                _selectedAssets.Clear();
                _selectedAssets.Add(asset);
            }
        }

        /// <summary>
        /// Handle right click on asset item
        /// </summary>
        private static void HandleAssetRightClick(AssetSchema asset)
        {
            // Context menu
            Debug.Log($"Right clicked on asset: {asset.Metadata.Name}");
        }

        // Public accessors for state
        public static AssetSchema SelectedAsset => _selectedAsset;
        public static List<AssetSchema> SelectedAssets => new List<AssetSchema>(_selectedAssets);
        public static void SetThumbnailSize(float size) => _thumbnailSize = size;
        public static void SetLeftPanelWidth(float width) => _leftPanelWidth = width;
    }
}