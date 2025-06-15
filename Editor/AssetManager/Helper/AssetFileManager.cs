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
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            // ファイルをCoreDirに移動
            string coreDirFilePath = MoveAssetToCoreDir(filePath);

            var asset = new AssetInfo
            {
                name = Path.GetFileNameWithoutExtension(filePath),
                filePath = GetRelativePath(coreDirFilePath),
                assetType = DetermineAssetType(filePath),
                fileSize = GetFileSize(coreDirFilePath),
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

            // CoreDirからの相対パスの場合
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            return Path.Combine(coreDir, relativePath.Replace('/', '\\'));
        }
        private string GetRelativePath(string fullPath)
        {
            // CoreDirからの相対パスを優先して計算
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            if (fullPath.StartsWith(coreDir))
            {
                return fullPath.Substring(coreDir.Length + 1).Replace('\\', '/');
            }

            if (fullPath.StartsWith(Application.dataPath))
            {
                return "Assets" + fullPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            }
            return fullPath.Replace('\\', '/');
        }        /// <summary>
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

                // インポート後、ファイルがCoreDirに存在しない場合は移動
                string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

                if (!fullPath.StartsWith(coreDir))
                {
                    string newPath = MoveAssetToCoreDir(fullPath);
                    asset.filePath = GetRelativePath(newPath);
                    Debug.Log($"[AssetFileManager] Unity Package moved to CoreDir: {newPath}");
                }
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

        /// <summary>
        /// アセットファイルをCoreDirに移動する
        /// </summary>
        public string MoveAssetToCoreDir(string originalFilePath)
        {
            if (string.IsNullOrEmpty(originalFilePath) || !File.Exists(originalFilePath))
            {
                Debug.LogWarning($"[AssetFileManager] Invalid file path or file not found: {originalFilePath}");
                return originalFilePath;
            }

            try
            {
                // CoreDirパスを取得
                string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

                // AssetManagerディレクトリを作成
                string assetManagerDir = Path.Combine(coreDir, "AssetManager", "Files");
                if (!Directory.Exists(assetManagerDir))
                {
                    Directory.CreateDirectory(assetManagerDir);
                }

                // ファイル名を取得し、重複を避ける
                string fileName = Path.GetFileName(originalFilePath);
                string targetPath = Path.Combine(assetManagerDir, fileName);

                // ファイルが既に存在する場合は、番号を付けて重複を避ける
                int counter = 1;
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);

                while (File.Exists(targetPath))
                {
                    string newFileName = $"{nameWithoutExtension}_{counter}{extension}";
                    targetPath = Path.Combine(assetManagerDir, newFileName);
                    counter++;
                }

                // ファイルをコピー（移動ではなくコピーで安全性を確保）
                File.Copy(originalFilePath, targetPath);

                Debug.Log($"[AssetFileManager] Asset file copied to CoreDir: {targetPath}");
                return targetPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to move asset to CoreDir: {ex.Message}");
                return originalFilePath;
            }
        }
    }
}
