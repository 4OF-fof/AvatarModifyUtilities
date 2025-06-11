using System;
using System.Collections.Generic;
using UnityEngine;

namespace AMU.AssetManager.Data
{
    [Serializable]
    public enum AssetType
    {
        Avatar,
        Clothing,
        Accessory,
        Texture,
        Material,
        Animation,
        Shader,
        Prefab,
        Script,
        Other
    }

    [Serializable]
    public class AssetInfo
    {
        public string uid;
        public string name;
        public string description;
        public AssetType assetType;
        public string filePath;
        public string thumbnailPath;
        public string authorName;
        public string version;
        public DateTime createdDate;
        public DateTime lastModifiedDate;
        public long fileSize;
        public List<string> tags;
        public List<string> dependencies;
        public bool isFavorite;
        public int rating;
        public string notes;

        public AssetInfo()
        {
            uid = Guid.NewGuid().ToString();
            name = "";
            description = "";
            assetType = AssetType.Other;
            filePath = "";
            thumbnailPath = "";
            authorName = "";
            version = "1.0";
            createdDate = DateTime.Now;
            lastModifiedDate = DateTime.Now;
            fileSize = 0;
            tags = new List<string>();
            dependencies = new List<string>();
            isFavorite = false;
            rating = 0;
            notes = "";
        }

        public AssetInfo Clone()
        {
            return new AssetInfo
            {
                uid = this.uid,
                name = this.name,
                description = this.description,
                assetType = this.assetType,
                filePath = this.filePath,
                thumbnailPath = this.thumbnailPath,
                authorName = this.authorName,
                version = this.version,
                createdDate = this.createdDate,
                lastModifiedDate = this.lastModifiedDate,
                fileSize = this.fileSize,
                tags = new List<string>(this.tags),
                dependencies = new List<string>(this.dependencies),
                isFavorite = this.isFavorite,
                rating = this.rating,
                notes = this.notes
            };
        }
    }

    [Serializable]
    public class AssetLibrary
    {
        public string version;
        public DateTime lastUpdated;
        public List<AssetInfo> assets;

        public AssetLibrary()
        {
            version = "1.0";
            lastUpdated = DateTime.Now;
            assets = new List<AssetInfo>();
        }
    }
}
