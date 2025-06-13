using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using AMU.Editor.Core.Helper;

namespace AMU.Editor.AutoVariant.Watcher
{
    public static class AvatarExporter
    {
        public static void ExportOptimizedAvatar(GameObject avatar)
        {
            if (avatar == null)
            {
                Debug.LogError("[AvatarExporter] Avatar is null");
                return;
            }

            var exportPath = GenerateExportPath(avatar);
            var assetPaths = CollectAvatarAssets(avatar);

            if (assetPaths.Count == 0)
            {
                Debug.LogWarning($"[AvatarExporter] No assets found to export for {avatar.name}");
                return;
            }

            try
            {
                // UnityPackageとしてエクスポート
                AssetDatabase.ExportPackage(assetPaths.ToArray(), exportPath, ExportPackageOptions.Recurse);
                Debug.Log($"[AvatarExporter] Exported optimized avatar to: {exportPath}");

                // 画像キャプチャと保存
                CaptureAvatarImage(avatar, exportPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarExporter] Failed to export {avatar.name}: {e.Message}");
            }
        }

        private static void CaptureAvatarImage(GameObject avatar, string unityPackagePath)
        {
            try
            {
                // UnityPackageと同じ場所に同じ名前でpngファイルを保存
                var imagePath = Path.ChangeExtension(unityPackagePath, ".png");

                // ObjectCaptureHelperを使用してアバターの画像をキャプチャ
                var capturedTexture = ObjectCaptureHelper.CaptureObject(avatar, imagePath, 512, 512);

                if (capturedTexture != null)
                {
                    Debug.Log($"[AvatarExporter] Captured avatar image: {imagePath}");
                    UnityEngine.Object.DestroyImmediate(capturedTexture);
                }
                else
                {
                    Debug.LogWarning($"[AvatarExporter] Failed to capture avatar image for {avatar.name}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarExporter] Failed to capture avatar image: {e.Message}");
            }
        }

        private static string GenerateExportPath(GameObject avatar)
        {
            var blueprintId = PipelineManagerHelper.GetBlueprintId(avatar);
            var exportDirectory = CreateExportDirectory(blueprintId);
            var fileName = GenerateUniqueFileName(exportDirectory, avatar.name, string.IsNullOrEmpty(blueprintId));

            return Path.Combine(exportDirectory, fileName);
        }

        private static string CreateExportDirectory(string blueprintId)
        {
            var basePath = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            EnsureDirectoryExists(basePath);

            var autoVariantPath = Path.Combine(basePath, "AutoVariant");
            EnsureDirectoryExists(autoVariantPath);

            var dirName = string.IsNullOrEmpty(blueprintId) ? "local" : blueprintId;
            var avatarDir = Path.Combine(autoVariantPath, dirName);
            EnsureDirectoryExists(avatarDir);

            if (string.IsNullOrEmpty(blueprintId))
            {
                Debug.Log("[AvatarExporter] No blueprint ID found, exporting to local directory");
            }

            return avatarDir;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"[AvatarExporter] Created directory: {path}");
            }
        }

        private static string GenerateUniqueFileName(string directory, string avatarName, bool isLocal)
        {
            var dateString = DateTime.Now.ToString("yyMMdd");
            var baseName = isLocal ? $"{dateString}-{avatarName}-" : $"{dateString}-";

            int number = 1;
            string fileName;

            do
            {
                fileName = $"{baseName}{number:D3}.unitypackage";
                number++;
            }
            while (File.Exists(Path.Combine(directory, fileName)));

            return fileName;
        }

        private static List<string> CollectAvatarAssets(GameObject avatar)
        {
            var assetPaths = new List<string>();
            var includeAllAssets = EditorPrefs.GetBool("Setting.AutoVariant_includeAllAssets", true);

            var avatarPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(avatar);
            if (string.IsNullOrEmpty(avatarPrefabPath))
            {
                Debug.LogWarning($"[AvatarExporter] Could not find prefab path for {avatar.name}");
                return assetPaths;
            }

            assetPaths.Add(avatarPrefabPath);
            CollectDependencies(avatarPrefabPath, assetPaths, includeAllAssets);

            Debug.Log($"[AvatarExporter] Collected {assetPaths.Count} assets for {avatar.name} (includeAllAssets: {includeAllAssets})");
            return assetPaths;
        }

        private static void CollectDependencies(string prefabPath, List<string> assetPaths, bool includeAllAssets)
        {
            var dependencies = AssetDatabase.GetDependencies(prefabPath, true);

            foreach (var dependency in dependencies)
            {
                if (ShouldIncludeDependency(dependency, includeAllAssets) && !assetPaths.Contains(dependency))
                {
                    assetPaths.Add(dependency);
                }
            }
        }

        private static bool ShouldIncludeDependency(string dependency, bool includeAllAssets)
        {
            if (!dependency.StartsWith("Assets/"))
                return false;

            if (includeAllAssets)
                return true;

            return dependency.StartsWith("Assets/AMU_Variants/");
        }
    }
}
