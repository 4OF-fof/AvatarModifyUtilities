using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Helper;
using AMU.Editor.VrcAssetManager.Schema;

namespace AMU.Editor.VrcAssetManager.Helper
{
    public static class AssetImportUtility
    {
        public static bool ImportAsset(AssetSchema asset, bool showImportDialog = true)
        {
            if (asset == null)
            {
                Debug.LogWarning("[AssetImportUtility] Asset is null");
                return false;
            }

            try
            {
                List<string> pathsToImport = new List<string>();

                if (asset.fileInfo != null && asset.fileInfo.importFiles != null && asset.fileInfo.importFiles.Count > 0)
                {
                    pathsToImport.AddRange(asset.fileInfo.importFiles);
                    Debug.Log($"[AssetImportUtility] Using importFiles for import: {string.Join(", ", asset.fileInfo.importFiles)}");
                }
                else if (asset.fileInfo != null && !string.IsNullOrEmpty(asset.fileInfo.filePath))
                {
                    pathsToImport.Add(asset.fileInfo.filePath);
                    Debug.Log($"[AssetImportUtility] Using filePath for import: {asset.fileInfo.filePath}");
                }
                else
                {
                    Debug.LogWarning("[AssetImportUtility] No valid file paths found for import");
                    return false;
                }

                return ImportAssetList(pathsToImport, showImportDialog);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import asset '{asset.metadata.name}': {ex.Message}");
                return false;
            }
        }

        public static bool ImportAssetList(List<string> relativePaths, bool showImportDialog = true)
        {
            if (relativePaths == null || relativePaths.Count == 0)
            {
                Debug.LogWarning("[AssetImportUtility] Relative paths list is null or empty");
                return false;
            }

            bool allSuccess = true;
            int successCount = 0;
            int totalCount = relativePaths.Count;

            Debug.Log($"[AssetImportUtility] Starting import of {totalCount} assets");

            foreach (string relativePath in relativePaths)
            {
                if (ImportSingleAsset(relativePath, showImportDialog))
                {
                    successCount++;
                }
                else
                {
                    allSuccess = false;
                }
            }

            Debug.Log($"[AssetImportUtility] Import completed: {successCount}/{totalCount} assets imported successfully");
            return allSuccess;
        }

        public static bool ImportSingleAsset(string relativePath, bool showImportDialog = true)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogWarning("[AssetImportUtility] Relative path is null or empty");
                return false;
            }

            try
            {
                string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                if (string.IsNullOrEmpty(coreDir))
                {
                    Debug.LogError("[AssetImportUtility] Core_dirPath setting not found");
                    return false;
                }

                string fullPath = Path.Combine(coreDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                fullPath = Path.GetFullPath(fullPath);

                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"[AssetImportUtility] File not found: {fullPath}");
                    return false;
                }

                string extension = Path.GetExtension(fullPath).ToLower();

                if (extension == ".unitypackage")
                {
                    return ImportUnityPackage(fullPath, showImportDialog);
                }
                else
                {
                    return ImportFileToAssets(fullPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import asset from relative path '{relativePath}': {ex.Message}");
                return false;
            }
        }

        private static bool ImportUnityPackage(string packagePath, bool showImportDialog)
        {
            try
            {
                Debug.Log($"[AssetImportUtility] Importing Unity Package: {packagePath}");
                
                AssetDatabase.ImportPackage(packagePath, showImportDialog);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import Unity Package '{packagePath}': {ex.Message}");
                return false;
            }
        }

        private static bool ImportFileToAssets(string sourceFilePath)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string targetPath = Path.Combine(Application.dataPath, fileName);
                string assetPath = "Assets/" + fileName;

                if (File.Exists(targetPath))
                {
                    Debug.Log($"[AssetImportUtility] File already exists in Assets folder: {assetPath}");
                    
                    SelectAssetInProject(assetPath);
                    return true;
                }

                File.Copy(sourceFilePath, targetPath, true);

                AssetDatabase.Refresh();

                Debug.Log($"[AssetImportUtility] File imported to Assets folder: {assetPath}");

                SelectAssetInProject(assetPath);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import file to Assets '{sourceFilePath}': {ex.Message}");
                return false;
            }
        }

        private static void SelectAssetInProject(string assetPath)
        {
            EditorApplication.delayCall += () =>
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            };
        }

        public static bool IsUnityPackage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".unitypackage";
        }

        public static bool IsImportable(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (IsUnityPackage(filePath))
                return true;

            string extension = Path.GetExtension(filePath).ToLower();
            
            string excludedExtensions = SettingAPI.GetSetting<string>("AssetManager_excludedImportExtensions");

            var separators = new char[] { ',', ' ', '\n', '\r', '\t' };
            var excludedList = excludedExtensions.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().ToLower())
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
                .ToArray();

            return !excludedList.Contains(extension);
        }

        public static bool FileExists(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;

            try
            {
                string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                if (string.IsNullOrEmpty(coreDir))
                    return false;

                string fullPath = Path.Combine(coreDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                fullPath = Path.GetFullPath(fullPath);

                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
    }
}