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
        private Vector2 _scrollPosition = Vector2.zero;

        private AssetDataManager _dataManager;
        private AssetThumbnailManager _thumbnailManager;
        private AssetFileManager _fileManager;

        // UI state for tags and dependencies
        private string _newTag = "";
        private string _newDependency = ""; private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            AssetTypeManager.LoadCustomTypes();
            InitializeManagers();
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
                            using (new GUILayout.HorizontalScope())
                            {
                                _newTag = EditorGUILayout.TextField(_newTag);
                                if (GUILayout.Button(LocalizationManager.GetText("AssetDetail_addTag"), GUILayout.Width(80)))
                                {
                                    AddTag();
                                }
                            }
                        }
                    }
                }

                GUILayout.Space(10);

                // Dependencies
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
                                    GUILayout.Label(_asset.dependencies[i]);
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
                            using (new GUILayout.HorizontalScope())
                            {
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

        private void AddTag()
        {
            if (!string.IsNullOrEmpty(_newTag) && !_asset.tags.Contains(_newTag))
            {
                _asset.tags.Add(_newTag);
                _newTag = "";
                GUI.FocusControl(null);
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
            _dataManager.UpdateAsset(_asset);
            _originalAsset = _asset.Clone();
            _isEditMode = false;

            // Refresh the main window
            var mainWindow = GetWindow<AssetManagerWindow>();
            if (mainWindow != null)
            {
                EditorApplication.delayCall += () => mainWindow.Repaint();
            }
        }

        private void CancelEdit()
        {
            _asset = _originalAsset?.Clone();
            _isEditMode = false;
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
