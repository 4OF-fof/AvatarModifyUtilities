using System;
using System.Collections.Generic;
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
            window.minSize = new Vector2(600, 500);
            window._asset = asset?.Clone();
            window._originalAsset = asset;
            window._isEditMode = editMode;
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
        private int _dependencySelectionMode = 0; // 0: Asset Selection, 1: Manual Input
        private int _selectedAssetIndex = -1;
        private List<AssetInfo> _availableAssets = new List<AssetInfo>();        // Tag suggestion state
        private List<string> _allTags = new List<string>();
        private List<string> _filteredTags = new List<string>();
        private bool _showTagSuggestions = false;
        private Vector2 _tagSuggestionScrollPos = Vector2.zero;

        // UI Style
        private GUIStyle _tabStyle;
        private bool _stylesInitialized = false; private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            AssetTypeManager.LoadCustomTypes();
            InitializeManagers();
            LoadTagSuggestions();
        }

        private void LoadTagSuggestions()
        {
            // 新しいTagTypeManagerからタグ一覧を取得
            _allTags = AssetTagManager.GetAllTagsFromTagTypeManager();
        }
        private void OnDisable()
        {
            _thumbnailManager?.ClearCache();
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
            if (_dataManager == null)
            {
                _dataManager = new AssetDataManager();
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

            LoadAllTags();
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
                        _isEditMode = true;
                        // Reset UI state when entering edit mode
                        _newTag = "";
                        _newDependency = "";
                        _selectedAssetIndex = -1;
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
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_thumbnail"), EditorStyles.boldLabel);

                _thumbnailManager.DrawThumbnail(_asset, 180);

                if (_isEditMode)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_selectThumbnail")))
                    {
                        SelectThumbnail();
                    }
                }
            }
        }

        private void DrawDetailsSection()
        {
            using (new GUILayout.VerticalScope())
            {
                DrawGeneralInfo();
                GUILayout.Space(10);
                DrawFileInfo();
                GUILayout.Space(10);
                DrawTagsAndDependencies();
            }
        }

        private void DrawGeneralInfo()
        {
            GUILayout.Label(LocalizationManager.GetText("AssetDetail_generalInfo"), EditorStyles.boldLabel);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Name
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_name"), GUILayout.Width(100));
                if (_isEditMode)
                {
                    _asset.name = EditorGUILayout.TextField(_asset.name);
                }
                else
                {
                    GUILayout.Label(_asset.name);
                }
                GUILayout.EndHorizontal();

                // Description
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_description"), GUILayout.Width(100));
                if (_isEditMode)
                {
                    _asset.description = EditorGUILayout.TextArea(_asset.description, GUILayout.Height(60));
                }
                else
                {
                    GUILayout.Label(_asset.description, EditorStyles.wordWrappedLabel);
                }
                GUILayout.EndHorizontal();                // Type
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_type"), GUILayout.Width(100));
                if (_isEditMode)
                {
                    var allTypes = AssetTypeManager.AllTypes;
                    var currentIndex = allTypes.IndexOf(_asset.assetType);
                    if (currentIndex < 0) currentIndex = 0;

                    var newIndex = EditorGUILayout.Popup(currentIndex, allTypes.ToArray());
                    if (newIndex >= 0 && newIndex < allTypes.Count)
                    {
                        _asset.assetType = allTypes[newIndex];
                    }
                }
                else
                {
                    GUILayout.Label(_asset.assetType);
                }
                GUILayout.EndHorizontal();

                // Author
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_author"), GUILayout.Width(100));
                if (_isEditMode)
                {
                    _asset.authorName = EditorGUILayout.TextField(_asset.authorName);
                }
                else
                {
                    GUILayout.Label(_asset.authorName);
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawFileInfo()
        {
            GUILayout.Label(LocalizationManager.GetText("AssetDetail_fileInfo"), EditorStyles.boldLabel);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // File Path
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_filePath"), GUILayout.Width(100));
                if (_isEditMode)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        _asset.filePath = EditorGUILayout.TextField(_asset.filePath);
                        if (GUILayout.Button(LocalizationManager.GetText("Common_browse"), GUILayout.Width(80)))
                        {
                            BrowseForFile();
                        }
                    }
                }
                else
                {
                    GUILayout.Label(_asset.filePath);
                }
                GUILayout.EndHorizontal();

                // File Size
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_fileSize"), GUILayout.Width(100));
                GUILayout.Label(_fileManager.FormatFileSize(_asset.fileSize));
                GUILayout.EndHorizontal();

                // Created Date
                GUILayout.BeginHorizontal();
                GUILayout.Label(LocalizationManager.GetText("AssetDetail_createdDate"), GUILayout.Width(100));
                GUILayout.Label(_asset.createdDate.ToString("yyyy/MM/dd HH:mm:ss"));
                GUILayout.EndHorizontal();

            }
        }

        private void DrawTagsAndDependencies()
        {
            using (new GUILayout.HorizontalScope())
            {
                // Tags
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
                                    GUILayout.Label(_asset.tags[i]);
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

                GUILayout.Space(10);                // Dependencies
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
                            // Selection mode toggle
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("Mode:", GUILayout.Width(40));
                                string[] modes = {
                                    LocalizationManager.GetText("AssetDetail_dependencyModeAsset"),
                                    LocalizationManager.GetText("AssetDetail_dependencyModeManual")
                                };
                                _dependencySelectionMode = GUILayout.Toolbar(_dependencySelectionMode, modes, _tabStyle);
                            }

                            GUILayout.Space(3);

                            using (new GUILayout.HorizontalScope())
                            {
                                if (_dependencySelectionMode == 0)
                                {
                                    // Asset selection mode
                                    _availableAssets = _dataManager.GetAllAssets().Where(a => a.uid != _asset.uid).ToList(); // Exclude self
                                    var assetNames = _availableAssets.Select(a => a.name).ToArray();

                                    if (assetNames.Length > 0)
                                    {
                                        _selectedAssetIndex = EditorGUILayout.Popup(_selectedAssetIndex, assetNames);

                                        if (_selectedAssetIndex >= 0 && _selectedAssetIndex < _availableAssets.Count)
                                        {
                                            if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_addDependency"), GUILayout.Width(80)))
                                            {
                                                var selectedAsset = _availableAssets[_selectedAssetIndex];
                                                if (!_asset.dependencies.Contains(selectedAsset.uid))
                                                {
                                                    _asset.dependencies.Add(selectedAsset.uid); // Add UUID instead of name
                                                    _selectedAssetIndex = -1; // Reset selection
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GUILayout.Label(LocalizationManager.GetText("AssetDetail_noOtherAssets"), EditorStyles.miniLabel);
                                    }
                                }
                                else
                                {
                                    // Manual input mode
                                    _newDependency = EditorGUILayout.TextField(_newDependency);
                                    if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_addDependency"), GUILayout.Width(80)))
                                    {
                                        AddDependency();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        private void SelectThumbnail()
        {
            string path = EditorUtility.OpenFilePanel("Select Thumbnail", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                _thumbnailManager.SetCustomThumbnail(_asset, path);
            }
        }

        private void BrowseForFile()
        {
            string path = EditorUtility.OpenFilePanel("Select Asset File", Application.dataPath, "");
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
            if (!string.IsNullOrEmpty(_newDependency) && !_asset.dependencies.Contains(_newDependency))
            {
                _asset.dependencies.Add(_newDependency);
                _newDependency = "";
                GUI.FocusControl(null);
            }
        }
        private void SaveAsset()
        {
            try
            {
                _dataManager.UpdateAsset(_asset);
                _originalAsset = _asset.Clone();
                _isEditMode = false;

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
            _asset = _originalAsset?.Clone();
            _isEditMode = false;
            // Reset UI state when canceling edit
            _newTag = "";
            _newDependency = "";
            _selectedAssetIndex = -1;
        }

        private void OnThumbnailSaved(AssetInfo asset)
        {
            if (asset != null && _dataManager != null)
            {
                _dataManager.UpdateAsset(asset);
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

                _newTag = "";
                _showTagSuggestions = false;
                GUI.FocusControl(null);
            }
        }
    }
}
