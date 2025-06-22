using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace AMU.Editor.VrcAssetManager.Schema
{
    public class AssetLibrarySchema
    {
        private DateTime _lastUpdated;
        private Dictionary<Guid, AssetSchema> _assets;
        private List<string> _tags;
        private List<string> _assetTypes;

        public AssetLibrarySchema()
        {
            _lastUpdated = DateTime.Now;
            _assets = new Dictionary<Guid, AssetSchema>();
            _tags = new List<string>();
            _assetTypes = new List<string>();
        }

        [JsonConstructor]
        public AssetLibrarySchema(DateTime lastUpdated, Dictionary<Guid, AssetSchema> assets,
                                  List<string> tags, List<string> assetTypes)
        {
            this._lastUpdated = lastUpdated;
            this._assets = assets ?? new Dictionary<Guid, AssetSchema>();
            this._tags = tags ?? new List<string>();
            this._assetTypes = assetTypes ?? new List<string>();
        }

        #region Properties
        public DateTime lastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            private set => _lastUpdated = value;
        }

        public IReadOnlyDictionary<Guid, AssetSchema> assets => _assets ?? new Dictionary<Guid, AssetSchema>();

        public IReadOnlyList<string> tags => _tags ?? new List<string>();

        public IReadOnlyList<string> assetTypes => _assetTypes ?? new List<string>();

        public int assetCount => _assets?.Count ?? 0;

        public int tagCount => _tags?.Count ?? 0;

        public int assetTypeCount => _assetTypes?.Count ?? 0;
        #endregion

        #region Asset Methods
        public void AddAsset(AssetSchema asset)
        {
            if (asset == null || _assets.ContainsKey(asset.AssetId))
                throw new ArgumentException("Asset is null or already exists in the library.");

            _assets[asset.AssetId] = asset;
            _lastUpdated = DateTime.Now;
        }

        public void UpdateAsset(AssetSchema asset)
        {
            if (asset == null || !_assets.ContainsKey(asset.AssetId))
                throw new ArgumentException("Asset is null or does not exist in the library.");

            _assets[asset.AssetId] = asset;
            _lastUpdated = DateTime.Now;
        }

        public void RemoveAsset(Guid assetId)
        {
            if (assetId == Guid.Empty || !_assets.ContainsKey(assetId))
                throw new ArgumentException("Asset ID is invalid or does not exist in the library.");

            _assets.Remove(assetId);
            _lastUpdated = DateTime.Now;
        }

        public AssetSchema GetAsset(Guid assetId)
        {
            if (assetId == Guid.Empty || !_assets.ContainsKey(assetId))
                return null;

            return _assets[assetId];
        }

        public List<AssetSchema> GetAllAssets()
        {
            return _assets.Values.ToList();
        }

        public List<AssetSchema> GetAssetsByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.Metadata.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.Metadata.Description.Contains(description, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByAuthorName(string authorName)
        {
            if (string.IsNullOrWhiteSpace(authorName))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.Metadata.AuthorName.Contains(authorName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.Metadata.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.Metadata.AssetType.Equals(assetType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByStateFavorite(bool isFavorite)
        {
            return _assets.Values.Where(a => a.State.IsFavorite == isFavorite).ToList();
        }

        public List<AssetSchema> GetAssetsByStateArchived(bool isArchived)
        {
            return _assets.Values.Where(a => a.State.IsArchived == isArchived).ToList();
        }

        public void ClearAssets()
        {
            _assets.Clear();
            _lastUpdated = DateTime.Now;
        }
        #endregion

        #region Tag Methods
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || _tags.Contains(tag))
                throw new ArgumentException("Tag is null, empty, or already exists in the library.");

            _tags.Add(tag.Trim());
            _lastUpdated = DateTime.Now;
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || !_tags.Contains(tag))
                throw new ArgumentException("Tag is null, empty, or does not exist in the library.");
            _tags.Remove(tag);
            _lastUpdated = DateTime.Now;
        }

        public bool TagExists(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && _tags.Contains(tag);
        }

        public List<string> GetAllTags()
        {
            return _tags.ToList();
        }

        public void ClearTags()
        {
            _tags.Clear();
            _lastUpdated = DateTime.Now;
        }
        #endregion

        #region AssetType Methods
        public void AddAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || _assetTypes.Contains(assetType))
                throw new ArgumentException("Asset type is null, empty, or already exists in the library.");

            _assetTypes.Add(assetType.Trim());
            _lastUpdated = DateTime.Now;
        }

        public void RemoveAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || !_assetTypes.Contains(assetType))
                throw new ArgumentException("Asset type is null, empty, or does not exist in the library.");

            _assetTypes.Remove(assetType);
            _lastUpdated = DateTime.Now;
        }

        public bool AssetTypeExists(string assetType)
        {
            return !string.IsNullOrWhiteSpace(assetType) && _assetTypes.Contains(assetType);
        }

        public List<string> GetAllAssetTypes()
        {
            return _assetTypes.ToList();
        }

        public void ClearAssetTypes()
        {
            _assetTypes.Clear();
            _lastUpdated = DateTime.Now;
        }
        #endregion
    }
}
