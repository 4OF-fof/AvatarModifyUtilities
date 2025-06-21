using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class MainGridComponent
    {
        // Mock data for display purposes only
        private static List<MockAssetInfo> _mockAssets = new List<MockAssetInfo>
        {
            new MockAssetInfo("Sample Avatar 1", "Avatar", favorite: true),
            new MockAssetInfo("Cool Accessory", "Accessories"),
            new MockAssetInfo("Cute Outfit", "Clothing", favorite: true),
            new MockAssetInfo("Long Hair Style", "Hair"),
            new MockAssetInfo("Sparkling Eyes", "Eyes"),
            new MockAssetInfo("Custom Shader", "Shader"),
            new MockAssetInfo("HD Texture Pack", "Texture", hidden: true),
            new MockAssetInfo("Dance Animation", "Animation"),
            new MockAssetInfo("Magic VFX", "VFX"),
            new MockAssetInfo("Utility Tools", "Tools", group: true),
            new MockAssetInfo("Another Avatar", "Avatar"),
            new MockAssetInfo("Face Makeup", "Face"),
            new MockAssetInfo("Body Parts", "Body"),
        };

        private static Vector2 _scrollPosition = Vector2.zero;
        private static MockAssetInfo _selectedAsset;
        private static List<MockAssetInfo> _selectedAssets = new List<MockAssetInfo>();
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
                DrawAssetGrid();
            }
        }

        private static void DrawAssetGrid()
        {
            if (_mockAssets == null || _mockAssets.Count == 0)
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
                for (int i = 0; i < _mockAssets.Count; i += columnsPerRow)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (int j = 0; j < columnsPerRow && i + j < _mockAssets.Count; j++)
                        {
                            var asset = _mockAssets[i + j];
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
        private static void HandleAssetLeftClick(MockAssetInfo asset)
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
        private static void HandleAssetRightClick(MockAssetInfo asset)
        {
            // Mock context menu
            Debug.Log($"Right clicked on asset: {asset.name}");
        }

        // Public accessors for state
        public static MockAssetInfo SelectedAsset => _selectedAsset;
        public static List<MockAssetInfo> SelectedAssets => new List<MockAssetInfo>(_selectedAssets);
        public static void SetThumbnailSize(float size) => _thumbnailSize = size;
        public static void SetLeftPanelWidth(float width) => _leftPanelWidth = width;
    }
}