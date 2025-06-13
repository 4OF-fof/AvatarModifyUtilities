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
        public DateTime createdDate;
        public long fileSize;
        public List<string> tags;
        public List<string> dependencies;
        public bool isFavorite;
        public bool isHidden;

        public AssetInfo()
        {
            uid = Guid.NewGuid().ToString();
            name = "";
            description = "";
            assetType = AssetType.Other;
            filePath = "";
            thumbnailPath = "";
            authorName = "";
            createdDate = DateTime.Now;
            fileSize = 0;
            tags = new List<string>();
            dependencies = new List<string>();
            isFavorite = false;
            isHidden = false;
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
                createdDate = this.createdDate,
                fileSize = this.fileSize,
                tags = new List<string>(this.tags),
                dependencies = new List<string>(this.dependencies),
                isFavorite = this.isFavorite,
                isHidden = this.isHidden,
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
