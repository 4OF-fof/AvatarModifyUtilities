using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// アセットの基本情報スキーマ
    /// </summary>
    [Serializable]
    public struct AssetId : IEquatable<AssetId>
    {
        private readonly string _value;

        public AssetId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("AssetId cannot be null or empty", nameof(value));
            _value = value;
        }

        public static AssetId NewId() => new AssetId(Guid.NewGuid().ToString());

        public static bool TryParse(string value, out AssetId assetId)
        {
            try
            {
                assetId = new AssetId(value);
                return true;
            }
            catch
            {
                assetId = default;
                return false;
            }
        }

        public string Value => _value ?? string.Empty;

        public bool Equals(AssetId other) => _value == other._value;
        public override bool Equals(object obj) => obj is AssetId other && Equals(other);
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(AssetId left, AssetId right) => left.Equals(right);
        public static bool operator !=(AssetId left, AssetId right) => !left.Equals(right);

        public static implicit operator string(AssetId assetId) => assetId.Value;
        public static explicit operator AssetId(string value) => new AssetId(value);
    }

    /// <summary>
    /// ファイルサイズの表現
    /// </summary>
    [Serializable]
    public struct FileSize : IComparable<FileSize>
    {
        private readonly long _bytes;

        public FileSize(long bytes)
        {
            _bytes = Math.Max(0, bytes);
        }

        public long Bytes => _bytes;
        public double Kilobytes => _bytes / 1024.0;
        public double Megabytes => _bytes / (1024.0 * 1024.0);
        public double Gigabytes => _bytes / (1024.0 * 1024.0 * 1024.0);

        public string ToHumanReadable()
        {
            if (_bytes < 1024) return $"{_bytes} B";
            if (_bytes < 1024 * 1024) return $"{Kilobytes:F1} KB";
            if (_bytes < 1024 * 1024 * 1024) return $"{Megabytes:F1} MB";
            return $"{Gigabytes:F1} GB";
        }

        public int CompareTo(FileSize other) => _bytes.CompareTo(other._bytes);

        public static bool operator >(FileSize left, FileSize right) => left._bytes > right._bytes;
        public static bool operator <(FileSize left, FileSize right) => left._bytes < right._bytes;
        public static bool operator >=(FileSize left, FileSize right) => left._bytes >= right._bytes;
        public static bool operator <=(FileSize left, FileSize right) => left._bytes <= right._bytes;

        public static implicit operator long(FileSize fileSize) => fileSize._bytes;
        public static implicit operator FileSize(long bytes) => new FileSize(bytes);
    }

    /// <summary>
    /// アセットのメタデータ
    /// </summary>
    [Serializable]
    public class AssetMetadata
    {
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private string _authorName;
        [SerializeField] private string _assetType;
        [SerializeField] private List<string> _tags;
        [SerializeField] private List<string> _dependencies;
        [SerializeField] private DateTime _createdDate;
        [SerializeField] private DateTime _modifiedDate;

        public string Name
        {
            get => _name ?? string.Empty;
            private set => _name = value?.Trim() ?? string.Empty;
        }

        public string Description
        {
            get => _description ?? string.Empty;
            private set => _description = value?.Trim() ?? string.Empty;
        }
        public string AuthorName
        {
            get => _authorName ?? string.Empty;
            private set => _authorName = value?.Trim() ?? string.Empty;
        }

        public string AssetType
        {
            get => _assetType;
            private set => _assetType = value;
        }

        public IReadOnlyList<string> Tags => _tags ?? new List<string>();
        public IReadOnlyList<string> Dependencies => _dependencies ?? new List<string>(); public DateTime CreatedDate
        {
            get => _createdDate == default ? DateTime.Now : _createdDate;
            private set => _createdDate = value;
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate == default ? DateTime.Now : _modifiedDate;
            private set => _modifiedDate = value;
        }
        public AssetMetadata()
        {
            _name = string.Empty;
            _description = string.Empty;
            _authorName = string.Empty;
            _assetType = null;
            _tags = new List<string>();
            _dependencies = new List<string>();
            _createdDate = DateTime.Now;
            _modifiedDate = DateTime.Now;
        }

        /// <summary>
        /// AssetMetadataの新しいインスタンスを作成します（内部使用専用）
        /// </summary>
        internal AssetMetadata(string name, string description, string authorName, string assetType,
            List<string> tags = null, List<string> dependencies = null,
            DateTime? createdDate = null, DateTime? modifiedDate = null)
        {
            _name = name?.Trim() ?? string.Empty;
            _description = description?.Trim() ?? string.Empty;
            _authorName = authorName?.Trim() ?? string.Empty;
            _assetType = assetType;
            _tags = tags ?? new List<string>();
            _dependencies = dependencies ?? new List<string>();
            _createdDate = createdDate ?? DateTime.Now;
            _modifiedDate = modifiedDate ?? DateTime.Now;
        }

        /// <summary>
        /// タグの判定（内部使用専用）
        /// </summary>
        internal bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            return _tags?.Contains(tag.Trim()) ?? false;
        }

        /// <summary>
        /// 依存関係の判定（内部使用専用）
        /// </summary>
        internal bool HasDependency(string dependency)
        {
            if (string.IsNullOrWhiteSpace(dependency)) return false;
            return _dependencies?.Contains(dependency.Trim()) ?? false;
        }
    }

    /// <summary>
    /// ファイル関連の情報
    /// </summary>
    [Serializable]
    public class AssetFileInfo
    {
        [SerializeField] private string _filePath;
        [SerializeField] private string _thumbnailPath;
        [SerializeField] private long _fileSizeBytes;
        [SerializeField] private List<string> _importFiles;

        public string FilePath
        {
            get => _filePath ?? string.Empty;
            private set => _filePath = value?.Trim() ?? string.Empty;
        }

        public string ThumbnailPath
        {
            get => _thumbnailPath ?? string.Empty;
            private set => _thumbnailPath = value?.Trim() ?? string.Empty;
        }

        public long FileSizeBytes
        {
            get => _fileSizeBytes;
            private set => _fileSizeBytes = Math.Max(0, value);
        }

        public IReadOnlyList<string> ImportFiles => _importFiles ?? new List<string>(); public AssetFileInfo()
        {
            _filePath = string.Empty;
            _thumbnailPath = string.Empty;
            _fileSizeBytes = 0;
            _importFiles = new List<string>();
        }

        /// <summary>
        /// AssetFileInfoの新しいインスタンスを作成します（内部使用専用）
        /// </summary>
        internal AssetFileInfo(string filePath, string thumbnailPath = "", long fileSizeBytes = 0, List<string> importFiles = null)
        {
            _filePath = filePath?.Trim() ?? string.Empty;
            _thumbnailPath = thumbnailPath?.Trim() ?? string.Empty;
            _fileSizeBytes = Math.Max(0, fileSizeBytes);
            _importFiles = importFiles ?? new List<string>();
        }

        public bool FileExists => !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);
        public bool ThumbnailExists => !string.IsNullOrEmpty(_thumbnailPath) && File.Exists(_thumbnailPath);
    }

    /// <summary>
    /// アセットの状態フラグ
    /// </summary>
    [Serializable]
    public class AssetState
    {
        [SerializeField] private bool _isFavorite;
        [SerializeField] private bool _isGroup;
        [SerializeField] private bool _isArchived;

        public bool IsFavorite
        {
            get => _isFavorite;
            private set => _isFavorite = value;
        }

        public bool IsGroup
        {
            get => _isGroup;
            private set => _isGroup = value;
        }

        public bool IsArchived
        {
            get => _isArchived;
            private set => _isArchived = value;
        }

        public AssetState()
        {
            _isFavorite = false;
            _isGroup = false;
            _isArchived = false;
        }

        /// <summary>
        /// AssetStateの新しいインスタンスを作成します（内部使用専用）
        /// </summary>
        internal AssetState(bool isFavorite, bool isGroup, bool isArchived)
        {
            _isFavorite = isFavorite;
            _isGroup = isGroup;
            _isArchived = isArchived;
        }
    }

    /// <summary>
    /// VRCアセットの完全なスキーマ
    /// </summary>
    [Serializable]
    public class AssetSchema
    {
        [SerializeField] private string _parentGroupId;
        [SerializeField] private AssetMetadata _metadata;
        [SerializeField] private AssetFileInfo _fileInfo;
        [SerializeField] private AssetState _state;
        [SerializeField] private BoothItemSchema _boothItem;
        [SerializeField] private DateTime _lastAccessed;
        [SerializeField] private List<string> _childAssetIds;

        public string ParentGroupId
        {
            get => _parentGroupId ?? string.Empty;
            private set => _parentGroupId = value?.Trim() ?? string.Empty;
        }

        public AssetMetadata Metadata
        {
            get => _metadata ?? (_metadata = new AssetMetadata());
            private set => _metadata = value ?? new AssetMetadata();
        }

        public AssetFileInfo FileInfo
        {
            get => _fileInfo ?? (_fileInfo = new AssetFileInfo());
            private set => _fileInfo = value ?? new AssetFileInfo();
        }

        public AssetState State
        {
            get => _state ?? (_state = new AssetState());
            private set => _state = value ?? new AssetState();
        }

        public BoothItemSchema BoothItem
        {
            get => _boothItem;
            private set => _boothItem = value;
        }

        public DateTime LastAccessed
        {
            get => _lastAccessed == default ? DateTime.Now : _lastAccessed;
            private set => _lastAccessed = value;
        }

        public IReadOnlyList<string> ChildAssetIds => _childAssetIds ?? new List<string>();

        public AssetSchema()
        {
            _parentGroupId = string.Empty;
            _metadata = new AssetMetadata();
            _fileInfo = new AssetFileInfo();
            _state = new AssetState();
            _boothItem = null;
            _lastAccessed = DateTime.Now;
            _childAssetIds = null;
        }

        /// <summary>
        /// AssetSchemaの新しいインスタンスを作成します
        /// </summary>
        internal AssetSchema(string name, string assetType, string filePath) : this()
        {
            _metadata = new AssetMetadata(name, "", "", assetType);
            _fileInfo = new AssetFileInfo(filePath);
        }

        /// <summary>
        /// AssetSchemaの新しいインスタンスを作成します（内部使用専用）
        /// </summary>
        internal AssetSchema(string parentGroupId, AssetMetadata metadata, AssetFileInfo fileInfo,
            AssetState state, BoothItemSchema boothItem = null, DateTime? lastAccessed = null,
            List<string> childAssetIds = null)
        {
            _parentGroupId = parentGroupId?.Trim() ?? string.Empty;
            _metadata = metadata ?? new AssetMetadata();
            _fileInfo = fileInfo ?? new AssetFileInfo();
            _state = state ?? new AssetState();
            _boothItem = boothItem;
            _lastAccessed = lastAccessed ?? DateTime.Now;
            _childAssetIds = childAssetIds;
        }

        /// <summary>
        /// 最終アクセス日時を更新します（内部使用専用）
        /// </summary>
        internal void UpdateLastAccessed()
        {
            _lastAccessed = DateTime.Now;
        }

        public AssetSchema Clone()
        {
            var clonedMetadata = new AssetMetadata(
                _metadata.Name,
                _metadata.Description,
                _metadata.AuthorName,
                _metadata.AssetType,
                new List<string>(_metadata.Tags),
                new List<string>(_metadata.Dependencies),
                _metadata.CreatedDate,
                _metadata.ModifiedDate
            );

            var clonedFileInfo = new AssetFileInfo(
                _fileInfo.FilePath,
                _fileInfo.ThumbnailPath,
                _fileInfo.FileSizeBytes,
                new List<string>(_fileInfo.ImportFiles)
            );

            var clonedState = new AssetState(
                _state.IsFavorite,
                _state.IsGroup,
                _state.IsArchived
            );

            return new AssetSchema(
                _parentGroupId,
                clonedMetadata,
                clonedFileInfo,
                clonedState,
                _boothItem?.Clone(),
                _lastAccessed,
                _childAssetIds != null ? new List<string>(_childAssetIds) : null
            );
        }

        /// <summary>
        /// タグの判定（内部使用専用）
        /// </summary>
        internal bool HasTag(string tag) => _metadata.HasTag(tag);

        /// <summary>
        /// 依存関係の判定（内部使用専用）
        /// </summary>
        internal bool HasDependency(string dependency) => _metadata.HasDependency(dependency);

        /// <summary>
        /// 子アセットの判定（内部使用専用）
        /// </summary>
        internal bool HasChildAsset(string childAssetId)
        {
            if (string.IsNullOrWhiteSpace(childAssetId)) return false;
            return _childAssetIds?.Contains(childAssetId.Trim()) ?? false;
        }
    }
}
