using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// アセットライブラリのバージョン管理
    /// </summary>
    [Serializable]
    public struct LibraryVersion : IComparable<LibraryVersion>
    {
        [SerializeField] private int _major;
        [SerializeField] private int _minor;
        [SerializeField] private int _patch;

        public int Major => _major;
        public int Minor => _minor;
        public int Patch => _patch;

        public LibraryVersion(int major, int minor, int patch)
        {
            _major = Math.Max(0, major);
            _minor = Math.Max(0, minor);
            _patch = Math.Max(0, patch);
        }

        public static LibraryVersion Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return new LibraryVersion(1, 0, 0);

            var parts = version.Split('.');
            var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 1;
            var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
            var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;

            return new LibraryVersion(major, minor, patch);
        }

        public override string ToString() => $"{_major}.{_minor}.{_patch}";

        public int CompareTo(LibraryVersion other)
        {
            var majorComparison = _major.CompareTo(other._major);
            if (majorComparison != 0) return majorComparison;

            var minorComparison = _minor.CompareTo(other._minor);
            if (minorComparison != 0) return minorComparison;

            return _patch.CompareTo(other._patch);
        }

        public static bool operator >(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) > 0;
        public static bool operator <(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) < 0;
        public static bool operator >=(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) >= 0;
        public static bool operator <=(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) <= 0;
        public static bool operator ==(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) == 0;
        public static bool operator !=(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) != 0;

        public override bool Equals(object obj) => obj is LibraryVersion other && CompareTo(other) == 0;
        public override int GetHashCode() => (_major << 16) | (_minor << 8) | _patch;

        public static LibraryVersion Current => new LibraryVersion(1, 0, 0);
    }

    /// <summary>
    /// アセットの統計情報
    /// </summary>
    [Serializable]
    public class AssetStatistics
    {
        [SerializeField] private int _totalAssets;
        [SerializeField] private int _favoriteAssets;
        [SerializeField] private int _groupAssets;
        [SerializeField] private long _totalFileSize;
        [SerializeField] private Dictionary<string, int> _assetTypeCount;
        [SerializeField] private Dictionary<string, int> _tagCount;
        [SerializeField] private Dictionary<string, int> _authorCount;
        [SerializeField] private DateTime _lastCalculated;

        public int TotalAssets => _totalAssets;
        public int FavoriteAssets => _favoriteAssets;
        public int GroupAssets => _groupAssets;
        public FileSize TotalFileSize => new FileSize(_totalFileSize);

        public IReadOnlyDictionary<string, int> AssetTypeCount =>
            _assetTypeCount ?? new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> TagCount =>
            _tagCount ?? new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> AuthorCount =>
            _authorCount ?? new Dictionary<string, int>();

        public DateTime LastCalculated => _lastCalculated;

        public AssetStatistics()
        {
            _totalAssets = 0;
            _favoriteAssets = 0;
            _groupAssets = 0;
            _totalFileSize = 0;
            _assetTypeCount = new Dictionary<string, int>();
            _tagCount = new Dictionary<string, int>();
            _authorCount = new Dictionary<string, int>();
            _lastCalculated = DateTime.Now;
        }

        public void Update(IEnumerable<AssetSchema> assets)
        {
            Reset();

            foreach (var asset in assets)
            {
                _totalAssets++;

                if (asset.State.IsFavorite) _favoriteAssets++;
                if (asset.State.IsGroup) _groupAssets++;

                _totalFileSize += asset.FileInfo.FileSize.Bytes;

                // アセットタイプ集計
                var assetType = asset.AssetType.Value;
                _assetTypeCount[assetType] = _assetTypeCount.GetValueOrDefault(assetType, 0) + 1;

                // 作者集計
                var author = asset.Metadata.AuthorName;
                if (!string.IsNullOrEmpty(author))
                {
                    _authorCount[author] = _authorCount.GetValueOrDefault(author, 0) + 1;
                }

                // タグ集計
                foreach (var tag in asset.Metadata.Tags)
                {
                    _tagCount[tag] = _tagCount.GetValueOrDefault(tag, 0) + 1;
                }
            }

            _lastCalculated = DateTime.Now;
        }

        private void Reset()
        {
            _totalAssets = 0;
            _favoriteAssets = 0;
            _groupAssets = 0;
            _totalFileSize = 0;
            _assetTypeCount.Clear();
            _tagCount.Clear();
            _authorCount.Clear();
        }

        public string GetMostCommonAssetType()
        {
            return _assetTypeCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "Other";
        }

        public string GetMostActiveAuthor()
        {
            return _authorCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? string.Empty;
        }

        public IEnumerable<string> GetTopTags(int count = 10)
        {
            return _tagCount.OrderByDescending(kvp => kvp.Value)
                           .Take(count)
                           .Select(kvp => kvp.Key);
        }
    }

    /// <summary>
    /// バックアップの情報
    /// </summary>
    [Serializable]
    public class BackupInfo
    {
        [SerializeField] private string _backupPath;
        [SerializeField] private DateTime _backupTime;
        [SerializeField] private long _backupSize;
        [SerializeField] private int _assetCount;
        [SerializeField] private string _checksum;

        public string BackupPath
        {
            get => _backupPath ?? string.Empty;
            set => _backupPath = value?.Trim() ?? string.Empty;
        }

        public DateTime BackupTime
        {
            get => _backupTime;
            set => _backupTime = value;
        }

        public FileSize BackupSize => new FileSize(_backupSize);

        public int AssetCount
        {
            get => _assetCount;
            set => _assetCount = Math.Max(0, value);
        }

        public string Checksum
        {
            get => _checksum ?? string.Empty;
            set => _checksum = value?.Trim() ?? string.Empty;
        }

        public BackupInfo()
        {
            _backupPath = string.Empty;
            _backupTime = DateTime.Now;
            _backupSize = 0;
            _assetCount = 0;
            _checksum = string.Empty;
        }

        public BackupInfo(string backupPath, int assetCount, long backupSize, string checksum) : this()
        {
            _backupPath = backupPath?.Trim() ?? string.Empty;
            _assetCount = Math.Max(0, assetCount);
            _backupSize = Math.Max(0, backupSize);
            _checksum = checksum?.Trim() ?? string.Empty;
        }
    }

    /// <summary>
    /// アセットライブラリの完全なスキーマ
    /// </summary>
    [Serializable]
    public class AssetLibrarySchema
    {
        [SerializeField] private LibraryVersion _version;
        [SerializeField] private DateTime _createdDate;
        [SerializeField] private DateTime _lastUpdated;
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private Dictionary<string, AssetSchema> _assets;
        [SerializeField] private Dictionary<string, AssetGroupSchema> _groups;
        [SerializeField] private AssetStatistics _statistics;
        [SerializeField] private List<BackupInfo> _backupHistory;
        [SerializeField] private string _dataFilePath;

        public LibraryVersion Version
        {
            get => _version;
            set => _version = value;
        }

        public DateTime CreatedDate
        {
            get => _createdDate == default ? DateTime.Now : _createdDate;
            set => _createdDate = value;
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            set => _lastUpdated = value;
        }

        public string Name
        {
            get => _name ?? "Asset Library";
            set => _name = value?.Trim() ?? "Asset Library";
        }

        public string Description
        {
            get => _description ?? string.Empty;
            set => _description = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyDictionary<AssetId, AssetSchema> Assets
        {
            get
            {
                if (_assets == null) return new Dictionary<AssetId, AssetSchema>();

                var result = new Dictionary<AssetId, AssetSchema>();
                foreach (var kvp in _assets)
                {
                    if (AssetId.TryParse(kvp.Key, out var assetId))
                    {
                        result[assetId] = kvp.Value;
                    }
                }
                return result;
            }
        }

        public IReadOnlyDictionary<AssetId, AssetGroupSchema> Groups
        {
            get
            {
                if (_groups == null) return new Dictionary<AssetId, AssetGroupSchema>();

                var result = new Dictionary<AssetId, AssetGroupSchema>();
                foreach (var kvp in _groups)
                {
                    if (AssetId.TryParse(kvp.Key, out var assetId))
                    {
                        result[assetId] = kvp.Value;
                    }
                }
                return result;
            }
        }

        public AssetStatistics Statistics => _statistics ?? new AssetStatistics();

        public IReadOnlyList<BackupInfo> BackupHistory =>
            _backupHistory ?? new List<BackupInfo>();

        public string DataFilePath
        {
            get => _dataFilePath ?? string.Empty;
            set => _dataFilePath = value?.Trim() ?? string.Empty;
        }

        public int AssetCount => _assets?.Count ?? 0;
        public int GroupCount => _groups?.Count ?? 0;
        public bool HasAssets => AssetCount > 0;

        public AssetLibrarySchema()
        {
            _version = LibraryVersion.Current;
            _createdDate = DateTime.Now;
            _lastUpdated = DateTime.Now;
            _name = "Asset Library";
            _description = string.Empty;
            _assets = new Dictionary<string, AssetSchema>();
            _groups = new Dictionary<string, AssetGroupSchema>();
            _statistics = new AssetStatistics();
            _backupHistory = new List<BackupInfo>();
            _dataFilePath = string.Empty;
        }

        public bool AddAsset(AssetSchema asset)
        {
            if (asset == null) return false;

            _assets ??= new Dictionary<string, AssetSchema>();
            _assets[asset.Id.Value] = asset;
            _lastUpdated = DateTime.Now;

            return true;
        }

        public bool RemoveAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return false;

            var removed = _assets?.Remove(assetId.Value) ?? false;
            if (removed)
            {
                _lastUpdated = DateTime.Now;

                // 関連するグループ情報も削除
                _groups?.Remove(assetId.Value);
            }

            return removed;
        }

        public AssetSchema GetAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return null;
            return _assets?.GetValueOrDefault(assetId.Value);
        }

        public bool ContainsAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return false;
            return _assets?.ContainsKey(assetId.Value) ?? false;
        }

        public void AddGroup(AssetId assetId, AssetGroupSchema group)
        {
            if (string.IsNullOrEmpty(assetId.Value) || group == null) return;

            _groups ??= new Dictionary<string, AssetGroupSchema>();
            _groups[assetId.Value] = group;
            _lastUpdated = DateTime.Now;
        }

        public void RemoveGroup(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return;

            if (_groups?.Remove(assetId.Value) == true)
            {
                _lastUpdated = DateTime.Now;
            }
        }

        public AssetGroupSchema GetGroup(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return null;
            return _groups?.GetValueOrDefault(assetId.Value);
        }

        public IEnumerable<AssetSchema> GetVisibleAssets()
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => !asset.State.IsArchived);
        }

        public IEnumerable<AssetSchema> GetFavoriteAssets()
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => asset.State.IsFavorite);
        }

        public IEnumerable<AssetSchema> GetAssetsByType(AssetType assetType)
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => asset.AssetType == assetType);
        }

        public IEnumerable<AssetSchema> GetAssetsByAuthor(string authorName)
        {
            if (_assets == null || string.IsNullOrWhiteSpace(authorName))
                return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset =>
                asset.Metadata.AuthorName.Equals(authorName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public void UpdateStatistics()
        {
            _statistics ??= new AssetStatistics();
            _statistics.Update(_assets?.Values ?? Enumerable.Empty<AssetSchema>());
            _lastUpdated = DateTime.Now;
        }

        public void AddBackup(BackupInfo backupInfo)
        {
            if (backupInfo == null) return;

            _backupHistory ??= new List<BackupInfo>();
            _backupHistory.Add(backupInfo);

            // 古いバックアップ情報を制限（最新50件まで保持）
            if (_backupHistory.Count > 50)
            {
                _backupHistory.RemoveAt(0);
            }

            _lastUpdated = DateTime.Now;
        }

        public void ClearAssets()
        {
            _assets?.Clear();
            _groups?.Clear();
            UpdateStatistics();
            _lastUpdated = DateTime.Now;
        }

        public void Optimize()
        {
            // 無効なアセット参照をクリーンアップ
            if (_groups != null)
            {
                var validAssetIds = new HashSet<string>(_assets?.Keys ?? Enumerable.Empty<string>());
                var invalidGroups = _groups.Where(kvp => !validAssetIds.Contains(kvp.Key)).ToList();

                foreach (var invalidGroup in invalidGroups)
                {
                    _groups.Remove(invalidGroup.Key);
                }
            }

            UpdateStatistics();
            _lastUpdated = DateTime.Now;
        }
    }
}
