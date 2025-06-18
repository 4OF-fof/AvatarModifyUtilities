using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    /// <summary>
    /// VRCアセットのファイル操作を管理するコントローラ
    /// </summary>
    public static class VrcAssetFileController
    {
        private static readonly string[] SupportedFileExtensions = {
            ".prefab", ".unity", ".unitypackage", ".fbx", ".obj",
            ".png", ".jpg", ".jpeg", ".tga", ".psd",
            ".mat", ".shader", ".hlsl", ".cginc",
            ".cs", ".dll", ".asmdef"
        };

        /// <summary>
        /// 指定されたパスがVRCアセットとして有効かどうかを判定します
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>有効な場合true</returns>
        public static bool IsValidVrcAssetFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLower();
            return SupportedFileExtensions.Contains(extension);
        }

        /// <summary>
        /// VRCアセットファイルをインポートします
        /// </summary>
        /// <param name="filePath">インポートするファイルパス</param>
        /// <returns>インポートに成功した場合、作成されたアセットデータ</returns>
        public static AssetSchema ImportAssetFile(string filePath)
        {
            try
            {
                if (!IsValidVrcAssetFile(filePath))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_invalidFile"), filePath));
                    return default(AssetSchema);
                }

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileInfo = new FileInfo(filePath);

                var assetData = new AssetSchema(fileName, new AssetType(DetermineAssetCategory(filePath)), filePath);
                assetData.FileInfo.FileSize = new FileSize(fileInfo.Length);
                assetData.Metadata.CreatedDate = DateTime.Now;
                assetData.Metadata.ModifiedDate = fileInfo.LastWriteTime;
                assetData.Metadata.Version = "1.0.0";

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_fileImported"), fileName));
                return assetData;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_importFailed"), ex.Message));
                return default(AssetSchema);
            }
        }

        /// <summary>
        /// 複数のVRCアセットファイルを一括インポートします
        /// </summary>
        /// <param name="filePaths">インポートするファイルパスのリスト</param>
        /// <returns>インポートに成功したアセットデータのリスト</returns>
        public static List<AssetSchema> ImportMultipleAssetFiles(IEnumerable<string> filePaths)
        {
            var importedAssets = new List<AssetSchema>();

            foreach (var filePath in filePaths)
            {
                var assetData = ImportAssetFile(filePath);
                if (assetData.Id != default(AssetId))
                {
                    importedAssets.Add(assetData);
                }
            }

            Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_multipleImported"), importedAssets.Count));
            return importedAssets;
        }

        /// <summary>
        /// 指定されたディレクトリ内のVRCアセットファイルをスキャンします
        /// </summary>
        /// <param name="directoryPath">スキャンするディレクトリパス</param>
        /// <param name="recursive">サブディレクトリも含めるかどうか</param>
        /// <returns>発見されたVRCアセットファイルのパスリスト</returns>
        public static List<string> ScanDirectory(string directoryPath, bool recursive = true)
        {
            var foundFiles = new List<string>();

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_directoryNotFound"), directoryPath));
                    return foundFiles;
                }

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                foreach (var extension in SupportedFileExtensions)
                {
                    var pattern = "*" + extension;
                    var files = Directory.GetFiles(directoryPath, pattern, searchOption);
                    foundFiles.AddRange(files);
                }

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_scanCompleted"), foundFiles.Count, directoryPath));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_scanFailed"), ex.Message));
            }

            return foundFiles.Distinct().ToList();
        }

        /// <summary>
        /// VRCアセットファイルを指定されたディレクトリにエクスポートします
        /// </summary>
        /// <param name="assetData">エクスポートするアセットデータ</param>
        /// <param name="destinationPath">エクスポート先ディレクトリ</param>
        /// <returns>エクスポートに成功した場合true</returns>
        public static bool ExportAsset(AssetSchema assetData, string destinationPath)
        {
            try
            {
                if (!File.Exists(assetData.FileInfo.FilePath))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_sourceFileNotFound"), assetData.FileInfo.FilePath));
                    return false;
                }

                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                var fileName = Path.GetFileName(assetData.FileInfo.FilePath);
                var destinationFilePath = Path.Combine(destinationPath, fileName);

                File.Copy(assetData.FileInfo.FilePath, destinationFilePath, true);

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_assetExported"), assetData.Metadata.Name, destinationFilePath));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_exportFailed"), ex.Message));
                return false;
            }
        }

        /// <summary>
        /// VRCアセットファイルが存在するかどうかを確認します
        /// </summary>
        /// <param name="assetData">確認するアセットデータ</param>
        /// <returns>ファイルが存在する場合true</returns>
        public static bool ValidateAssetFile(AssetSchema assetData)
        {
            return !string.IsNullOrEmpty(assetData.FileInfo.FilePath) && File.Exists(assetData.FileInfo.FilePath);
        }

        /// <summary>
        /// VRCアセットファイルの情報を更新します
        /// </summary>
        /// <param name="assetData">更新するアセットデータ</param>
        /// <returns>更新されたアセットデータ</returns>
        public static AssetSchema RefreshAssetFileInfo(AssetSchema assetData)
        {
            try
            {
                if (!ValidateAssetFile(assetData))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_fileNotFound"), assetData.FileInfo.FilePath));
                    return assetData;
                }

                var fileInfo = new FileInfo(assetData.FileInfo.FilePath);
                assetData.FileInfo.FileSize = new FileSize(fileInfo.Length);
                assetData.Metadata.ModifiedDate = fileInfo.LastWriteTime;

                return assetData;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_refreshFailed"), ex.Message));
                return assetData;
            }
        }

        /// <summary>
        /// サポートされているファイル拡張子の一覧を取得します
        /// </summary>
        /// <returns>サポートされているファイル拡張子のリスト</returns>
        public static string[] GetSupportedFileExtensions()
        {
            return (string[])SupportedFileExtensions.Clone();
        }

        /// <summary>
        /// ファイルパスからアセットカテゴリを推定します
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>推定されたカテゴリ名</returns>
        private static string DetermineAssetCategory(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            var fileName = Path.GetFileName(filePath).ToLower();

            // ファイル拡張子に基づくカテゴリ判定
            switch (extension)
            {
                case ".prefab":
                    return "Prefabs";
                case ".unity":
                    return "Scenes";
                case ".unitypackage":
                    return "Packages";
                case ".fbx":
                case ".obj":
                    return "Models";
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                    return "Textures";
                case ".mat":
                    return "Materials";
                case ".shader":
                case ".hlsl":
                case ".cginc":
                    return "Shaders";
                case ".cs":
                case ".dll":
                case ".asmdef":
                    return "Scripts";
                default:
                    return "Other";
            }
        }
    }
}
