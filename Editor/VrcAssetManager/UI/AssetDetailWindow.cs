using System;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.UI.Components;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class AssetDetailWindow : EditorWindow
    {
        private AssetSchema _asset;

        private GUIStyle _sectionBoxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _chipStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _dividerStyle;

        public static void ShowWindow(AssetSchema asset)
        {
            var window = GetWindow<AssetDetailWindow>("Asset Detail");
            window._asset = asset;
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void InitStyles()
        {
            if (_sectionBoxStyle == null)
            {
                _sectionBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(16, 16, 12, 12),
                    margin = new RectOffset(0, 0, 8, 8),
                    normal = { background = MakeTex(2, 2, new Color(0.18f, 0.18f, 0.18f, 0.7f)) }
                };
                _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    normal = { textColor = new Color(0.9f, 0.7f, 1f) },
                    margin = new RectOffset(0, 0, 0, 8)
                };
                _chipStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 12,
                    normal = { textColor = Color.white, background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.5f, 0.8f)) },
                    padding = new RectOffset(8, 8, 2, 2),
                    margin = new RectOffset(2, 2, 2, 2)
                };
                _labelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
                _valueStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                _dividerStyle = new GUIStyle(GUI.skin.box)
                {
                    fixedHeight = 1,
                    margin = new RectOffset(0, 0, 8, 8),
                    normal = { background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.7f)) }
                };
            }
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
            InitStyles();
            if (_asset == null)
            {
                EditorGUILayout.LabelField("No asset selected.");
                return;
            }
            var metadata = _asset.Metadata;
            var fileInfo = _asset.FileInfo;
            var state = _asset.State;

            GUILayout.BeginVertical(_sectionBoxStyle);
            // サムネイル
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect thumbRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
            DrawThumbnailComponent.Draw(thumbRect, _asset);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            GUILayout.Label(metadata.Name, _titleStyle);
            GUILayout.Label(metadata.Description, EditorStyles.wordWrappedLabel);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Author:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label(metadata.AuthorName, _valueStyle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Asset Type:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label(metadata.AssetType, _valueStyle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Created:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label(metadata.CreatedDate.ToString("yyyy-MM-dd HH:mm"), _valueStyle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Modified:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label(metadata.ModifiedDate.ToString("yyyy-MM-dd HH:mm"), _valueStyle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("File Path:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label(fileInfo.FilePath, _valueStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Favorite:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label(state.IsFavorite ? "Yes" : "No", _valueStyle);
            GUILayout.Label("Archived:", _labelStyle, GUILayout.Width(70));
            GUILayout.Label(state.IsArchived ? "Yes" : "No", _valueStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // タグ
            if (metadata.Tags.Count > 0)
            {
                GUILayout.BeginVertical(_sectionBoxStyle);
                GUILayout.Label("Tags", _labelStyle);
                GUILayout.BeginHorizontal();
                foreach (var tag in metadata.Tags)
                {
                    GUILayout.Label(tag, _chipStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            // 依存関係
            if (metadata.Dependencies.Count > 0)
            {
                GUILayout.BeginVertical(_sectionBoxStyle);
                GUILayout.Label("Dependencies", _labelStyle);
                GUILayout.BeginHorizontal();
                foreach (var dep in metadata.Dependencies)
                {
                    GUILayout.Label(dep, _chipStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            // Booth情報
            if (_asset.BoothItem != null)
            {
                GUILayout.BeginVertical(_sectionBoxStyle);
                GUILayout.Label("Booth Item", _labelStyle);
                GUILayout.Label(_asset.BoothItem.ItemName, _valueStyle);
                GUILayout.Label(_asset.BoothItem.AuthorName, _labelStyle);
                if (!string.IsNullOrEmpty(_asset.BoothItem.ItemUrl))
                {
                    if (GUILayout.Button("Open Booth Page"))
                    {
                        Application.OpenURL(_asset.BoothItem.ItemUrl);
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}
