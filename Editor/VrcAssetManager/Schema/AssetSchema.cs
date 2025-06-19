using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    #region AssetSchema
    [Serializable]
    public class AssetSchema
    {
        [SerializeField] private AssetId _assetId;
        [SerializeField] private AssetMetadata _metadata;
        [SerializeField] private AssetFileInfo _fileInfo;
        [SerializeField] private AssetState _state;
        [SerializeField] private BoothItemSchema _boothItem;
        [SerializeField] private string _parentGroupId;
        [SerializeField] private List<string> _childAssetIds;
        [SerializeField] private DateTime _lastAccessed;

        public AssetSchema()
        {
            _assetId = AssetId.NewId();
            _metadata = new AssetMetadata();
            _fileInfo = new AssetFileInfo();
            _state = new AssetState();
            _boothItem = null;
            _parentGroupId = string.Empty;
            _childAssetIds = new List<string>();
            _lastAccessed = DateTime.Now;
        }

        #region Properties
        public AssetId AssetId => _assetId;

        public AssetMetadata Metadata => _metadata ?? (_metadata = new AssetMetadata());

        public AssetFileInfo FileInfo => _fileInfo ?? (_fileInfo = new AssetFileInfo());

        public AssetState State => _state ?? (_state = new AssetState());

        public BoothItemSchema BoothItem
        {
            get => _boothItem;
            private set => _boothItem = value;
        }

        public string ParentGroupId
        {
            get => _parentGroupId ?? string.Empty;
            private set => _parentGroupId = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<string> ChildAssetIds
        {
            get => _childAssetIds ?? new List<string>();
            private set => _childAssetIds = value != null ? new List<string>(value) : new List<string>();
        }

        public DateTime LastAccessed
        {
            get => _lastAccessed == default ? DateTime.Now : _lastAccessed;
            private set => _lastAccessed = value;
        }

        public void UpdateLastAccessed()
        {
            _lastAccessed = DateTime.Now;
        }

        public bool HasParentGroup => !string.IsNullOrWhiteSpace(_parentGroupId);

        public bool HasChildAssets => _childAssetIds != null && _childAssetIds.Count > 0;
        #endregion
    }
    #endregion

    #region AssetId
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

        public string Value => _value ?? string.Empty;

        #region Methods
        public static AssetId NewId() => new AssetId(Guid.NewGuid().ToString());

        public bool Equals(AssetId other) => _value == other._value;
        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(AssetId left, AssetId right) => left.Equals(right);
        public static bool operator !=(AssetId left, AssetId right) => !left.Equals(right);

        public static implicit operator string(AssetId assetId) => assetId.Value;
        public static explicit operator AssetId(string value) => new AssetId(value);
        #endregion
    }
    #endregion

    #region AssetMetadata
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

        public AssetMetadata()
        {
            _name = string.Empty;
            _description = string.Empty;
            _authorName = string.Empty;
            _assetType = string.Empty;
            _tags = new List<string>();
            _dependencies = new List<string>();
            _createdDate = DateTime.Now;
            _modifiedDate = DateTime.Now;
        }

        #region Properties
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

        public IReadOnlyList<string> Dependencies => _dependencies ?? new List<string>();
        
        public DateTime CreatedDate
        {
            get => _createdDate == default ? DateTime.Now : _createdDate;
            private set => _createdDate = value;
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate == default ? DateTime.Now : _modifiedDate;
            private set => _modifiedDate = value;
        }
        #endregion
    }
    #endregion

    #region AssetFileInfo
    [Serializable]
    public class AssetFileInfo
    {
        [SerializeField] private string _filePath;
        [SerializeField] private string _thumbnailPath;
        [SerializeField] private List<string> _importFiles;

        public AssetFileInfo()
        {
            _filePath = string.Empty;
            _thumbnailPath = string.Empty;
            _importFiles = new List<string>();
        }

        #region Properties
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

        public IReadOnlyList<string> ImportFiles => _importFiles ?? new List<string>();
        #endregion
    }
    #endregion

    #region AssetState
    [Serializable]
    public class AssetState
    {
        [SerializeField] private bool _isFavorite;
        [SerializeField] private bool _isGroup;
        [SerializeField] private bool _isArchived;

        public AssetState()
        {
            _isFavorite = false;
            _isGroup = false;
            _isArchived = false;
        }

        #region Properties
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
        #endregion
    }
    #endregion

    #region BoothItemSchema
    [Serializable]
    public class BoothItemSchema
    {
        [SerializeField] private string _itemName;
        [SerializeField] private string _authorName;
        [SerializeField] private string _itemUrl;
        [SerializeField] private string _imageUrl;
        [SerializeField] private string _fileName;
        [SerializeField] private string _downloadUrl;

        public BoothItemSchema()
        {
            _itemName = string.Empty;
            _authorName = string.Empty;
            _itemUrl = string.Empty;
            _imageUrl = string.Empty;
            _fileName = string.Empty;
            _downloadUrl = string.Empty;
        }

        #region Properties
        public string ItemName
        {
            get => _itemName ?? string.Empty;
            private set => _itemName = value?.Trim() ?? string.Empty;
        }

        public string AuthorName
        {
            get => _authorName ?? string.Empty;
            private set => _authorName = value?.Trim() ?? string.Empty;
        }

        public string ItemUrl
        {
            get => _itemUrl ?? string.Empty;
            private set => _itemUrl = value?.Trim() ?? string.Empty;
        }

        public string ImageUrl
        {
            get => _imageUrl ?? string.Empty;
            private set => _imageUrl = value?.Trim() ?? string.Empty;
        }

        public string FileName
        {
            get => _fileName ?? string.Empty;
            private set => _fileName = value?.Trim() ?? string.Empty;
        }

        public string DownloadUrl
        {
            get => _downloadUrl ?? string.Empty;
            private set => _downloadUrl = value?.Trim() ?? string.Empty;
        }
        #endregion
    }
    #endregion
}
