using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public static class DrawThumbnailComponent
    {
        public static void Draw(Rect rect, AssetSchema asset)
        {
            Texture2D thumbnailTexture = null;
            string thumbnailPath = asset.metadata.thumbnailPath;
            if (!string.IsNullOrEmpty(thumbnailPath))
            {
                string resolvedPath = thumbnailPath;
                if (!Path.IsPathRooted(thumbnailPath))
                {
                    string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                    string corePath = Path.Combine(coreDir, thumbnailPath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(corePath))
                    {
                        resolvedPath = corePath;
                    }
                }
                if (File.Exists(resolvedPath))
                {
                    thumbnailTexture = ThumbnailCacheController.Instance.Load(resolvedPath);
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
    }
}
