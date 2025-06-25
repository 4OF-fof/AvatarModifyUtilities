using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.UI.Components;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.Core.Api;
using System.IO;
using AMU.Editor.VrcAssetManager.Helper;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class AssetDetailWindow : EditorWindow
    {
        private AssetSchema _asset;
        private bool _isEditMode = false;
        private static List<Guid> _history = new List<Guid>();
        private static AssetSchema _currentAsset = null;
        private Vector2 _descScroll = Vector2.zero;
        private Vector2 _tagsScroll = Vector2.zero;
        private Vector2 _depsScroll = Vector2.zero;
        private Vector2 _childrenScroll = Vector2.zero;

        private string newName = string.Empty;
        private string newDescription = string.Empty;
        private string newAuthorName = string.Empty;
        private string newAssetType = string.Empty;
        private string newFilePath = string.Empty;
        private List<string> newChildAssetIds= new List<string>();
        private bool newIsFavorite = false;
        private bool newIsArchived = false;
        private List<string> newTags = new List<string>();
        private List<string> newDependencies = new List<string>();
        private List<string> newImportFiles = new List<string>();

        public static List<Guid> history { get => _history; set => _history = value; }

        public static void ShowWindow(AssetSchema asset, bool isBack = false)
        {
            if (!isBack)
            {
                if (_currentAsset != null && asset != null && _currentAsset.assetId != asset.assetId)
                {
                    _history.Add(_currentAsset.assetId);
                }
            }
            var window = GetWindow<AssetDetailWindow>();
            window._asset = asset;
            window.newChildAssetIds = asset.childAssetIds.ToList();
            window.newTags = asset.metadata.tags.ToList();
            window.newDependencies = asset.metadata.dependencies.ToList();
            window.newImportFiles = asset.fileInfo.importFiles.ToList();
            window.titleContent = new GUIContent(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_title") + ": " + asset.metadata.name);
            window.minSize = window.maxSize = new Vector2(800, 760);
            window.maximized = false;
            window.Show();
            _currentAsset = asset;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            var controller = AssetLibraryController.Instance;
            var sectionBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(16, 16, 12, 12),
                margin = new RectOffset(0, 0, 8, 8),
                normal = { background = MakeTex(2, 2, new Color(0.18f, 0.18f, 0.18f, 0.7f)) }
            };
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                normal = { textColor = new Color(0.9f, 0.7f, 1f) },
                margin = new RectOffset(0, 0, 0, 8)
            };
            var chipStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                normal = { textColor = Color.white, background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.5f, 0.8f)) },
                padding = new RectOffset(8, 8, 2, 2),
                margin = new RectOffset(2, 2, 2, 2),
                wordWrap = false,
                fixedHeight = 22
            };
            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            var valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white }
            };
            var dividerStyle = new GUIStyle(GUI.skin.box)
            {
                fixedHeight = 1,
                margin = new RectOffset(0, 0, 8, 8),
                normal = { background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.7f)) }
            };

            GUILayout.Space(8);

            using (new GUILayout.HorizontalScope())
            {
                if (_isEditMode)
                {
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_common_cancel"), GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        InitEditData(_asset);
                        GUI.FocusControl(null);
                        _isEditMode = false;
                    }
                }
                
                if (_history.Count > 0 && !_isEditMode)
                {
                    var backIcon = EditorGUIUtility.IconContent("ArrowNavigationLeft");
                    if (GUILayout.Button(backIcon, GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        if (controller != null && _history.Count > 0)
                        {
                            var prevId = _history[_history.Count - 1];
                            _history.RemoveAt(_history.Count - 1);
                            var prevAsset = controller.GetAsset(prevId);
                            if (prevAsset != null)
                            {
                                ShowWindow(prevAsset, true);
                                return;
                            }
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                
                if (!_isEditMode)
                {
                    var editIcon = EditorGUIUtility.IconContent("editicon.sml");
                    if (GUILayout.Button(editIcon, GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        InitEditData(_asset);
                        GUI.FocusControl(null);
                        _isEditMode = true;
                    }
                }
                else
                {
                    var saveIcon = EditorGUIUtility.IconContent("SaveActive");
                    if (GUILayout.Button(saveIcon, GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        if (!string.IsNullOrEmpty(newFilePath))
                        {
                            string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                            string absCoreDir = Path.GetFullPath(coreDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                            string absNewFilePath = Path.GetFullPath(newFilePath);
                            if (!absNewFilePath.StartsWith(absCoreDir, StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    string relPath = AssetFileUtility.MoveToCoreSubDirectory(absNewFilePath, "VrcAssetManager/Package", Path.GetFileName(absNewFilePath));
                                    newFilePath = relPath;
                                }
                                catch (Exception ex)
                                {
                                    EditorUtility.DisplayDialog(
                                        LocalizationAPI.GetText("VrcAssetManager_ui_error"),
                                        string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_moveFileFailed"), ex.Message),
                                        LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                                    return;
                                }
                            }
                            else
                            {
                                newFilePath = absNewFilePath.Substring(absCoreDir.Length).Replace('\\', '/');
                            }
                        }
                        _asset.metadata.SetName(newName);
                        _asset.metadata.SetDescription(newDescription);
                        _asset.metadata.SetAuthorName(newAuthorName);
                        _asset.metadata.SetAssetType(newAssetType);
                        _asset.fileInfo.SetFilePath(newFilePath);
                        _asset.SetChildAssetIds(newChildAssetIds);
                        _asset.state.SetFavorite(newIsFavorite);
                        _asset.state.SetArchived(newIsArchived);
                        _asset.metadata.SetTags(newTags);
                        _asset.metadata.SetDependencies(newDependencies);
                        _asset.fileInfo.SetImportFiles(newImportFiles);
                        controller.UpdateAsset(_asset);
                        controller.OptimizeTags();
                        GUI.FocusControl(null);
                        _isEditMode = false;
                    }

                }
            }

            if (_asset == null)
            {
                EditorGUILayout.LabelField(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_noAsset"));
                return;
            }

            using (new GUILayout.VerticalScope(sectionBoxStyle))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    Rect thumbRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                    DrawThumbnailComponent.Draw(thumbRect, _asset);
                    if (_isEditMode)
                    {
                        var buttonRect = new Rect(
                            thumbRect.x + (thumbRect.width - 80) / 2,
                            thumbRect.y + (thumbRect.height - 25) / 2 + 45,
                            80,
                            25
                        );
                        if (GUI.Button(buttonRect, LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_updateThumbnail")))
                        {
                            string defaultPath = Path.Combine(
                                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                                "Downloads"
                            );
                            string selectedPath = EditorUtility.OpenFilePanel(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_selectThumbnail"), defaultPath, "png,jpg,jpeg");
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                                string absCoreDir = Path.GetFullPath(coreDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                                string absSelectedPath = Path.GetFullPath(selectedPath);
                                string relThumbPath;
                                if (absSelectedPath.StartsWith(absCoreDir, StringComparison.OrdinalIgnoreCase))
                                {
                                    relThumbPath = absSelectedPath.Substring(absCoreDir.Length).Replace('\\', '/');
                                }
                                else
                                {
                                    relThumbPath = AssetFileUtility.MoveToCoreSubDirectory(absSelectedPath, "VrcAssetManager/Thumbnail");
                                }
                                _asset.metadata.SetThumbnailPath(relThumbPath);
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(8);

                if (_isEditMode)
                {
                    newName = EditorGUILayout.TextField(newName, EditorStyles.textField);
                }
                else
                {
                    if (_asset.boothItem != null && !string.IsNullOrEmpty(_asset.boothItem.itemUrl))
                    {
                        if (GUILayout.Button(_asset.metadata.name, titleStyle))
                        {
                            Application.OpenURL(_asset.boothItem.itemUrl);
                        }
                    }
                    else
                    {
                        var normalTitleStyle = new GUIStyle(titleStyle);
                        normalTitleStyle.normal.textColor = EditorStyles.label.normal.textColor;
                        GUILayout.Label(_asset.metadata.name, normalTitleStyle);
                    }
                }

                if (!string.IsNullOrEmpty(_asset.metadata.description))
                {
                    if (_isEditMode)
                    {
                        using (new GUILayout.VerticalScope(sectionBoxStyle))
                        {
                            using (var _newDescScroll = new GUILayout.ScrollViewScope(_descScroll, GUILayout.Height(120)))
                            {
                                _descScroll = _newDescScroll.scrollPosition;
                                newDescription = EditorGUILayout.TextArea(newDescription, EditorStyles.textArea);
                            }
                        }
                    }
                    else
                    {
                        using (new GUILayout.VerticalScope(sectionBoxStyle))
                        {
                            using (var _newDescScroll = new GUILayout.ScrollViewScope(_descScroll, GUILayout.Height(120)))
                            {
                                _descScroll = _newDescScroll.scrollPosition;
                                EditorGUILayout.LabelField(_asset.metadata.description, EditorStyles.wordWrappedLabel);
                            }
                        }
                    }
                }

                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_author"), labelStyle, GUILayout.Width(70));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.metadata.authorName, valueStyle);
                    }
                    else
                    {
                        newAuthorName = EditorGUILayout.TextField(newAuthorName, EditorStyles.textField);
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_assetType"), labelStyle, GUILayout.Width(70));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.metadata.assetType, valueStyle);
                    }
                    else
                    {
                        var assetTypes = controller.GetAllAssetTypes().ToList();
                        int selectedIndex = Mathf.Max(0, assetTypes.IndexOf(newAssetType));
                        if (assetTypes.Count == 0)
                        {
                            GUILayout.Label(_asset.metadata.assetType, valueStyle);
                        }
                        else
                        {
                            selectedIndex = EditorGUILayout.Popup(selectedIndex, assetTypes.ToArray(), GUILayout.Width(180));
                            newAssetType = assetTypes.Count > 0 ? assetTypes[selectedIndex] : string.Empty;
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_created"), labelStyle, GUILayout.Width(70));
                    GUILayout.Label(_asset.metadata.createdDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_modified"), labelStyle, GUILayout.Width(70));
                    GUILayout.Label(_asset.metadata.modifiedDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (_asset.childAssetIds == null || _asset.childAssetIds.Count == 0)
                    {
                        GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_filePath"), labelStyle, GUILayout.Width(70));
                        if (!_isEditMode)
                        {
                            GUILayout.Label(_asset.fileInfo.filePath, valueStyle);
                        }
                        else
                        {
                            newFilePath = EditorGUILayout.TextField(newFilePath, EditorStyles.textField);
                            if (GUILayout.Button("...", GUILayout.Width(28)))
                            {
                                string defaultPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                                string downloads = Path.Combine(defaultPath, "Downloads");
                                var selectedPath = EditorUtility.OpenFilePanel(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_selectFile"), downloads, "");
                                if (!string.IsNullOrEmpty(selectedPath))
                                {
                                    newFilePath = selectedPath;
                                }
                            }
                        }
                    }
                }

                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_favorite"), labelStyle, GUILayout.Width(60));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.state.isFavorite ? LocalizationAPI.GetText("VrcAssetManager_common_yes") : LocalizationAPI.GetText("VrcAssetManager_common_no"), valueStyle);
                    }
                    else
                    {
                        newIsFavorite = EditorGUILayout.Toggle(newIsFavorite, GUILayout.Width(20));
                    }
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_archived"), labelStyle, GUILayout.Width(60));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.state.isArchived ? LocalizationAPI.GetText("VrcAssetManager_common_yes") : LocalizationAPI.GetText("VrcAssetManager_common_no"), valueStyle);
                    }
                    else
                    {
                        newIsArchived = EditorGUILayout.Toggle(newIsArchived, GUILayout.Width(20));
                    }
                }

                if (!string.IsNullOrEmpty(_asset.parentGroupId) && controller != null)
                {
                    var parentAsset = controller.GetAsset(Guid.Parse(_asset.parentGroupId));
                    if (parentAsset != null)
                    {
                        GUILayout.Space(4);

                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_parentAsset"), labelStyle, GUILayout.Width(70));

                            if (GUILayout.Button(parentAsset.metadata.name, chipStyle) && !_isEditMode)
                            {
                                if (_history.Count > 0 && _history[_history.Count - 1] == parentAsset.assetId)
                                {
                                    _history.RemoveAt(_history.Count - 1);
                                    ShowWindow(parentAsset, true);
                                }
                                else
                                {
                                    ShowWindow(parentAsset);
                                }
                            }
                        }
                    }
                }

                if (_asset.childAssetIds != null && _asset.childAssetIds.Count > 0 && controller != null)
                {
                    GUILayout.Space(4);

                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_childAssets"), labelStyle);

                    using (var _newChildrenScroll = new GUILayout.ScrollViewScope(_childrenScroll, GUILayout.Height(40)))
                    {
                        _childrenScroll = _newChildrenScroll.scrollPosition;
                        using (new GUILayout.HorizontalScope())
                        {
                            foreach (var childId in newChildAssetIds)
                            {
                                if (Guid.TryParse(childId, out var childGuid))
                                {
                                    var childAsset = controller.GetAsset(childGuid);
                                    if (childAsset != null)
                                    {
                                        if (GUILayout.Button(childAsset.metadata.name, chipStyle) && !_isEditMode)
                                        {
                                            if (_history.Count > 0 && _history[_history.Count - 1] == childAsset.assetId)
                                            {
                                                _history.RemoveAt(_history.Count - 1);
                                                ShowWindow(childAsset, true);

                                            }
                                            else
                                            {
                                                ShowWindow(childAsset);
                                            }
                                        }
                                    }
                                }
                            }
                            if (_isEditMode)
                                {
                                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(24), GUILayout.Height(24)))
                                {
                                    AssetSelectorWindow.ShowWindow(
                                        (selectedChildAssetIds) =>
                                        {
                                            newChildAssetIds = selectedChildAssetIds.ToList();
                                        },
                                        newChildAssetIds,
                                        true,
                                        1
                                    );
                                }
                            }
                        }
                    }
                }
                if ((_asset.fileInfo.importFiles.Count > 0) || (_isEditMode && ZipFileUtility.IsZipFile(_asset.fileInfo.filePath)))
                {
                    GUILayout.Space(4);
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_importFiles"), labelStyle);
                    using (var _newImportScroll = new GUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(40)))
                    {
                        _tagsScroll = _newImportScroll.scrollPosition;
                        using (new GUILayout.HorizontalScope())
                        {
                            var importFilesToShow = _isEditMode ? newImportFiles : _asset.fileInfo.importFiles;
                            foreach (var importFile in importFilesToShow)
                            {
                                GUILayout.Button(Path.GetFileName(importFile), chipStyle);
                            }
                            if (_isEditMode)
                            {
                                if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(24), GUILayout.Height(24)))
                                {
                                    ShowImportPathSelector();
                                }
                            }
                        }
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (_asset.metadata.tags.Count > 0 || _isEditMode)
                {
                    using (new GUILayout.VerticalScope(sectionBoxStyle))
                    {
                        GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_tags"), labelStyle);
                        using (var _newTagsScroll = new GUILayout.ScrollViewScope(_tagsScroll, GUILayout.Height(40)))
                        {
                            _tagsScroll = _newTagsScroll.scrollPosition;
                            using (new GUILayout.HorizontalScope())
                            {
                                foreach (var tag in newTags)
                                {
                                    if (GUILayout.Button(tag, chipStyle) && !_isEditMode)
                                    {
                                        if (controller != null)
                                        {
                                            controller.filterOptions.ClearFilter();
                                            controller.filterOptions.tags = new List<string> { tag };
                                            controller.filterOptions.tagsAnd = false;
                                        }

                                        ToolbarComponent.isUsingAdvancedSearch = true;
                                        VrcAssetManagerWindow.ShowWindow();
                                    }
                                }
                                if (_isEditMode)
                                {
                                    if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(24), GUILayout.Height(24)))
                                    {
                                        TagSelectorWindow.ShowWindow(
                                            (selectedTags) =>
                                            {
                                                newTags = selectedTags.ToList();
                                            },
                                            newTags,
                                            true,
                                            true
                                        );
                                    }
                                }
                            }
                        }
                    }
                }

                if (_asset.metadata.tags.Count > 0 && _asset.metadata.dependencies.Count > 0)
                    GUILayout.Space(5);

                if (_asset.metadata.dependencies.Count > 0 || _isEditMode)
                {
                    using (new GUILayout.VerticalScope(sectionBoxStyle))
                    {
                        GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_dependencies"), labelStyle);
                        using (var _newDepsScroll = new GUILayout.ScrollViewScope(_depsScroll, GUILayout.Height(40)))
                        {
                            _depsScroll = _newDepsScroll.scrollPosition;
                            using (new GUILayout.HorizontalScope())
                            {
                                foreach (var dep in newDependencies)
                                {
                                    string depName = dep;
                                    AssetSchema depAsset = null;

                                    if (controller != null)
                                    {
                                        depAsset = controller.GetAsset(new Guid(dep));
                                        if (depAsset != null && depAsset.metadata != null)
                                        {
                                            depName = depAsset.metadata.name;
                                        }
                                    }

                                    if (depAsset != null)
                                    {
                                        if (GUILayout.Button(depName, chipStyle) && !_isEditMode)
                                        {
                                            if (_history.Count > 0 && _history[_history.Count - 1] == depAsset.assetId)
                                            {
                                                _history.RemoveAt(_history.Count - 1);
                                                AssetDetailWindow.ShowWindow(depAsset, true);
                                            }
                                            else
                                            {
                                                AssetDetailWindow.ShowWindow(depAsset);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        GUILayout.Label(depName, chipStyle);
                                    }
                                }
                                if (_isEditMode)
                                {
                                    if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), GUILayout.Width(24), GUILayout.Height(24)))
                                    {
                                        AssetSelectorWindow.ShowWindow(
                                            (selectedDeps) =>
                                            {
                                                newDependencies = selectedDeps.ToList();
                                            },
                                            newDependencies,
                                            true,
                                            2
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (_asset != null && _asset.fileInfo != null && !string.IsNullOrEmpty(_asset.fileInfo.filePath) && !_isEditMode)
            {
                var filePath = _asset.fileInfo.filePath;
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var excludedExts = SettingAPI.GetSetting<string>("VrcAssetManager_excludedImportExtensions");
                if (ZipFileUtility.IsZipFile(filePath) && _asset.fileInfo.importFiles.Count == 0 || _asset.fileInfo.importFiles == null)
                {
                    GUILayout.Space(3);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_selectImportPath"), GUILayout.Width(240), GUILayout.Height(32)))
                        {
                            ShowImportPathSelector();
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
                else if ((excludedExts != null && !excludedExts.Contains(ext)) || _asset.fileInfo.importFiles.Count > 0)
                {
                    GUILayout.Space(3);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_import"), GUILayout.Width(240), GUILayout.Height(36)))
                        {
                            ImportAsset();
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            else if ((_asset.fileInfo == null || string.IsNullOrEmpty(_asset.fileInfo.filePath)) && _asset.boothItem != null && !string.IsNullOrEmpty(_asset.boothItem.downloadUrl) && !_isEditMode)
            {
                GUILayout.Space(3);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_download"), GUILayout.Width(240), GUILayout.Height(36)))
                    {
                        Application.OpenURL(_asset.boothItem.downloadUrl);
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void ImportAsset()
        {
            try
            {
                bool importSuccess = AssetImportUtility.ImportAsset(_asset, true);

                if (importSuccess)
                {
                    Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_importSuccess"), _asset.metadata.name));
                }
                else
                {
                    Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_importFailed"), _asset.metadata.name));
                    EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_warning"), LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_importFailedDialog"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_importFailedException"), ex.Message));
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_assetDetail_importFailedExceptionDialog"), ex.Message), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
            }
        }

        private void ShowImportPathSelector()
        {
            if (_asset == null || string.IsNullOrEmpty(_asset.fileInfo.filePath))
            {
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), LocalizationAPI.GetText("VrcAssetManager_ui_filePathNotSet"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                return;
            }

            if (!ZipFileUtility.IsZipFile(_asset.fileInfo.filePath))
            {
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), LocalizationAPI.GetText("VrcAssetManager_ui_notZipFile"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                return;
            }

            ImportPathSelectorWindow.ShowWindow((selectedPaths) =>
            {
                if (selectedPaths != null && selectedPaths.Count > 0)
                {
                    newImportFiles = selectedPaths.ToList();
                    Debug.Log($"[AssetDetailWindow] Selected import paths for asset '{_asset.metadata.name}': {string.Join(", ", selectedPaths)}");
                }
            },
            _asset,
            newImportFiles
            );
        }

        private void InitEditData(AssetSchema asset)
        {
            newName = asset.metadata.name;
            newDescription = asset.metadata.description;
            newAuthorName = asset.metadata.authorName;
            newAssetType = asset.metadata.assetType;
            if (asset.fileInfo != null && !string.IsNullOrEmpty(asset.fileInfo.filePath))
            {
                string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                string absPath = Path.Combine(Path.GetFullPath(coreDir), asset.fileInfo.filePath.Replace('/', Path.DirectorySeparatorChar));
                newFilePath = absPath;
            }
            else
            {
                newFilePath = string.Empty;
            }
            newIsFavorite = asset.state != null ? asset.state.isFavorite : false;
            newIsArchived = asset.state != null ? asset.state.isArchived : false;
            newTags = asset.metadata.tags.ToList();
            newDependencies = asset.metadata.dependencies.ToList();
            newImportFiles = asset.fileInfo.importFiles.Select(f => Path.GetFileName(f)).ToList();
        }
    }
}
