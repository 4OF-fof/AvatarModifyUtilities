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
        private string _thumbnailDirectory;

        public event Action<AssetInfo> OnThumbnailSaved;
        public event Action OnThumbnailLoaded;

        public AssetThumbnailManager()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            _thumbnailDirectory = Path.Combine(coreDir, "AssetManager", "Thumbnails");
            EnsureThumbnailDirectory();
        }        public Texture2D GetThumbnail(AssetInfo asset)
        {
            if (asset == null) return null;

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
                    return texture;
                }
            }

            return GetDefaultThumbnail(asset.assetType);
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
                _thumbnailCache[asset.uid] = texture;
                OnThumbnailLoaded?.Invoke();
                OnThumbnailSaved?.Invoke(asset);
            }        }

        public void ClearCache()
        {
            foreach (var texture in _thumbnailCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            _thumbnailCache.Clear();        }

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

        private Texture2D GetDefaultThumbnail(AssetType assetType)
        {
            // Return default Unity icons based on asset type
            switch (assetType)
            {
                case AssetType.Avatar:
                case AssetType.Prefab:
                    return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                case AssetType.Material:
                    return EditorGUIUtility.IconContent("Material Icon").image as Texture2D;
                case AssetType.Texture:
                    return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
                case AssetType.Animation:
                    return EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
                case AssetType.Script:
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
