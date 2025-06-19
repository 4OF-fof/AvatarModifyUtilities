using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// アセットライブラリの完全なスキーマ
    /// </summary>
    [Serializable]
    public class AssetLibrarySchema
    {
        [SerializeField]
        private DateTime _lastUpdated;
        [SerializeField]
        private Dictionary<string, AssetSchema> _assets;

        public DateTime LastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            set => _lastUpdated = value;
        }

        public IReadOnlyDictionary<AssetId, AssetSchema> Assets
        {
            get
            {
                if (_assets == null) return new Dictionary<AssetId, AssetSchema>();

                var result = new Dictionary<AssetId, AssetSchema>();
                foreach (var kvp in _assets)
                {
                    if (AssetId.TryParse(kvp.Key, out var assetId))
                    {
                        result[assetId] = kvp.Value;
                    }
                }
                return result;
            }
        }

        public int AssetCount => _assets?.Count ?? 0;

        public AssetLibrarySchema()
        {
            _lastUpdated = DateTime.Now;
            _assets = new Dictionary<string, AssetSchema>();
        }

        public bool AddAsset(AssetId assetId, AssetSchema asset)
        {
            if (string.IsNullOrEmpty(assetId.Value) || asset == null) return false;

            _assets ??= new Dictionary<string, AssetSchema>();
            _assets[assetId.Value] = asset;
            _lastUpdated = DateTime.Now;

            return true;
        }

        public bool RemoveAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return false;

            var removed = _assets?.Remove(assetId.Value) ?? false;
            if (removed)
            {
                _lastUpdated = DateTime.Now;
            }

            return removed;
        }

        public AssetSchema GetAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return null;
            return _assets?.GetValueOrDefault(assetId.Value);
        }

        public bool ContainsAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return false;
            return _assets?.ContainsKey(assetId.Value) ?? false;
        }

        public IEnumerable<AssetSchema> GetVisibleAssets()
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => !asset.State.IsArchived);
        }

        public IEnumerable<AssetSchema> GetFavoriteAssets()
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => asset.State.IsFavorite);
        }

        public IEnumerable<AssetSchema> GetAssetsByType(string assetType)
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => asset.Metadata.AssetType == assetType);
        }

        public IEnumerable<AssetSchema> GetAssetsByAuthor(string authorName)
        {
            if (_assets == null || string.IsNullOrWhiteSpace(authorName))
                return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset =>
                asset.Metadata.AuthorName.Equals(authorName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public void ClearAssets()
        {
            _assets?.Clear();
            _lastUpdated = DateTime.Now;
        }

        public void Optimize()
        {
            // 今後必要に応じて最適化処理を実装
            _lastUpdated = DateTime.Now;
        }
    }
}
