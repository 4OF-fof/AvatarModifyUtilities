using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// 検証結果の重要度レベル
    /// </summary>
    public enum ValidationLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 検証結果の項目
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        [SerializeField] private ValidationLevel _level;
        [SerializeField] private string _message;
        [SerializeField] private string _fieldName;
        [SerializeField] private string _suggestion;
        [SerializeField] private DateTime _timestamp;

        public ValidationLevel Level
        {
            get => _level;
            set => _level = value;
        }

        public string Message
        {
            get => _message ?? string.Empty;
            set => _message = value?.Trim() ?? string.Empty;
        }

        public string FieldName
        {
            get => _fieldName ?? string.Empty;
            set => _fieldName = value?.Trim() ?? string.Empty;
        }

        public string Suggestion
        {
            get => _suggestion ?? string.Empty;
            set => _suggestion = value?.Trim() ?? string.Empty;
        }

        public DateTime Timestamp
        {
            get => _timestamp == default ? DateTime.Now : _timestamp;
            set => _timestamp = value;
        }

        public bool IsValid => _level == ValidationLevel.Info;
        public bool HasSuggestion => !string.IsNullOrEmpty(_suggestion);

        public ValidationResult()
        {
            _level = ValidationLevel.Info;
            _message = string.Empty;
            _fieldName = string.Empty;
            _suggestion = string.Empty;
            _timestamp = DateTime.Now;
        }

        public ValidationResult(ValidationLevel level, string message, string fieldName = "", string suggestion = "") : this()
        {
            _level = level;
            _message = message?.Trim() ?? string.Empty;
            _fieldName = fieldName?.Trim() ?? string.Empty;
            _suggestion = suggestion?.Trim() ?? string.Empty;
        }

        public static ValidationResult Info(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Info, message, fieldName, suggestion);

        public static ValidationResult Warning(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Warning, message, fieldName, suggestion);

        public static ValidationResult Error(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Error, message, fieldName, suggestion);

        public static ValidationResult Critical(string message, string fieldName = "", string suggestion = "")
            => new ValidationResult(ValidationLevel.Critical, message, fieldName, suggestion);
    }

    /// <summary>
    /// 検証結果のコレクション
    /// </summary>
    [Serializable]
    public class ValidationResults
    {
        [SerializeField] private List<ValidationResult> _results;
        [SerializeField] private DateTime _validationTime;
        [SerializeField] private string _targetName;

        public IReadOnlyList<ValidationResult> Results => _results ?? new List<ValidationResult>();

        public DateTime ValidationTime
        {
            get => _validationTime == default ? DateTime.Now : _validationTime;
            set => _validationTime = value;
        }

        public string TargetName
        {
            get => _targetName ?? string.Empty;
            set => _targetName = value?.Trim() ?? string.Empty;
        }

        public int Count => _results?.Count ?? 0;
        public bool HasResults => Count > 0;
        public bool IsValid => !HasErrors && !HasCritical;
        public bool HasErrors => _results?.Any(r => r.Level == ValidationLevel.Error) ?? false;
        public bool HasWarnings => _results?.Any(r => r.Level == ValidationLevel.Warning) ?? false;
        public bool HasCritical => _results?.Any(r => r.Level == ValidationLevel.Critical) ?? false;

        public int ErrorCount => _results?.Count(r => r.Level == ValidationLevel.Error) ?? 0;
        public int WarningCount => _results?.Count(r => r.Level == ValidationLevel.Warning) ?? 0;
        public int CriticalCount => _results?.Count(r => r.Level == ValidationLevel.Critical) ?? 0;

        public ValidationResults()
        {
            _results = new List<ValidationResult>();
            _validationTime = DateTime.Now;
            _targetName = string.Empty;
        }

        public ValidationResults(string targetName) : this()
        {
            _targetName = targetName?.Trim() ?? string.Empty;
        }

        public void Add(ValidationResult result)
        {
            if (result == null) return;

            _results ??= new List<ValidationResult>();
            _results.Add(result);
        }

        public void AddRange(IEnumerable<ValidationResult> results)
        {
            if (results == null) return;

            _results ??= new List<ValidationResult>();
            _results.AddRange(results.Where(r => r != null));
        }

        public void Clear()
        {
            _results?.Clear();
            _validationTime = DateTime.Now;
        }

        public IEnumerable<ValidationResult> GetByLevel(ValidationLevel level)
        {
            return _results?.Where(r => r.Level == level) ?? Enumerable.Empty<ValidationResult>();
        }

        public IEnumerable<ValidationResult> GetByField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return Enumerable.Empty<ValidationResult>();

            return _results?.Where(r => r.FieldName.Equals(fieldName.Trim(), StringComparison.OrdinalIgnoreCase))
                   ?? Enumerable.Empty<ValidationResult>();
        }

        public ValidationLevel GetHighestLevel()
        {
            if (_results == null || _results.Count == 0) return ValidationLevel.Info;

            if (HasCritical) return ValidationLevel.Critical;
            if (HasErrors) return ValidationLevel.Error;
            if (HasWarnings) return ValidationLevel.Warning;
            return ValidationLevel.Info;
        }
    }

    /// <summary>
    /// アセット検証のルール
    /// </summary>
    public static class AssetValidationRules
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
                results.Add(ValidationResult.Warning(
                    "作者名が入力されていません",
                    "AuthorName",
                    "作者名を入力することをお勧めします"));
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
                results.Add(ValidationResult.Error(
                    "ファイルパスが設定されていません",
                    "FilePath",
                    "有効なファイルパスを設定してください"));
            }
            else if (!File.Exists(fileInfo.FilePath))
            {
                results.Add(ValidationResult.Error(
                    "指定されたファイルが存在しません",
                    "FilePath",
                    "正しいファイルパスを設定してください"));
            }

            // サムネイルの検証
            if (!string.IsNullOrWhiteSpace(fileInfo.ThumbnailPath) && !File.Exists(fileInfo.ThumbnailPath))
            {
                results.Add(ValidationResult.Warning(
                    "指定されたサムネイルファイルが存在しません",
                    "ThumbnailPath",
                    "正しいサムネイルパスを設定するか、空にしてください"));
            }

            // ファイルサイズの検証
            if (fileInfo.FileSize.Bytes < 0)
            {
                results.Add(ValidationResult.Error(
                    "ファイルサイズが不正です",
                    "FileSize",
                    "正しいファイルサイズを設定してください"));
            }
            else if (fileInfo.FileSize.Bytes > 1024 * 1024 * 1024) // 1GB
            {
                results.Add(ValidationResult.Warning(
                    "ファイルサイズが非常に大きいです",
                    "FileSize",
                    "ファイルサイズを確認してください"));
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
                results.Add(ValidationResult.Error(
                    "アセットタイプが設定されていません",
                    "AssetType",
                    "適切なアセットタイプを選択してください"));
            }

            return results;
        }

        /// <summary>
        /// アセット状態の検証
        /// </summary>
        public static ValidationResults ValidateAssetState(AssetState state)
        {
            var results = new ValidationResults("AssetState");

            // 特に重要な検証はないが、将来的な拡張のために残しておく
            if (state.IsHidden && state.IsFavorite)
            {
                results.Add(ValidationResult.Warning(
                    "隠されたアセットがお気に入りに設定されています",
                    "State",
                    "この設定が意図的かどうか確認してください"));
            }

            return results;
        }

        /// <summary>
        /// グループ情報の検証
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
    }

    /// <summary>
    /// 完全なアセット検証
    /// </summary>
    public static class AssetValidator
    {
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
            results.AddRange(AssetValidationRules.ValidateAssetId(asset.Id).Results);
            results.AddRange(AssetValidationRules.ValidateMetadata(asset.Metadata).Results);
            results.AddRange(AssetValidationRules.ValidateFileInfo(asset.FileInfo).Results);
            results.AddRange(AssetValidationRules.ValidateAssetType(asset.AssetType).Results);
            results.AddRange(AssetValidationRules.ValidateAssetState(asset.State).Results);

            // グループ情報の検証（存在する場合）
            if (allGroups?.TryGetValue(asset.Id, out var groupSchema) == true)
            {
                results.AddRange(AssetValidationRules.ValidateGroupSchema(groupSchema, allGroups).Results);
            }

            // Booth情報の検証（存在する場合）
            if (asset.BoothItem != null)
            {
                results.AddRange(AssetValidationRules.ValidateBoothItem(asset.BoothItem).Results);
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

            // 基本情報の検証
            if (string.IsNullOrWhiteSpace(library.Name))
            {
                results.Add(ValidationResult.Warning("ライブラリ名が設定されていません", "Name", "ライブラリ名を設定してください"));
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
