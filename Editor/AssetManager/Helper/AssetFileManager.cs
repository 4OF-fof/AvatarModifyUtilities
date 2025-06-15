using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;

namespace AMU.AssetManager.Helper
{
    public class AssetFileManager
    {
        public void OpenFileLocation(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
            {
                Debug.LogWarning("[AssetFileManager] Invalid asset or file path");
                return;
            }

            string fullPath = GetFullPath(asset.filePath);
            if (File.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                Debug.LogWarning($"[AssetFileManager] File not found: {fullPath}");
            }
        }

        public long GetFileSize(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                if (File.Exists(fullPath))
                {
                    return new FileInfo(fullPath).Length;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to get file size for {filePath}: {ex.Message}");
                return 0;
            }
        }



        public bool FileExists(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        public AssetInfo CreateAssetFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !FileExists(filePath))
            {
                return null;
            }

            var asset = new AssetInfo
            {
                name = Path.GetFileNameWithoutExtension(filePath),
                filePath = GetRelativePath(filePath),
                assetType = DetermineAssetType(filePath),
                fileSize = GetFileSize(filePath),
                createdDate = DateTime.Now,
            };

            return asset;
        }



        public List<string> GetAssetDependencies(string assetPath)
        {
            try
            {
                if (assetPath.StartsWith("Assets/"))
                {
                    return AssetDatabase.GetDependencies(assetPath, true).ToList();
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to get dependencies for {assetPath}: {ex.Message}");
                return new List<string>();
            }
        }

        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        private string DetermineAssetType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            switch (extension)
            {
                case ".prefab":
                    return "Prefab";
                case ".mat":
                    return "Material";
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".tiff":
                    return "Texture";
                case ".anim":
                case ".controller":
                    return "Animation";
                case ".cs":
                    return "Script";
                case ".shader":
                    return "Shader";
                case ".fbx":
                case ".obj":
                case ".blend":
                    return "Avatar";
                case ".unitypackage":
                    return "Package";
                default:
                    return "Other";
            }
        }

        private string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            if (relativePath.StartsWith("Assets/"))
            {
                return Path.Combine(Application.dataPath, relativePath.Substring(7));
            }

            return Path.Combine(Application.dataPath, relativePath);
        }

        private string GetRelativePath(string fullPath)
        {
            if (fullPath.StartsWith(Application.dataPath))
            {
                return "Assets" + fullPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            }
            return fullPath.Replace('\\', '/');
        }

        /// <summary>
        /// UnityPackageファイルをプロジェクトにインポートする
        /// </summary>
        public void ImportUnityPackage(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
            {
                Debug.LogWarning("[AssetFileManager] Invalid asset or file path");
                return;
            }

            string fullPath = GetFullPath(asset.filePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[AssetFileManager] File not found: {fullPath}");
                return;
            }

            string extension = Path.GetExtension(fullPath).ToLower();
            if (extension != ".unitypackage")
            {
                Debug.LogWarning($"[AssetFileManager] File is not a Unity Package: {fullPath}");
                return;
            }

            try
            {
                Debug.Log($"[AssetFileManager] Importing Unity Package: {asset.name}");
                AssetDatabase.ImportPackage(fullPath, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to import Unity Package {asset.name}: {ex.Message}");
            }
        }

        /// <summary>
        /// ファイルがUnityPackageかどうかを判定する
        /// </summary>
        public bool IsUnityPackage(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
                return false;

            return Path.GetExtension(asset.filePath).ToLower() == ".unitypackage";
        }
    }
}
