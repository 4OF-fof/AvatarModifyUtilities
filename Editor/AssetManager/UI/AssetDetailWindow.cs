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
            window.minSize = new Vector2(700, 400);
            window.maxSize = new Vector2(700, 400);

            // シングルトンインスタンスから最新データを取得
            var updatedAsset = AssetDataManager.Instance.GetAsset(asset.uid) ?? asset;

            window._asset = updatedAsset.Clone();
            window._originalAsset = updatedAsset;
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
            if (_dataManager?.Library?.assets != null)
            {
                _allAssets = _dataManager.GetAllAssets().ToList();
            }
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
            // シングルトンインスタンスを使用
            _dataManager = AssetDataManager.Instance;

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

                // タグ情報も更新                LoadAllTags();
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
                GUILayout.Space(10); // 左マージンを追加

                // サムネイルを中央に配置するためのフレキシブルスペース
                GUILayout.FlexibleSpace();

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
                        if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_selectThumbnail")))
                        {
                            SelectThumbnail();
                        }
                    }
                    else if (_fileManager.IsUnityPackage(_asset))
                    {
                        if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_importPackage"), GUILayout.Width(180)))
                        {
                            ImportUnityPackage();
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
                        _asset.name = EditorGUILayout.TextField(_asset.name, GUILayout.Width(360));
                    }
                    else
                    {
                        GUILayout.Label(_asset.name, GUILayout.Width(360));
                    }
                }                // Description
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_description"), GUILayout.Width(100));
                    if (_isEditMode)
                    {
                        _asset.description = EditorGUILayout.TextArea(_asset.description, GUILayout.Height(60), GUILayout.Width(360));
                    }
                    else
                    {
                        GUILayout.Label(_asset.description, EditorStyles.wordWrappedLabel, GUILayout.Width(360));
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

                        var newIndex = EditorGUILayout.Popup(currentIndex, allTypes.ToArray(), GUILayout.Width(360));
                        if (newIndex >= 0 && newIndex < allTypes.Count)
                        {
                            _asset.assetType = allTypes[newIndex];
                        }
                    }
                    else
                    {
                        GUILayout.Label(_asset.assetType, GUILayout.Width(360));
                    }
                }                // Author
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_author"), GUILayout.Width(100));
                    if (_isEditMode)
                    {
                        _asset.authorName = EditorGUILayout.TextField(_asset.authorName, GUILayout.Width(360));
                    }
                    else
                    {
                        GUILayout.Label(_asset.authorName, GUILayout.Width(360));
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
                            _asset.filePath = EditorGUILayout.TextField(_asset.filePath, GUILayout.Width(280));
                            if (GUILayout.Button(LocalizationManager.GetText("Common_browse"), GUILayout.Width(80)))
                            {
                                BrowseForFile();
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label(_asset.filePath, GUILayout.Width(360));
                    }
                }                // File Size
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_fileSize"), GUILayout.Width(100));
                    GUILayout.Label(_fileManager.FormatFileSize(_asset.fileSize), GUILayout.Width(360));
                }                // Created Date
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("AssetDetail_createdDate"), GUILayout.Width(100));
                    GUILayout.Label(_asset.createdDate.ToString("yyyy/MM/dd HH:mm:ss"), GUILayout.Width(360));
                }

            }
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
        private void ImportUnityPackage()
        {
            try
            {
                _fileManager.ImportUnityPackage(_asset);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to import Unity Package: {ex.Message}");
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
            if (!string.IsNullOrEmpty(_newDependency.Trim()))
            {
                var trimmedInput = _newDependency.Trim();

                // Try to find a matching asset by name first
                var matchingAsset = _allAssets.FirstOrDefault(a => a.name.Equals(trimmedInput, StringComparison.OrdinalIgnoreCase) && a.uid != _asset.uid);

                if (matchingAsset != null)
                {
                    // Add the asset UID if it's an existing asset
                    if (!_asset.dependencies.Contains(matchingAsset.uid))
                    {
                        _asset.dependencies.Add(matchingAsset.uid);
                    }
                }
                else
                {
                    // Add as manual dependency if it's not an existing asset
                    if (!_asset.dependencies.Contains(trimmedInput))
                    {
                        _asset.dependencies.Add(trimmedInput);
                    }
                }

                _newDependency = "";
                _showDependencySuggestions = false;
                GUI.FocusControl(null);
            }
        }

        private void DrawDependencyInput()
        {
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

            var input = _newDependency.ToLower();
            foreach (var asset in _allAssets)
            {
                // Skip self and already added dependencies
                if (asset.uid == _asset.uid || _asset.dependencies.Contains(asset.uid))
                    continue;

                // Filter assets that contain the input text in their name
                if (asset.name.ToLower().Contains(input))
                {
                    _filteredAssets.Add(asset);
                }
            }

            _showDependencySuggestions = _filteredAssets.Count > 0;
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
                _dataManager.UpdateAsset(_asset);
                _originalAsset = _asset.Clone();
                _isEditMode = false;

                Debug.Log($"アセット情報を保存しました: {_asset.name}");

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
                    Debug.Log($"サムネイルが保存されアセット情報を更新しました: {asset.name}");
                    Repaint();
                }
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
    }
}
