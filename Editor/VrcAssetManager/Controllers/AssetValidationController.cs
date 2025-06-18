using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    /// <summary>
    /// アセット検証のルール
    /// </summary>
    public static class AssetValidationController
    {
        /// <summary>
        /// アセットIDの検証
        /// </summary>
        public static ValidationResults ValidateAssetId(AssetId assetId)
        {
            var results = new ValidationResults("AssetId");

            if (string.IsNullOrEmpty(assetId.Value))
            {
                results.Add(ValidationResult.Critical(
                    LocalizationController.GetText("VrcAssetManager_validation_error_assetIdEmpty"),
                    "Id",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_generateNewGuid")));
            }
            else if (!Guid.TryParse(assetId.Value, out _))
            {
                results.Add(ValidationResult.Error(
                    LocalizationController.GetText("VrcAssetManager_validation_error_assetIdInvalid"),
                    "Id",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_validGuidFormat")));
            }

            return results;
        }

        /// <summary>
        /// アセットメタデータの検証
        /// </summary>
        public static ValidationResults ValidateMetadata(AssetMetadata metadata)
        {
            var results = new ValidationResults("Metadata");

            // 名前の検証
            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                results.Add(ValidationResult.Error(
                    LocalizationController.GetText("VrcAssetManager_validation_error_nameEmpty"),
                    "Name",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_clearName")));
            }
            else if (metadata.Name.Length > 100)
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_nameTooLong"),
                    "Name",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_nameLength")));
            }

            // 説明の検証
            if (metadata.Description.Length > 1000)
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_descriptionTooLong"),
                    "Description",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_descriptionLength")));
            }

            // 作者名の検証
            if (string.IsNullOrWhiteSpace(metadata.AuthorName))
            {
                results.Add(ValidationResult.Info(
                    LocalizationController.GetText("VrcAssetManager_validation_info_authorNameEmpty"),
                    "AuthorName",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_setAuthorName")));
            }
            else if (metadata.AuthorName.Length > 50)
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_authorNameTooLong"),
                    "AuthorName",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_authorNameLength")));
            }

            // バージョンの検証
            if (string.IsNullOrWhiteSpace(metadata.Version))
            {
                results.Add(ValidationResult.Info(
                    LocalizationController.GetText("VrcAssetManager_validation_info_versionEmpty"),
                    "Version",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_setVersion")));
            }

            // タグの検証
            var invalidTags = metadata.Tags.Where(tag => string.IsNullOrWhiteSpace(tag)).ToList();
            if (invalidTags.Any())
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_emptyTags"),
                    "Tags",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_removeEmptyTags")));
            }

            var longTags = metadata.Tags.Where(tag => tag.Length > 20).ToList();
            if (longTags.Any())
            {
                results.Add(ValidationResult.Warning(
                    string.Format(LocalizationController.GetText("VrcAssetManager_validation_warning_longTags"), string.Join(", ", longTags)),
                    "Tags",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_tagLength")));
            }

            // 日付の検証
            if (metadata.CreatedDate > DateTime.Now)
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_futureDateCreated"),
                    "CreatedDate",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_correctCreatedDate")));
            }

            if (metadata.ModifiedDate < metadata.CreatedDate)
            {
                results.Add(ValidationResult.Error(
                    LocalizationController.GetText("VrcAssetManager_validation_error_modifiedBeforeCreated"),
                    "ModifiedDate",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_correctModifiedDate")));
            }

            return results;
        }

        /// <summary>
        /// ファイル情報の検証
        /// </summary>
        public static ValidationResults ValidateFileInfo(AssetFileInfo fileInfo)
        {
            var results = new ValidationResults("FileInfo");

            // ファイルパスの検証
            if (string.IsNullOrWhiteSpace(fileInfo.FilePath))
            {
                results.Add(ValidationResult.Critical(
                    LocalizationController.GetText("VrcAssetManager_validation_critical_filePathEmpty"),
                    "FilePath",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_setValidFilePath")));
            }
            else
            {
                // パスの妥当性チェック
                try
                {
                    var fullPath = Path.GetFullPath(fileInfo.FilePath);
                    if (!File.Exists(fullPath))
                    {
                        results.Add(ValidationResult.Error(
                            LocalizationController.GetText("VrcAssetManager_validation_error_fileNotFound"),
                            "FilePath",
                            LocalizationController.GetText("VrcAssetManager_validation_suggestion_existingFilePath")));
                    }
                }
                catch (Exception)
                {
                    results.Add(ValidationResult.Error(
                        LocalizationController.GetText("VrcAssetManager_validation_error_filePathInvalid"),
                        "FilePath",
                        LocalizationController.GetText("VrcAssetManager_validation_suggestion_setValidFilePath")));
                }
            }

            // ファイルサイズの検証
            if (fileInfo.FileSize.Bytes <= 0)
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_fileSizeInvalid"),
                    "FileSize",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_correctFileSize")));
            }
            else if (fileInfo.FileSize.Bytes > 1024L * 1024 * 1024 * 2) // 2GB
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_fileSizeLarge"),
                    "FileSize",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_performanceWarning")));
            }

            return results;
        }

        /// <summary>
        /// アセットタイプの検証
        /// </summary>
        public static ValidationResults ValidateAssetType(AssetType assetType)
        {
            var results = new ValidationResults("AssetType");

            if (string.IsNullOrWhiteSpace(assetType.Value))
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_assetTypeUnknown"),
                    "AssetType",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_setAssetType")));
            }

            return results;
        }

        /// <summary>
        /// グループスキーマの検証
        /// </summary>
        public static ValidationResults ValidateGroupSchema(AssetGroupSchema group, IReadOnlyDictionary<AssetId, AssetGroupSchema> allGroups)
        {
            var results = new ValidationResults("GroupSchema");

            // 循環参照の検証
            if (group.HasParent && allGroups != null)
            {
                var visited = new HashSet<AssetId>();
                var current = group.ParentGroupId;

                while (!string.IsNullOrEmpty(current.Value) && visited.Add(current))
                {
                    if (!allGroups.TryGetValue(current, out var parentGroup))
                    {
                        results.Add(ValidationResult.Error(
                            LocalizationController.GetText("VrcAssetManager_validation_error_parentGroupNotFound"),
                            "ParentGroupId",
                            LocalizationController.GetText("VrcAssetManager_validation_suggestion_setValidParentGroup")));
                        break;
                    }

                    current = parentGroup.ParentGroupId;
                }

                if (visited.Count >= 100) // 循環参照の可能性
                {
                    results.Add(ValidationResult.Critical(
                        LocalizationController.GetText("VrcAssetManager_validation_critical_circularReference"),
                        "ParentGroupId",
                        LocalizationController.GetText("VrcAssetManager_validation_suggestion_reviewGroupHierarchy")));
                }
            }

            // グループ名の検証
            if (group.IsTopLevel && string.IsNullOrWhiteSpace(group.GroupName))
            {
                results.Add(ValidationResult.Warning(
                    LocalizationController.GetText("VrcAssetManager_validation_warning_topLevelGroupNameEmpty"),
                    "GroupName",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_setGroupName")));
            }

            return results;
        }

        /// <summary>
        /// Booth情報の検証
        /// </summary>
        public static ValidationResults ValidateBoothItem(BoothItemSchema boothItem)
        {
            var results = new ValidationResults("BoothItem");

            if (boothItem.HasData)
            {
                // URLの検証
                if (boothItem.ItemUrl.IsEmpty)
                {
                    results.Add(ValidationResult.Warning(
                        LocalizationController.GetText("VrcAssetManager_validation_warning_boothUrlEmpty"),
                        "ItemUrl",
                        LocalizationController.GetText("VrcAssetManager_validation_suggestion_setBoothUrl")));
                }
                else if (!boothItem.ItemUrl.IsValid)
                {
                    results.Add(ValidationResult.Error(
                        LocalizationController.GetText("VrcAssetManager_validation_error_boothUrlInvalid"),
                        "ItemUrl",
                        LocalizationController.GetText("VrcAssetManager_validation_suggestion_correctBoothUrlFormat")));
                }

                // ファイル名の検証
                if (string.IsNullOrWhiteSpace(boothItem.FileName))
                {
                    results.Add(ValidationResult.Warning(
                        LocalizationController.GetText("VrcAssetManager_validation_warning_boothFileNameEmpty"),
                        "FileName",
                        LocalizationController.GetText("VrcAssetManager_validation_suggestion_setFileName")));
                }
            }

            return results;
        }

        /// <summary>
        /// アセット全体の検証
        /// </summary>
        public static ValidationResults ValidateAsset(AssetSchema asset, IReadOnlyDictionary<AssetId, AssetGroupSchema> allGroups = null)
        {
            var results = new ValidationResults($"Asset: {asset?.Metadata?.Name ?? "Unknown"}");

            if (asset == null)
            {
                results.Add(ValidationResult.Critical(
                    LocalizationController.GetText("VrcAssetManager_validation_critical_assetNull"),
                    "",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_provideValidAsset")));
                return results;
            }

            // 各コンポーネントの検証
            results.AddRange(ValidateAssetId(asset.Id).Results);
            results.AddRange(ValidateMetadata(asset.Metadata).Results);
            results.AddRange(ValidateFileInfo(asset.FileInfo).Results);
            results.AddRange(ValidateAssetType(asset.AssetType).Results);

            // グループ情報の検証（存在する場合）
            if (allGroups?.TryGetValue(asset.Id, out var groupSchema) == true)
            {
                results.AddRange(ValidateGroupSchema(groupSchema, allGroups).Results);
            }

            // Booth情報の検証（存在する場合）
            if (asset.BoothItem != null)
            {
                results.AddRange(ValidateBoothItem(asset.BoothItem).Results);
            }

            return results;
        }

        /// <summary>
        /// ライブラリ全体の検証
        /// </summary>
        public static ValidationResults ValidateLibrary(AssetLibrarySchema library)
        {
            var results = new ValidationResults("AssetLibrary");

            if (library == null)
            {
                results.Add(ValidationResult.Critical(
                    LocalizationController.GetText("VrcAssetManager_validation_critical_libraryNull"),
                    "",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_provideValidLibrary")));
                return results;
            }

            // 各アセットの検証
            foreach (var asset in library.Assets.Values)
            {
                var assetResults = ValidateAsset(asset, library.Groups);
                if (assetResults.HasErrors || assetResults.HasCritical)
                {
                    results.AddRange(assetResults.Results);
                }
            }

            // 重複チェック
            var duplicateNames = library.Assets.Values
                .GroupBy(a => a.Metadata.Name.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateName in duplicateNames)
            {
                results.Add(ValidationResult.Warning(
                    string.Format(LocalizationController.GetText("VrcAssetManager_validation_warning_duplicateNames"), duplicateName),
                    "Name",
                    LocalizationController.GetText("VrcAssetManager_validation_suggestion_uniqueAssetNames")));
            }

            return results;
        }
    }
}
