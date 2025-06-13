using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Editor.Core.Helper;
using AMU.Data.Lang;
using AMU.Data.TagType;

namespace AMU.AssetManager.UI
{
    public enum AssetFilterType
    {
        All,
        Favorites,
        ArchivedOnly
    }

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
        }        // Managers
        private AssetDataManager _dataManager;
        private AssetThumbnailManager _thumbnailManager;
        private AssetFileManager _fileManager;

        // UI State
        private Vector2 _leftScrollPosition = Vector2.zero;
        private Vector2 _rightScrollPosition = Vector2.zero;
        private string _searchText = "";
        private string _selectedAssetType = "Avatar";
        private AssetFilterType _currentFilter = AssetFilterType.All;
        private int _selectedSortOption = 1;
        private bool _sortDescending = true;        // Data synchronization
        private bool _needsDataReload = false;  // データファイルの再読み込みが必要
        private bool _needsUIRefresh = false;   // UIの再描画のみ必要
        private double _lastDataCheckTime = 0;
        private bool _isLoadingTypeChange = false;

        // Type Management
        private string _newTypeName = "";

        // Layout
        private float _leftPanelWidth = 250f;

        // Asset Grid
        private float _thumbnailSize = 100f;
        private List<AssetInfo> _filteredAssets = new List<AssetInfo>();
        private AssetInfo _selectedAsset;

        // UI Styles
        private GUIStyle _typeButtonStyle;
        private GUIStyle _selectedTypeButtonStyle;
        private GUIStyle _typeHeaderStyle;
        private bool _stylesInitialized = false; private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            AssetTypeManager.LoadCustomTypes();
            InitializeManagers();            // 初回データ読み込み
            _needsUIRefresh = true;
            CheckAndRefreshData();

            // TagTypeManagerとの統合初期化
            InitializeTagTypeIntegration();
        }
        private void InitializeManagers()
        {
            if (_dataManager == null)
            {
                _dataManager = new AssetDataManager();
                _dataManager.OnDataLoaded += OnDataLoaded;
                _dataManager.OnDataChanged += OnDataChanged;
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
        private void OnDataLoaded()
        {
            _needsUIRefresh = true;
            Repaint();
        }
        private void OnDataChanged()
        {
            // 他のウィンドウからのデータ変更通知時は、データは既に最新なのでUI更新のみ
            _needsUIRefresh = true;
            Repaint();
        }
        private void InitializeTagTypeIntegration()
        {
            // TagTypeManagerからのデータ変更通知を受け取る
            TagTypeManager.OnDataChanged += OnTagTypeDataChanged;
        }
        private void OnDisable()
        {
            _thumbnailManager?.ClearCache();

            // イベントハンドラーの解除
            TagTypeManager.OnDataChanged -= OnTagTypeDataChanged; if (_dataManager != null)
            {
                _dataManager.OnDataLoaded -= OnDataLoaded;
                _dataManager.OnDataChanged -= OnDataChanged;
                _dataManager.Dispose();
            }

            if (_thumbnailManager != null)
            {
                _thumbnailManager.OnThumbnailLoaded -= Repaint;
                _thumbnailManager.OnThumbnailSaved -= OnThumbnailSaved;
            }
        }
        private void OnTagTypeDataChanged()
        {
            // タイプやタグが変更された時の処理
            _needsUIRefresh = true;
            Repaint();
        }
        private void OnGUI()
        {
            InitializeStyles();            // 定期的に外部ファイル変更をチェック
            CheckForExternalFileChanges();

            // データの再読み込みが必要な場合のみ実行
            CheckAndRefreshData();

            // UI更新が必要な場合はアセットリストを更新
            if (_needsUIRefresh)
            {
                RefreshAssetList();
                _needsUIRefresh = false;
            }

            if (_dataManager?.IsLoading == true)
            {
                DrawLoadingUI();
                return;
            }

            DrawToolbar();
            DrawMainContent();
            HandleEvents();
        }/// <summary>
         /// データの外部変更をチェックして必要に応じてリフレッシュ
         /// データファイルの再読み込みが必要な場合のみ実行
         /// </summary>
        private void CheckAndRefreshData()
        {            // データの再読み込みが必要な場合のみチェック
            if (!_needsDataReload) return;

            // 即座にデータ再読み込みを実行
            _dataManager?.CheckForExternalChanges();
            _needsDataReload = false;
            _needsUIRefresh = true;
        }

        /// <summary>
        /// 定期的に外部ファイル変更をチェックし、必要に応じてデータ再読み込みフラグを設定
        /// </summary>
        private void CheckForExternalFileChanges()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            // チェック間隔を2秒に延長してパフォーマンスを向上
            if (currentTime - _lastDataCheckTime > 2.0f)
            {
                _lastDataCheckTime = currentTime;

                // データマネージャーで外部変更をチェック
                if (_dataManager?.CheckForExternalChanges() == true)
                {
                    _needsUIRefresh = true;
                }
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Type header style
            _typeHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 8, 8)
            };            // Type button style (unselected)
            _typeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 6, 6),
                margin = new RectOffset(2, 2, 2, 2),
                fixedHeight = 32,
                normal = { background = null, textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                hover = { background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn hover.png") as Texture2D }
            };

            // Type button style (selected)
            _selectedTypeButtonStyle = new GUIStyle(_typeButtonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = {
                    background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn on.png") as Texture2D,
                    textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue
                }
            };

            _stylesInitialized = true;
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
                // Search field area - same width as left panel
                using (new GUILayout.HorizontalScope(GUILayout.Width(_leftPanelWidth)))
                {
                    var newSearchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
                    if (newSearchText != _searchText)
                    {
                        _searchText = newSearchText;
                        _needsUIRefresh = true;
                    }
                }

                // Right panel area - starts immediately after left panel
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.All, LocalizationManager.GetText("AssetManager_filterAll"), EditorStyles.toolbarButton))
                    {
                        if (_currentFilter != AssetFilterType.All)
                        {
                            _currentFilter = AssetFilterType.All;
                            _needsUIRefresh = true;
                            _isLoadingTypeChange = true;
                        }
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.Favorites, LocalizationManager.GetText("AssetManager_filterFavorite"), EditorStyles.toolbarButton))
                    {
                        if (_currentFilter != AssetFilterType.Favorites)
                        {
                            _currentFilter = AssetFilterType.Favorites;
                            _needsUIRefresh = true;
                            _isLoadingTypeChange = true;
                        }
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.ArchivedOnly, LocalizationManager.GetText("AssetManager_filterArchived"), EditorStyles.toolbarButton))
                    {
                        if (_currentFilter != AssetFilterType.ArchivedOnly)
                        {
                            _currentFilter = AssetFilterType.ArchivedOnly;
                            _needsUIRefresh = true;
                            _isLoadingTypeChange = true;
                        }
                    }
                    GUILayout.Space(10);

                    GUILayout.FlexibleSpace();

                    // Sort options
                    string[] sortOptions = {
                        LocalizationManager.GetText("AssetManager_sortName"),
                        LocalizationManager.GetText("AssetManager_sortDate"),
                        LocalizationManager.GetText("AssetManager_sortSize")
                    }; var newSortOption = EditorGUILayout.Popup(_selectedSortOption, sortOptions, EditorStyles.toolbarPopup, GUILayout.Width(100));
                    if (newSortOption != _selectedSortOption)
                    {
                        _selectedSortOption = newSortOption;
                        _needsUIRefresh = true;
                    }

                    string sortArrow = _sortDescending ? "↓" : "↑";
                    var newSortDescending = GUILayout.Toggle(_sortDescending, sortArrow, EditorStyles.toolbarButton, GUILayout.Width(25));
                    if (newSortDescending != _sortDescending)
                    {
                        _sortDescending = newSortDescending;
                        _needsUIRefresh = true;
                    }

                    GUILayout.Space(10);

                    if (GUILayout.Button(LocalizationManager.GetText("AssetManager_addAsset"), EditorStyles.toolbarButton))
                    {
                        ShowAddAssetDialog();
                    }
                    if (GUILayout.Button(LocalizationManager.GetText("Common_refresh"), EditorStyles.toolbarButton))
                    {
                        _needsDataReload = true;
                        _dataManager?.CheckForExternalChanges();
                    }
                }
            }
        }

        private void DrawMainContent()
        {
            using (new GUILayout.HorizontalScope())
            {
                DrawLeftPanel();
                DrawRightPanel();
            }
        }
        private void DrawLeftPanel()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(_leftPanelWidth)))
            {
                // Header with improved style
                GUILayout.Label("Asset Types", _typeHeaderStyle);
                GUILayout.Space(5);

                // Asset types list with scroll view
                using (var scrollView = new GUILayout.ScrollViewScope(_leftScrollPosition, GUILayout.ExpandHeight(true)))
                {
                    _leftScrollPosition = scrollView.scrollPosition;

                    // All types button with improved style
                    bool isAllSelected = string.IsNullOrEmpty(_selectedAssetType);
                    var allStyle = isAllSelected ? _selectedTypeButtonStyle : _typeButtonStyle; if (GUILayout.Button(LocalizationManager.GetText("AssetType_all"), allStyle))
                    {
                        if (_selectedAssetType != "")
                        {
                            _selectedAssetType = "";
                            _isLoadingTypeChange = true;
                            _needsUIRefresh = true;
                            Repaint();
                        }
                    }

                    GUILayout.Space(8);

                    // Individual type buttons with improved styles
                    foreach (var assetType in AssetTypeManager.AllTypes)
                    {
                        bool isSelected = _selectedAssetType == assetType;
                        var style = isSelected ? _selectedTypeButtonStyle : _typeButtonStyle;

                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(assetType, style, GUILayout.ExpandWidth(true)))
                            {
                                if (_selectedAssetType != assetType)
                                {
                                    _selectedAssetType = assetType;
                                    _isLoadingTypeChange = true;
                                    _needsUIRefresh = true;
                                    Repaint();
                                }
                            }

                            // Show delete button for custom types
                            if (!AssetTypeManager.IsDefaultType(assetType))
                            {
                                var deleteButtonStyle = new GUIStyle(GUI.skin.button)
                                {
                                    fontSize = 12,
                                    fontStyle = FontStyle.Bold,
                                    fixedWidth = 24,
                                    fixedHeight = 24,
                                    normal = { textColor = Color.red }
                                };

                                if (GUILayout.Button("×", deleteButtonStyle))
                                {
                                    if (EditorUtility.DisplayDialog(
                                        LocalizationManager.GetText("AssetType_confirmDeleteTitle"),
                                        string.Format(LocalizationManager.GetText("AssetType_confirmDeleteMessage"), assetType),
                                        LocalizationManager.GetText("Common_yes"),
                                        LocalizationManager.GetText("Common_no")))
                                    {
                                        AssetTypeManager.RemoveCustomType(assetType);
                                        if (_selectedAssetType == assetType)
                                        {
                                            _selectedAssetType = "";
                                        }
                                        _needsUIRefresh = true;
                                    }
                                }
                            }
                        }

                        GUILayout.Space(2);
                    }

                    GUILayout.Space(10);

                    // Add new type form at the bottom
                    DrawAddTypeForm();
                }
            }
        }
        private void DrawAddTypeForm()
        {
            // Separator line
            var rect = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.7f));

            GUILayout.Space(8);

            // Add new type section with improved styling
            using (new GUILayout.VerticalScope())
            {
                var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                GUILayout.Label(LocalizationManager.GetText("AssetType_addNewType"), labelStyle);

                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    var textFieldStyle = new GUIStyle(EditorStyles.textField)
                    {
                        fontSize = 12,
                        fixedHeight = 24
                    };
                    _newTypeName = EditorGUILayout.TextField(_newTypeName, textFieldStyle, GUILayout.ExpandWidth(true));

                    GUI.enabled = !string.IsNullOrWhiteSpace(_newTypeName) &&
                                  !AssetTypeManager.AllTypes.Contains(_newTypeName.Trim());

                    var addButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 14,
                        fontStyle = FontStyle.Bold,
                        fixedWidth = 30,
                        fixedHeight = 24
                    }; if (GUILayout.Button("+", addButtonStyle))
                    {
                        AssetTypeManager.AddCustomType(_newTypeName.Trim());
                        _newTypeName = "";
                        _needsUIRefresh = true;
                    }

                    GUI.enabled = true;
                }

                // Show validation message if needed
                if (!string.IsNullOrWhiteSpace(_newTypeName))
                {
                    var trimmedName = _newTypeName.Trim();
                    var messageStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        normal = { textColor = Color.red }
                    };

                    if (AssetTypeManager.AllTypes.Contains(trimmedName))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5);
                            GUILayout.Label(LocalizationManager.GetText("AssetType_typeAlreadyExists"), messageStyle);
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(trimmedName))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5);
                            GUILayout.Label(LocalizationManager.GetText("AssetType_typeNameRequired"), messageStyle);
                        }
                    }
                }
            }
        }
        private void DrawRightPanel()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f); // 薄いグレー色

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = originalColor;
                DrawAssetGrid();
            }
        }
        private void DrawAssetGrid()
        {
            // Type切り替え中の読み込み表示
            if (_isLoadingTypeChange)
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(LocalizationManager.GetText("AssetManager_loading"), EditorStyles.largeLabel);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
                return;
            }

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

                // 仮想化：表示範囲内のアイテムのみ描画
                float itemHeight = _thumbnailSize + 40; // サムネイル + テキストの高さ
                float scrollAreaHeight = position.height - 100; // ツールバーなどを除く

                int visibleRows = Mathf.CeilToInt(scrollAreaHeight / itemHeight) + 2; // バッファ行を追加
                int totalRows = Mathf.CeilToInt((float)_filteredAssets.Count / columnsPerRow);

                int startRow = Mathf.Max(0, Mathf.FloorToInt(_rightScrollPosition.y / itemHeight) - 1);
                int endRow = Mathf.Min(totalRows, startRow + visibleRows);

                // 上部の空白スペース
                if (startRow > 0)
                {
                    GUILayout.Space(startRow * itemHeight);
                }

                // 表示範囲内のアイテムを描画
                for (int row = startRow; row < endRow; row++)
                {
                    int startIndex = row * columnsPerRow;
                    if (startIndex >= _filteredAssets.Count) break;

                    using (new GUILayout.HorizontalScope())
                    {
                        for (int j = 0; j < columnsPerRow && startIndex + j < _filteredAssets.Count; j++)
                        {
                            DrawAssetItem(_filteredAssets[startIndex + j]);
                        }
                        GUILayout.FlexibleSpace();
                    }
                }

                // 下部の空白スペース
                int remainingRows = totalRows - endRow;
                if (remainingRows > 0)
                {
                    GUILayout.Space(remainingRows * itemHeight);
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

                // Show archived overlay
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

                // Archived indicator
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

            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_viewDetails")), false, () =>
            {
                AssetDetailWindow.ShowWindow(asset);
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_editAsset")), false, () =>
            {
                AssetDetailWindow.ShowWindow(asset, true);
            });

            menu.AddSeparator("");

            string favoriteText = asset.isFavorite ?
                LocalizationManager.GetText("AssetManager_removeFromFavorites") :
                LocalizationManager.GetText("AssetManager_addToFavorites"); menu.AddItem(new GUIContent(favoriteText), false, () =>
 {
     asset.isFavorite = !asset.isFavorite;
     _dataManager.UpdateAsset(asset);
     _needsDataReload = true;
 });

            menu.AddSeparator("");

            string hiddenText = asset.isHidden ?
                LocalizationManager.GetText("AssetManager_showAsset") :
                LocalizationManager.GetText("AssetManager_hideAsset"); menu.AddItem(new GUIContent(hiddenText), false, () =>
 {
     asset.isHidden = !asset.isHidden;
     _dataManager.UpdateAsset(asset);
     _needsDataReload = true;
 });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_openLocation")), false, () =>
            {
                _fileManager.OpenFileLocation(asset);
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_deleteAsset")), false, () =>
            {
                if (EditorUtility.DisplayDialog("Confirm Delete",
                    LocalizationManager.GetText("AssetManager_confirmDelete"),
                    LocalizationManager.GetText("Common_yes"), LocalizationManager.GetText("Common_no")))
                {
                    _dataManager.RemoveAsset(asset.uid);
                    _needsDataReload = true;
                }
            });

            menu.ShowAsContext();
        }

        private void ShowAddAssetDialog()
        {
            string path = EditorUtility.OpenFilePanel("Select Asset File", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                var asset = _fileManager.CreateAssetFromFile(path); if (asset != null)
                {
                    _dataManager.AddAsset(asset);
                    AssetDetailWindow.ShowWindow(asset, true);
                    _needsDataReload = true;
                }
            }
        }
        private void RefreshAssetList()
        {
            if (_dataManager?.Library?.assets == null)
            {
                _filteredAssets = new List<AssetInfo>();
                _isLoadingTypeChange = false;
                return;
            }

            // Apply filter based on current filter type
            bool? favoritesOnly = null;
            bool? archivedOnly = null;
            bool showHidden = false; switch (_currentFilter)
            {
                case AssetFilterType.Favorites:
                    favoritesOnly = true;
                    showHidden = false; // Only show non-archived favorites
                    break;
                case AssetFilterType.ArchivedOnly:
                    archivedOnly = true;
                    showHidden = true;
                    break;
                case AssetFilterType.All:
                default:
                    showHidden = false; // Only show non-archived items
                    break;
            }

            _filteredAssets = _dataManager.SearchAssets(_searchText, _selectedAssetType, favoritesOnly, showHidden, archivedOnly);

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

            _isLoadingTypeChange = false;
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
                        _needsDataReload = true;
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
