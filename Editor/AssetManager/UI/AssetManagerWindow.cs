using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Data.Lang;

namespace AMU.AssetManager.UI
{
    public class AssetManagerWindow : EditorWindow
    {
        [MenuItem("AMU/Asset Manager", priority = 2)]
        public static void ShowWindow()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            var window = GetWindow<AssetManagerWindow>(LocalizationManager.GetText("AssetManager_windowTitle"));
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }

        // Managers
        private AssetDataManager _dataManager;
        private AssetThumbnailManager _thumbnailManager;
        private AssetFileManager _fileManager;

        // UI State
        private Vector2 _leftScrollPosition = Vector2.zero;
        private Vector2 _rightScrollPosition = Vector2.zero;
        private string _searchText = "";
        private AssetType _selectedAssetType = AssetType.Avatar;
        private bool _showFavoritesOnly = false;
        private bool _showHidden = false;
        private int _selectedSortOption = 1;
        private bool _sortDescending = true;

        // Layout
        private float _leftPanelWidth = 250f;
        private bool _isResizing = false;
        private Rect _resizeRect;

        // Asset Grid
        private float _thumbnailSize = 100f;
        private List<AssetInfo> _filteredAssets = new List<AssetInfo>();
        private AssetInfo _selectedAsset;

        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            InitializeManagers();
            RefreshAssetList();
        }

        private void OnDisable()
        {
            _thumbnailManager?.ClearCache();
        }

        private void InitializeManagers()
        {
            if (_dataManager == null)
            {
                _dataManager = new AssetDataManager();
                _dataManager.OnDataLoaded += RefreshAssetList;
                _dataManager.OnDataChanged += RefreshAssetList;
                _dataManager.LoadData();
            }

            if (_thumbnailManager == null)
            {
                _thumbnailManager = new AssetThumbnailManager();
                _thumbnailManager.OnThumbnailLoaded += Repaint;
                _thumbnailManager.OnThumbnailSaved += OnThumbnailSaved;
            }

            if (_fileManager == null)
            {
                _fileManager = new AssetFileManager();
            }
        }

        private void OnGUI()
        {
            if (_dataManager?.IsLoading == true)
            {
                DrawLoadingUI();
                return;
            }

            DrawToolbar();
            DrawMainContent();
            HandleEvents();
        }

        private void DrawLoadingUI()
        {
            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(LocalizationManager.GetText("AssetManager_loading"), EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Search field
                var newSearchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
                if (newSearchText != _searchText)
                {
                    _searchText = newSearchText;
                    RefreshAssetList();
                }

                GUILayout.Space(10);

                // Filter buttons
                if (GUILayout.Button(LocalizationManager.GetText("AssetManager_filterAll"), EditorStyles.toolbarButton))
                {
                    _showFavoritesOnly = false;
                    RefreshAssetList();
                }

                if (GUILayout.Button(LocalizationManager.GetText("AssetManager_filterFavorite"), EditorStyles.toolbarButton))
                {
                    _showFavoritesOnly = true;
                    RefreshAssetList();
                }

                GUILayout.Space(10);

                // Show hidden checkbox
                var newShowHidden = GUILayout.Toggle(_showHidden, LocalizationManager.GetText("AssetManager_showHidden"), EditorStyles.toolbarButton);
                if (newShowHidden != _showHidden)
                {
                    _showHidden = newShowHidden;
                    RefreshAssetList();
                }

                GUILayout.FlexibleSpace();

                // Sort options
                string[] sortOptions = {
                    LocalizationManager.GetText("AssetManager_sortName"),
                    LocalizationManager.GetText("AssetManager_sortDate"),
                    LocalizationManager.GetText("AssetManager_sortSize")
                };

                var newSortOption = EditorGUILayout.Popup(_selectedSortOption, sortOptions, EditorStyles.toolbarPopup, GUILayout.Width(100));
                if (newSortOption != _selectedSortOption)
                {
                    _selectedSortOption = newSortOption;
                    RefreshAssetList();
                }

                string sortArrow = _sortDescending ? "↓" : "↑";
                var newSortDescending = GUILayout.Toggle(_sortDescending, sortArrow, EditorStyles.toolbarButton, GUILayout.Width(25));
                if (newSortDescending != _sortDescending)
                {
                    _sortDescending = newSortDescending;
                    RefreshAssetList();
                }

                GUILayout.Space(10);

                if (GUILayout.Button(LocalizationManager.GetText("AssetManager_addAsset"), EditorStyles.toolbarButton))
                {
                    ShowAddAssetDialog();
                }

                if (GUILayout.Button(LocalizationManager.GetText("Common_refresh"), EditorStyles.toolbarButton))
                {
                    RefreshAssetList();
                }
            }
        }

        private void DrawMainContent()
        {
            using (new GUILayout.HorizontalScope())
            {
                DrawLeftPanel();
                DrawResizeHandle();
                DrawRightPanel();
            }
        }

        private void DrawLeftPanel()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(_leftPanelWidth)))
            {
                GUILayout.Label("Asset Types", EditorStyles.boldLabel);
                using (var scrollView = new GUILayout.ScrollViewScope(_leftScrollPosition, GUILayout.ExpandHeight(true)))
                {
                    _leftScrollPosition = scrollView.scrollPosition;

                    foreach (AssetType assetType in Enum.GetValues(typeof(AssetType)))
                    {
                        bool isSelected = _selectedAssetType == assetType;
                        var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                        
                        if (GUILayout.Button(assetType.ToString(), style))
                        {
                            _selectedAssetType = assetType;
                            RefreshAssetList();
                        }
                    }
                }
            }
        }

        private void DrawResizeHandle()
        {
            _resizeRect = new Rect(_leftPanelWidth, 0, 5, position.height);
            EditorGUIUtility.AddCursorRect(_resizeRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && _resizeRect.Contains(Event.current.mousePosition))
            {
                _isResizing = true;
            }

            if (_isResizing)
            {
                _leftPanelWidth = Event.current.mousePosition.x;
                _leftPanelWidth = Mathf.Clamp(_leftPanelWidth, 150, position.width - 400);
                Repaint();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                _isResizing = false;
            }
        }

        private void DrawRightPanel()
        {
            using (new GUILayout.VerticalScope())
            {
                DrawAssetGrid();
            }
        }

        private void DrawAssetGrid()
        {
            if (_filteredAssets == null || _filteredAssets.Count == 0)
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(LocalizationManager.GetText("AssetManager_noAssets"), EditorStyles.largeLabel);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                return;
            }

            using (var scrollView = new GUILayout.ScrollViewScope(_rightScrollPosition))
            {
                _rightScrollPosition = scrollView.scrollPosition;

                float availableWidth = position.width - _leftPanelWidth - 20;
                int columnsPerRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (_thumbnailSize + 10)));

                for (int i = 0; i < _filteredAssets.Count; i += columnsPerRow)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (int j = 0; j < columnsPerRow && i + j < _filteredAssets.Count; j++)
                        {
                            DrawAssetItem(_filteredAssets[i + j]);
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        private void DrawAssetItem(AssetInfo asset)
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(_thumbnailSize + 10)))
            {
                // Thumbnail
                var thumbnailRect = GUILayoutUtility.GetRect(_thumbnailSize, _thumbnailSize);
                var thumbnail = _thumbnailManager.GetThumbnail(asset);
                
                bool isSelected = _selectedAsset == asset;
                if (isSelected)
                {
                    EditorGUI.DrawRect(thumbnailRect, new Color(0.3f, 0.5f, 1f, 0.3f));
                }

                // Show hidden overlay
                if (asset.isHidden)
                {
                    EditorGUI.DrawRect(thumbnailRect, new Color(0f, 0f, 0f, 0.5f));
                }

                if (thumbnail != null)
                {
                    GUI.DrawTexture(thumbnailRect, thumbnail, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Box(thumbnailRect, "No Image");
                }

                // Favorite indicator
                if (asset.isFavorite)
                {
                    var starRect = new Rect(thumbnailRect.x + thumbnailRect.width - 20, thumbnailRect.y + 5, 15, 15);
                    GUI.Label(starRect, "★");
                }

                // Hidden indicator
                if (asset.isHidden)
                {
                    var hiddenRect = new Rect(thumbnailRect.x + 5, thumbnailRect.y + 5, 15, 15);
                    var oldColor = GUI.color;
                    GUI.color = Color.red;
                    GUI.Label(hiddenRect, "👁");
                    GUI.color = oldColor;
                }

                // Asset name
                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 10
                };
                
                GUILayout.Label(asset.name, nameStyle, GUILayout.Height(30));

                // Handle click events
                if (Event.current.type == EventType.MouseDown && thumbnailRect.Contains(Event.current.mousePosition))
                {
                    _selectedAsset = asset;
                    
                    if (Event.current.clickCount == 2)
                    {
                        // Double click - open details
                        AssetDetailWindow.ShowWindow(asset);
                    }
                    else if (Event.current.button == 1)
                    {
                        // Right click - context menu
                        ShowContextMenu(asset);
                    }
                    
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        private void ShowContextMenu(AssetInfo asset)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_viewDetails")), false, () => {
                AssetDetailWindow.ShowWindow(asset);
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_editAsset")), false, () => {
                AssetDetailWindow.ShowWindow(asset, true);
            });
            
            menu.AddSeparator("");
            
            string favoriteText = asset.isFavorite ? 
                LocalizationManager.GetText("AssetManager_removeFromFavorites") : 
                LocalizationManager.GetText("AssetManager_addToFavorites");
            
            menu.AddItem(new GUIContent(favoriteText), false, () => {
                asset.isFavorite = !asset.isFavorite;
                _dataManager.UpdateAsset(asset);
            });
            
            menu.AddSeparator("");
            
            string hiddenText = asset.isHidden ? 
                LocalizationManager.GetText("AssetManager_showAsset") : 
                LocalizationManager.GetText("AssetManager_hideAsset");
            
            menu.AddItem(new GUIContent(hiddenText), false, () => {
                asset.isHidden = !asset.isHidden;
                _dataManager.UpdateAsset(asset);
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_openLocation")), false, () => {
                _fileManager.OpenFileLocation(asset);
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_deleteAsset")), false, () => {
                if (EditorUtility.DisplayDialog("Confirm Delete", 
                    LocalizationManager.GetText("AssetManager_confirmDelete"), 
                    LocalizationManager.GetText("Common_yes"), 
                    LocalizationManager.GetText("Common_no")))
                {
                    _dataManager.RemoveAsset(asset.uid);
                }
            });
            
            menu.ShowAsContext();
        }

        private void ShowAddAssetDialog()
        {
            string path = EditorUtility.OpenFilePanel("Select Asset File", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                var asset = _fileManager.CreateAssetFromFile(path);
                if (asset != null)
                {
                    _dataManager.AddAsset(asset);
                    AssetDetailWindow.ShowWindow(asset, true);
                }
            }
        }

        private void RefreshAssetList()
        {
            if (_dataManager?.Library?.assets == null)
            {
                _filteredAssets = new List<AssetInfo>();
                return;
            }

            _filteredAssets = _dataManager.SearchAssets(_searchText, _selectedAssetType, _showFavoritesOnly ? true : (bool?)null, _showHidden);

            // Sort assets
            switch (_selectedSortOption)
            {
                case 0: // Name
                    _filteredAssets = _sortDescending ? 
                        _filteredAssets.OrderByDescending(a => a.name).ToList() : 
                        _filteredAssets.OrderBy(a => a.name).ToList();
                    break;
                case 1: // Date
                    _filteredAssets = _sortDescending ? 
                        _filteredAssets.OrderByDescending(a => a.createdDate).ToList() : 
                        _filteredAssets.OrderBy(a => a.createdDate).ToList();
                    break;
                case 2: // Size
                    _filteredAssets = _sortDescending ? 
                        _filteredAssets.OrderByDescending(a => a.fileSize).ToList() : 
                        _filteredAssets.OrderBy(a => a.fileSize).ToList();
                    break;
            }

            Repaint();
        }

        private void HandleEvents()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Delete && _selectedAsset != null)
                {
                    if (EditorUtility.DisplayDialog("Confirm Delete", 
                        LocalizationManager.GetText("AssetManager_confirmDelete"), 
                        LocalizationManager.GetText("Common_yes"), 
                        LocalizationManager.GetText("Common_no")))
                    {
                        _dataManager.RemoveAsset(_selectedAsset.uid);
                        _selectedAsset = null;
                    }
                    Event.current.Use();
                }
            }
        }

        private void OnThumbnailSaved(AssetInfo asset)
        {
            if (asset != null && _dataManager != null)
            {
                _dataManager.UpdateAsset(asset);
            }
        }
    }
}
