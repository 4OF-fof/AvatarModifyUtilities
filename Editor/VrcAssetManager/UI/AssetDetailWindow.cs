using System;
using System.Collections.Generic;
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

        private string newAuthorName = string.Empty;
        private string newAssetType = string.Empty;
        private string newFilePath = string.Empty;
        private bool newIsFavorite = false;
        private bool newIsArchived = false;

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
            window.titleContent = new GUIContent("Asset Detail: " + asset.metadata.name);
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
                normal = { textColor = Color.white }
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
                var backIcon = EditorGUIUtility.IconContent("ArrowNavigationLeft");
                if (_history.Count > 0)
                {
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
                        newAuthorName = _asset.metadata.authorName;
                        newAssetType = _asset.metadata.assetType;
                        if (_asset.fileInfo != null && !string.IsNullOrEmpty(_asset.fileInfo.filePath))
                        {
                            string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                            string absPath = Path.Combine(Path.GetFullPath(coreDir), _asset.fileInfo.filePath.Replace('/', Path.DirectorySeparatorChar));
                            newFilePath = absPath;
                        }
                        else
                        {
                            newFilePath = string.Empty;
                        }
                        newIsFavorite = _asset.state != null ? _asset.state.isFavorite : false;
                        newIsArchived = _asset.state != null ? _asset.state.isArchived : false;
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
                                    string relPath = AssetFileUtility.MoveToCoreSubDirectory(absNewFilePath, "VrcAssetManager/package", Path.GetFileName(absNewFilePath));
                                    newFilePath = relPath;
                                }
                                catch (Exception ex)
                                {
                                    EditorUtility.DisplayDialog("エラー", $"ファイルの移動に失敗しました: {ex.Message}", "OK");
                                    return;
                                }
                            }
                            else
                            {
                                newFilePath = absNewFilePath.Substring(absCoreDir.Length).Replace('\\', '/');
                            }
                        }
                        _asset.metadata.SetAuthorName(newAuthorName);
                        _asset.metadata.SetAssetType(newAssetType);
                        _asset.fileInfo.SetFilePath(newFilePath);
                        _asset.state.SetFavorite(newIsFavorite);
                        _asset.state.SetArchived(newIsArchived);
                        controller.UpdateAsset(_asset);
                        _isEditMode = false;
                    }

                }
            }

            if (_asset == null)
            {
                EditorGUILayout.LabelField("No asset selected.");
                return;
            }

            using (new GUILayout.VerticalScope(sectionBoxStyle))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    Rect thumbRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                    DrawThumbnailComponent.Draw(thumbRect, _asset);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(8);

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

                if (!string.IsNullOrEmpty(_asset.metadata.description))
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

                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Author:", labelStyle, GUILayout.Width(70));
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
                    GUILayout.Label("Asset Type:", labelStyle, GUILayout.Width(70));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.metadata.assetType, valueStyle);
                    }
                    else
                    {
                        newAssetType = EditorGUILayout.TextField(newAssetType, EditorStyles.textField);
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Created:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(_asset.metadata.createdDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Modified:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(_asset.metadata.modifiedDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("File Path:", labelStyle, GUILayout.Width(70));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.fileInfo.filePath, valueStyle);
                    }
                    else
                    {
                        // TextFieldの戻り値をnewFilePathに必ず代入する
                        newFilePath = EditorGUILayout.TextField(newFilePath, EditorStyles.textField);
                        if (GUILayout.Button("...", GUILayout.Width(28)))
                        {
                            string defaultPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                            string downloads = Path.Combine(defaultPath, "Downloads");
                            var selectedPath = EditorUtility.OpenFilePanel("ファイルを選択", downloads, "");
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                newFilePath = selectedPath;
                            }
                        }
                    }
                }

                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Favorite:", labelStyle, GUILayout.Width(70));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.state.isFavorite ? "Yes" : "No", valueStyle);
                    }
                    else
                    {
                        newIsFavorite = EditorGUILayout.Toggle(newIsFavorite, GUILayout.Width(20));
                        GUILayout.Label(newIsFavorite ? "Yes" : "No", valueStyle);
                    }
                    GUILayout.Label("Archived:", labelStyle, GUILayout.Width(70));
                    if (!_isEditMode)
                    {
                        GUILayout.Label(_asset.state.isArchived ? "Yes" : "No", valueStyle);
                    }
                    else
                    {
                        newIsArchived = EditorGUILayout.Toggle(newIsArchived, GUILayout.Width(20));
                        GUILayout.Label(newIsArchived ? "Yes" : "No", valueStyle);
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
                            GUILayout.Label("親アセット:", labelStyle, GUILayout.Width(70));

                            if (GUILayout.Button(parentAsset.metadata.name, chipStyle))
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

                    GUILayout.Label("子アセット:", labelStyle);

                    using (var _newChildrenScroll = new GUILayout.ScrollViewScope(_childrenScroll, GUILayout.Height(40)))
                    {
                        _childrenScroll = _newChildrenScroll.scrollPosition;
                        using (new GUILayout.HorizontalScope())
                        {
                            foreach (var childId in _asset.childAssetIds)
                            {
                                if (Guid.TryParse(childId, out var childGuid))
                                {
                                    var childAsset = controller.GetAsset(childGuid);
                                    if (childAsset != null)
                                    {
                                        if (GUILayout.Button(childAsset.metadata.name, chipStyle))
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
                        }
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (_asset.metadata.tags.Count > 0)
                {
                    using (new GUILayout.VerticalScope(sectionBoxStyle))
                    {
                        GUILayout.Label("Tags", labelStyle);
                        using (var _newTagsScroll = new GUILayout.ScrollViewScope(_tagsScroll, GUILayout.Height(40)))
                        {
                            _tagsScroll = _newTagsScroll.scrollPosition;
                            using (new GUILayout.HorizontalScope())
                            {
                                foreach (var tag in _asset.metadata.tags)
                                {
                                    if (GUILayout.Button(tag, chipStyle))
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
                            }
                        }
                    }
                }

                if (_asset.metadata.tags.Count > 0 && _asset.metadata.dependencies.Count > 0)
                    GUILayout.Space(5);

                if (_asset.metadata.dependencies.Count > 0)
                {
                    using (new GUILayout.VerticalScope(sectionBoxStyle))
                    {
                        GUILayout.Label("Dependencies", labelStyle);
                        using (var _newDepsScroll = new GUILayout.ScrollViewScope(_depsScroll, GUILayout.Height(40)))
                        {
                            _depsScroll = _newDepsScroll.scrollPosition;
                            using (new GUILayout.HorizontalScope())
                            {
                                foreach (var dep in _asset.metadata.dependencies)
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
                                        if (GUILayout.Button(depName, chipStyle))
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
                            }
                        }
                    }
                }
            }
            if (_asset != null && _asset.fileInfo != null && !string.IsNullOrEmpty(_asset.fileInfo.filePath))
            {
                var filePath = _asset.fileInfo.filePath;
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var excludedExts = SettingAPI.GetSetting<string>("AssetManager_excludedImportExtensions");
                if (excludedExts != null && !excludedExts.Contains(ext))
                {
                    GUILayout.Space(3);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("インポート", GUILayout.Width(240), GUILayout.Height(36)))
                        {
                            Debug.Log("Import clicked");
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            else if ((_asset.fileInfo == null || string.IsNullOrEmpty(_asset.fileInfo.filePath)) && _asset.boothItem != null && !string.IsNullOrEmpty(_asset.boothItem.downloadUrl))
            {
                GUILayout.Space(3);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("ダウンロード", GUILayout.Width(240), GUILayout.Height(36)))
                    {
                        Application.OpenURL(_asset.boothItem.downloadUrl);
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }
    }
}
