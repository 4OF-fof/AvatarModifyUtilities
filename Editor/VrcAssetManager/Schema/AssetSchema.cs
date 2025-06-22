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
        private Guid assetId;
        private AssetMetadata metadata;
        private AssetFileInfo fileInfo;
        private AssetState state;
        private BoothItemSchema boothItem;
        private string parentGroupId;
        private List<string> childAssetIds;
        private DateTime lastAccessed;

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

        [JsonConstructor]
        public AssetSchema(Guid assetId, AssetMetadata metadata, AssetFileInfo fileInfo, 
                           AssetState state, BoothItemSchema boothItem, string parentGroupId,
                           List<string> childAssetIds, DateTime lastAccessed)
        {
        this.assetId = assetId;
        this.metadata = metadata ?? new AssetMetadata();
        this.fileInfo = fileInfo ?? new AssetFileInfo();
        this.state = state ?? new AssetState();
        this.boothItem = boothItem;
        this.parentGroupId = parentGroupId ?? string.Empty;
        this.childAssetIds = childAssetIds ?? new List<string>();
        this.lastAccessed = lastAccessed;
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

        public bool HasParentGroup => !string.IsNullOrWhiteSpace(parentGroupId);

        public bool HasChildAssets => childAssetIds != null && childAssetIds.Count > 0;
        #endregion
        #region Methods
        public void SetMetadata(AssetMetadata newMetadata)
        {
            metadata = newMetadata ?? new AssetMetadata();
            lastAccessed = DateTime.Now;
        }

        public void SetFileInfo(AssetFileInfo newFileInfo)
        {
            fileInfo = newFileInfo ?? new AssetFileInfo();
            lastAccessed = DateTime.Now;
        }

        public void SetState(AssetState newState)
        {
            state = newState ?? new AssetState();
            lastAccessed = DateTime.Now;
        }

        public void SetBoothItem(BoothItemSchema newBoothItem)
        {
            boothItem = newBoothItem;
            lastAccessed = DateTime.Now;
        }

        public void SetParentGroupId(string newParentGroupId)
        {
            parentGroupId = newParentGroupId?.Trim() ?? string.Empty;
            lastAccessed = DateTime.Now;
        }

        public void AddChildAssetId(string childAssetId)
        {
            if (string.IsNullOrWhiteSpace(childAssetId))
            {
                Debug.LogError("Cannot add an empty or whitespace child asset ID.");
            }
            else if (childAssetIds.Contains(childAssetId))
            {
                Debug.LogWarning($"Child asset ID '{childAssetId}' already exists in the list.");
            }
            else
            {
                childAssetIds.Add(childAssetId.Trim());
                lastAccessed = DateTime.Now;
            }
        }

        public void RemoveChildAssetId(string childAssetId)
        {
            if (childAssetIds.Contains(childAssetId))
            {
                childAssetIds.Remove(childAssetId);
                lastAccessed = DateTime.Now;
            }
            else
            {
                Debug.LogWarning($"Child asset ID '{childAssetId}' does not exist in the list.");
            }
        }

        public void ClearChildAssetIds()
        {
            childAssetIds.Clear();
            lastAccessed = DateTime.Now;
        }

        public void ClearBoothItem()
        {
            boothItem = null;
            lastAccessed = DateTime.Now;
        }
        #endregion
    }
    #endregion

    #region AssetMetadata
    public class AssetMetadata
    {
        private string name;
        private string description;
        private string authorName;
        private string thumbnailPath;
        private string assetType;
        private List<string> tags;
        private List<string> dependencies;
        private DateTime createdDate;
        private DateTime modifiedDate;

        public AssetMetadata()
        {
            name = string.Empty;
            description = string.Empty;
            authorName = string.Empty;
            thumbnailPath = string.Empty;
            assetType = string.Empty;
            tags = new List<string>();
            dependencies = new List<string>();
            createdDate = DateTime.Now;
            modifiedDate = DateTime.Now;
        }

        [JsonConstructor]
        public AssetMetadata(string name, string description, string authorName, string thumbnailPath,
                             string assetType, List<string> tags, List<string> dependencies,
                             DateTime createdDate, DateTime modifiedDate)
        {
            this.name = name;
            this.description = description;
            this.authorName = authorName;
            this.thumbnailPath = thumbnailPath;
            this.assetType = assetType;
            this.tags = tags ?? new List<string>();
            this.dependencies = dependencies ?? new List<string>();
            this.createdDate = createdDate;
            this.modifiedDate = modifiedDate;
        }

        #region Properties
        public string Name
        {
            get => name ?? string.Empty;
            private set => name = value?.Trim() ?? string.Empty;
        }

        public string Description
        {
            get => description ?? string.Empty;
            private set => description = value?.Trim() ?? string.Empty;
        }

        public string AuthorName
        {
            get => authorName ?? string.Empty;
            private set => authorName = value?.Trim() ?? string.Empty;
        }

        public string ThumbnailPath
        {
            get => thumbnailPath ?? string.Empty;
            private set => thumbnailPath = value?.Trim() ?? string.Empty;
        }

        public string AssetType
        {
            get => assetType;
            private set => assetType = value;
        }

        public IReadOnlyList<string> Tags => tags ?? new List<string>();

        public IReadOnlyList<string> Dependencies => dependencies ?? new List<string>();

        public DateTime CreatedDate
        {
            get => createdDate == default ? DateTime.Now : createdDate;
            private set => createdDate = value;
        }

        public DateTime ModifiedDate
        {
            get => modifiedDate == default ? DateTime.Now : modifiedDate;
            private set => modifiedDate = value;
        }
        #endregion

        #region Methods
        public void SetName(string newName)
        {
            name = newName?.Trim() ?? string.Empty;
            modifiedDate = DateTime.Now;
        }

        public void SetDescription(string newDescription)
        {
            description = newDescription?.Trim() ?? string.Empty;
            modifiedDate = DateTime.Now;
        }

        public void SetAuthorName(string newAuthorName)
        {
            authorName = newAuthorName?.Trim() ?? string.Empty;
            modifiedDate = DateTime.Now;
        }

        public void SetThumbnailPath(string newThumbnailPath)
        {
            thumbnailPath = newThumbnailPath?.Trim() ?? string.Empty;
            modifiedDate = DateTime.Now;
        }

        public void SetAssetType(string newAssetType)
        {
            assetType = newAssetType?.Trim() ?? string.Empty;
            modifiedDate = DateTime.Now;
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogError("Cannot add an empty or whitespace tag.");
            }
            else if (tags.Contains(tag))
            {
                Debug.LogWarning($"Tag '{tag}' already exists in the list.");
            }
            else
            {
                tags.Add(tag.Trim());
                modifiedDate = DateTime.Now;
            }
        }

        public void RemoveTag(string tag)
        {
            if (tags.Contains(tag))
            {
                tags.Remove(tag);
                modifiedDate = DateTime.Now;
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
            else if (dependencies.Contains(dependency))
            {
                Debug.LogWarning($"Dependency '{dependency}' already exists in the list.");
            }
            else
            {
                dependencies.Add(dependency.Trim());
                modifiedDate = DateTime.Now;
            }
        }

        public void RemoveDependency(string dependency)
        {
            if (dependencies.Contains(dependency))
            {
                dependencies.Remove(dependency);
                modifiedDate = DateTime.Now;
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
        private string filePath;
        private List<string> importFiles;

        public AssetFileInfo()
        {
            filePath = string.Empty;
            importFiles = new List<string>();
        }

        [JsonConstructor]
        public AssetFileInfo(string filePath, List<string> importFiles)
        {
            this.filePath = filePath;
            this.importFiles = importFiles ?? new List<string>();
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
    public class AssetState
    {
        private bool isFavorite;
        private bool isArchived;

        public AssetState()
        {
            isFavorite = false;
            isArchived = false;
        }

        [JsonConstructor]
        public AssetState(bool isFavorite, bool isGroup, bool isArchived)
        {
            this.isFavorite = isFavorite;
            this.isArchived = isArchived;
        }

        #region Properties
        public bool IsFavorite
        {
            get => isFavorite;
            private set => isFavorite = value;
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
    public class BoothItemSchema
    {
        private string itemName;
        private string authorName;
        private string itemUrl;
        private string imageUrl;
        private string fileName;
        private string downloadUrl;

        public BoothItemSchema()
        {
            itemName = string.Empty;
            authorName = string.Empty;
            itemUrl = string.Empty;
            imageUrl = string.Empty;
            fileName = string.Empty;
            downloadUrl = string.Empty;
        }

        [JsonConstructor]
        public BoothItemSchema(string itemName, string authorName, string itemUrl, 
                               string imageUrl, string fileName, string downloadUrl)
        {
            this.itemName = itemName;
            this.authorName = authorName;
            this.itemUrl = itemUrl;
            this.imageUrl = imageUrl;
            this.fileName = fileName;
            this.downloadUrl = downloadUrl;
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
