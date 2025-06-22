using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class DrawThumbnailComponent
    {
        public static void Draw(Rect rect, AssetSchema asset)
        {
            Texture2D thumbnailTexture = null;
            string thumbnailPath = asset.Metadata.ThumbnailPath;
            if (!string.IsNullOrEmpty(thumbnailPath))
            {
                string resolvedPath = thumbnailPath;
                if (!Path.IsPathRooted(thumbnailPath))
                {
                    string coreDir = SettingsAPI.GetSetting<string>("Core_dirPath");
                    string corePath = Path.Combine(coreDir, thumbnailPath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(corePath))
                    {
                        resolvedPath = corePath;
                    }
                }
                if (File.Exists(resolvedPath))
                {
                    thumbnailTexture = LoadTextureFromFileSync(resolvedPath);
                }
            }
            var prefabIcon = EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;

            if (thumbnailTexture != null)
            {
                GUI.DrawTexture(rect, thumbnailTexture, ScaleMode.ScaleToFit);
            }
            else if (prefabIcon != null)
            {
                GUI.DrawTexture(rect, prefabIcon, ScaleMode.ScaleToFit);
            }
        }

        private static Texture2D LoadTextureFromFileSync(string filePath)
        {
            try
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
            catch (Exception ex)
            {
                Debug.LogError($"[DrawThumbnailComponent] Failed to load texture from {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}
