using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    #region AssetSchema
    public class AssetSchema
    {
        private Guid _assetId;
        private AssetMetadata _metadata;
        private AssetFileInfo _fileInfo;
        private AssetState _state;
        private BoothItemSchema _boothItem;
        private string _parentGroupId;
        private List<string> _childAssetIds;
        private DateTime _lastAccessed;

        public AssetSchema()
        {
            _assetId = Guid.NewGuid();
            _metadata = new AssetMetadata();
            _fileInfo = new AssetFileInfo();
            _state = new AssetState();
            _boothItem = null;
            _parentGroupId = string.Empty;
            _childAssetIds = new List<string>();
            _lastAccessed = DateTime.Now;
        }

        [JsonConstructor]
        public AssetSchema(Guid assetId, AssetMetadata metadata, AssetFileInfo fileInfo,
                           AssetState state, BoothItemSchema boothItem, string parentGroupId,
                           List<string> childAssetIds, DateTime lastAccessed)
        {
            _assetId = assetId;
            _metadata = metadata ?? new AssetMetadata();
            _fileInfo = fileInfo ?? new AssetFileInfo();
            _state = state ?? new AssetState();
            _boothItem = boothItem;
            _parentGroupId = parentGroupId ?? string.Empty;
            _childAssetIds = childAssetIds ?? new List<string>();
            _lastAccessed = lastAccessed;
        }

        #region Properties
        public Guid assetId => _assetId;

        public AssetMetadata metadata => _metadata ?? (_metadata = new AssetMetadata());

        public AssetFileInfo fileInfo => _fileInfo ?? (_fileInfo = new AssetFileInfo());

        public AssetState state => _state ?? (_state = new AssetState());

        public BoothItemSchema boothItem
        {
            get => _boothItem;
            private set => _boothItem = value;
        }

        public string parentGroupId
        {
            get => _parentGroupId ?? string.Empty;
            private set => _parentGroupId = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<string> childAssetIds
        {
            get => _childAssetIds ?? new List<string>();
            private set => _childAssetIds = value != null ? new List<string>(value) : new List<string>();
        }

        public DateTime lastAccessed
        {
            get => _lastAccessed == default ? DateTime.Now : _lastAccessed;
            private set => _lastAccessed = value;
        }

        public bool hasParentGroup => !string.IsNullOrWhiteSpace(_parentGroupId);

        public bool hasChildAssets => _childAssetIds != null && _childAssetIds.Count > 0;
        #endregion
        #region Methods
        public void SetMetadata(AssetMetadata newMetadata)
        {
            _metadata = newMetadata ?? new AssetMetadata();
            _lastAccessed = DateTime.Now;
        }

        public void SetFileInfo(AssetFileInfo newFileInfo)
        {
            _fileInfo = newFileInfo ?? new AssetFileInfo();
            _lastAccessed = DateTime.Now;
        }

        public void SetState(AssetState newState)
        {
            _state = newState ?? new AssetState();
            _lastAccessed = DateTime.Now;
        }

        public void SetBoothItem(BoothItemSchema newBoothItem)
        {
            _boothItem = newBoothItem;
            _lastAccessed = DateTime.Now;
        }

        public void SetParentGroupId(string newParentGroupId)
        {
            _parentGroupId = newParentGroupId?.Trim() ?? string.Empty;
            _lastAccessed = DateTime.Now;
        }

        public void AddChildAssetId(string childAssetId)
        {
            if (string.IsNullOrWhiteSpace(childAssetId))
            {
                Debug.LogError("Cannot add an empty or whitespace child asset ID.");
            }
            else if (_childAssetIds.Contains(childAssetId))
            {
                Debug.LogWarning($"Child asset ID '{childAssetId}' already exists in the list.");
            }
            else
            {
                _childAssetIds.Add(childAssetId.Trim());
                _lastAccessed = DateTime.Now;
            }
        }

        public void RemoveChildAssetId(string childAssetId)
        {
            if (_childAssetIds.Contains(childAssetId))
            {
                _childAssetIds.Remove(childAssetId);
                _lastAccessed = DateTime.Now;
            }
            else
            {
                Debug.LogWarning($"Child asset ID '{childAssetId}' does not exist in the list.");
            }
        }

        public void ClearChildAssetIds()
        {
            _childAssetIds.Clear();
            _lastAccessed = DateTime.Now;
        }

        public void ClearBoothItem()
        {
            _boothItem = null;
            _lastAccessed = DateTime.Now;
        }
        #endregion
    }
    #endregion

    #region AssetMetadata
    public class AssetMetadata
    {
        private string _name;
        private string _description;
        private string _authorName;
        private string _thumbnailPath;
        private string _assetType;
        private List<string> _tags;
        private List<string> _dependencies;
        private DateTime _createdDate;
        private DateTime _modifiedDate;

        public AssetMetadata()
        {
            _name = string.Empty;
            _description = string.Empty;
            _authorName = string.Empty;
            _thumbnailPath = string.Empty;
            _assetType = string.Empty;
            _tags = new List<string>();
            _dependencies = new List<string>();
            _createdDate = DateTime.Now;
            _modifiedDate = DateTime.Now;
        }

        [JsonConstructor]
        public AssetMetadata(string name, string description, string authorName, string thumbnailPath,
                             string assetType, List<string> tags, List<string> dependencies,
                             DateTime createdDate, DateTime modifiedDate)
        {
            _name = name;
            _description = description;
            _authorName = authorName;
            _thumbnailPath = thumbnailPath;
            _assetType = assetType;
            _tags = tags ?? new List<string>();
            _dependencies = dependencies ?? new List<string>();
            _createdDate = createdDate;
            _modifiedDate = modifiedDate;
        }

        #region Properties
        public string name
        {
            get => _name ?? string.Empty;
            private set => _name = value?.Trim() ?? string.Empty;
        }

        public string description
        {
            get => _description ?? string.Empty;
            private set => _description = value?.Trim() ?? string.Empty;
        }

        public string authorName
        {
            get => _authorName ?? string.Empty;
            private set => _authorName = value?.Trim() ?? string.Empty;
        }

        public string thumbnailPath
        {
            get => _thumbnailPath ?? string.Empty;
            private set => _thumbnailPath = value?.Trim() ?? string.Empty;
        }

        public string assetType
        {
            get => _assetType;
            private set => _assetType = value;
        }

        public IReadOnlyList<string> tags => _tags ?? new List<string>();

        public IReadOnlyList<string> dependencies => _dependencies ?? new List<string>();

        public DateTime createdDate
        {
            get => _createdDate == default ? DateTime.Now : _createdDate;
            private set => _createdDate = value;
        }

        public DateTime modifiedDate
        {
            get => _modifiedDate == default ? DateTime.Now : _modifiedDate;
            private set => _modifiedDate = value;
        }
        #endregion

        #region Methods
        public void SetName(string newName)
        {
            _name = newName?.Trim() ?? string.Empty;
            _modifiedDate = DateTime.Now;
        }

        public void SetDescription(string newDescription)
        {
            _description = newDescription?.Trim() ?? string.Empty;
            _modifiedDate = DateTime.Now;
        }

        public void SetAuthorName(string newAuthorName)
        {
            _authorName = newAuthorName?.Trim() ?? string.Empty;
            _modifiedDate = DateTime.Now;
        }

        public void SetThumbnailPath(string newThumbnailPath)
        {
            _thumbnailPath = newThumbnailPath?.Trim() ?? string.Empty;
            _modifiedDate = DateTime.Now;
        }

        public void SetAssetType(string newAssetType)
        {
            _assetType = newAssetType?.Trim() ?? string.Empty;
            _modifiedDate = DateTime.Now;
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogError("Cannot add an empty or whitespace tag.");
            }
            else if (_tags.Contains(tag))
            {
                Debug.LogWarning($"Tag '{tag}' already exists in the list.");
            }
            else
            {
                _tags.Add(tag.Trim());
                _modifiedDate = DateTime.Now;
            }
        }

        public void RemoveTag(string tag)
        {
            if (_tags.Contains(tag))
            {
                _tags.Remove(tag);
                _modifiedDate = DateTime.Now;
            }
            else
            {
                Debug.LogWarning($"Tag '{tag}' does not exist in the list.");
            }
        }

        public void AddDependency(string dependency)
        {
            if (string.IsNullOrWhiteSpace(dependency))
            {
                Debug.LogError("Cannot add an empty or whitespace dependency.");
            }
            else if (_dependencies.Contains(dependency))
            {
                Debug.LogWarning($"Dependency '{dependency}' already exists in the list.");
            }
            else
            {
                _dependencies.Add(dependency.Trim());
                _modifiedDate = DateTime.Now;
            }
        }

        public void RemoveDependency(string dependency)
        {
            if (_dependencies.Contains(dependency))
            {
                _dependencies.Remove(dependency);
                _modifiedDate = DateTime.Now;
            }
            else
            {
                Debug.LogWarning($"Dependency '{dependency}' does not exist in the list.");
            }
        }
        #endregion
    }
    #endregion

    #region AssetFileInfo
    public class AssetFileInfo
    {
        private string _filePath;
        private List<string> _importFiles;

        public AssetFileInfo()
        {
            _filePath = string.Empty;
            _importFiles = new List<string>();
        }

        [JsonConstructor]
        public AssetFileInfo(string filePath, List<string> importFiles)
        {
            _filePath = filePath;
            _importFiles = importFiles ?? new List<string>();
        }

        #region Properties
        public string filePath
        {
            get => _filePath ?? string.Empty;
            private set => _filePath = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<string> importFiles => _importFiles ?? new List<string>();
        #endregion
    }
    #endregion

    #region AssetState
    public class AssetState
    {
        private bool _isFavorite;
        private bool _isArchived;

        public AssetState()
        {
            _isFavorite = false;
            _isArchived = false;
        }

        [JsonConstructor]
        public AssetState(bool isFavorite, bool isGroup, bool isArchived)
        {
            _isFavorite = isFavorite;
            _isArchived = isArchived;
        }

        #region Properties
        public bool isFavorite
        {
            get => _isFavorite;
            private set => _isFavorite = value;
        }

        public bool isArchived
        {
            get => _isArchived;
            private set => _isArchived = value;
        }
        #endregion

        #region Methods
        public void SetFavorite(bool isFavorite)
        {
            _isFavorite = isFavorite;
        }

        public void SetArchived(bool isArchived)
        {
            _isArchived = isArchived;
        }
        #endregion
    }
    #endregion

    #region BoothItemSchema
    public class BoothItemSchema
    {
        private string _itemName;
        private string _authorName;
        private string _description;
        private string _itemUrl;
        private string _imageUrl;
        private string _fileName;
        private string _downloadUrl;

        public BoothItemSchema()
        {
            _itemName = string.Empty;
            _authorName = string.Empty;
            _description = string.Empty;
            _itemUrl = string.Empty;
            _imageUrl = string.Empty;
            _fileName = string.Empty;
            _downloadUrl = string.Empty;
        }

        [JsonConstructor]
        public BoothItemSchema(string itemName, string authorName, string description, string itemUrl,
                               string imageUrl, string fileName, string downloadUrl)
        {
            _itemName = itemName;
            _authorName = authorName;
            _description = description;
            _itemUrl = itemUrl;
            _imageUrl = imageUrl;
            _fileName = fileName;
            _downloadUrl = downloadUrl;
        }

        #region Properties
        public string itemName
        {
            get => _itemName ?? string.Empty;
            private set => _itemName = value?.Trim() ?? string.Empty;
        }

        public string authorName
        {
            get => _authorName ?? string.Empty;
            private set => _authorName = value?.Trim() ?? string.Empty;
        }

        public string description
        {
            get => _description ?? string.Empty;
            private set => _description = value?.Trim() ?? string.Empty;
        }

        public string itemUrl
        {
            get => _itemUrl ?? string.Empty;
            private set => _itemUrl = value?.Trim() ?? string.Empty;
        }

        public string imageUrl
        {
            get => _imageUrl ?? string.Empty;
            private set => _imageUrl = value?.Trim() ?? string.Empty;
        }

        public string fileName
        {
            get => _fileName ?? string.Empty;
            private set => _fileName = value?.Trim() ?? string.Empty;
        }

        public string downloadUrl
        {
            get => _downloadUrl ?? string.Empty;
            private set => _downloadUrl = value?.Trim() ?? string.Empty;
        }
        #endregion
    }
    #endregion
}
