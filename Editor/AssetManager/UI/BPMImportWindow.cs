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
        private AssetDataManager _assetDataManager;
        private BPMDataManager _bpmDataManager;
        private Action _onImportComplete;

        private string _selectedAssetType = "Avatar";
        private List<string> _selectedTags = new List<string>();
        private string _newTag = "";
        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 _tagsScrollPosition = Vector2.zero;

        private bool _isLoading = false;
        private string _statusMessage = "";

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInitialized = false;

        public static void ShowWindow(AssetDataManager assetDataManager, Action onImportComplete = null)
        {
            var window = GetWindow<BPMImportWindow>(LocalizationManager.GetText("BPMImport_windowTitle"));
            window.minSize = new Vector2(500, 400);
            window.maxSize = new Vector2(500, 400);
            window._assetDataManager = assetDataManager;
            window._onImportComplete = onImportComplete;
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
                    DrawImportSettings();
                    DrawImportButton();
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
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label("Import Settings", EditorStyles.boldLabel);
                GUILayout.Space(10);

                // Asset Type selection
                GUILayout.Label(LocalizationManager.GetText("BPMImport_assetType"), EditorStyles.label);
                var allTypes = AssetTypeManager.AllTypes;
                int selectedIndex = allTypes.IndexOf(_selectedAssetType);
                if (selectedIndex == -1) selectedIndex = 0;

                selectedIndex = EditorGUILayout.Popup(selectedIndex, allTypes.ToArray());
                if (selectedIndex >= 0 && selectedIndex < allTypes.Count)
                {
                    _selectedAssetType = allTypes[selectedIndex];
                }

                GUILayout.Space(10);

                // Tags section
                GUILayout.Label(LocalizationManager.GetText("BPMImport_tags"), EditorStyles.label);

                // New tag input
                using (new GUILayout.HorizontalScope())
                {
                    _newTag = EditorGUILayout.TextField(LocalizationManager.GetText("BPMImport_addTag"), _newTag);
                    if (GUILayout.Button("Add", GUILayout.Width(50)) && !string.IsNullOrWhiteSpace(_newTag))
                    {
                        if (!_selectedTags.Contains(_newTag.Trim()))
                        {
                            _selectedTags.Add(_newTag.Trim());
                        }
                        _newTag = "";
                    }
                }

                // Selected tags display
                if (_selectedTags.Count > 0)
                {
                    using (var scrollView = new GUILayout.ScrollViewScope(_tagsScrollPosition, GUILayout.Height(80)))
                    {
                        _tagsScrollPosition = scrollView.scrollPosition;

                        for (int i = _selectedTags.Count - 1; i >= 0; i--)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(_selectedTags[i], EditorStyles.miniLabel);
                                if (GUILayout.Button("×", GUILayout.Width(20)))
                                {
                                    _selectedTags.RemoveAt(i);
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

                var importedAssets = _assetDataManager.ImportFromBPMLibrary(
                    _bpmDataManager,
                    _selectedAssetType,
                    _selectedTags.Count > 0 ? _selectedTags : null
                );

                _isLoading = false;

                if (importedAssets.Count > 0)
                {
                    _statusMessage = string.Format(LocalizationManager.GetText("BPMImport_importSuccess"), importedAssets.Count);

                    // 完了後に少し待ってからウィンドウを閉じる
                    EditorApplication.delayCall += () =>
                    {
                        _onImportComplete?.Invoke();
                        EditorApplication.delayCall += () => Close();
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
