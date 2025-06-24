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
                var pathsToImport = GetImportPaths(asset);
                if (pathsToImport.Count == 0)
                {
                    Debug.LogWarning("[AssetImportUtility] No valid file paths found for import");
                    return false;
                }

                return ImportFiles(pathsToImport, showImportDialog);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import asset '{asset.metadata.name}': {ex.Message}");
                return false;
            }
        }

        public static bool ImportAsset(List<AssetSchema> assets, bool showImportDialog = true)
        {
            if (assets == null || assets.Count == 0)
            {
                Debug.LogWarning("[AssetImportUtility] Assets list is null or empty");
                return false;
            }

            bool allSuccess = true;
            int successCount = 0;
            Debug.Log($"[AssetImportUtility] Starting import of {assets.Count} assets");

            foreach (var asset in assets)
            {
                if (ImportAsset(asset, showImportDialog))
                {
                    successCount++;
                }
                else
                {
                    allSuccess = false;
                }
            }

            Debug.Log($"[AssetImportUtility] Batch import completed: {successCount}/{assets.Count} assets imported successfully");
            return allSuccess;
        }

        private static bool ImportFiles(List<string> relativePaths, bool showImportDialog = true)
        {
            if (relativePaths == null || relativePaths.Count == 0)
            {
                Debug.LogWarning("[AssetImportUtility] Relative paths list is null or empty");
                return false;
            }

            string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
            if (string.IsNullOrEmpty(coreDir))
            {
                Debug.LogError("[AssetImportUtility] Core_dirPath setting not found");
                return false;
            }

            bool allSuccess = true;
            int successCount = 0;
            Debug.Log($"[AssetImportUtility] Starting import of {relativePaths.Count} assets");

            foreach (string relativePath in relativePaths)
            {
                if (string.IsNullOrEmpty(relativePath))
                {
                    Debug.LogWarning("[AssetImportUtility] Relative path is null or empty");
                    allSuccess = false;
                    continue;
                }

                try
                {
                    string fullPath = Path.GetFullPath(Path.Combine(coreDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

                    if (!File.Exists(fullPath))
                    {
                        Debug.LogError($"[AssetImportUtility] File not found: {fullPath}");
                        allSuccess = false;
                        continue;
                    }

                    bool isUnityPackage = Path.GetExtension(fullPath).ToLower() == ".unitypackage";
                    
                    if (isUnityPackage)
                    {
                        Debug.Log($"[AssetImportUtility] Importing Unity Package: {fullPath}");
                        AssetDatabase.ImportPackage(fullPath, showImportDialog);
                    }
                    else
                    {
                        string fileName = Path.GetFileName(fullPath);
                        string targetPath = Path.Combine(Application.dataPath, fileName);
                        string assetPath = "Assets/" + fileName;

                        if (File.Exists(targetPath))
                        {
                            Debug.Log($"[AssetImportUtility] File already exists in Assets folder: {assetPath}");
                        }
                        else
                        {
                            File.Copy(fullPath, targetPath, true);
                            AssetDatabase.Refresh();
                            Debug.Log($"[AssetImportUtility] File imported to Assets folder: {assetPath}");
                        }

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

                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AssetImportUtility] Failed to import '{relativePath}': {ex.Message}");
                    allSuccess = false;
                }
            }

            Debug.Log($"[AssetImportUtility] Import completed: {successCount}/{relativePaths.Count} assets imported successfully");
            return allSuccess;
        }

        private static List<string> GetImportPaths(AssetSchema asset)
        {
            var paths = new List<string>();
            
            if (asset.fileInfo?.importFiles?.Count > 0)
            {
                paths.AddRange(asset.fileInfo.importFiles);
                Debug.Log($"[AssetImportUtility] Using importFiles: {string.Join(", ", asset.fileInfo.importFiles)}");
            }
            else if (!string.IsNullOrEmpty(asset.fileInfo?.filePath))
            {
                paths.Add(asset.fileInfo.filePath);
                Debug.Log($"[AssetImportUtility] Using filePath: {asset.fileInfo.filePath}");
            }
            
            return paths;
        }

        public static bool IsImportable(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string extension = Path.GetExtension(filePath).ToLower();
            
            if (extension == ".unitypackage")
                return true;

            string excludedExtensions = SettingAPI.GetSetting<string>("AssetManager_excludedImportExtensions");
            if (string.IsNullOrEmpty(excludedExtensions))
                return true;

            var excludedList = excludedExtensions
                .Split(new char[] { ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().ToLower())
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
                .ToArray();

            return !excludedList.Contains(extension);
        }
    }
}