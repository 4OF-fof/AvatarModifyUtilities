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

            // ウィンドウ表示前にライブラリファイルの存在を確保
            AssetDataManager.Instance.EnsureLibraryFileExists();

            var window = GetWindow<AssetManagerWindow>(LocalizationManager.GetText("AssetManager_windowTitle"));
            window.minSize = new Vector2(1100, 700);
            window.maxSize = new Vector2(1100, 700);
            window.Show();
        }

        // Managers - シングルトンを使用
        private AssetDataManager _dataManager;
        private AssetThumbnailManager _thumbnailManager;
        private AssetFileManager _fileManager;        // UI State
        private Vector2 _leftScrollPosition = Vector2.zero;
        private Vector2 _rightScrollPosition = Vector2.zero;
        private string _searchText = "";
        private string _selectedAssetType = "Avatar";
        private AssetFilterType _currentFilter = AssetFilterType.All;
        private int _selectedSortOption = 1;
        private bool _sortDescending = true;

        // Advanced Search
        private AdvancedSearchCriteria _advancedSearchCriteria = null;
        private bool _isUsingAdvancedSearch = false;

        // データ管理の簡素化
        private bool _needsUIRefresh = false;   // UIの再描画のみ必要

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
            InitializeManagers();

            _needsUIRefresh = true;

            // TagTypeManagerとの統合初期化
            InitializeTagTypeIntegration();
        }
        private void InitializeManagers()
        {
            // シングルトンインスタンスを取得し初期化
            _dataManager = AssetDataManager.Instance;
            _dataManager.Initialize(); // 明示的に初期化を実行
            _dataManager.OnDataLoaded += OnDataLoaded;
            _dataManager.OnDataChanged += OnDataChanged;            // シングルトンインスタンスを使用
            _thumbnailManager = AssetThumbnailManager.Instance;
            _thumbnailManager.OnThumbnailLoaded += Repaint;
            _thumbnailManager.OnThumbnailSaved += OnThumbnailSaved;
            _thumbnailManager.OnThumbnailUpdated += OnThumbnailUpdated;

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
            TagTypeManager.OnDataChanged -= OnTagTypeDataChanged;

            if (_dataManager != null)
            {
                _dataManager.OnDataLoaded -= OnDataLoaded;
                _dataManager.OnDataChanged -= OnDataChanged;
                // シングルトンなのでDisposeは呼ばない
            }
            if (_thumbnailManager != null)
            {
                _thumbnailManager.OnThumbnailLoaded -= Repaint;
                _thumbnailManager.OnThumbnailSaved -= OnThumbnailSaved;
                _thumbnailManager.OnThumbnailUpdated -= OnThumbnailUpdated;
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
            InitializeStyles();

            // データの読み込みが完了していない場合の処理
            if (_dataManager?.IsLoading == true)
            {
                DrawLoadingUI();
                return;
            }

            // UI更新が必要な場合はアセットリストを更新
            if (_needsUIRefresh)
            {
                RefreshAssetList();
                _needsUIRefresh = false;
            }

            DrawToolbar();
            DrawMainContent();
            HandleEvents();
        }

        /// <summary>
        /// 明示的なデータリフレッシュ（ユーザーが更新ボタンを押した時のみ）
        /// </summary>
        private void RefreshData()
        {
            _dataManager?.ForceRefresh();
            _needsUIRefresh = true;
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
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(2, 2, 1, 1),
                fixedHeight = 36,
                normal = {
                    background = null,
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                },
                hover = {
                    background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.5f)),
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
                }
            };

            // Type button style (selected)
            _selectedTypeButtonStyle = new GUIStyle(_typeButtonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = {
                    background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.90f, 0.8f) : new Color(0.24f, 0.48f, 0.90f, 0.6f)),
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.white
                },
                hover = {
                    background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.90f, 0.9f) : new Color(0.24f, 0.48f, 0.90f, 0.7f)),
                    textColor = EditorGUIUtility.isProSkin ? Color.white : Color.white
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
                        _isUsingAdvancedSearch = false; // 通常検索に切り替え
                        _advancedSearchCriteria = null;
                        _needsUIRefresh = true;
                    }

                    // 詳細検索ボタン
                    var advancedSearchButtonStyle = _isUsingAdvancedSearch
                        ? new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold }
                        : EditorStyles.toolbarButton;
                    if (GUILayout.Button("詳細", advancedSearchButtonStyle, GUILayout.Width(40)))
                    {
                        ShowAdvancedSearchWindow();
                    }

                    // 詳細検索の状態表示
                    if (_isUsingAdvancedSearch)
                    {
                        var statusText = GetAdvancedSearchStatusText();
                        var statusStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.3f, 0.6f, 1f) }
                        };
                        GUILayout.Label(statusText, statusStyle, GUILayout.Width(150));

                        // クリアボタン
                        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
                        {
                            ClearAdvancedSearch();
                        }
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
                        }
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.Favorites, LocalizationManager.GetText("AssetManager_filterFavorite"), EditorStyles.toolbarButton))
                    {
                        if (_currentFilter != AssetFilterType.Favorites)
                        {
                            _currentFilter = AssetFilterType.Favorites;
                            _needsUIRefresh = true;
                        }
                    }

                    if (GUILayout.Toggle(_currentFilter == AssetFilterType.ArchivedOnly, LocalizationManager.GetText("AssetManager_filterArchived"), EditorStyles.toolbarButton))
                    {
                        if (_currentFilter != AssetFilterType.ArchivedOnly)
                        {
                            _currentFilter = AssetFilterType.ArchivedOnly;
                            _needsUIRefresh = true;
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
                        RefreshData();
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
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Asset Types", _typeHeaderStyle);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(5);

                // Asset types list with scroll view
                using (var scrollView = new GUILayout.ScrollViewScope(_leftScrollPosition, GUILayout.ExpandHeight(true)))
                {
                    _leftScrollPosition = scrollView.scrollPosition;                    // All types button with improved style
                    bool isAllSelected = string.IsNullOrEmpty(_selectedAssetType);
                    bool allPressed = GUILayout.Toggle(isAllSelected, LocalizationManager.GetText("AssetType_all"), _typeButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(36));

                    if (allPressed && !isAllSelected)
                    {
                        _selectedAssetType = "";
                        _needsUIRefresh = true;
                        Repaint();
                    }

                    GUILayout.Space(8);                    // Individual type buttons with improved styles
                    foreach (var assetType in AssetTypeManager.AllTypes)
                    {
                        bool isSelected = _selectedAssetType == assetType;

                        using (new GUILayout.HorizontalScope())
                        {
                            bool pressed = GUILayout.Toggle(isSelected, assetType, _typeButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(36));

                            if (pressed && !isSelected)
                            {
                                _selectedAssetType = assetType;
                                _needsUIRefresh = true;
                                Repaint();
                            }

                            // Show delete button for custom types
                            if (!AssetTypeManager.IsDefaultType(assetType))
                            {
                                var deleteButtonStyle = new GUIStyle(GUI.skin.button)
                                {
                                    fontSize = 12,
                                    fontStyle = FontStyle.Bold,
                                    fixedWidth = 24,
                                    fixedHeight = 36,
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

                // 改善された仮想化：表示範囲内のアイテムのみ描画
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

                // サムネイルの取得を遅延させる（可視範囲内の場合のみ）
                Texture2D thumbnail = null;
                if (IsRectVisible(thumbnailRect))
                {
                    thumbnail = _thumbnailManager.GetThumbnail(asset);
                }

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
                    // デフォルトアイコンまたはプレースホルダーを表示
                    var defaultIcon = GetDefaultIcon(asset.assetType);
                    if (defaultIcon != null)
                    {
                        GUI.DrawTexture(thumbnailRect, defaultIcon, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        GUI.Box(thumbnailRect, "No Image");
                    }
                }

                // インジケーターの描画を最適化
                DrawAssetIndicators(asset, thumbnailRect);

                // Asset name
                DrawAssetName(asset);

                // Handle click events
                HandleAssetItemEvents(asset, thumbnailRect);
            }
        }

        /// <summary>
        /// 矩形が現在表示されている範囲内にあるかチェック
        /// </summary>
        private bool IsRectVisible(Rect rect)
        {
            var scrollViewRect = new Rect(0, _rightScrollPosition.y, position.width, position.height);
            return rect.Overlaps(scrollViewRect);
        }

        /// <summary>
        /// アセットタイプに応じたデフォルトアイコンを取得
        /// </summary>
        private Texture2D GetDefaultIcon(string assetType)
        {
            switch (assetType)
            {
                case "Avatar":
                case "Prefab":
                    return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                case "Material":
                    return EditorGUIUtility.IconContent("Material Icon").image as Texture2D;
                case "Texture":
                    return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
                case "Animation":
                    return EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
                case "Script":
                    return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                default:
                    return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
            }
        }

        /// <summary>
        /// アセットのインジケーター（お気に入り、非表示など）を描画
        /// </summary>
        private void DrawAssetIndicators(AssetInfo asset, Rect thumbnailRect)
        {
            // Favorite indicator
            if (asset.isFavorite)
            {
                DrawFavoriteIndicator(thumbnailRect);
            }

            // Archived indicator
            if (asset.isHidden)
            {
                DrawArchivedIndicator(thumbnailRect);
            }
        }

        /// <summary>
        /// お気に入りインジケーターを描画
        /// </summary>
        private void DrawFavoriteIndicator(Rect thumbnailRect)
        {
            var starSize = 25f;
            var starRect = new Rect(thumbnailRect.x + thumbnailRect.width - starSize - 3, thumbnailRect.y + 3, starSize, starSize);

            var originalColor = GUI.color;
            var starStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(starSize * 0.8f),
                alignment = TextAnchor.MiddleCenter
            };

            // 黒い縁取りを描画（最適化）
            GUI.color = Color.black;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var outlineRect = new Rect(starRect.x + x, starRect.y + y, starRect.width, starRect.height);
                    GUI.Label(outlineRect, "★", starStyle);
                }
            }

            // メインの星を描画
            GUI.color = Color.yellow;
            GUI.Label(starRect, "★", starStyle);

            GUI.color = originalColor;
        }

        /// <summary>
        /// アーカイブインジケーターを描画
        /// </summary>
        private void DrawArchivedIndicator(Rect thumbnailRect)
        {
            var hiddenRect = new Rect(thumbnailRect.x + 5, thumbnailRect.y + 5, 15, 15);
            var oldColor = GUI.color;
            GUI.color = Color.red;
            GUI.Label(hiddenRect, "👁");
            GUI.color = oldColor;
        }

        /// <summary>
        /// アセット名を描画
        /// </summary>
        private void DrawAssetName(AssetInfo asset)
        {
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                fontSize = 10
            };

            GUILayout.Label(asset.name, nameStyle, GUILayout.Height(30));
        }

        /// <summary>
        /// アセットアイテムのイベントを処理
        /// </summary>
        private void HandleAssetItemEvents(AssetInfo asset, Rect thumbnailRect)
        {
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
     _needsUIRefresh = true;
 });

            menu.AddSeparator("");

            string hiddenText = asset.isHidden ?
                LocalizationManager.GetText("AssetManager_showAsset") :
                LocalizationManager.GetText("AssetManager_hideAsset"); menu.AddItem(new GUIContent(hiddenText), false, () =>
 {
     asset.isHidden = !asset.isHidden;
     _dataManager.UpdateAsset(asset);
     _needsUIRefresh = true;
 });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_openLocation")), false, () =>
            {
                _fileManager.OpenFileLocation(asset);
            });

            menu.AddSeparator(""); menu.AddItem(new GUIContent(LocalizationManager.GetText("AssetManager_deleteAsset")), false, () =>
 {
     if (EditorUtility.DisplayDialog("Confirm Delete",
         LocalizationManager.GetText("AssetManager_confirmDelete"),
         LocalizationManager.GetText("Common_yes"), LocalizationManager.GetText("Common_no")))
     {
         _dataManager.RemoveAsset(asset.uid);
         _needsUIRefresh = true;
     }
 });

            menu.ShowAsContext();
        }

        private void ShowAddAssetDialog()
        {
            string downloadPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
            string path = EditorUtility.OpenFilePanel("Select Asset File", downloadPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                var asset = _fileManager.CreateAssetFromFile(path); if (asset != null)
                {
                    _dataManager.AddAsset(asset);
                    AssetDetailWindow.ShowWindow(asset, true);
                    _needsUIRefresh = true;
                }
            }
        }
        /// <summary>
        /// 高速化されたアセットリスト更新
        /// </summary>
        private void RefreshAssetList()
        {
            if (_dataManager?.Library?.assets == null)
            {
                _filteredAssets = new List<AssetInfo>();
                return;
            }

            // フィルター条件を設定
            bool? favoritesOnly = null;
            bool? archivedOnly = null;
            bool showHidden = false;

            switch (_currentFilter)
            {
                case AssetFilterType.Favorites:
                    favoritesOnly = true;
                    showHidden = false;
                    break;
                case AssetFilterType.ArchivedOnly:
                    archivedOnly = true;
                    showHidden = true;
                    break;
                case AssetFilterType.All:
                default:
                    showHidden = false;
                    break;
            }

            // 詳細検索または通常検索を実行
            if (_isUsingAdvancedSearch && _advancedSearchCriteria != null)
            {
                _filteredAssets = _dataManager.AdvancedSearchAssets(_advancedSearchCriteria, _selectedAssetType, favoritesOnly, showHidden, archivedOnly);
            }
            else
            {
                _filteredAssets = _dataManager.SearchAssets(_searchText, _selectedAssetType, favoritesOnly, showHidden, archivedOnly);
            }

            // ソート処理の最適化
            ApplySorting();

            Repaint();
        }

        /// <summary>
        /// ソート処理を分離して高速化
        /// </summary>
        private void ApplySorting()
        {
            switch (_selectedSortOption)
            {
                case 0: // Name
                    _filteredAssets.Sort((a, b) => _sortDescending ?
                        string.Compare(b.name, a.name, StringComparison.OrdinalIgnoreCase) :
                        string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
                    break;
                case 1: // Date
                    _filteredAssets.Sort((a, b) => _sortDescending ?
                        DateTime.Compare(b.createdDate, a.createdDate) :
                        DateTime.Compare(a.createdDate, b.createdDate));
                    break;
                case 2: // Size
                    _filteredAssets.Sort((a, b) => _sortDescending ?
                        b.fileSize.CompareTo(a.fileSize) :
                        a.fileSize.CompareTo(b.fileSize));
                    break;
            }
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
                        _needsUIRefresh = true;
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

        private void OnThumbnailUpdated(string assetUid)
        {
            // 特定のアセットのサムネイルが更新された時の処理
            Repaint();
        }

        /// <summary>
        /// Creates a solid color texture for UI backgrounds
        /// </summary>
        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void ShowAdvancedSearchWindow()
        {
            AdvancedSearchWindow.ShowWindow(_advancedSearchCriteria, OnAdvancedSearchApplied);
        }

        private void OnAdvancedSearchApplied(AdvancedSearchCriteria criteria)
        {
            _advancedSearchCriteria = criteria;
            _isUsingAdvancedSearch = criteria.HasCriteria();
            if (_isUsingAdvancedSearch)
            {
                _searchText = ""; // 通常検索をクリア
            }
            _needsUIRefresh = true;
        }

        private string GetAdvancedSearchStatusText()
        {
            if (_advancedSearchCriteria == null) return "";

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(_advancedSearchCriteria.nameQuery))
                parts.Add($"名前:{_advancedSearchCriteria.nameQuery}");

            if (!string.IsNullOrEmpty(_advancedSearchCriteria.descriptionQuery))
                parts.Add($"説明:{_advancedSearchCriteria.descriptionQuery}");

            if (!string.IsNullOrEmpty(_advancedSearchCriteria.authorQuery))
                parts.Add($"作者:{_advancedSearchCriteria.authorQuery}");

            if (_advancedSearchCriteria.selectedTags.Count > 0)
                parts.Add($"タグ:{_advancedSearchCriteria.selectedTags.Count}個");

            if (parts.Count == 0) return "詳細検索中";

            var result = string.Join(", ", parts);
            return result.Length > 30 ? result.Substring(0, 27) + "..." : result;
        }

        private void ClearAdvancedSearch()
        {
            _isUsingAdvancedSearch = false;
            _advancedSearchCriteria = null;
            _needsUIRefresh = true;
        }
    }
}
