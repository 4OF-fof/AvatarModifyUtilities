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
    /// アセットタイプの定義
    /// </summary>
    [Serializable]
    public struct AssetType : IEquatable<AssetType>
    {
        private readonly string _value;

        public AssetType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("AssetType cannot be null or empty", nameof(value));
            _value = value.Trim();
        }

        public string Value => _value ?? "Other";

        public bool Equals(AssetType other) => _value == other._value;
        public override bool Equals(object obj) => obj is AssetType other && Equals(other);
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
        public override string ToString() => _value ?? "Other";

        public static bool operator ==(AssetType left, AssetType right) => left.Equals(right);
        public static bool operator !=(AssetType left, AssetType right) => !left.Equals(right);

        public static implicit operator string(AssetType assetType) => assetType.Value;
        public static explicit operator AssetType(string value) => new AssetType(value);

        // 標準的なアセットタイプ
        public static readonly AssetType Avatar = new AssetType("Avatar");
        public static readonly AssetType Clothing = new AssetType("Clothing");
        public static readonly AssetType Accessory = new AssetType("Accessory");
        public static readonly AssetType Other = new AssetType("Other");
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
        [SerializeField] private AssetType _assetType;
        [SerializeField] private List<string> _tags;
        [SerializeField] private List<string> _dependencies;
        [SerializeField] private DateTime _createdDate;
        [SerializeField] private DateTime _modifiedDate;

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value?.Trim() ?? string.Empty;
        }

        public string Description
        {
            get => _description ?? string.Empty;
            set => _description = value?.Trim() ?? string.Empty;
        }
        public string AuthorName
        {
            get => _authorName ?? string.Empty;
            set => _authorName = value?.Trim() ?? string.Empty;
        }

        public AssetType AssetType
        {
            get => _assetType;
            set => _assetType = value;
        }

        public IReadOnlyList<string> Tags => _tags ?? new List<string>();
        public IReadOnlyList<string> Dependencies => _dependencies ?? new List<string>();

        public DateTime CreatedDate
        {
            get => _createdDate == default ? DateTime.Now : _createdDate;
            set => _createdDate = value;
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate == default ? DateTime.Now : _modifiedDate;
            set => _modifiedDate = value;
        }

        public AssetMetadata()
        {
            _name = string.Empty;
            _description = string.Empty;
            _authorName = string.Empty;
            _assetType = AssetType.Other;
            _tags = new List<string>();
            _dependencies = new List<string>();
            _createdDate = DateTime.Now;
            _modifiedDate = DateTime.Now;
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            _tags ??= new List<string>();
            var trimmedTag = tag.Trim();
            if (!_tags.Contains(trimmedTag))
            {
                _tags.Add(trimmedTag);
                _modifiedDate = DateTime.Now;
            }
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            _tags ??= new List<string>();
            if (_tags.Remove(tag.Trim()))
            {
                _modifiedDate = DateTime.Now;
            }
        }

        public void ClearTags()
        {
            _tags?.Clear();
            _modifiedDate = DateTime.Now;
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            return _tags?.Contains(tag.Trim()) ?? false;
        }

        public void AddDependency(string dependency)
        {
            if (string.IsNullOrWhiteSpace(dependency)) return;

            _dependencies ??= new List<string>();
            var trimmedDep = dependency.Trim();
            if (!_dependencies.Contains(trimmedDep))
            {
                _dependencies.Add(trimmedDep);
                _modifiedDate = DateTime.Now;
            }
        }

        public void RemoveDependency(string dependency)
        {
            if (string.IsNullOrWhiteSpace(dependency)) return;

            _dependencies ??= new List<string>();
            if (_dependencies.Remove(dependency.Trim()))
            {
                _modifiedDate = DateTime.Now;
            }
        }

        public void ClearDependencies()
        {
            _dependencies?.Clear();
            _modifiedDate = DateTime.Now;
        }

        public bool HasDependency(string dependency)
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
        [SerializeField] private FileSize _fileSize;
        [SerializeField] private List<string> _importFiles;

        public string FilePath
        {
            get => _filePath ?? string.Empty;
            set => _filePath = value?.Trim() ?? string.Empty;
        }

        public string ThumbnailPath
        {
            get => _thumbnailPath ?? string.Empty;
            set => _thumbnailPath = value?.Trim() ?? string.Empty;
        }

        public FileSize FileSize
        {
            get => _fileSize;
            set => _fileSize = value;
        }

        public IReadOnlyList<string> ImportFiles => _importFiles ?? new List<string>();

        public AssetFileInfo()
        {
            _filePath = string.Empty;
            _thumbnailPath = string.Empty;
            _fileSize = new FileSize(0);
            _importFiles = new List<string>();
        }

        public bool FileExists => !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);
        public bool ThumbnailExists => !string.IsNullOrEmpty(_thumbnailPath) && File.Exists(_thumbnailPath);

        public void AddImportFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            _importFiles ??= new List<string>();
            var trimmedPath = filePath.Trim();
            if (!_importFiles.Contains(trimmedPath))
            {
                _importFiles.Add(trimmedPath);
            }
        }

        public void RemoveImportFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            _importFiles?.Remove(filePath.Trim());
        }
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
            set => _isFavorite = value;
        }

        public bool IsGroup
        {
            get => _isGroup;
            set => _isGroup = value;
        }

        public bool IsArchived
        {
            get => _isArchived;
            set => _isArchived = value;
        }

        public AssetState()
        {
            _isFavorite = false;
            _isGroup = false;
            _isArchived = false;
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
        public string ParentGroupId
        {
            get => _parentGroupId ?? string.Empty;
            set => _parentGroupId = value?.Trim() ?? string.Empty;
        }

        public AssetMetadata Metadata
        {
            get => _metadata ?? (_metadata = new AssetMetadata());
            set => _metadata = value ?? new AssetMetadata();
        }

        public AssetFileInfo FileInfo
        {
            get => _fileInfo ?? (_fileInfo = new AssetFileInfo());
            set => _fileInfo = value ?? new AssetFileInfo();
        }

        public AssetState State
        {
            get => _state ?? (_state = new AssetState());
            set => _state = value ?? new AssetState();
        }

        public BoothItemSchema BoothItem
        {
            get => _boothItem;
            set => _boothItem = value;
        }
        public DateTime LastAccessed
        {
            get => _lastAccessed == default ? DateTime.Now : _lastAccessed;
            set => _lastAccessed = value;
        }
        public bool HasParentGroup => !string.IsNullOrEmpty(_parentGroupId);

        public AssetSchema()
        {
            _parentGroupId = string.Empty;
            _metadata = new AssetMetadata();
            _fileInfo = new AssetFileInfo();
            _state = new AssetState();
            _boothItem = null;
            _lastAccessed = DateTime.Now;
        }

        public AssetSchema(string name, AssetType assetType, string filePath) : this()
        {
            _metadata.Name = name;
            _metadata.AssetType = assetType;
            _fileInfo.FilePath = filePath;
        }

        public void UpdateLastAccessed()
        {
            _lastAccessed = DateTime.Now;
        }

        public AssetSchema Clone()
        {
            return new AssetSchema
            {
                _parentGroupId = _parentGroupId,
                _metadata = new AssetMetadata
                {
                    Name = _metadata.Name,
                    Description = _metadata.Description,
                    AuthorName = _metadata.AuthorName,
                    AssetType = _metadata.AssetType,
                    CreatedDate = _metadata.CreatedDate,
                    ModifiedDate = _metadata.ModifiedDate
                },
                _fileInfo = new AssetFileInfo
                {
                    FilePath = _fileInfo.FilePath,
                    ThumbnailPath = _fileInfo.ThumbnailPath,
                    FileSize = _fileInfo.FileSize
                },
                _state = new AssetState
                {
                    IsFavorite = _state.IsFavorite,
                    IsGroup = _state.IsGroup,
                    IsArchived = _state.IsArchived
                },
                _boothItem = _boothItem?.Clone(),
                _lastAccessed = _lastAccessed
            };
        }

        // タグとDependencyの管理はAssetMetadataに、インポートファイルはAssetFileInfoに委譲
        public void AddTag(string tag) => _metadata.AddTag(tag);
        public void RemoveTag(string tag) => _metadata.RemoveTag(tag);
        public bool HasTag(string tag) => _metadata.HasTag(tag);

        public void AddDependency(string dependency) => _metadata.AddDependency(dependency);
        public void RemoveDependency(string dependency) => _metadata.RemoveDependency(dependency);
        public bool HasDependency(string dependency) => _metadata.HasDependency(dependency);

        public void AddImportFile(string filePath) => _fileInfo.AddImportFile(filePath);
        public void RemoveImportFile(string filePath) => _fileInfo.RemoveImportFile(filePath);        // 親グループの管理
        public void SetParentGroup(string parentGroupId)
        {
            _parentGroupId = parentGroupId?.Trim() ?? string.Empty;
        }

        public void RemoveFromParentGroup()
        {
            _parentGroupId = string.Empty;
        }
    }
}
