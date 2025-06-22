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
        private AssetLibraryController _controller;

        public static void ShowWindow(AssetSchema asset, AssetLibraryController controller)
        {
            var window = GetWindow<AssetDetailWindow>(typeof(VrcAssetManagerWindow));
            window._asset = asset;
            window._controller = controller;
            window.titleContent = new GUIContent("Asset Detail: " + asset.Metadata.Name);
            window.minSize = window.maxSize = new Vector2(1200, 800);
            window.Show();
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
                margin = new RectOffset(2, 2, 2, 2)
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
            var metadata = _asset.Metadata;
            var fileInfo = _asset.FileInfo;
            var state = _asset.State;

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
                GUILayout.Label(metadata.Name, titleStyle);
                GUILayout.Label(metadata.Description, EditorStyles.wordWrappedLabel);
                GUILayout.Space(4);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Author:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.AuthorName, valueStyle);
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Asset Type:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.AssetType, valueStyle);
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Created:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.CreatedDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Modified:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(metadata.ModifiedDate.ToString("yyyy-MM-dd HH:mm"), valueStyle);
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("File Path:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(fileInfo.FilePath, valueStyle);
                }
                GUILayout.Space(4);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Favorite:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(state.IsFavorite ? "Yes" : "No", valueStyle);
                    GUILayout.Label("Archived:", labelStyle, GUILayout.Width(70));
                    GUILayout.Label(state.IsArchived ? "Yes" : "No", valueStyle);
                }
            }

            if (metadata.Tags.Count > 0)
            {
                using (new GUILayout.VerticalScope(sectionBoxStyle))
                {
                    GUILayout.Label("Tags", labelStyle);
                    using (new GUILayout.HorizontalScope())
                    {
                        foreach (var tag in metadata.Tags)
                        {
                            if (GUILayout.Button(tag, chipStyle))
                            {
                                if (_controller != null)
                                {
                                    _controller.filterOptions.ClearFilter();
                                    _controller.filterOptions.tags = new List<string> { tag };
                                    _controller.filterOptions.tagsAnd = false;
                                }
                                ToolbarComponent.IsUsingAdvancedSearch = true;
                                VrcAssetManagerWindow.ShowWindow();
                            }
                        }
                    }
                }
            }

            if (metadata.Dependencies.Count > 0)
            {
                using (new GUILayout.VerticalScope(sectionBoxStyle))
                {
                    GUILayout.Label("Dependencies", labelStyle);
                    using (new GUILayout.HorizontalScope())
                    {
                        foreach (var dep in metadata.Dependencies)
                        {
                            string depName = dep;
                            AssetSchema depAsset = null;
                            if (_controller != null)
                            {
                                depAsset = _controller.GetAsset(new Guid(dep));
                                if (depAsset != null && depAsset.Metadata != null)
                                {
                                    depName = depAsset.Metadata.Name;
                                }
                            }
                            if (depAsset != null)
                            {
                                if (GUILayout.Button(depName, chipStyle))
                                {
                                    AssetDetailWindow.ShowWindow(depAsset, _controller);
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

            if (_asset.BoothItem != null)
            {
                using (new GUILayout.VerticalScope(sectionBoxStyle))
                {
                    GUILayout.Label("Booth Item", labelStyle);
                    GUILayout.Label(_asset.BoothItem.ItemName, valueStyle);
                    GUILayout.Label(_asset.BoothItem.AuthorName, labelStyle);
                    if (!string.IsNullOrEmpty(_asset.BoothItem.ItemUrl))
                    {
                        if (GUILayout.Button("Open Booth Page"))
                        {
                            Application.OpenURL(_asset.BoothItem.ItemUrl);
                        }
                    }
                }
            }
        }
    }
}
