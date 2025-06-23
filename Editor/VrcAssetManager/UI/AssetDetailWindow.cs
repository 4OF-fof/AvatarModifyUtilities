using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.UI.Components;
using AMU.Editor.VrcAssetManager.Controller;

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
            var window = GetWindow<AssetDetailWindow>(typeof(VrcAssetManagerWindow));
            window._asset = asset;
            window.titleContent = new GUIContent("Asset Detail: " + asset.metadata.name);
            window.minSize = window.maxSize = new Vector2(1200, 800);
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

                var editIcon = EditorGUIUtility.IconContent("d_editicon.sml");
                if (!_isEditMode)
                {
                    if (GUILayout.Button(editIcon, GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        _isEditMode = true;
                    }
                }

                var closeIcon = EditorGUIUtility.IconContent("winbtn_win_close");
                if (GUILayout.Button(closeIcon, GUILayout.Width(32), GUILayout.Height(32)))
                {
                    Close();
                }
            }

            if (_asset == null)
            {
                EditorGUILayout.LabelField("No asset selected.");
                return;
            }

            var metadata = _asset.metadata;
            var fileInfo = _asset.fileInfo;
            var state = _asset.state;

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
                    if (GUILayout.Button(metadata.name, titleStyle))
                    {
                        Application.OpenURL(_asset.boothItem.itemUrl);
                    }
                }
                else
                {
                    var normalTitleStyle = new GUIStyle(titleStyle);
                    normalTitleStyle.normal.textColor = EditorStyles.label.normal.textColor;
                    GUILayout.Label(metadata.name, normalTitleStyle);
                }

                if (!string.IsNullOrEmpty(metadata.description))
                {
                    using (new GUILayout.VerticalScope(sectionBoxStyle))
                    {
                        _descScroll = GUILayout.BeginScrollView(_descScroll, GUILayout.Height(120));
                        EditorGUILayout.LabelField(metadata.description, EditorStyles.wordWrappedLabel);
                        GUILayout.EndScrollView();
                    }
                }

                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Author:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.authorName, valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Asset Type:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.assetType, valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Created:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.createdDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Modified:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.modifiedDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("File Path:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(fileInfo.filePath, valueStyle);
                }

                GUILayout.Space(4);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Favorite:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(state.isFavorite ? "Yes" : "No", valueStyle);
                    GUILayout.Label("Archived:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(state.isArchived ? "Yes" : "No", valueStyle);
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

                    _childrenScroll = GUILayout.BeginScrollView(_childrenScroll, GUILayout.Height(40));
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
                    GUILayout.EndScrollView();
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (metadata.tags.Count > 0)
                {
                    using (new GUILayout.VerticalScope(sectionBoxStyle))
                    {
                        GUILayout.Label("Tags", labelStyle);
                        _tagsScroll = GUILayout.BeginScrollView(_tagsScroll, GUILayout.Height(40));
                        using (new GUILayout.HorizontalScope())
                        {
                            foreach (var tag in metadata.tags)
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
                        GUILayout.EndScrollView();
                    }
                }

                if (metadata.tags.Count > 0 && metadata.dependencies.Count > 0)
                    GUILayout.Space(5);

                if (metadata.dependencies.Count > 0)
                {
                    using (new GUILayout.VerticalScope(sectionBoxStyle))
                    {
                        GUILayout.Label("Dependencies", labelStyle);
                        _depsScroll = GUILayout.BeginScrollView(_depsScroll, GUILayout.Height(40));
                        using (new GUILayout.HorizontalScope())
                        {
                            foreach (var dep in metadata.dependencies)
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
                        GUILayout.EndScrollView();
                    }
                }
            }
        }
    }
}
