using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.Helper;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class MainGridComponent
    {
        private static Vector2 _scrollPosition = Vector2.zero;
        private static AssetSchema _selectedAsset;
        private static List<AssetSchema> _selectedAssets = new List<AssetSchema>();
        private static AssetItemComponent _assetItemComponent = new AssetItemComponent();
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
                            GUILayout.Label(LocalizationAPI.GetText("AssetManager_libraryNotInitialized"), EditorStyles.largeLabel);
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
                            GUILayout.Label(LocalizationAPI.GetText("AssetManager_noAssets"), EditorStyles.largeLabel);
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
                            _maxPage = Mathf.Max(1, Mathf.CeilToInt((float)assets.Count / 35));
                            _currentPage = Mathf.Clamp(_currentPage, 1, _maxPage);
                            int _startIndex = (_currentPage - 1) * 35;
                            int _endIndex = Mathf.Min(_startIndex + 35, assets.Count);

                            for (int i = _startIndex; i < _endIndex && i < assets.Count; i += 7)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    for (int j = 0; j < 7 && i+j < assets.Count; j++)
                                    {
                                        var asset = assets[i + j];
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
                
                bool allImportable = _selectedAssets.All(a => AssetImportUtility.IsImportable(a.fileInfo.filePath));
                if (allImportable && _selectedAssets.Count > 1)
                {
                    menu.AddItem(new GUIContent("Import Selected Assets"), false, () => 
                    {
                        AssetImportUtility.ImportAsset(_selectedAssets);
                    });

                    menu.AddSeparator("");
                }

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

                if (AssetImportUtility.IsImportable(asset.fileInfo.filePath))
                {
                    menu.AddItem(new GUIContent("Import Asset"), false, () => 
                    {
                        AssetImportUtility.ImportAsset(asset);
                    });

                    menu.AddSeparator("");
                }

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

        private static void HandleAssetDoubleClick(AssetSchema asset)
        {
            AssetDetailWindow.ShowWindow(asset);
            AssetDetailWindow.history.Clear();
        }
    }
}