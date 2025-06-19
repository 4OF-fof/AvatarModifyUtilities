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
        /// <summary>
        /// VRCアセットファイルをインポートします
        /// </summary>
        /// <param name="filePath">インポートするファイルパス</param>
        /// <returns>インポートに成功した場合、作成されたアセットデータ</returns>
        public static AssetSchema ImportAssetFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_invalidFile"), filePath));
                    return default(AssetSchema);
                }

                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileInfo = new FileInfo(filePath);

                // 新しいスキーマ構造に合わせてAssetMetadataを作成
                var metadata = new AssetMetadata(
                    fileName,
                    "",
                    "",
                    "Other",
                    null,
                    null,
                    DateTime.Now,
                    fileInfo.LastWriteTime
                );

                // AssetFileInfoを作成
                var assetFileInfo = new AssetFileInfo(
                    filePath,
                    "",
                    fileInfo.Length
                );

                // AssetStateを作成
                var state = new AssetState();

                // AssetSchemaを作成
                var assetData = new AssetSchema(
                    "",
                    metadata,
                    assetFileInfo,
                    state
                );

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
        /// VRCアセットファイルの情報を更新します
        /// </summary>
        /// <param name="assetData">更新するアセットデータ</param>
        /// <returns>更新されたアセットデータ</returns>
        public static AssetSchema UpdateAssetFileInfo(AssetSchema assetData)
        {
            try
            {
                if (string.IsNullOrEmpty(assetData.FileInfo.FilePath) || !File.Exists(assetData.FileInfo.FilePath))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_fileNotFound"), assetData.FileInfo.FilePath));
                    return assetData;
                }

                var fileInfo = new FileInfo(assetData.FileInfo.FilePath);

                // 新しいスキーマ構造に合わせて更新されたAssetMetadataを作成
                var updatedMetadata = new AssetMetadata(
                    assetData.Metadata.Name,
                    assetData.Metadata.Description,
                    assetData.Metadata.AuthorName,
                    assetData.Metadata.AssetType,
                    new List<string>(assetData.Metadata.Tags),
                    new List<string>(assetData.Metadata.Dependencies),
                    assetData.Metadata.CreatedDate,
                    fileInfo.LastWriteTime
                );

                // 更新されたAssetFileInfoを作成
                var updatedFileInfo = new AssetFileInfo(
                    assetData.FileInfo.FilePath,
                    assetData.FileInfo.ThumbnailPath,
                    fileInfo.Length,
                    new List<string>(assetData.FileInfo.ImportFiles)
                );

                // 更新されたAssetSchemaを作成
                var updatedAssetData = new AssetSchema(
                    assetData.ParentGroupId,
                    updatedMetadata,
                    updatedFileInfo,
                    assetData.State,
                    assetData.BoothItem,
                    assetData.LastAccessed,
                    new List<string>(assetData.ChildAssetIds)
                );

                return updatedAssetData;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_refreshFailed"), ex.Message));
                return assetData;
            }
        }

        /// <summary>
        /// ファイルサイズを人間が読みやすい文字列形式に変換します
        /// </summary>
        /// <param name="bytes">バイト数</param>
        /// <returns>読みやすい形式の文字列（例: "1.2 MB", "345 KB"）</returns>
        public static string ConvertBytesToString(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        /// <summary>
        /// 指定されたアセットのファイルサイズを人間が読みやすい文字列形式で取得します
        /// </summary>
        /// <param name="asset">アセットデータ</param>
        /// <returns>読みやすい形式の文字列</returns>
        public static string GetFormattedFileSize(AssetSchema asset)
        {
            if (asset?.FileInfo == null)
            {
                return "0 B";
            }
            return ConvertBytesToString(asset.FileInfo.FileSizeBytes);
        }

        /// <summary>
        /// 指定されたアセットのファイルサイズを人間が読みやすい文字列形式で取得します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>読みやすい形式の文字列</returns>
        public static string GetFormattedFileSize(AssetId assetId)
        {
            var asset = VrcAssetController.GetAsset(assetId);
            if (asset?.FileInfo == null)
            {
                return "0 B";
            }
            return ConvertBytesToString(asset.FileInfo.FileSizeBytes);
        }
    }
}
