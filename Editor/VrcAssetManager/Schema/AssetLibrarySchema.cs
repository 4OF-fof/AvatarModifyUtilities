using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    public class AssetLibrarySchema
    {
        private DateTime lastUpdated;
        private Dictionary<Guid, AssetSchema> assets;
        private List<string> tags;
        private List<string> assetTypes;
        
        public AssetLibrarySchema()
        {
            lastUpdated = DateTime.Now;
            assets = new Dictionary<Guid, AssetSchema>();
            tags = new List<string>();
            assetTypes = new List<string>();
        }

        #region Properties
        public DateTime LastUpdated
        {
            get => lastUpdated == default ? DateTime.Now : lastUpdated;
            private set => lastUpdated = value;
        }

        public IReadOnlyDictionary<Guid, AssetSchema> Assets => assets ?? new Dictionary<Guid, AssetSchema>();

        public IReadOnlyList<string> Tags => tags ?? new List<string>();

        public IReadOnlyList<string> AssetTypes => assetTypes ?? new List<string>();

        public int AssetCount => assets?.Count ?? 0;

        public int TagsCount => tags?.Count ?? 0;

        public int AssetTypeCount => assetTypes?.Count ?? 0;
        #endregion

        #region Asset Methods
        public bool AddAsset(AssetSchema asset)
        {
            if (asset == null || assets.ContainsKey(asset.AssetId))
                return false;

            assets[asset.AssetId] = asset;
            LastUpdated = DateTime.Now;
            return true;
        }

        public bool UpdateAsset(AssetSchema asset)
        {
            if (asset == null || !assets.ContainsKey(asset.AssetId))
                return false;

            assets[asset.AssetId] = asset;
            LastUpdated = DateTime.Now;
            return true;
        }
        
        public bool RemoveAsset(Guid assetId)
        {
            if (assetId == Guid.Empty || !assets.ContainsKey(assetId))
                return false;

            assets.Remove(assetId);
            LastUpdated = DateTime.Now;
            return true;
        }

        public AssetSchema GetAsset(Guid assetId)
        {
            if (assetId == Guid.Empty || !assets.ContainsKey(assetId))
                return null;

            return assets[assetId];
        }

        public List<AssetSchema> GetAssetsByType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || !assetTypes.Contains(assetType))
                return new List<AssetSchema>();

            return assets.Values.Where(asset => asset.Metadata.AssetType.Equals(assetType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || !tags.Contains(tag))
                return new List<AssetSchema>();

            return assets.Values.Where(asset => asset.Metadata.Tags.Contains(tag)).ToList();
        }

        public List<AssetSchema> GetAllAssets()
        {
            return assets.Values.ToList();
        }

        public void ClearAssets()
        {
            assets.Clear();
            LastUpdated = DateTime.Now;
        }
        #endregion

        #region Tag Methods
        public bool AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || tags.Contains(tag))
                return false;

            tags.Add(tag.Trim());
            LastUpdated = DateTime.Now;
            return true;
        }

        public bool RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || !tags.Contains(tag))
                return false;

            tags.Remove(tag);
            LastUpdated = DateTime.Now;
            return true;
        }

        public bool TagExists(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && tags.Contains(tag);
        }

        public List<string> GetAllTags()
        {
            return tags.ToList();
        }

        public void ClearTags()
        {
            tags.Clear();
            LastUpdated = DateTime.Now;
        }
        #endregion

        #region AssetType Methods
        public bool AddAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || assetTypes.Contains(assetType))
                return false;

            assetTypes.Add(assetType.Trim());
            LastUpdated = DateTime.Now;
            return true;
        }

        public bool RemoveAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || !assetTypes.Contains(assetType))
                return false;

            assetTypes.Remove(assetType);
            LastUpdated = DateTime.Now;
            return true;
        }

        public bool AssetTypeExists(string assetType)
        {
            return !string.IsNullOrWhiteSpace(assetType) && assetTypes.Contains(assetType);
        }

        public List<string> GetAllAssetTypes()
        {
            return assetTypes.ToList();
        }

        public void ClearAssetTypes()
        {
            assetTypes.Clear();
            LastUpdated = DateTime.Now;
        }
        #endregion
    }
}
