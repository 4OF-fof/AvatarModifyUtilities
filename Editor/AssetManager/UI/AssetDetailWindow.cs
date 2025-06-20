using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Data.Lang;
using AMU.Data.TagType;

namespace AMU.AssetManager.UI
{
    public class AssetDetailWindow : EditorWindow
    {
        public static void ShowWindow(AssetInfo asset, bool editMode = false)
        {
            var window = GetWindow<AssetDetailWindow>(LocalizationManager.GetText("AssetDetail_windowTitle"));
            window.minSize = new Vector2(800, 500);
            window.maxSize = new Vector2(800, 500);

            // ウィンドウ表示前にライブラリファイルの存在を確保
            AssetDataManager.Instance.EnsureLibraryFileExists();

            // シングルトンインスタンスから最新データを取得
            var updatedAsset = AssetDataManager.Instance.GetAsset(asset.uid) ?? asset;

            window._asset = updatedAsset.Clone();
            window._originalAsset = updatedAsset;
            window._isEditMode = editMode;

            // ウィンドウ表示時にサジェストデータを更新
            EditorApplication.delayCall += () =>
            {
                window.LoadAssetSuggestions();
                window.LoadTagSuggestions();
            };

            window.Show();
        }

        private AssetInfo _asset;
        private AssetInfo _originalAsset;
        private bool _isEditMode = false;
        private Vector2 _scrollPosition = Vector2.zero; private AssetDataManager _dataManager;
        private AssetThumbnailManager _thumbnailManager;
        private AssetFileManager _fileManager;        // UI state for tags and dependencies
        private string _newTag = "";
        private string _newDependency = "";

        // Tag suggestion state
        private List<string> _allTags = new List<string>();
        private List<string> _filteredTags = new List<string>();
        private bool _showTagSuggestions = false;
        private Vector2 _tagSuggestionScrollPos = Vector2.zero;

        // Dependency suggestion state
        private List<AssetInfo> _allAssets = new List<AssetInfo>();
        private List<AssetInfo> _filteredAssets = new List<AssetInfo>();
        private bool _showDependencySuggestions = false;
        private Vector2 _dependencySuggestionScrollPos = Vector2.zero;

        // UI Style
        private GUIStyle _tabStyle;
        private bool _stylesInitialized = false; private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            AssetTypeManager.LoadCustomTypes();
            InitializeManagers();
            LoadTagSuggestions();
            LoadAssetSuggestions();
        }
        private void LoadTagSuggestions()
        {
            // 新しいTagTypeManagerからタグ一覧を取得
            _allTags = AssetTagManager.GetAllTags();
        }
        private void LoadAssetSuggestions()
        {
            // 依存関係サジェスト用のアセット一覧を取得
            _allAssets.Clear();
            if (_dataManager != null)
            {
                var allAssets = _dataManager.GetAllAssets();
                if (allAssets != null)
                {
                    _allAssets = allAssets.ToList();
                    Debug.Log($"[AssetDetailWindow] Loaded {_allAssets.Count} assets for dependency suggestions");
                }
                else
                {
                    Debug.LogWarning("[AssetDetailWindow] GetAllAssets() returned null");
                }
            }
            else
            {
                Debug.LogWarning("[AssetDetailWindow] _dataManager is null");
            }
        }
        private void OnDisable()
        {
            // イベントの購読を解除
            if (_dataManager != null)
            {
                _dataManager.OnDataLoaded -= OnDataLoaded;
                _dataManager.OnDataChanged -= OnDataChanged;
            }

            if (_thumbnailManager != null)
            {
                _thumbnailManager.OnThumbnailLoaded -= Repaint;
                _thumbnailManager.OnThumbnailSaved -= OnThumbnailSaved;
                _thumbnailManager.OnThumbnailUpdated -= OnThumbnailUpdated;
            }

            // シングルトンインスタンスのため、他のウィンドウでも使用されている可能性があるのでキャッシュクリアしない
            // _thumbnailManager?.ClearCache();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // より見やすいタブスタイル
            _tabStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 24,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }
        private void InitializeManagers()
        {
            // シングルトンインスタンスを使用し初期化
            _dataManager = AssetDataManager.Instance;
            _dataManager.Initialize(); // 明示的に初期化を実行

            // データロード完了時の イベントを購読
            _dataManager.OnDataLoaded += OnDataLoaded;
            _dataManager.OnDataChanged += OnDataChanged;

            // シングルトンインスタンスを使用
            _thumbnailManager = AssetThumbnailManager.Instance;
            _thumbnailManager.OnThumbnailLoaded += Repaint;
            _thumbnailManager.OnThumbnailSaved += OnThumbnailSaved;
            _thumbnailManager.OnThumbnailUpdated += OnThumbnailUpdated;

            if (_fileManager == null)
            {
                _fileManager = new AssetFileManager();
            }

            LoadAllTags();
        }

        /// <summary>
        /// 最新のアセット情報を取得
        /// </summary>
        private AssetInfo GetLatestAssetInfo()
        {
            if (_asset == null) return null;
            return _dataManager?.GetAsset(_asset.uid) ?? _asset;
        }

        /// <summary>
        /// 現在表示中のアセット情報を最新の状態に更新
        /// </summary>
        private void RefreshAssetInfo()
        {
            if (_asset == null) return;

            var latestAsset = GetLatestAssetInfo();
            if (latestAsset != null && latestAsset != _asset)
            {
                _asset = latestAsset.Clone();
                _originalAsset = latestAsset;

                // タグ情報も更新                
                LoadAllTags();
                LoadTagSuggestions();

                Repaint();
            }
        }
        private void LoadAllTags()
        {
            _allTags.Clear();
            if (_dataManager?.Library?.assets != null)
            {
                var tagSet = new HashSet<string>();
                foreach (var asset in _dataManager.Library.assets)
                {
                    if (asset.tags != null)
                    {
                        foreach (var tag in asset.tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                tagSet.Add(tag.Trim());
                            }
                        }
                    }
                }
                _allTags = tagSet.OrderBy(tag => tag).ToList();
            }

            // 依存関係サジェスト用のアセット一覧も更新
            LoadAssetSuggestions();
        }
        private void OnGUI()
        {
            InitializeStyles();

            if (_asset == null)
            {
                GUILayout.Label("No asset selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            DrawHeader();
            DrawContent();
        }
        private void DrawHeader()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_asset.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (_isEditMode)
                {
                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_save"), EditorStyles.toolbarButton))
                    {
                        SaveAsset();
                    }

                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_cancel"), EditorStyles.toolbarButton))
                    {
                        CancelEdit();
                    }
                }
                else
                {
                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_edit"), EditorStyles.toolbarButton))
                    {
                        // 編集モードに入る前にアセット情報を最新に更新
                        RefreshAssetInfo();

                        _isEditMode = true;
                        // Reset UI state when entering edit mode
                        _newTag = "";
                        _newDependency = "";
                    }
                }
            }
        }

        private void DrawContent()
        {
            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;

                using (new GUILayout.HorizontalScope())
                {
                    DrawThumbnailSection();
                    DrawDetailsSection();
                }
            }
        }
        private void DrawThumbnailSection()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(200)))
            {
                GUILayout.Space(130);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(10); // サムネイルの左マージン
                    _thumbnailManager.DrawThumbnail(_asset, 180);
                }
                GUILayout.Space(5);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(10); // ボタンの左マージン
                    if (_isEditMode)
                    {
                        if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_selectThumbnail"), GUILayout.Width(180)))
                        {
                            SelectThumbnail();
                        }
                    }
                    // インポートファイルがある場合の処理
                    else if (HasImportFiles())
                    {
                        if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_importSelectedFiles"), GUILayout.Width(180)))
                        {
                            ImportSelectedFiles();
                        }
                    }
                    else if (_fileManager.ShouldShowImportButton(_asset))
                    {
                        string buttonText;
                        if (_fileManager.IsUnityPackage(_asset))
                        {
                            buttonText = LocalizationManager.GetText("AssetDetail_importPackage");
                        }
                        else
                        {
                            buttonText = LocalizationManager.GetText("AssetDetail_importFile");
                        }

                        if (GUILayout.Button(buttonText, GUILayout.Width(180)))
                        {
                            ImportAsset();
                        }
                    }
                    // Display download button when boothDownloadUrl exists and no file path is set
                    else if (!string.IsNullOrEmpty(_asset.boothItem?.boothDownloadUrl) && string.IsNullOrEmpty(_asset.filePath))
                    {
                        if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_downloadFile"), GUILayout.Width(180)))
                        {
                            Application.OpenURL(_asset.boothItem.boothDownloadUrl);
                        }
                    }
                }

                // 下側にもフレキシブルスペースを追加
                GUILayout.Space(20);
                GUILayout.FlexibleSpace();
            }
        }
        private void DrawDetailsSection()
        {
            using (new GUILayout.VerticalScope())
            {
                // 詳細セクションを中央に配置するためのフレキシブルスペース
                GUILayout.FlexibleSpace();
                DrawGeneralInfo();
                GUILayout.Space(10);
                DrawFileInfo();
                GUILayout.Space(10);
                DrawImportFiles();
                GUILayout.Space(10);
                DrawTagsAndDependencies();

                // 下側にもフレキシブルスペースを追加
                GUILayout.Space(20);
                GUILayout.FlexibleSpace();
            }
        }
        private void DrawGeneralInfo()
        {
            GUILayout.Label(LocalizationManager.GetText("AssetDetail_generalInfo"), EditorStyles.boldLabel);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {                // Name
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_name"), GUILayout.Width(100));
                    if (_isEditMode)
                    {
                        _asset.name = EditorGUILayout.TextField(_asset.name, GUILayout.Width(460));
                    }
                    else
                    {
                        GUILayout.Label(_asset.name, GUILayout.Width(460));
                    }
                }                // Description
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_description"), GUILayout.Width(100));
                    if (_isEditMode)
                    {
                        _asset.description = EditorGUILayout.TextArea(_asset.description, GUILayout.Height(60), GUILayout.Width(460));
                    }
                    else
                    {
                        GUILayout.Label(_asset.description, EditorStyles.wordWrappedLabel, GUILayout.Width(460));
                    }
                }                // Type
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_type"), GUILayout.Width(100));
                    if (_isEditMode)
                    {
                        var allTypes = AssetTypeManager.AllTypes;
                        var currentIndex = allTypes.IndexOf(_asset.assetType);
                        if (currentIndex < 0) currentIndex = 0;

                        var newIndex = EditorGUILayout.Popup(currentIndex, allTypes.ToArray(), GUILayout.Width(460));
                        if (newIndex >= 0 && newIndex < allTypes.Count)
                        {
                            _asset.assetType = allTypes[newIndex];
                        }
                    }
                    else
                    {
                        GUILayout.Label(_asset.assetType, GUILayout.Width(460));
                    }
                }                // Author
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_author"), GUILayout.Width(100));
                    if (_isEditMode)
                    {
                        _asset.authorName = EditorGUILayout.TextField(_asset.authorName, GUILayout.Width(460));
                    }
                    else
                    {
                        GUILayout.Label(_asset.authorName, GUILayout.Width(460));
                    }
                }
                if (_asset.boothItem != null && (!string.IsNullOrEmpty(_asset.boothItem.boothItemUrl) || _isEditMode))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("URL", GUILayout.Width(100));
                        if (_isEditMode)
                        {
                            if (_asset.boothItem == null)
                            {
                                _asset.boothItem = new BoothItem();
                            }
                            _asset.boothItem.boothItemUrl = EditorGUILayout.TextField(_asset.boothItem.boothItemUrl ?? "", GUILayout.Width(460));
                        }
                        else
                        {
                            // 表示モードの場合、クリック可能なリンクとして表示
                            if (GUILayout.Button(_asset.boothItem.boothItemUrl, EditorStyles.linkLabel, GUILayout.Width(460)))
                            {
                                Application.OpenURL(_asset.boothItem.boothItemUrl);
                            }
                        }
                    }
                }
            }
        }
        private void DrawFileInfo()
        {
            GUILayout.Label(LocalizationManager.GetText("AssetDetail_fileInfo"), EditorStyles.boldLabel);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {                // File Path
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_filePath"), GUILayout.Width(100));
                    if (_isEditMode)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            _asset.filePath = EditorGUILayout.TextField(_asset.filePath, GUILayout.Width(380));
                            if (GUILayout.Button(LocalizationManager.GetText("Common_browse"), GUILayout.Width(80)))
                            {
                                BrowseForFile();
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label(_asset.filePath, GUILayout.Width(460));
                    }
                }                // File Size
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_fileSize"), GUILayout.Width(100));
                    GUILayout.Label(_fileManager.FormatFileSize(_asset.fileSize), GUILayout.Width(460));
                }                // Created Date
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_createdDate"), GUILayout.Width(100));
                    GUILayout.Label(_asset.createdDate.ToString("yyyy/MM/dd HH:mm:ss"), GUILayout.Width(460));
                }

            }
        }
        private void DrawImportFiles()
        {
            // zipファイルでない場合、または編集モードでなくimportFilesが空の場合は表示しない
            bool isZipFile = _fileManager.IsZipFile(_asset);
            bool hasImportFiles = _asset.importFiles != null && _asset.importFiles.Count > 0;

            if (!isZipFile && !hasImportFiles)
                return;

            if (!_isEditMode && !hasImportFiles)
                return;

            GUILayout.Label(LocalizationManager.GetText("AssetDetail_importFiles"), EditorStyles.boldLabel);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_asset.importFiles != null && _asset.importFiles.Count > 0)
                {
                    for (int i = _asset.importFiles.Count - 1; i >= 0; i--)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            string fileName = Path.GetFileName(_asset.importFiles[i]);
                            GUILayout.Label(fileName, GUILayout.Width(400));

                            if (_isEditMode && GUILayout.Button(LocalizationManager.GetText("AssetDetail_removeFile"), GUILayout.Width(60)))
                            {
                                _asset.importFiles.RemoveAt(i);
                            }
                        }
                    }
                }

                if (_isEditMode && isZipFile)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_selectFromZip"), GUILayout.Width(150)))
                    {
                        ShowZipFileSelector();
                    }
                    GUILayout.Space(5);
                }
            }
        }

        private void ShowZipFileSelector()
        {
            if (_asset == null || string.IsNullOrEmpty(_asset.filePath) || !_fileManager.IsZipFile(_asset))
            {
                EditorUtility.DisplayDialog("エラー", "zipファイルが設定されていません。", "OK");
                return;
            }

            var zipFiles = _fileManager.GetZipFileList(_asset.filePath);
            if (zipFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "zipファイル内にファイルが見つかりません。", "OK");
                return;
            }

            // ファイル選択ウィンドウを表示
            ZipFileSelector.ShowWindow(_asset, zipFiles, _fileManager, (selectedFiles) =>
            {
                if (_asset.importFiles == null)
                    _asset.importFiles = new List<string>();

                foreach (var file in selectedFiles)
                {
                    if (!_asset.importFiles.Contains(file))
                    {
                        _asset.importFiles.Add(file);
                    }
                }
            });
        }

        private void DrawTagsAndDependencies()
        {
            // 編集モードでない場合は、タグまたは依存関係が存在する場合のみ表示
            bool hasTagsOrDependencies = (_asset.tags != null && _asset.tags.Count > 0) ||
                                       (_asset.dependencies != null && _asset.dependencies.Count > 0);

            if (!_isEditMode && !hasTagsOrDependencies)
            {
                return; // タグも依存関係もない場合は何も表示しない
            }

            using (new GUILayout.HorizontalScope())
            {
                // Tags - 編集モードまたはタグが存在する場合のみ表示
                if (_isEditMode || (_asset.tags != null && _asset.tags.Count > 0))
                {
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Label(LocalizationManager.GetText("AssetDetail_tags"), EditorStyles.boldLabel);
                        using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(120)))
                        {
                            if (_asset.tags != null && _asset.tags.Count > 0)
                            {
                                for (int i = _asset.tags.Count - 1; i >= 0; i--)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        var tagName = _asset.tags[i];
                                        var originalColor = GUI.color;

                                        // タグの色を取得して背景色に設定
                                        var tagColor = AssetTagManager.GetTagColor(tagName);
                                        GUI.color = tagColor;

                                        var tagContent = new GUIContent(tagName);
                                        GUILayout.Button(tagContent, EditorStyles.miniButton);

                                        GUI.color = originalColor;

                                        if (_isEditMode && GUILayout.Button("×", GUILayout.Width(20)))
                                        {
                                            _asset.tags.RemoveAt(i);
                                        }
                                    }
                                }
                            }
                            if (_isEditMode)
                            {
                                GUILayout.FlexibleSpace();
                                DrawTagInput();
                            }
                        }
                    }
                }

                // タグと依存関係の両方が表示される場合のスペース
                if ((_isEditMode || (_asset.tags != null && _asset.tags.Count > 0)) &&
                    (_isEditMode || (_asset.dependencies != null && _asset.dependencies.Count > 0)))
                {
                    GUILayout.Space(10);
                }                // Dependencies - 編集モードまたは依存関係が存在する場合のみ表示
                if (_isEditMode || (_asset.dependencies != null && _asset.dependencies.Count > 0))
                {
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Label(LocalizationManager.GetText("AssetDetail_dependencies"), EditorStyles.boldLabel);
                        using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(120)))
                        {
                            if (_asset.dependencies != null && _asset.dependencies.Count > 0)
                            {
                                for (int i = _asset.dependencies.Count - 1; i >= 0; i--)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        var dependency = _asset.dependencies[i];
                                        var referencedAsset = _dataManager.GetAsset(dependency); // Use UUID instead of name

                                        if (referencedAsset != null)
                                        {
                                            // This is a reference to an existing asset
                                            var originalColor = GUI.color;
                                            GUI.color = new Color(0.7f, 1f, 0.7f, 1f); // Light green background

                                            var content = new GUIContent(referencedAsset.name, LocalizationManager.GetText("AssetDetail_clickToOpenAsset"));
                                            if (GUILayout.Button(content, EditorStyles.miniButton))
                                            {
                                                // Open the referenced asset's detail window
                                                AssetDetailWindow.ShowWindow(referencedAsset);
                                            }

                                            GUI.color = originalColor;
                                        }
                                        else
                                        {
                                            // This is a manual text entry or broken reference
                                            var originalColor = GUI.color; if (dependency.Length == 36 && dependency.Contains("-")) // Looks like a UUID
                                            {
                                                GUI.color = new Color(1f, 0.7f, 0.7f, 1f); // Light red for broken reference
                                                var missingContent = new GUIContent($"{LocalizationManager.GetText("AssetDetail_missingDependency")} {dependency}");
                                                GUILayout.Button(missingContent, EditorStyles.miniButton);
                                            }
                                            else
                                            {
                                                GUI.color = new Color(0.9f, 0.9f, 1f, 1f); // Light blue background for manual entries
                                                var manualContent = new GUIContent(dependency);
                                                GUILayout.Button(manualContent, EditorStyles.miniButton);
                                            }
                                            GUI.color = originalColor;
                                        }

                                        if (_isEditMode && GUILayout.Button("×", GUILayout.Width(20)))
                                        {
                                            _asset.dependencies.RemoveAt(i);
                                        }
                                    }
                                }
                            }
                            if (_isEditMode)
                            {
                                GUILayout.FlexibleSpace();
                                DrawDependencyInput();
                            }
                        }
                    }
                }
            }
        }
        private void SelectThumbnail()
        {
            // 既存のサムネイルパスがある場合はそのディレクトリを、ない場合は空文字を使用
            string defaultPath = "";
            if (!string.IsNullOrEmpty(_asset.thumbnailPath))
            {
                try
                {
                    // パスの形式に応じて絶対パスに変換
                    string absolutePath = _asset.thumbnailPath;

                    if (_asset.thumbnailPath.StartsWith("Assets"))
                    {
                        // Assetsパスの場合
                        absolutePath = Application.dataPath + _asset.thumbnailPath.Substring(6).Replace('/', System.IO.Path.DirectorySeparatorChar);
                    }
                    else if (_asset.thumbnailPath.StartsWith("AssetManager/"))
                    {
                        // AssetManager/から始まるパスの場合（自動登録パス）
                        string coreDir = UnityEditor.EditorPrefs.GetString("Setting.Core_dirPath",
                            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
                        absolutePath = System.IO.Path.Combine(coreDir, _asset.thumbnailPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    }
                    else if (!System.IO.Path.IsPathRooted(_asset.thumbnailPath))
                    {
                        // 相対パスの場合（念のため）
                        string coreDir = UnityEditor.EditorPrefs.GetString("Setting.Core_dirPath",
                            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
                        absolutePath = System.IO.Path.Combine(coreDir, _asset.thumbnailPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    }

                    // ファイルが存在する場合はそのディレクトリを、存在しない場合はパスのディレクトリ部分を使用
                    if (System.IO.File.Exists(absolutePath))
                    {
                        defaultPath = System.IO.Path.GetDirectoryName(absolutePath);
                    }
                    else if (!string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(absolutePath)))
                    {
                        defaultPath = System.IO.Path.GetDirectoryName(absolutePath);
                    }
                }
                catch (System.Exception)
                {
                    // パス解析でエラーが発生した場合は空文字を使用
                    defaultPath = "";
                }
            }

            string path = EditorUtility.OpenFilePanel("Select Thumbnail", defaultPath, "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                _thumbnailManager.SetCustomThumbnail(_asset, path);
            }
        }
        private void BrowseForFile()
        {
            // 既存のファイルパスがある場合はそのディレクトリを、ない場合はApplication.dataPathを使用
            string defaultPath = Application.dataPath;
            if (!string.IsNullOrEmpty(_asset.filePath))
            {
                try
                {
                    // パスの形式に応じて絶対パスに変換
                    string absolutePath = _asset.filePath;

                    if (_asset.filePath.StartsWith("Assets"))
                    {
                        // Assetsパスの場合
                        absolutePath = Application.dataPath + _asset.filePath.Substring(6).Replace('/', System.IO.Path.DirectorySeparatorChar);
                    }
                    else if (_asset.filePath.StartsWith("AssetManager/"))
                    {
                        // AssetManager/から始まるパスの場合（自動登録パス）
                        string coreDir = UnityEditor.EditorPrefs.GetString("Setting.Core_dirPath",
                            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
                        absolutePath = System.IO.Path.Combine(coreDir, _asset.filePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    }
                    else if (!System.IO.Path.IsPathRooted(_asset.filePath))
                    {
                        // 相対パスの場合（念のため）
                        string coreDir = UnityEditor.EditorPrefs.GetString("Setting.Core_dirPath",
                            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
                        absolutePath = System.IO.Path.Combine(coreDir, _asset.filePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                    }

                    // ファイルが存在する場合はそのディレクトリを、存在しない場合はパスのディレクトリ部分を使用
                    if (System.IO.File.Exists(absolutePath))
                    {
                        defaultPath = System.IO.Path.GetDirectoryName(absolutePath);
                    }
                    else if (!string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(absolutePath)))
                    {
                        defaultPath = System.IO.Path.GetDirectoryName(absolutePath);
                    }
                }
                catch (System.Exception)
                {
                    // パス解析でエラーが発生した場合はデフォルトパスを使用
                    defaultPath = Application.dataPath;
                }
            }

            string path = EditorUtility.OpenFilePanel("Select Asset File", defaultPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    _asset.filePath = "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/');
                }
                else
                {
                    _asset.filePath = path;
                }

                // Update file info
                _asset.fileSize = _fileManager.GetFileSize(_asset.filePath);
            }
        }
        private void ImportAsset()
        {
            try
            {
                _fileManager.ImportAsset(_asset);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to import asset: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to import asset: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// インポートファイルが存在するかチェック
        /// </summary>
        private bool HasImportFiles()
        {
            return _asset.importFiles != null && _asset.importFiles.Count > 0;
        }        /// <summary>
                 /// 選択されたファイルをインポートする
                 /// </summary>
        private void ImportSelectedFiles()
        {
            try
            {
                if (_asset.importFiles == null || _asset.importFiles.Count == 0)
                {
                    Debug.LogWarning("No import files specified");
                    return;
                }

                // UnityPackageファイルとその他のファイルを分ける
                var unityPackageFiles = new List<string>();
                var otherFiles = new List<string>();

                foreach (var importFile in _asset.importFiles)
                {
                    if (string.IsNullOrEmpty(importFile))
                        continue;

                    string extension = Path.GetExtension(importFile).ToLower();
                    if (extension == ".unitypackage")
                    {
                        unityPackageFiles.Add(importFile);
                    }
                    else
                    {
                        otherFiles.Add(importFile);
                    }
                }

                // まず一般ファイルをインポート（これらは同期的）
                foreach (var otherFile in otherFiles)
                {
                    ImportFileToAssets(otherFile);
                }

                // UnityPackageファイルを順次インポート（非同期処理）
                if (unityPackageFiles.Count > 0)
                {
                    ImportUnityPackagesSequentially(unityPackageFiles, 0);
                }
                else
                {
                    Debug.Log($"Imported {_asset.importFiles.Count} files successfully");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to import selected files: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to import selected files: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// UnityPackageファイルを順次インポートする
        /// </summary>
        private void ImportUnityPackagesSequentially(List<string> unityPackageFiles, int currentIndex)
        {
            if (currentIndex >= unityPackageFiles.Count)
            {
                Debug.Log($"All Unity packages imported successfully. Total files: {_asset.importFiles.Count}");
                return;
            }

            string currentFile = unityPackageFiles[currentIndex]; Debug.Log($"Importing Unity Package {currentIndex + 1}/{unityPackageFiles.Count}: {currentFile}");

            // インポート完了を監視するコールバックを登録
            AssetDatabase.ImportPackageCallback importCompleteCallback = null;
            AssetDatabase.ImportPackageCallback importCancelledCallback = null;
            AssetDatabase.ImportPackageFailedCallback importFailedCallback = null;

            System.Action nextImport = () =>
            {
                // コールバックを解除
                if (importCompleteCallback != null)
                    AssetDatabase.importPackageCompleted -= importCompleteCallback;
                if (importCancelledCallback != null)
                    AssetDatabase.importPackageCancelled -= importCancelledCallback;
                if (importFailedCallback != null)
                    AssetDatabase.importPackageFailed -= importFailedCallback;

                // 次のファイルをインポート
                EditorApplication.delayCall += () =>
                {
                    ImportUnityPackagesSequentially(unityPackageFiles, currentIndex + 1);
                };
            };

            importCompleteCallback = (packageName) => nextImport();
            importCancelledCallback = (packageName) => nextImport();
            importFailedCallback = (packageName, errorMessage) => nextImport();

            AssetDatabase.importPackageCompleted += importCompleteCallback;
            AssetDatabase.importPackageCancelled += importCancelledCallback;
            AssetDatabase.importPackageFailed += importFailedCallback;

            // UnityPackageをインポート
            ImportUnityPackageFile(currentFile);
        }

        /// <summary>
        /// UnityPackageファイルをインポート
        /// </summary>        
        private void ImportUnityPackageFile(string importFile)
        {
            // 相対パスを絶対パスに変換
            string fullPath = _fileManager.GetFullPath(importFile);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"Unity Package file not found: {fullPath}");
                return;
            }

            Debug.Log($"Importing Unity Package: {fullPath}");
            AssetDatabase.ImportPackage(fullPath, true);
        }

        /// <summary>
        /// 一般ファイルをAssetsフォルダにインポート
        /// </summary>        
        private void ImportFileToAssets(string importFile)
        {
            // 相対パスを絶対パスに変換
            string fullPath = _fileManager.GetFullPath(importFile);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"Import file not found: {fullPath}");
                return;
            }

            string targetPath = Path.Combine("Assets", Path.GetFileName(importFile));
            string fullTargetPath = Path.Combine(Application.dataPath, Path.GetFileName(importFile));

            File.Copy(fullPath, fullTargetPath, true);
            AssetDatabase.Refresh();

            Debug.Log($"Imported file to Assets: {importFile}");

            // インポート後にファイルを選択状態にする
            EditorApplication.delayCall += () =>
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            };
        }
        private void DrawTagInput()
        {
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    var newTagInput = EditorGUILayout.TextField(_newTag);

                    // Check if input changed to update suggestions
                    if (newTagInput != _newTag)
                    {
                        _newTag = newTagInput;
                        UpdateTagSuggestions();
                    }

                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_addTag"), GUILayout.Width(80)))
                    {
                        AddTag();
                    }
                }

                // Show suggestions if available and input is not empty
                if (_showTagSuggestions && _filteredTags.Count > 0 && !string.IsNullOrEmpty(_newTag))
                {
                    DrawTagSuggestions();
                }
            }
        }

        private void UpdateTagSuggestions()
        {
            _filteredTags.Clear();
            _showTagSuggestions = false;

            if (string.IsNullOrEmpty(_newTag))
            {
                return;
            }

            var input = _newTag.ToLower();
            foreach (var tag in _allTags)
            {
                // Skip tags that are already added to the current asset
                if (_asset.tags.Contains(tag))
                    continue;

                // Filter tags that contain the input text
                if (tag.ToLower().Contains(input))
                {
                    _filteredTags.Add(tag);
                }
            }

            _showTagSuggestions = _filteredTags.Count > 0;
        }
        private void DrawTagSuggestions()
        {
            var suggestionHeight = Mathf.Min(_filteredTags.Count * 20f, 100f);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(suggestionHeight)))
            {
                _tagSuggestionScrollPos = GUILayout.BeginScrollView(_tagSuggestionScrollPos);

                for (int i = 0; i < _filteredTags.Count; i++)
                {
                    var tag = _filteredTags[i];
                    var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.Height(18));

                    if (GUI.Button(rect, tag, EditorStyles.label))
                    {
                        _newTag = tag;
                        AddTag();
                        _showTagSuggestions = false;
                        GUI.FocusControl(null);
                        break;
                    }

                    // Highlight on hover
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 1f, 0.3f));
                        GUI.Label(rect, tag);
                    }
                }

                GUILayout.EndScrollView();
            }

            // Handle keyboard input for tag suggestions
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    _showTagSuggestions = false;
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (_filteredTags.Count > 0)
                    {
                        _newTag = _filteredTags[0];
                        AddTag();
                        _showTagSuggestions = false;
                        Event.current.Use();
                    }
                }
            }
        }
        private void AddDependency()
        {
            if (!string.IsNullOrEmpty(_newDependency.Trim()))
            {
                // 依存関係リストが初期化されていない場合は初期化
                if (_asset.dependencies == null)
                {
                    _asset.dependencies = new List<string>();
                }

                var trimmedInput = _newDependency.Trim();
                bool wasAdded = false;

                // Try to find a matching asset by name first
                var matchingAsset = _allAssets.FirstOrDefault(a => a.name.Equals(trimmedInput, StringComparison.OrdinalIgnoreCase) && a.uid != _asset.uid);

                if (matchingAsset != null)
                {
                    // Add the asset UID if it's an existing asset
                    if (!_asset.dependencies.Contains(matchingAsset.uid))
                    {
                        _asset.dependencies.Add(matchingAsset.uid);
                        wasAdded = true;
                    }
                }
                else
                {
                    // Add as manual dependency if it's not an existing asset
                    if (!_asset.dependencies.Contains(trimmedInput))
                    {
                        _asset.dependencies.Add(trimmedInput);
                        wasAdded = true;
                    }
                }

                // Only clear the input if the dependency was actually added
                if (wasAdded)
                {
                    _newDependency = "";
                }
                _showDependencySuggestions = false;
                GUI.FocusControl(null);
            }
        }
        private void DrawDependencyInput()
        {
            // 依存関係リストが初期化されていない場合は初期化
            if (_asset.dependencies == null)
            {
                _asset.dependencies = new List<string>();
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    var newDependencyInput = EditorGUILayout.TextField(_newDependency);

                    // Check if input changed to update suggestions
                    if (newDependencyInput != _newDependency)
                    {
                        _newDependency = newDependencyInput;
                        UpdateDependencySuggestions();
                    }

                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_addDependency"), GUILayout.Width(80)))
                    {
                        AddDependency();
                    }
                }

                // Show suggestions if available and input is not empty
                if (_showDependencySuggestions && _filteredAssets.Count > 0 && !string.IsNullOrEmpty(_newDependency))
                {
                    DrawDependencySuggestions();
                }
            }
        }
        private void UpdateDependencySuggestions()
        {
            _filteredAssets.Clear();
            _showDependencySuggestions = false;

            if (string.IsNullOrEmpty(_newDependency))
            {
                return;
            }

            if (_allAssets == null || _allAssets.Count == 0)
            {
                Debug.LogWarning($"[AssetDetailWindow] No assets available for dependency suggestions. _allAssets count: {_allAssets?.Count ?? 0}");
                return;
            }

            var input = _newDependency.ToLower();
            foreach (var asset in _allAssets)
            {
                // Skip self and already added dependencies
                if (asset.uid == _asset.uid || (_asset.dependencies != null && _asset.dependencies.Contains(asset.uid)))
                    continue;

                // Filter assets that contain the input text in their name
                if (asset.name.ToLower().Contains(input))
                {
                    _filteredAssets.Add(asset);
                }
            }

            _showDependencySuggestions = _filteredAssets.Count > 0;
            Debug.Log($"[AssetDetailWindow] Updated dependency suggestions: input='{_newDependency}', found {_filteredAssets.Count} matches");
        }

        private void DrawDependencySuggestions()
        {
            var suggestionHeight = Mathf.Min(_filteredAssets.Count * 20f, 100f);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(suggestionHeight)))
            {
                _dependencySuggestionScrollPos = GUILayout.BeginScrollView(_dependencySuggestionScrollPos);

                for (int i = 0; i < _filteredAssets.Count; i++)
                {
                    var asset = _filteredAssets[i];
                    var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.Height(18));

                    if (GUI.Button(rect, asset.name, EditorStyles.label))
                    {
                        _newDependency = asset.name;
                        AddDependency();
                        _showDependencySuggestions = false;
                        GUI.FocusControl(null);
                        break;
                    }

                    // Highlight on hover
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 1f, 0.3f));
                        GUI.Label(rect, asset.name);
                    }
                }

                GUILayout.EndScrollView();
            }

            // Handle keyboard input for dependency suggestions
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    _showDependencySuggestions = false;
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (_filteredAssets.Count > 0)
                    {
                        _newDependency = _filteredAssets[0].name;
                        AddDependency();
                        _showDependencySuggestions = false;
                        Event.current.Use();
                    }
                }
            }
        }
        private void SaveAsset()
        {
            try
            {
                // ファイルパスが空の場合はファイルサイズを0にする
                if (string.IsNullOrEmpty(_asset.filePath))
                {
                    _asset.fileSize = 0;
                }

                _dataManager.UpdateAsset(_asset);
                _originalAsset = _asset.Clone();
                _isEditMode = false;

                Debug.Log(string.Format(LocalizationManager.GetText("AssetDetail_saveSuccess"), _asset.name));

                // Refresh the main window without stealing focus
                EditorApplication.delayCall += () =>
                {
                    var windows = Resources.FindObjectsOfTypeAll<AssetManagerWindow>();
                    foreach (var window in windows)
                    {
                        window.Repaint();
                    }
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save asset: {ex.Message}");
                EditorUtility.DisplayDialog("Error", "Failed to save asset. Check console for details.", "OK");
            }
        }
        private void CancelEdit()
        {
            _asset = _originalAsset?.Clone(); _isEditMode = false;
            // Reset UI state when canceling edit
            _newTag = "";
            _newDependency = "";
            _showTagSuggestions = false;
            _showDependencySuggestions = false;
        }
        private void OnThumbnailSaved(AssetInfo asset)
        {
            if (asset != null && _dataManager != null)
            {
                _dataManager.UpdateAsset(asset);

                // 現在編集中のアセットと同じ場合は情報を更新
                if (_asset != null && _asset.uid == asset.uid)
                {
                    _asset.thumbnailPath = asset.thumbnailPath;
                    _originalAsset = _asset.Clone();
                    Debug.Log(string.Format(LocalizationManager.GetText("AssetDetail_thumbnailSaveSuccess"), asset.name));
                    Repaint();
                }
            }
        }

        private void OnThumbnailUpdated(string assetUid)
        {
            // 現在編集中のアセットのサムネイルが更新された場合
            if (_asset != null && _asset.uid == assetUid)
            {
                Repaint();
            }
        }
        private void AddTag()
        {
            if (!string.IsNullOrEmpty(_newTag.Trim()) && !_asset.tags.Contains(_newTag.Trim()))
            {
                var trimmedTag = _newTag.Trim();
                _asset.tags.Add(trimmedTag);

                // Add to global tag list if it's not already there
                if (!_allTags.Contains(trimmedTag))
                {
                    _allTags.Add(trimmedTag);
                    _allTags.Sort();
                }

                // TagTypeManagerに新しいタグが存在しない場合、ランダムな視認性の良い色で追加
                var existingTag = TagTypeManager.GetTagByName(trimmedTag);
                if (existingTag == null)
                {
                    var randomColor = GenerateRandomVisibleColor();
                    AssetTagManager.AddCustomTag(trimmedTag, randomColor);
                }

                _newTag = "";
                _showTagSuggestions = false;
                GUI.FocusControl(null);
            }
        }

        /// <summary>
        /// 視認性の良いランダムな色を生成します
        /// </summary>
        /// <returns>HEX形式の色文字列</returns>
        private string GenerateRandomVisibleColor()
        {
            // 視認性の良い色のパレット
            var visibleColors = new string[]
            {
                "#FF6B6B", // 明るい赤
                "#4ECDC4", // ティール
                "#45B7D1", // 明るい青
                "#96CEB4", // ミントグリーン
                "#FFEAA7", // 明るい黄色
                "#DDA0DD", // プラム
                "#98D8C8", // ライトシーグリーン
                "#F7DC6F", // ライトゴールド
                "#BB8FCE", // ライトパープル
                "#85C1E9", // ライトブルー
                "#F8C471", // ライトオレンジ
                "#82E0AA", // ライトグリーン
                "#F1948A", // ライトピンク
                "#85C1E9", // スカイブルー
                "#F4D03F", // ライトイエロー
                "#AED6F1", // ベビーブルー
                "#A9DFBF", // ライトターコイズ
                "#F5B7B1", // ライトローズ
                "#D7BDE2", // ライトラベンダー
                "#FAD7A0"  // ライトピーチ
            };

            var random = new System.Random();
            return visibleColors[random.Next(visibleColors.Length)];
        }

        /// <summary>
        /// データロード完了時のコールバック
        /// </summary>
        private void OnDataLoaded()
        {
            LoadAssetSuggestions();
            LoadTagSuggestions();
            Repaint();
        }

        /// <summary>
        /// データ変更時のコールバック
        /// </summary>
        private void OnDataChanged()
        {
            LoadAssetSuggestions();
            LoadTagSuggestions();
            Repaint();
        }
    }
}
