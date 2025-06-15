using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.BoothPackageManager.Helper;
using AMU.Data.Lang;

namespace AMU.AssetManager.UI
{
    public class BPMImportWindow : EditorWindow
    {
        [Serializable]
        public class AssetImportSettings
        {
            public string assetType = "Avatar";
            public List<string> tags = new List<string>();
        }

        private AssetDataManager _assetDataManager;
        private BPMDataManager _bpmDataManager;
        private Action _onImportComplete;

        // グローバル設定
        private string _globalAssetType = "Avatar";
        private List<string> _globalTags = new List<string>();
        private string _newGlobalTag = "";

        // 個別設定
        private Dictionary<string, AssetImportSettings> _packageSettings = new Dictionary<string, AssetImportSettings>();
        private Dictionary<string, AssetImportSettings> _fileSettings = new Dictionary<string, AssetImportSettings>();

        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 _globalTagsScrollPosition = Vector2.zero;
        private Vector2 _packageListScrollPosition = Vector2.zero;

        private bool _isLoading = false;
        private string _statusMessage = "";
        private bool _useGlobalSettings = true;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _packageHeaderStyle;
        private GUIStyle _fileItemStyle;
        private bool _stylesInitialized = false; public static void ShowWindow(AssetDataManager assetDataManager, Action onImportComplete = null)
        {
            var window = GetWindow<BPMImportWindow>(LocalizationManager.GetText("BPMImport_windowTitle"));
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(800, 800);
            window._assetDataManager = assetDataManager;
            window._onImportComplete = onImportComplete;
            window.Show();
        }

        public static void ShowWindowWithFile(AssetDataManager assetDataManager, string bpmLibraryPath, Action onImportComplete = null)
        {
            var window = GetWindow<BPMImportWindow>(LocalizationManager.GetText("BPMImport_windowTitle"));
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(800, 800);
            window._assetDataManager = assetDataManager;
            window._onImportComplete = onImportComplete;
            window.LoadFromSpecificFile(bpmLibraryPath);
            window.Show();
        }

        private void OnEnable()
        {
            _bpmDataManager = new BPMDataManager();
            _bpmDataManager.OnDataLoaded += OnBPMDataLoaded;
            _bpmDataManager.OnLoadError += OnBPMLoadError;

            // BPMデータの読み込み
            _isLoading = true;
            _statusMessage = LocalizationManager.GetText("BPMImport_loadingLibrary");
            _bpmDataManager.LoadJsonIfNeeded();
        }

        private void LoadFromSpecificFile(string filePath)
        {
            if (_bpmDataManager == null)
            {
                _bpmDataManager = new BPMDataManager();
                _bpmDataManager.OnDataLoaded += OnBPMDataLoaded;
                _bpmDataManager.OnLoadError += OnBPMLoadError;
            }

            _isLoading = true;
            _statusMessage = LocalizationManager.GetText("BPMImport_loadingLibrary");

            // 指定されたファイルから読み込み
            try
            {
                _bpmDataManager.LoadFromFile(filePath);
            }
            catch (System.Exception ex)
            {
                _isLoading = false;
                _statusMessage = $"Failed to load from file: {ex.Message}";
                Debug.LogError($"[BPMImportWindow] Failed to load from file: {ex}");
            }
        }

        private void OnDisable()
        {
            if (_bpmDataManager != null)
            {
                _bpmDataManager.OnDataLoaded -= OnBPMDataLoaded;
                _bpmDataManager.OnLoadError -= OnBPMLoadError;
            }
        }

        private void OnBPMDataLoaded()
        {
            _isLoading = false;
            _statusMessage = $"BPM Library loaded successfully. Found {GetTotalPackageCount()} packages.";
            Repaint();
        }

        private void OnBPMLoadError()
        {
            _isLoading = false;
            _statusMessage = $"Failed to load BPM Library: {_bpmDataManager?.LoadError ?? "Unknown error"}";
            Repaint();
        }

        private int GetTotalPackageCount()
        {
            if (_bpmDataManager?.Library?.authors == null)
                return 0;

            int count = 0;
            foreach (var author in _bpmDataManager.Library.authors)
            {
                count += author.Value?.Count ?? 0;
            }
            return count;
        }

        private void OnGUI()
        {
            InitializeStyles();

            using (new GUILayout.VerticalScope())
            {
                DrawHeader();

                if (_isLoading)
                {
                    DrawLoadingUI();
                }
                else if (_bpmDataManager?.Library?.authors == null || _bpmDataManager.Library.authors.Count == 0)
                {
                    DrawEmptyLibraryUI();
                }
                else
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition))
                    {
                        _scrollPosition = scrollView.scrollPosition;
                        DrawImportSettings();
                        DrawImportButton();
                    }
                }

                DrawStatusMessage();
            }
        }
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _packageHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            _fileItemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true,
                padding = new RectOffset(15, 5, 2, 2)
            };

            _stylesInitialized = true;
        }

        private void DrawHeader()
        {
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label(LocalizationManager.GetText("BPMImport_windowTitle"), _headerStyle);
                GUILayout.Space(5);
                GUILayout.Label(LocalizationManager.GetText("BPMImport_selectSettings"), EditorStyles.wordWrappedLabel);
            }
        }

        private void DrawLoadingUI()
        {
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.FlexibleSpace();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(LocalizationManager.GetText("BPMImport_loadingLibrary"), EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }

                var rect = GUILayoutUtility.GetRect(200, 20);
                EditorGUI.ProgressBar(rect, Mathf.PingPong(Time.realtimeSinceStartup, 1.0f), "");

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawEmptyLibraryUI()
        {
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.FlexibleSpace();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(LocalizationManager.GetText("BPMImport_libraryNotFound"), EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(LocalizationManager.GetText("BPMImport_ensureLibraryExists"), EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
        }
        private void DrawImportSettings()
        {
            // 設定モード選択
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label("Settings Mode", EditorStyles.boldLabel);

                bool newUseGlobalSettings = EditorGUILayout.Toggle("Use Global Settings", _useGlobalSettings);
                if (newUseGlobalSettings != _useGlobalSettings)
                {
                    _useGlobalSettings = newUseGlobalSettings;
                    if (_useGlobalSettings)
                    {
                        // グローバル設定に切り替えた時、個別設定をクリア
                        _packageSettings.Clear();
                        _fileSettings.Clear();
                    }
                }
            }

            GUILayout.Space(5);

            if (_useGlobalSettings)
            {
                DrawGlobalSettings();
            }
            else
            {
                DrawIndividualSettings();
            }
        }

        private void DrawGlobalSettings()
        {
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label("Global Import Settings", EditorStyles.boldLabel);
                GUILayout.Space(10);

                // Asset Type selection
                GUILayout.Label(LocalizationManager.GetText("BPMImport_assetType"), EditorStyles.label);
                var allTypes = AssetTypeManager.AllTypes;
                int selectedIndex = allTypes.IndexOf(_globalAssetType);
                if (selectedIndex == -1) selectedIndex = 0;

                selectedIndex = EditorGUILayout.Popup(selectedIndex, allTypes.ToArray());
                if (selectedIndex >= 0 && selectedIndex < allTypes.Count)
                {
                    _globalAssetType = allTypes[selectedIndex];
                }

                GUILayout.Space(10);

                // Tags section
                GUILayout.Label(LocalizationManager.GetText("BPMImport_tags"), EditorStyles.label);

                // New tag input
                using (new GUILayout.HorizontalScope())
                {
                    _newGlobalTag = EditorGUILayout.TextField(LocalizationManager.GetText("BPMImport_addTag"), _newGlobalTag);
                    if (GUILayout.Button("Add", GUILayout.Width(50)) && !string.IsNullOrWhiteSpace(_newGlobalTag))
                    {
                        if (!_globalTags.Contains(_newGlobalTag.Trim()))
                        {
                            _globalTags.Add(_newGlobalTag.Trim());
                        }
                        _newGlobalTag = "";
                    }
                }

                // Selected tags display
                if (_globalTags.Count > 0)
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(_globalTagsScrollPosition, GUILayout.Height(80)))
                    {
                        _globalTagsScrollPosition = scrollView.scrollPosition;

                        for (int i = _globalTags.Count - 1; i >= 0; i--)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(_globalTags[i], EditorStyles.miniLabel);
                                if (GUILayout.Button("×", GUILayout.Width(20)))
                                {
                                    _globalTags.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label(LocalizationManager.GetText("BPMImport_noTagsSelected"), EditorStyles.centeredGreyMiniLabel);
                }
            }
        }

        private void DrawIndividualSettings()
        {
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label("Individual Package Settings", EditorStyles.boldLabel);
                GUILayout.Space(5);
                GUILayout.Label("Configure tags and types for each package or file individually.", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(10);

                if (_bpmDataManager?.Library?.authors != null)
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(_packageListScrollPosition))
                    {
                        _packageListScrollPosition = scrollView.scrollPosition;

                        foreach (var author in _bpmDataManager.Library.authors)
                        {
                            string authorName = author.Key;
                            DrawAuthorSection(authorName, author.Value);
                        }
                    }
                }
            }
        }

        private void DrawAuthorSection(string authorName, List<BPMPackage> packages)
        {
            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label($"Author: {authorName}", EditorStyles.boldLabel);

                foreach (var package in packages)
                {
                    DrawPackageSection(package, authorName);
                }
            }
        }

        private void DrawPackageSection(BPMPackage package, string authorName)
        {
            using (new GUILayout.VerticalScope("box"))
            {
                // パッケージヘッダー
                GUILayout.Label(package.packageName ?? "Unknown Package", _packageHeaderStyle);

                // グループ化されるかどうかの表示
                bool isGrouped = package.files?.Count > 1;
                if (isGrouped)
                {
                    GUILayout.Label($"[Group - {package.files.Count} files]", EditorStyles.miniLabel);

                    // グループ設定
                    string packageKey = $"{authorName}|{package.itemUrl}";
                    if (!_packageSettings.ContainsKey(packageKey))
                    {
                        _packageSettings[packageKey] = new AssetImportSettings();
                    }

                    DrawAssetSettings(_packageSettings[packageKey], "Group Settings:");
                }

                // 個別ファイル
                if (package.files != null)
                {
                    foreach (var file in package.files)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(20);
                            using (new GUILayout.VerticalScope())
                            {
                                GUILayout.Label($"File: {file.fileName}", _fileItemStyle);

                                if (!isGrouped)
                                {
                                    // グループ化されない場合は個別設定
                                    string fileKey = $"{authorName}|{package.itemUrl}|{file.fileName}";
                                    if (!_fileSettings.ContainsKey(fileKey))
                                    {
                                        _fileSettings[fileKey] = new AssetImportSettings();
                                    }

                                    DrawAssetSettings(_fileSettings[fileKey], "Asset Settings:");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawAssetSettings(AssetImportSettings settings, string label)
        {
            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label(label, EditorStyles.miniLabel);

                // Asset Type
                var allTypes = AssetTypeManager.AllTypes;
                int selectedIndex = allTypes.IndexOf(settings.assetType);
                if (selectedIndex == -1) selectedIndex = 0;

                selectedIndex = EditorGUILayout.Popup("Type", selectedIndex, allTypes.ToArray());
                if (selectedIndex >= 0 && selectedIndex < allTypes.Count)
                {
                    settings.assetType = allTypes[selectedIndex];
                }

                // Tags
                using (new GUILayout.HorizontalScope())
                {
                    string newTag = EditorGUILayout.TextField("Add Tag", "");
                    if (GUILayout.Button("Add", GUILayout.Width(50)) && !string.IsNullOrWhiteSpace(newTag))
                    {
                        if (!settings.tags.Contains(newTag.Trim()))
                        {
                            settings.tags.Add(newTag.Trim());
                        }
                    }
                }

                // Display tags
                if (settings.tags.Count > 0)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Tags: ", GUILayout.Width(40));
                        using (new GUILayout.VerticalScope())
                        {
                            for (int i = settings.tags.Count - 1; i >= 0; i--)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Label(settings.tags[i], EditorStyles.miniLabel);
                                    if (GUILayout.Button("×", GUILayout.Width(20)))
                                    {
                                        settings.tags.RemoveAt(i);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawImportButton()
        {
            GUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                GUI.enabled = !_isLoading;
                if (GUILayout.Button(LocalizationManager.GetText("BPMImport_importAssets"), GUILayout.Width(120), GUILayout.Height(30)))
                {
                    PerformImport();
                }
                GUI.enabled = true;

                if (GUILayout.Button(LocalizationManager.GetText("Common_cancel"), GUILayout.Width(80), GUILayout.Height(30)))
                {
                    Close();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawStatusMessage()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(10);
                using (new GUILayout.VerticalScope(_boxStyle))
                {
                    GUILayout.Label(LocalizationManager.GetText("BPMImport_status"), EditorStyles.boldLabel);
                    GUILayout.Label(_statusMessage, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }
        private void PerformImport()
        {
            try
            {
                _isLoading = true;
                _statusMessage = LocalizationManager.GetText("BPMImport_importing");
                Repaint();

                List<AssetInfo> importedAssets;

                if (_useGlobalSettings)
                {
                    // グローバル設定を使用
                    importedAssets = _assetDataManager.ImportFromBPMLibrary(
                        _bpmDataManager,
                        _globalAssetType,
                        _globalTags.Count > 0 ? _globalTags : null
                    );
                }
                else
                {
                    // 個別設定を使用
                    importedAssets = _assetDataManager.ImportFromBPMLibraryWithIndividualSettings(
                        _bpmDataManager,
                        _packageSettings,
                        _fileSettings
                    );
                }

                _isLoading = false;

                if (importedAssets.Count > 0)
                {
                    _statusMessage = string.Format(LocalizationManager.GetText("BPMImport_importSuccess"), importedAssets.Count);

                    // サムネイル処理の完了を少し待ってからウィンドウを閉じる
                    EditorApplication.delayCall += () =>
                    {
                        _onImportComplete?.Invoke();
                        // サムネイル処理に時間がかかる場合があるため、追加の遅延を設ける
                        EditorApplication.delayCall += () =>
                        {
                            EditorApplication.delayCall += () => Close();
                        };
                    };
                }
                else
                {
                    _statusMessage = LocalizationManager.GetText("BPMImport_noNewAssets");
                }
            }
            catch (Exception ex)
            {
                _isLoading = false;
                _statusMessage = string.Format(LocalizationManager.GetText("BPMImport_importFailed"), ex.Message);
                Debug.LogError($"[BPMImportWindow] Import failed: {ex}");
            }

            Repaint();
        }
    }
}
