using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

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

        [JsonConstructor]
        public AssetLibrarySchema(DateTime lastUpdated, Dictionary<Guid, AssetSchema> assets,
                                  List<string> tags, List<string> assetTypes)
        {
            this.lastUpdated = lastUpdated;
            this.assets = assets ?? new Dictionary<Guid, AssetSchema>();
            this.tags = tags ?? new List<string>();
            this.assetTypes = assetTypes ?? new List<string>();
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

        public int TagCount => tags?.Count ?? 0;

        public int AssetTypeCount => assetTypes?.Count ?? 0;
        #endregion

        #region Asset Methods
        public void AddAsset(AssetSchema asset)
        {
            if (asset == null || assets.ContainsKey(asset.AssetId))
                throw new ArgumentException("Asset is null or already exists in the library.");

            assets[asset.AssetId] = asset;
            LastUpdated = DateTime.Now;
        }

        public void UpdateAsset(AssetSchema asset)
        {
            if (asset == null || !assets.ContainsKey(asset.AssetId))
                throw new ArgumentException("Asset is null or does not exist in the library.");

            assets[asset.AssetId] = asset;
            LastUpdated = DateTime.Now;
        }

        public void RemoveAsset(Guid assetId)
        {
            if (assetId == Guid.Empty || !assets.ContainsKey(assetId))
                throw new ArgumentException("Asset ID is invalid or does not exist in the library.");

            assets.Remove(assetId);
            LastUpdated = DateTime.Now;
        }

        public AssetSchema GetAsset(Guid assetId)
        {
            if (assetId == Guid.Empty || !assets.ContainsKey(assetId))
                return null;

            return assets[assetId];
        }

        public List<AssetSchema> GetAllAssets()
        {
            return assets.Values.ToList();
        }

        public List<AssetSchema> GetAssetsByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<AssetSchema>();

            return assets.Values.Where(a => a.Metadata.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new List<AssetSchema>();

            return assets.Values.Where(a => a.Metadata.Description.Contains(description, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByAuthorName(string authorName)
        {
            if (string.IsNullOrWhiteSpace(authorName))
                return new List<AssetSchema>();

            return assets.Values.Where(a => a.Metadata.AuthorName.Contains(authorName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new List<AssetSchema>();

            return assets.Values.Where(a => a.Metadata.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType))
                return new List<AssetSchema>();

            return assets.Values.Where(a => a.Metadata.AssetType.Equals(assetType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByStateFavorite(bool isFavorite)
        {
            return assets.Values.Where(a => a.State.IsFavorite == isFavorite).ToList();
        }

        public List<AssetSchema> GetAssetsByStateArchived(bool isArchived)
        {
            return assets.Values.Where(a => a.State.IsArchived == isArchived).ToList();
        }

        public void ClearAssets()
        {
            assets.Clear();
            LastUpdated = DateTime.Now;
        }
        #endregion

        #region Tag Methods
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || tags.Contains(tag))
                throw new ArgumentException("Tag is null, empty, or already exists in the library.");

            tags.Add(tag.Trim());
            LastUpdated = DateTime.Now;
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || !tags.Contains(tag))
                throw new ArgumentException("Tag is null, empty, or does not exist in the library.");
            tags.Remove(tag);
            LastUpdated = DateTime.Now;
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
        public void AddAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || assetTypes.Contains(assetType))
                throw new ArgumentException("Asset type is null, empty, or already exists in the library.");

            assetTypes.Add(assetType.Trim());
            LastUpdated = DateTime.Now;
        }

        public void RemoveAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || !assetTypes.Contains(assetType))
                throw new ArgumentException("Asset type is null, empty, or does not exist in the library.");

            assetTypes.Remove(assetType);
            LastUpdated = DateTime.Now;
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
