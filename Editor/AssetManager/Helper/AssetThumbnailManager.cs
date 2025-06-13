using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;

namespace AMU.AssetManager.Helper
{
    public class AssetThumbnailManager
    {
        private Dictionary<string, Texture2D> _thumbnailCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, DateTime> _thumbnailFileModified = new Dictionary<string, DateTime>();
        private string _thumbnailDirectory;

        public event Action<AssetInfo> OnThumbnailSaved;
        public event Action OnThumbnailLoaded;

        public AssetThumbnailManager()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            _thumbnailDirectory = Path.Combine(coreDir, "AssetManager", "Thumbnails");
            EnsureThumbnailDirectory();
        }
        public Texture2D GetThumbnail(AssetInfo asset)
        {
            if (asset == null) return null;

            // サムネイルファイルの変更をチェック
            CheckThumbnailFileChanges(asset);

            // Check cache first
            if (_thumbnailCache.TryGetValue(asset.uid, out var cachedTexture) && cachedTexture != null)
            {
                return cachedTexture;
            }

            // Try to load from thumbnail path
            if (!string.IsNullOrEmpty(asset.thumbnailPath) && File.Exists(asset.thumbnailPath))
            {
                var texture = LoadTextureFromFile(asset.thumbnailPath);
                if (texture != null)
                {
                    _thumbnailCache[asset.uid] = texture;
                    UpdateThumbnailModifiedTime(asset);
                    return texture;
                }
            }

            return GetDefaultThumbnail(asset.assetType);
        }

        /// <summary>
        /// サムネイルファイルが変更されているかチェックし、必要に応じてキャッシュを無効化する
        /// </summary>
        private void CheckThumbnailFileChanges(AssetInfo asset)
        {
            if (string.IsNullOrEmpty(asset.thumbnailPath) || !File.Exists(asset.thumbnailPath))
                return;

            try
            {
                var fileInfo = new FileInfo(asset.thumbnailPath);
                var currentModified = fileInfo.LastWriteTime;

                if (_thumbnailFileModified.TryGetValue(asset.uid, out var lastModified))
                {
                    if (currentModified > lastModified)
                    {
                        // ファイルが更新されているのでキャッシュを無効化
                        InvalidateThumbnailCache(asset.uid);
                    }
                }
                else
                {
                    // 初回アクセスの場合
                    _thumbnailFileModified[asset.uid] = currentModified;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to check thumbnail file changes for {asset.uid}: {ex.Message}");
            }
        }

        /// <summary>
        /// 特定のアセットのサムネイルキャッシュを無効化する
        /// </summary>
        private void InvalidateThumbnailCache(string assetUid)
        {
            if (_thumbnailCache.TryGetValue(assetUid, out var texture) && texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            _thumbnailCache.Remove(assetUid);
            _thumbnailFileModified.Remove(assetUid);
        }

        /// <summary>
        /// サムネイルの更新時刻を記録する
        /// </summary>
        private void UpdateThumbnailModifiedTime(AssetInfo asset)
        {
            if (!string.IsNullOrEmpty(asset.thumbnailPath) && File.Exists(asset.thumbnailPath))
            {
                try
                {
                    var fileInfo = new FileInfo(asset.thumbnailPath);
                    _thumbnailFileModified[asset.uid] = fileInfo.LastWriteTime;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AssetThumbnailManager] Failed to update thumbnail modified time for {asset.uid}: {ex.Message}");
                }
            }
        }
        public void SetCustomThumbnail(AssetInfo asset, string imagePath)
        {
            if (asset == null || string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return;

            var texture = LoadTextureFromFile(imagePath);
            if (texture != null)
            {
                // Save to thumbnail directory
                string thumbnailPath = Path.Combine(_thumbnailDirectory, $"{asset.uid}.png");
                SaveTextureToFile(texture, thumbnailPath);

                // Convert path to use forward slashes for JSON storage
                asset.thumbnailPath = thumbnailPath.Replace('\\', '/');

                // 古いキャッシュを無効化してから新しいテクスチャをキャッシュ
                InvalidateThumbnailCache(asset.uid);
                _thumbnailCache[asset.uid] = texture;
                UpdateThumbnailModifiedTime(asset);

                OnThumbnailLoaded?.Invoke();
                OnThumbnailSaved?.Invoke(asset);
            }
        }
        public void ClearCache()
        {
            foreach (var texture in _thumbnailCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            _thumbnailCache.Clear();
            _thumbnailFileModified.Clear();
        }

        private Texture2D LoadTextureFromFile(string filePath)
        {
            try
            {
                if (filePath.StartsWith("Assets/"))
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                }
                else
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {
                        return texture;
                    }
                    UnityEngine.Object.DestroyImmediate(texture);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to load texture from {filePath}: {ex.Message}");
                return null;
            }
        }

        private void SaveThumbnail(AssetInfo asset, Texture2D texture)
        {
            string thumbnailPath = Path.Combine(_thumbnailDirectory, $"{asset.uid}.png");
            SaveTextureToFile(texture, thumbnailPath);
            // Convert path to use forward slashes for JSON storage
            asset.thumbnailPath = thumbnailPath.Replace('\\', '/');
        }

        private void SaveTextureToFile(Texture2D texture, string filePath)
        {
            try
            {
                byte[] pngData = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to save texture to {filePath}: {ex.Message}");
            }
        }
        private Texture2D GetDefaultThumbnail(string assetType)
        {
            // Return default Unity icons based on asset type
            switch (assetType)
            {
                case "Avatar":
                case "Prefab":
                    return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                case "Material":
                    return EditorGUIUtility.IconContent("Material Icon").image as Texture2D;
                case "Texture":
                    return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
                case "Animation":
                    return EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
                case "Script":
                    return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                default:
                    return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
            }
        }

        private void EnsureThumbnailDirectory()
        {
            if (!Directory.Exists(_thumbnailDirectory))
            {
                Directory.CreateDirectory(_thumbnailDirectory);
            }
        }

        public void DrawThumbnail(AssetInfo asset, float size)
        {
            var thumbnail = GetThumbnail(asset);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            if (thumbnail != null)
            {
                GUI.DrawTexture(rect, thumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Box(rect, "No Image");
            }
        }
    }
}
