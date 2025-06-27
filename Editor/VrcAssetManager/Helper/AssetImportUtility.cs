using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Helper;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.Helper
{
    public static class AssetImportUtility
    {
        private static bool isImporting = false;
        private static Queue<System.Action> importQueue = new Queue<System.Action>();

        public static bool ImportAsset(AssetSchema asset, bool showImportDialog = true, HashSet<Guid> processingAssets = null)
        {
            if (asset == null)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_assetNull"));
                return false;
            }

            if (processingAssets == null)
            {
                processingAssets = new HashSet<Guid>();
            }
            
            if (processingAssets.Contains(asset.assetId))
            {
                Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_circularDependency"), asset.metadata.name, asset.assetId));
                return false;
            }

            processingAssets.Add(asset.assetId);

            try
            {
                var dependencies = asset.metadata?.dependencies;
                if (dependencies != null && dependencies.Count > 0)
                {
                    foreach (var depIdStr in dependencies)
                    {
                        if (Guid.TryParse(depIdStr, out var depGuid))
                        {
                            var depAsset = AssetLibraryController.Instance.GetAsset(depGuid);
                            if (depAsset != null)
                            {
                                ImportAsset(depAsset, showImportDialog, processingAssets);
                            }
                            else
                            {
                                Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_dependencyNotFound"), depIdStr));
                            }
                        }
                        else
                        {
                            Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_invalidDependencyGuid"), depIdStr));
                        }
                    }
                }
            }
            finally
            {
                processingAssets.Remove(asset.assetId);
            }

            try
            {
                var pathsToImport = GetImportPaths(asset);
                if (pathsToImport.Count == 0)
                {
                    Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_noValidFilePaths"));
                    return false;
                }

                return ImportFiles(pathsToImport, showImportDialog);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importFailed"), asset.metadata.name, ex.Message));
                return false;
            }
        }

        public static bool ImportAsset(List<AssetSchema> assets, bool showImportDialog = true)
        {
            if (assets == null || assets.Count == 0)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_assetsListNullOrEmpty"));
                return false;
            }

            bool allSuccess = true;
            int successCount = 0;
            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_startingBatchImport"), assets.Count));

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

            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_batchImportCompleted"), successCount, assets.Count));
            return allSuccess;
        }

        private static bool ImportFiles(List<string> relativePaths, bool showImportDialog = true)
        {
            if (relativePaths == null || relativePaths.Count == 0)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_relativePathsNullOrEmpty"));
                return false;
            }

            string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
            if (string.IsNullOrEmpty(coreDir))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_coreDirPathNotFound"));
                return false;
            }

            bool allSuccess = true;
            int successCount = 0;
            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_startingImport"), relativePaths.Count));

            foreach (string relativePath in relativePaths)
            {
                if (string.IsNullOrEmpty(relativePath))
                {
                    Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_relativePathNullOrEmpty"));
                    allSuccess = false;
                    continue;
                }

                try
                {
                    string fullPath = Path.GetFullPath(Path.Combine(coreDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

                    if (!File.Exists(fullPath))
                    {
                        Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_fileNotFound"), fullPath));
                        allSuccess = false;
                        continue;
                    }

                    bool isUnityPackage = Path.GetExtension(fullPath).ToLower() == ".unitypackage";
                    
                    if (isUnityPackage)
                    {
                        Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importingUnityPackage"), fullPath));
                        ImportPackageWithQueue(fullPath, showImportDialog);
                    }
                    else
                    {
                        string fileName = Path.GetFileName(fullPath);
                        string targetPath = Path.Combine(Application.dataPath, fileName);
                        string assetPath = "Assets/" + fileName;

                        if (File.Exists(targetPath))
                        {
                            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_fileAlreadyExists"), assetPath));
                        }
                        else
                        {
                            File.Copy(fullPath, targetPath, true);
                            AssetDatabase.Refresh();
                            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_fileImported"), assetPath));
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
                    Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importFailedForPath"), relativePath, ex.Message));
                    allSuccess = false;
                }
            }

            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importCompleted"), successCount, relativePaths.Count));
            return allSuccess;
        }

        private static List<string> GetImportPaths(AssetSchema asset)
        {
            var paths = new List<string>();
            
            if (asset.fileInfo?.importFiles?.Count > 0)
            {
                paths.AddRange(asset.fileInfo.importFiles);
                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_usingImportFiles"), string.Join(", ", asset.fileInfo.importFiles)));
            }
            else if (!string.IsNullOrEmpty(asset.fileInfo?.filePath))
            {
                paths.Add(asset.fileInfo.filePath);
                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_usingFilePath"), asset.fileInfo.filePath));
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

            string excludedExtensions = SettingAPI.GetSetting<string>("VrcAssetManager_excludedImportExtensions");
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

        private static void ImportPackageWithQueue(string packagePath, bool showImportDialog)
        {
            if (isImporting)
            {
                importQueue.Enqueue(() => ImportPackageWithQueue(packagePath, showImportDialog));
                return;
            }

            isImporting = true;
            
            AssetDatabase.ImportPackageCallback onImportCompleted = null;
            AssetDatabase.ImportPackageCallback onImportCancelled = null;
            AssetDatabase.ImportPackageFailedCallback onImportFailed = null;

            onImportCompleted = (packageName) =>
            {
                AssetDatabase.importPackageCompleted -= onImportCompleted;
                AssetDatabase.importPackageCancelled -= onImportCancelled;
                AssetDatabase.importPackageFailed -= onImportFailed;
                
                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_packageImportCompleted"), packageName));
                OnImportComplete();
            };

            onImportCancelled = (packageName) =>
            {
                AssetDatabase.importPackageCompleted -= onImportCompleted;
                AssetDatabase.importPackageCancelled -= onImportCancelled;
                AssetDatabase.importPackageFailed -= onImportFailed;
                
                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_packageImportCancelled"), packageName));
                OnImportComplete();
            };

            onImportFailed = (packageName, errorMessage) =>
            {
                AssetDatabase.importPackageCompleted -= onImportCompleted;
                AssetDatabase.importPackageCancelled -= onImportCancelled;
                AssetDatabase.importPackageFailed -= onImportFailed;
                
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_packageImportFailed"), packageName, errorMessage));
                OnImportComplete();
            };

            AssetDatabase.importPackageCompleted += onImportCompleted;
            AssetDatabase.importPackageCancelled += onImportCancelled;
            AssetDatabase.importPackageFailed += onImportFailed;

            AssetDatabase.ImportPackage(packagePath, showImportDialog);
        }

        private static void OnImportComplete()
        {
            isImporting = false;
            
            if (importQueue.Count > 0)
            {
                var nextImport = importQueue.Dequeue();
                EditorApplication.delayCall += () => nextImport?.Invoke();
            }
        }
    }
}