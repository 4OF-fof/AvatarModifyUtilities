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

        public DateTime GetFileCreationTime(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                if (File.Exists(fullPath))
                {
                    return File.GetCreationTime(fullPath);
                }
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to get creation time for {filePath}: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        public DateTime GetFileModificationTime(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                if (File.Exists(fullPath))
                {
                    return File.GetLastWriteTime(fullPath);
                }
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to get modification time for {filePath}: {ex.Message}");
                return DateTime.MinValue;
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
                createdDate = GetFileCreationTime(filePath),
            };

            return asset;
        }

        public string ImportAssetFile(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return null;
            }

            try
            {
                string fileName = Path.GetFileName(sourcePath);
                string targetDir = Path.Combine(Application.dataPath, "ImportedAssets");
                
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string targetPath = Path.Combine(targetDir, fileName);
                
                // Generate unique filename if file already exists
                int counter = 1;
                while (File.Exists(targetPath))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
                    string extension = Path.GetExtension(sourcePath);
                    string newFileName = $"{nameWithoutExt}_{counter}{extension}";
                    targetPath = Path.Combine(targetDir, newFileName);
                    counter++;
                }

                File.Copy(sourcePath, targetPath);
                AssetDatabase.Refresh();

                return GetRelativePath(targetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to import asset from {sourcePath}: {ex.Message}");
                return null;
            }
        }

        public void ExportAsset(AssetInfo asset, string targetPath)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath) || string.IsNullOrEmpty(targetPath))
            {
                return;
            }

            try
            {
                string sourcePath = GetFullPath(asset.filePath);
                if (File.Exists(sourcePath))
                {
                    string targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    File.Copy(sourcePath, targetPath, true);
                    Debug.Log($"[AssetFileManager] Asset exported to: {targetPath}");
                }
                else
                {
                    Debug.LogWarning($"[AssetFileManager] Source file not found: {sourcePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to export asset to {targetPath}: {ex.Message}");
            }
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

        private AssetType DetermineAssetType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".prefab":
                    return AssetType.Prefab;
                case ".mat":
                    return AssetType.Material;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".tiff":
                    return AssetType.Texture;
                case ".anim":
                case ".controller":
                    return AssetType.Animation;
                case ".cs":
                    return AssetType.Script;
                case ".shader":
                    return AssetType.Shader;
                case ".fbx":
                case ".obj":
                case ".blend":
                    return AssetType.Avatar;
                default:
                    return AssetType.Other;
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
    }
}
