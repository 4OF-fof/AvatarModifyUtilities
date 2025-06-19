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
        [SerializeField] private Guid assetId;
        [SerializeField] private AssetMetadata metadata;
        [SerializeField] private AssetFileInfo fileInfo;
        [SerializeField] private AssetState state;
        [SerializeField] private BoothItemSchema boothItem;
        [SerializeField] private string parentGroupId;
        [SerializeField] private List<string> childAssetIds;
        [SerializeField] private DateTime lastAccessed;

        public AssetSchema()
        {
            assetId = Guid.NewGuid();
            metadata = new AssetMetadata();
            fileInfo = new AssetFileInfo();
            state = new AssetState();
            boothItem = null;
            parentGroupId = string.Empty;
            childAssetIds = new List<string>();
            lastAccessed = DateTime.Now;
        }
        
        #region Properties
        public Guid AssetId => assetId;

        public AssetMetadata Metadata => metadata ?? (metadata = new AssetMetadata());

        public AssetFileInfo FileInfo => fileInfo ?? (fileInfo = new AssetFileInfo());

        public AssetState State => state ?? (state = new AssetState());

        public BoothItemSchema BoothItem
        {
            get => boothItem;
            private set => boothItem = value;
        }

        public string ParentGroupId
        {
            get => parentGroupId ?? string.Empty;
            private set => parentGroupId = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<string> ChildAssetIds
        {
            get => childAssetIds ?? new List<string>();
            private set => childAssetIds = value != null ? new List<string>(value) : new List<string>();
        }

        public DateTime LastAccessed
        {
            get => lastAccessed == default ? DateTime.Now : lastAccessed;
            private set => lastAccessed = value;
        }

        public void UpdateLastAccessed()
        {
            lastAccessed = DateTime.Now;
        }

        public bool HasParentGroup => !string.IsNullOrWhiteSpace(parentGroupId);

        public bool HasChildAssets => childAssetIds != null && childAssetIds.Count > 0;
        #endregion
        }
    #endregion

    #region AssetMetadata
    [Serializable]
    public class AssetMetadata
    {
        [SerializeField] private string _name;
        [SerializeField] private string _description;
        [SerializeField] private string authorName;
        [SerializeField] private string _thumbnailPath;
        [SerializeField] private string _assetType;
        [SerializeField] private List<string> _tags;
        [SerializeField] private List<string> _dependencies;
        [SerializeField] private DateTime _createdDate;
        [SerializeField] private DateTime _modifiedDate;

        public AssetMetadata()
        {
            _name = string.Empty;
            _description = string.Empty;
            authorName = string.Empty;
            _thumbnailPath = string.Empty;
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
            get => authorName ?? string.Empty;
            private set => authorName = value?.Trim() ?? string.Empty;
        }

        public string ThumbnailPath
        {
            get => _thumbnailPath ?? string.Empty;
            private set => _thumbnailPath = value?.Trim() ?? string.Empty;
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
        [SerializeField] private string filePath;
        [SerializeField] private List<string> importFiles;

        public AssetFileInfo()
        {
            filePath = string.Empty;
            importFiles = new List<string>();
        }

        #region Properties
        public string FilePath
        {
            get => filePath ?? string.Empty;
            private set => filePath = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<string> ImportFiles => importFiles ?? new List<string>();
        #endregion
    }
    #endregion

    #region AssetState
    [Serializable]
    public class AssetState
    {
        [SerializeField] private bool isFavorite;
        [SerializeField] private bool isGroup;
        [SerializeField] private bool isArchived;

        public AssetState()
        {
            isFavorite = false;
            isGroup = false;
            isArchived = false;
        }

        #region Properties
        public bool IsFavorite
        {
            get => isFavorite;
            private set => isFavorite = value;
        }

        public bool IsGroup
        {
            get => isGroup;
            private set => isGroup = value;
        }

        public bool IsArchived
        {
            get => isArchived;
            private set => isArchived = value;
        }
        #endregion
    }
    #endregion

    #region BoothItemSchema
    [Serializable]
    public class BoothItemSchema
    {
        [SerializeField] private string itemName;
        [SerializeField] private string authorName;
        [SerializeField] private string itemUrl;
        [SerializeField] private string imageUrl;
        [SerializeField] private string fileName;
        [SerializeField] private string downloadUrl;

        public BoothItemSchema()
        {
            itemName = string.Empty;
            authorName = string.Empty;
            itemUrl = string.Empty;
            imageUrl = string.Empty;
            fileName = string.Empty;
            downloadUrl = string.Empty;
        }

        #region Properties
        public string ItemName
        {
            get => itemName ?? string.Empty;
            private set => itemName = value?.Trim() ?? string.Empty;
        }

        public string AuthorName
        {
            get => authorName ?? string.Empty;
            private set => authorName = value?.Trim() ?? string.Empty;
        }

        public string ItemUrl
        {
            get => itemUrl ?? string.Empty;
            private set => itemUrl = value?.Trim() ?? string.Empty;
        }

        public string ImageUrl
        {
            get => imageUrl ?? string.Empty;
            private set => imageUrl = value?.Trim() ?? string.Empty;
        }

        public string FileName
        {
            get => fileName ?? string.Empty;
            private set => fileName = value?.Trim() ?? string.Empty;
        }

        public string DownloadUrl
        {
            get => downloadUrl ?? string.Empty;
            private set => downloadUrl = value?.Trim() ?? string.Empty;
        }
        #endregion
    }
    #endregion
}
