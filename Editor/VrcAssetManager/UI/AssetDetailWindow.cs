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

        public static void ShowWindow(AssetSchema asset)
        {
            var window = GetWindow<AssetDetailWindow>("Asset Detail");
            window._asset = asset;
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            if (_asset == null)
            {
                EditorGUILayout.LabelField("No asset selected.");
                return;
            }

            var metadata = _asset.Metadata;
            var fileInfo = _asset.FileInfo;
            var state = _asset.State;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Asset Detail", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // サムネイル（DrawThumbnailComponentを使用）
            Rect thumbRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
            DrawThumbnailComponent.Draw(thumbRect, _asset);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Name", metadata.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Description", metadata.Description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Author", metadata.AuthorName);
            EditorGUILayout.LabelField("Asset Type", metadata.AssetType);
            EditorGUILayout.LabelField("Created", metadata.CreatedDate.ToString("yyyy-MM-dd HH:mm"));
            EditorGUILayout.LabelField("Modified", metadata.ModifiedDate.ToString("yyyy-MM-dd HH:mm"));
            EditorGUILayout.LabelField("File Path", fileInfo.FilePath);
            EditorGUILayout.LabelField("Favorite", state.IsFavorite ? "Yes" : "No");
            EditorGUILayout.LabelField("Archived", state.IsArchived ? "Yes" : "No");

            // タグ
            if (metadata.Tags.Count > 0)
            {
                GUILayout.Label("Tags:");
                GUILayout.BeginHorizontal();
                foreach (var tag in metadata.Tags)
                {
                    GUILayout.Box(tag, GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
            }

            // 依存関係
            if (metadata.Dependencies.Count > 0)
            {
                GUILayout.Label("Dependencies:");
                foreach (var dep in metadata.Dependencies)
                {
                    EditorGUILayout.LabelField(dep);
                }
            }

            // Booth情報
            if (_asset.BoothItem != null)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Booth Item", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Item Name", _asset.BoothItem.ItemName);
                EditorGUILayout.LabelField("Author", _asset.BoothItem.AuthorName);
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
