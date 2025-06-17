using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using AMU.Editor.Core.API;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.AutoVariant.Services
{
    /// <summary>
    /// アバターエクスポートサービス
    /// 最適化されたアバターのエクスポート処理を提供
    /// </summary>
    public static class AvatarExportService
    {
        /// <summary>
        /// 最適化されたアバターをエクスポートする
        /// </summary>
        /// <param name="avatar">エクスポート対象のアバター</param>
        /// <returns>エクスポートが成功したかどうか</returns>
        public static bool ExportOptimizedAvatar(GameObject avatar)
        {
            if (avatar == null)
            {
                Debug.LogError($"[AvatarExportService] {LocalizationController.GetText("message_error_avatar_null")}");
                return false;
            }

            var exportPath = GenerateExportPath(avatar);
            var assetPaths = CollectAvatarAssets(avatar);

            if (assetPaths.Count == 0)
            {
                Debug.LogWarning($"[AvatarExportService] No assets found to export for {avatar.name}");
                return false;
            }

            try
            {
                // UnityPackageとしてエクスポート
                AssetDatabase.ExportPackage(assetPaths.ToArray(), exportPath, ExportPackageOptions.Recurse);
                Debug.Log($"[AvatarExportService] Exported optimized avatar to: {exportPath}");

                // 画像キャプチャと保存
                CaptureAvatarImage(avatar, exportPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarExportService] Failed to export {avatar.name}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// アバターのアセット情報を収集する
        /// </summary>
        /// <param name="avatar">対象のアバター</param>
        /// <returns>アセットパスのリスト</returns>
        public static List<string> GetAvatarAssets(GameObject avatar)
        {
            if (avatar == null)
            {
                Debug.LogError("[AvatarExportService] Avatar is null");
                return new List<string>();
            }

            return CollectAvatarAssets(avatar);
        }

        private static void CaptureAvatarImage(GameObject avatar, string unityPackagePath)
        {
            try
            {
                // UnityPackageと同じ場所に同じ名前でpngファイルを保存
                var imagePath = Path.ChangeExtension(unityPackagePath, ".png");
                var capturedTexture = ObjectCaptureAPI.CaptureObject(avatar, imagePath, 512, 512);

                if (capturedTexture != null)
                {
                    Debug.Log($"[AvatarExportService] Captured avatar image: {imagePath}");
                    UnityEngine.Object.DestroyImmediate(capturedTexture);
                }
                else
                {
                    Debug.LogWarning($"[AvatarExportService] Failed to capture avatar image for {avatar.name}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarExportService] Failed to capture avatar image: {e.Message}");
            }
        }

        private static string GenerateExportPath(GameObject avatar)
        {
            var blueprintId = VRChatAPI.GetBlueprintId(avatar);
            var exportDirectory = CreateExportDirectory(blueprintId);
            var fileName = GenerateUniqueFileName(exportDirectory, avatar.name, string.IsNullOrEmpty(blueprintId));

            return Path.Combine(exportDirectory, fileName);
        }

        private static string CreateExportDirectory(string blueprintId)
        {
            var basePath = SettingsController.GetSetting<string>("Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            EnsureDirectoryExists(basePath);

            var autoVariantPath = Path.Combine(basePath, "AutoVariant");
            EnsureDirectoryExists(autoVariantPath);

            var dirName = string.IsNullOrEmpty(blueprintId) ? "local" : blueprintId;
            var avatarDir = Path.Combine(autoVariantPath, dirName);
            EnsureDirectoryExists(avatarDir);

            if (string.IsNullOrEmpty(blueprintId))
            {
                Debug.Log($"[AvatarExportService] {LocalizationController.GetText("message_info_export_no_blueprint")}");
            }

            return avatarDir;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"[AvatarExportService] Created directory: {path}");
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
            var includeAllAssets = SettingsController.GetSetting<bool>("AutoVariant_includeAllAssets", true);

            var avatarPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(avatar);
            if (string.IsNullOrEmpty(avatarPrefabPath))
            {
                Debug.LogWarning($"[AvatarExportService] Could not find prefab path for {avatar.name}");
                return assetPaths;
            }

            assetPaths.Add(avatarPrefabPath);
            CollectDependencies(avatarPrefabPath, assetPaths, includeAllAssets);

            Debug.Log($"[AvatarExportService] Collected {assetPaths.Count} assets for {avatar.name} (includeAllAssets: {includeAllAssets})");
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
