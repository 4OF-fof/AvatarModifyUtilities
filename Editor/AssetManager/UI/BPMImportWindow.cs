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
    public class BPMImportWindow : EditorWindow
    {
        [System.Serializable]
        public class AssetImportSettings
        {
            public string assetType = "Assets";
            public List<string> tags = new List<string>();
        }

        private AssetDataManager _assetDataManager;
        private Data.BPMLibrary _bmpLibrary;
        private Action _onImportComplete;

        // 個別設定
        private Dictionary<string, AssetImportSettings> _packageSettings = new Dictionary<string, AssetImportSettings>();
        private Dictionary<string, AssetImportSettings> _fileSettings = new Dictionary<string, AssetImportSettings>();

        // 未登録ファイルフィルタリング用
        private HashSet<string> _existingDownloadUrls = new HashSet<string>();

        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 _packageListScrollPosition = Vector2.zero;
        private bool _isLoading = false;
        private string _statusMessage = "";

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _packageHeaderStyle;
        private GUIStyle _fileItemStyle;
        private GUIStyle _messageStyle;
        private bool _stylesInitialized = false;

        // アセットタイプ選択用
        private string[] _assetTypeOptions;
        private int _defaultAssetTypeIndex = 0;

        // グループ子アセットの設定表示制御
        private bool _showChildAssetSettings = false;

        public static void ShowWindowWithFile(AssetDataManager assetDataManager, string bmpLibraryPath, Action onImportComplete = null)
        {
            var window = GetWindow<BPMImportWindow>(LocalizationManager.GetText("BPMImport_windowTitle"));
            window.minSize = new Vector2(600, 800);
            window.maxSize = new Vector2(600, 800);
            window._assetDataManager = assetDataManager;
            window._onImportComplete = onImportComplete;
            window.LoadFromSpecificFile(bmpLibraryPath);
            window.Show();
        }

        private async void OnEnable()
        {
            // アセットタイプ選択肢の初期化
            InitializeAssetTypeOptions();

            // BPMデータの読み込み
            _isLoading = true;
            _statusMessage = LocalizationManager.GetText("BPMImport_loadingLibrary");

            try
            {
                var (filePath, library) = await BPMHelper.FindLatestBPMLibraryAsync();
                _bmpLibrary = library;
                OnBPMDataLoaded();
            }
            catch (System.Exception ex)
            {
                OnBPMLoadError(ex.Message);
            }
        }

        private async void LoadFromSpecificFile(string filePath)
        {
            // アセットタイプ選択肢の初期化
            InitializeAssetTypeOptions();

            _isLoading = true;
            _statusMessage = LocalizationManager.GetText("BPMImport_loadingLibrary");

            try
            {
                _bmpLibrary = await BPMHelper.LoadBPMLibraryAsync(filePath);
                OnBPMDataLoaded();
            }
            catch (System.Exception ex)
            {
                OnBPMLoadError(ex.Message);
            }
        }

        private void OnDisable()
        {
            // 新しい実装では特にクリーンアップは不要
        }

        private void OnBPMDataLoaded()
        {
            _isLoading = false;
            _statusMessage = string.Format(LocalizationManager.GetText("BPMImport_libraryLoaded"), GetPackageCount());
            CollectExistingDownloadUrls();
            Repaint();
        }

        private void OnBPMLoadError(string error)
        {
            _isLoading = false;
            _statusMessage = $"Failed to load BMP Library: {error}";
            Repaint();
        }

        private int GetPackageCount()
        {
            if (_bmpLibrary?.authors == null)
                return 0;

            return _bmpLibrary.authors.Values.Sum(packages => packages.Count);
        }

        private void CollectExistingDownloadUrls()
        {
            if (_bmpLibrary?.authors == null)
                return;

            foreach (var author in _bmpLibrary.authors)
            {
                foreach (var package in author.Value)
                {
                    if (package.files != null)
                    {
                        foreach (var file in package.files)
                        {
                            _existingDownloadUrls.Add(file.downloadLink);
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            EditorGUILayout.LabelField(LocalizationManager.GetText("BPMImport_title"), _headerStyle);

            GUILayout.Space(10);

            // ステータス表示
            EditorGUILayout.LabelField(LocalizationManager.GetText("BPMImport_status"), _statusMessage);

            if (_isLoading)
            {
                EditorGUILayout.HelpBox(LocalizationManager.GetText("BPMImport_loadingMessage"), MessageType.Info);
                return;
            }

            if (_bmpLibrary?.authors == null)
            {
                EditorGUILayout.HelpBox(LocalizationManager.GetText("BPMImport_noDataMessage"), MessageType.Warning);
                return;
            }

            GUILayout.Space(10);

            // 設定オプション
            DrawSettingsOptions();

            GUILayout.Space(10);

            // パッケージリスト表示
            DrawPackageList();

            GUILayout.Space(20);

            // インポートボタン
            DrawImportButtons();
        }

        private void DrawSettingsOptions()
        {
            EditorGUILayout.LabelField(LocalizationManager.GetText("BPMImport_importSettings"), _headerStyle);

            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                _showChildAssetSettings = EditorGUILayout.Toggle(LocalizationManager.GetText("BPMImport_showChildAssetSettings"), _showChildAssetSettings);
                EditorGUILayout.HelpBox(
                    _showChildAssetSettings
                        ? LocalizationManager.GetText("BPMImport_childAssetSettingsEnabled")
                        : LocalizationManager.GetText("BPMImport_childAssetSettingsDisabled"),
                    MessageType.Info);
            }
        }

        private void DrawPackageList()
        {
            EditorGUILayout.LabelField(LocalizationManager.GetText("BPMImport_packageList"), _headerStyle);

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_packageListScrollPosition, GUILayout.Height(400)))
            {
                _packageListScrollPosition = scrollScope.scrollPosition;

                // 未登録のアセットのみを取得
                var unregisteredAssets = FindUnregisteredAssets();

                if (unregisteredAssets.Count == 0)
                {
                    // 未登録のアセットが存在しない場合のメッセージ
                    EditorGUILayout.Space(20);
                    EditorGUILayout.LabelField(
                        LocalizationManager.GetText("BPMImport_noUnregisteredAssets"),
                        _messageStyle
                    );
                    EditorGUILayout.Space(20);
                }
                else
                {
                    foreach (var author in _bmpLibrary.authors)
                    {
                        string authorName = author.Key;

                        foreach (var package in author.Value)
                        {
                            string packageKey = $"{authorName}|{package.itemUrl}";

                            // 未登録のファイルが存在するパッケージのみ表示
                            if (unregisteredAssets.ContainsKey(packageKey))
                            {
                                DrawPackageItem(authorName, package, unregisteredAssets[packageKey]);
                            }
                        }
                    }
                }
            }
        }

        private void DrawPackageItem(string authorName, Data.BPMPackage package, List<Data.BPMFileInfo> unregisteredFiles)
        {
            string packageKey = $"{authorName}|{package.itemUrl}";

            using (new EditorGUILayout.VerticalScope(_boxStyle))
            {
                // パッケージヘッダー
                EditorGUILayout.LabelField($"[{authorName}] {package.packageName}", _packageHeaderStyle);

                if (unregisteredFiles.Count > 1)
                {
                    // パッケージ全体の設定（複数ファイルの場合）
                    if (!_packageSettings.ContainsKey(packageKey))
                    {
                        _packageSettings[packageKey] = new AssetImportSettings
                        {
                            assetType = _assetTypeOptions[_defaultAssetTypeIndex]
                        };
                    }

                    var packageSetting = _packageSettings[packageKey];

                    // アセットタイプをセレクタで選択
                    int currentIndex = GetAssetTypeIndex(packageSetting.assetType);
                    int newIndex = EditorGUILayout.Popup(LocalizationManager.GetText("BPMImport_assetType"), currentIndex, _assetTypeOptions);

                    if (newIndex != currentIndex && newIndex >= 0 && newIndex < _assetTypeOptions.Length)
                    {
                        packageSetting.assetType = _assetTypeOptions[newIndex];
                    }
                }

                // 未登録ファイルリストのみ表示
                foreach (var file in unregisteredFiles)
                {
                    DrawFileItem(authorName, package, file, unregisteredFiles.Count == 1);
                }
            }

            GUILayout.Space(5);
        }

        private void DrawFileItem(string authorName, Data.BPMPackage package, Data.BPMFileInfo file, bool isSingleFile)
        {
            string fileKey = $"{authorName}|{package.itemUrl}|{file.fileName}";

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"  • {file.fileName}", _fileItemStyle);

                // アセットタイプ設定を表示する条件：
                // 1. 単一ファイルの場合は常に表示
                // 2. 複数ファイルの場合は _showChildAssetSettings が true の時のみ表示
                bool showAssetTypeSettings = isSingleFile || _showChildAssetSettings;

                if (showAssetTypeSettings)
                {
                    // 個別ファイル設定
                    if (!_fileSettings.ContainsKey(fileKey))
                    {
                        _fileSettings[fileKey] = new AssetImportSettings
                        {
                            assetType = _assetTypeOptions[_defaultAssetTypeIndex]
                        };
                    }

                    var fileSetting = _fileSettings[fileKey];

                    // アセットタイプをセレクタで選択（右揃え）
                    int currentIndex = GetAssetTypeIndex(fileSetting.assetType);
                    int newIndex = EditorGUILayout.Popup(currentIndex, _assetTypeOptions, GUILayout.Width(120));

                    if (newIndex != currentIndex && newIndex >= 0 && newIndex < _assetTypeOptions.Length)
                    {
                        fileSetting.assetType = _assetTypeOptions[newIndex];
                    }
                }
                else
                {
                    // 設定を表示しない場合でも、設定オブジェクトは作成しておく（グループのみ設定で使用）
                    if (!_fileSettings.ContainsKey(fileKey))
                    {
                        _fileSettings[fileKey] = new AssetImportSettings
                        {
                            assetType = _assetTypeOptions[_defaultAssetTypeIndex]
                        };
                    }
                }
            }
        }

        private void DrawImportButtons()
        {
            // 未登録のアセットがあるかチェック
            var unregisteredAssets = FindUnregisteredAssets();
            bool hasUnregisteredAssets = unregisteredAssets.Count > 0;

            EditorGUI.BeginDisabledGroup(!hasUnregisteredAssets);
            if (GUILayout.Button(LocalizationManager.GetText("BPMImport_importUnregistered"), GUILayout.Height(30)))
            {
                ImportUnregisteredAssets();
            }
            EditorGUI.EndDisabledGroup();
        }



        private async void ImportUnregisteredAssets()
        {
            try
            {
                _isLoading = true;
                _statusMessage = LocalizationManager.GetText("BPMImport_importing");
                Repaint();

                var unregisteredAssets = FindUnregisteredAssets();
                var importedAssets = await _assetDataManager.ImportFromBPMLibraryWithIndividualSettingsAsync(
                    _packageSettings, _fileSettings, unregisteredAssets);

                _statusMessage = string.Format(LocalizationManager.GetText("BPMImport_importComplete"), importedAssets.Count);
                _onImportComplete?.Invoke();
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Import failed: {ex.Message}";
                Debug.LogError($"[BPMImportWindow] Import failed: {ex}");
            }
            finally
            {
                _isLoading = false;
                Repaint();
            }
        }

        private Dictionary<string, List<Data.BPMFileInfo>> FindUnregisteredAssets()
        {
            var unregistered = new Dictionary<string, List<Data.BPMFileInfo>>();
            var existingUrls = _assetDataManager.GetAllAssets()
                .Where(a => a.boothItem != null && !string.IsNullOrEmpty(a.boothItem.boothDownloadUrl))
                .Select(a => a.boothItem.boothDownloadUrl)
                .ToHashSet();

            foreach (var author in _bmpLibrary.authors)
            {
                string authorName = author.Key;
                foreach (var package in author.Value)
                {
                    string packageKey = $"{authorName}|{package.itemUrl}";
                    var unregisteredFiles = new List<Data.BPMFileInfo>();

                    if (package.files != null)
                    {
                        foreach (var file in package.files)
                        {
                            if (!existingUrls.Contains(file.downloadLink))
                            {
                                unregisteredFiles.Add(file);
                            }
                        }
                    }

                    if (unregisteredFiles.Count > 0)
                    {
                        unregistered[packageKey] = unregisteredFiles;
                    }
                }
            }

            return unregistered;
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _packageHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            _fileItemStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal = { textColor = Color.gray }
            };

            _messageStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };

            _stylesInitialized = true;
        }

        private void InitializeAssetTypeOptions()
        {
            var allTypes = AssetTypeManager.AllTypes;
            _assetTypeOptions = allTypes.ToArray();

            _defaultAssetTypeIndex = 0;
            for (int i = 0; i < _assetTypeOptions.Length; i++)
            {
                if (_assetTypeOptions[i] == "Other")
                {
                    _defaultAssetTypeIndex = i;
                    break;
                }
            }
        }

        private int GetAssetTypeIndex(string assetType)
        {
            if (string.IsNullOrEmpty(assetType))
                return _defaultAssetTypeIndex;

            for (int i = 0; i < _assetTypeOptions.Length; i++)
            {
                if (_assetTypeOptions[i] == assetType)
                    return i;
            }
            return _defaultAssetTypeIndex;
        }
    }
}
