using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AMU.Editor.VrcAssetManager.Schema;

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
                    "アセットIDが空です",
                    "Id",
                    "新しいGUIDを生成してください"));
            }
            else if (!Guid.TryParse(assetId.Value, out _))
            {
                results.Add(ValidationResult.Error(
                    "アセットIDの形式が不正です",
                    "Id",
                    "有効なGUID形式で入力してください"));
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
                    "アセット名が入力されていません",
                    "Name",
                    "わかりやすいアセット名を入力してください"));
            }
            else if (metadata.Name.Length > 100)
            {
                results.Add(ValidationResult.Warning(
                    "アセット名が長すぎます",
                    "Name",
                    "100文字以内で入力してください"));
            }

            // 説明の検証
            if (metadata.Description.Length > 1000)
            {
                results.Add(ValidationResult.Warning(
                    "説明が長すぎます",
                    "Description",
                    "1000文字以内で入力してください"));
            }

            // 作者名の検証
            if (string.IsNullOrWhiteSpace(metadata.AuthorName))
            {
                results.Add(ValidationResult.Info(
                    "作者名が設定されていません",
                    "AuthorName",
                    "作者名を設定することをお勧めします"));
            }
            else if (metadata.AuthorName.Length > 50)
            {
                results.Add(ValidationResult.Warning(
                    "作者名が長すぎます",
                    "AuthorName",
                    "50文字以内で入力してください"));
            }

            // バージョンの検証
            if (string.IsNullOrWhiteSpace(metadata.Version))
            {
                results.Add(ValidationResult.Info(
                    "バージョンが設定されていません",
                    "Version",
                    "バージョン情報を設定することをお勧めします"));
            }

            // タグの検証
            var invalidTags = metadata.Tags.Where(tag => string.IsNullOrWhiteSpace(tag)).ToList();
            if (invalidTags.Any())
            {
                results.Add(ValidationResult.Warning(
                    "空のタグが含まれています",
                    "Tags",
                    "空のタグを削除してください"));
            }

            var longTags = metadata.Tags.Where(tag => tag.Length > 20).ToList();
            if (longTags.Any())
            {
                results.Add(ValidationResult.Warning(
                    $"長すぎるタグがあります: {string.Join(", ", longTags)}",
                    "Tags",
                    "タグは20文字以内にしてください"));
            }

            // 日付の検証
            if (metadata.CreatedDate > DateTime.Now)
            {
                results.Add(ValidationResult.Warning(
                    "作成日が未来の日付になっています",
                    "CreatedDate",
                    "正しい作成日を設定してください"));
            }

            if (metadata.ModifiedDate < metadata.CreatedDate)
            {
                results.Add(ValidationResult.Error(
                    "更新日が作成日より前になっています",
                    "ModifiedDate",
                    "正しい更新日を設定してください"));
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
                    "ファイルパスが設定されていません",
                    "FilePath",
                    "有効なファイルパスを設定してください"));
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
                            "指定されたファイルが存在しません",
                            "FilePath",
                            "存在するファイルのパスを指定してください"));
                    }
                }
                catch (Exception)
                {
                    results.Add(ValidationResult.Error(
                        "ファイルパスの形式が不正です",
                        "FilePath",
                        "有効なファイルパスを指定してください"));
                }
            }

            // ファイルサイズの検証
            if (fileInfo.FileSize.Bytes <= 0)
            {
                results.Add(ValidationResult.Warning(
                    "ファイルサイズが不正です",
                    "FileSize",
                    "正しいファイルサイズを設定してください"));
            }
            else if (fileInfo.FileSize.Bytes > 1024L * 1024 * 1024 * 2) // 2GB
            {
                results.Add(ValidationResult.Warning(
                    "ファイルサイズが非常に大きいです",
                    "FileSize",
                    "大きなファイルはパフォーマンスに影響する可能性があります"));
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
                    "アセットタイプが不明です",
                    "AssetType",
                    "適切なアセットタイプを設定してください"));
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
                            "親グループが見つかりません",
                            "ParentGroupId",
                            "有効な親グループを設定してください"));
                        break;
                    }

                    current = parentGroup.ParentGroupId;
                }

                if (visited.Count >= 100) // 循環参照の可能性
                {
                    results.Add(ValidationResult.Critical(
                        "グループ階層に循環参照の可能性があります",
                        "ParentGroupId",
                        "グループ階層を見直してください"));
                }
            }

            // グループ名の検証
            if (group.IsTopLevel && string.IsNullOrWhiteSpace(group.GroupName))
            {
                results.Add(ValidationResult.Warning(
                    "トップレベルグループに名前が設定されていません",
                    "GroupName",
                    "グループ名を設定することをお勧めします"));
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
                        "BoothアイテムURLが設定されていません",
                        "ItemUrl",
                        "正しいBoothアイテムURLを設定してください"));
                }
                else if (!boothItem.ItemUrl.IsValid)
                {
                    results.Add(ValidationResult.Error(
                        "BoothアイテムURLの形式が不正です",
                        "ItemUrl",
                        "正しいBooth URLの形式で入力してください"));
                }

                // ファイル名の検証
                if (string.IsNullOrWhiteSpace(boothItem.FileName))
                {
                    results.Add(ValidationResult.Warning(
                        "ファイル名が設定されていません",
                        "FileName",
                        "ダウンロードファイル名を設定してください"));
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
                results.Add(ValidationResult.Critical("アセットがnullです", "", "有効なアセットを提供してください"));
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
                results.Add(ValidationResult.Critical("ライブラリがnullです", "", "有効なライブラリを提供してください"));
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
                    $"重複する名前のアセットがあります: {duplicateName}",
                    "Name",
                    "アセット名を一意にすることをお勧めします"));
            }

            return results;
        }
    }
}
