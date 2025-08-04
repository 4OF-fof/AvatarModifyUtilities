using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using AMU.Editor.Core.Api;

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
            _lastUpdated = lastUpdated;
            _assets = assets ?? new Dictionary<Guid, AssetSchema>();
            _tags = tags ?? new List<string>();
            _assetTypes = assetTypes ?? new List<string>();
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
            if (asset == null || _assets.ContainsKey(asset.assetId))
                throw new ArgumentException(LocalizationAPI.GetText("VrcAssetManager_message_schema_assetNullOrExists"));

            _assets[asset.assetId] = asset;
            _lastUpdated = DateTime.Now;
        }

        public void UpdateAsset(AssetSchema asset)
        {
            if (asset == null || !_assets.ContainsKey(asset.assetId))
                throw new ArgumentException(LocalizationAPI.GetText("VrcAssetManager_message_schema_assetNullOrNotExists"));

            _assets[asset.assetId] = asset;
            _lastUpdated = DateTime.Now;
        }

        public void RemoveAsset(Guid assetId)
        {
            if (assetId == Guid.Empty || !_assets.ContainsKey(assetId))
                throw new ArgumentException(LocalizationAPI.GetText("VrcAssetManager_message_schema_assetIdInvalidOrNotExists"));

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

            return _assets.Values.Where(a => a.metadata.name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.metadata.description.Contains(description, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByAuthorName(string authorName)
        {
            if (string.IsNullOrWhiteSpace(authorName))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.metadata.authorName.Contains(authorName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.metadata.tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType))
                return new List<AssetSchema>();

            return _assets.Values.Where(a => a.metadata.assetType.Equals(assetType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<AssetSchema> GetAssetsByStateFavorite(bool isFavorite)
        {
            return _assets.Values.Where(a => a.state.isFavorite == isFavorite).ToList();
        }

        public List<AssetSchema> GetAssetsByStateArchived(bool isArchived)
        {
            return _assets.Values.Where(a => a.state.isArchived == isArchived).ToList();
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
                throw new ArgumentException(LocalizationAPI.GetText("VrcAssetManager_message_schema_tagNullOrExists"));

            _tags.Add(tag.Trim());
            _lastUpdated = DateTime.Now;
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || !_tags.Contains(tag))
                throw new ArgumentException(LocalizationAPI.GetText("VrcAssetManager_message_schema_tagNullOrNotExists"));
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
                throw new ArgumentException(LocalizationAPI.GetText("VrcAssetManager_message_schema_assetTypeNullOrExists"));

            _assetTypes.Add(assetType.Trim());
            _lastUpdated = DateTime.Now;
        }

        public void RemoveAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType) || !_assetTypes.Contains(assetType))
                throw new ArgumentException(LocalizationAPI.GetText("VrcAssetManager_message_schema_assetTypeNullOrNotExists"));

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

        public void ReorderAssetType(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _assetTypes.Count ||
                toIndex < 0 || toIndex >= _assetTypes.Count ||
                fromIndex == toIndex)
            {
                return;
            }

            var assetType = _assetTypes[fromIndex];
            _assetTypes.RemoveAt(fromIndex);
            _assetTypes.Insert(toIndex, assetType);
            _lastUpdated = DateTime.Now;
        }
        #endregion
    }
}
