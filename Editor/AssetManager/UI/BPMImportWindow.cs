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
            public string assetType = "Other";
        }

        private AssetDataManager _assetDataManager;
        private BPMDataManager _bpmDataManager;
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
        private GUIStyle _packageHeaderStyle; private GUIStyle _fileItemStyle;
        private bool _stylesInitialized = false;

        public static void ShowWindowWithFile(AssetDataManager assetDataManager, string bpmLibraryPath, Action onImportComplete = null)
        {
            var window = GetWindow<BPMImportWindow>(LocalizationManager.GetText("BPMImport_windowTitle"));
            window.minSize = new Vector2(600, 800);
            window.maxSize = new Vector2(600, 800);
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
                _statusMessage = string.Format(LocalizationManager.GetText("BPMImport_loadFromFileError"), ex.Message);
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
            UpdateExistingDownloadUrls();
            _statusMessage = string.Format(LocalizationManager.GetText("BPMImport_loadSuccess"), GetUnregisteredPackageCount());
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

        private int GetUnregisteredPackageCount()
        {
            if (_bpmDataManager?.Library?.authors == null)
                return 0;

            int count = 0;
            foreach (var author in _bpmDataManager.Library.authors)
            {
                foreach (var package in author.Value)
                {
                    if (HasUnregisteredFiles(package))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private void UpdateExistingDownloadUrls()
        {
            _existingDownloadUrls.Clear();
            if (_assetDataManager?.Library?.assets != null)
            {
                foreach (var asset in _assetDataManager.Library.assets)
                {
                    if (!string.IsNullOrEmpty(asset.boothItem?.boothDownloadUrl))
                    {
                        _existingDownloadUrls.Add(asset.boothItem.boothDownloadUrl);
                    }
                }
            }
        }

        private bool HasUnregisteredFiles(BPMPackage package)
        {
            if (package.files == null) return false;

            foreach (var file in package.files)
            {
                if (!string.IsNullOrEmpty(file.downloadLink) && !_existingDownloadUrls.Contains(file.downloadLink))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsFileUnregistered(BPMFileInfo file)
        {
            return !string.IsNullOrEmpty(file.downloadLink) && !_existingDownloadUrls.Contains(file.downloadLink);
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
                        DrawIndividualSettings();
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
        private void DrawIndividualSettings()
        {
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label(LocalizationManager.GetText("BPMImport_individualSettings"), EditorStyles.boldLabel);
                GUILayout.Space(5);
                GUILayout.Label(LocalizationManager.GetText("BPMImport_individualSettingsDesc"), EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(10);

                if (_bpmDataManager?.Library?.authors != null)
                {
                    bool hasUnregisteredAssets = false;

                    using (var scrollView = new GUILayout.ScrollViewScope(_packageListScrollPosition))
                    {
                        _packageListScrollPosition = scrollView.scrollPosition;

                        foreach (var author in _bpmDataManager.Library.authors)
                        {
                            string authorName = author.Key;
                            var unregisteredPackages = author.Value.Where(p => HasUnregisteredFiles(p)).ToList();

                            if (unregisteredPackages.Count > 0)
                            {
                                hasUnregisteredAssets = true;
                                DrawAuthorSection(authorName, author.Value);
                            }
                        }
                    }

                    if (!hasUnregisteredAssets)
                    {
                        GUILayout.Space(20);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("All assets are already registered.", EditorStyles.centeredGreyMiniLabel);
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.Space(20);
                    }
                }
            }
        }
        private void DrawAuthorSection(string authorName, List<BPMPackage> packages)
        {
            // 未登録のパッケージのみをフィルタリング
            var unregisteredPackages = packages.Where(p => HasUnregisteredFiles(p)).ToList();

            if (unregisteredPackages.Count == 0)
                return; // 未登録のものがない場合はこのセクションを表示しない

            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label($"Author: {authorName}", EditorStyles.boldLabel);

                foreach (var package in unregisteredPackages)
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
                GUILayout.Label(package.packageName ?? LocalizationManager.GetText("BPMImport_unknownPackage"), _packageHeaderStyle);

                // 未登録ファイルのみをフィルタリング
                var unregisteredFiles = package.files?.Where(f => IsFileUnregistered(f)).ToList() ?? new List<BPMFileInfo>();

                if (unregisteredFiles.Count == 0)
                    return; // 未登録ファイルがない場合は表示しない

                // グループ化されるかどうかの表示
                bool isGrouped = unregisteredFiles.Count > 1;
                if (isGrouped)
                {
                    GUILayout.Label(string.Format(LocalizationManager.GetText("BPMImport_groupFiles"), unregisteredFiles.Count), EditorStyles.miniLabel);

                    // グループ設定
                    string packageKey = $"{authorName}|{package.itemUrl}";
                    if (!_packageSettings.ContainsKey(packageKey))
                    {
                        _packageSettings[packageKey] = new AssetImportSettings();
                    }

                    DrawAssetSettings(_packageSettings[packageKey], LocalizationManager.GetText("BPMImport_groupSettings"));
                }

                // 個別ファイル（未登録のもののみ）
                foreach (var file in unregisteredFiles)
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
        private void DrawAssetSettings(AssetImportSettings settings, string label)
        {
            using (new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label(label, EditorStyles.miniLabel);

                // Asset Type
                var allTypes = AssetTypeManager.AllTypes;
                int selectedIndex = allTypes.IndexOf(settings.assetType);
                if (selectedIndex == -1) selectedIndex = 0;

                selectedIndex = EditorGUILayout.Popup(LocalizationManager.GetText("BPMImport_type"), selectedIndex, allTypes.ToArray());
                if (selectedIndex >= 0 && selectedIndex < allTypes.Count)
                {
                    settings.assetType = allTypes[selectedIndex];
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

                // 未登録のアセット情報を収集
                var unregisteredAssets = CollectUnregisteredAssets();

                // 個別設定を使用（未登録のアセットのみをインポート）
                importedAssets = _assetDataManager.ImportFromBPMLibraryWithIndividualSettings(
                    _bpmDataManager,
                    _packageSettings,
                    _fileSettings,
                    unregisteredAssets
                );

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

        /// <summary>
        /// 未登録のアセット情報を収集
        /// </summary>
        private Dictionary<string, List<BPMFileInfo>> CollectUnregisteredAssets()
        {
            var unregisteredAssets = new Dictionary<string, List<BPMFileInfo>>();

            if (_bpmDataManager?.Library?.authors == null)
                return unregisteredAssets;

            foreach (var author in _bpmDataManager.Library.authors)
            {
                foreach (var package in author.Value)
                {
                    if (package.files == null) continue;

                    var unregisteredFiles = package.files.Where(f => IsFileUnregistered(f)).ToList();
                    if (unregisteredFiles.Count > 0)
                    {
                        string packageKey = $"{author.Key}|{package.itemUrl}";
                        unregisteredAssets[packageKey] = unregisteredFiles;
                    }
                }
            }

            return unregisteredAssets;
        }
    }
}
