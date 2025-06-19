using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    [Serializable]
    public class AssetLibrarySchema
    {
        [SerializeField]
        private DateTime _lastUpdated;
        [SerializeField]
        private Dictionary<AssetId, AssetSchema> _assets;
        [SerializeField]
        private List<string> _tags;
        [SerializeField]
        private List<string> _assetTypes;

        public AssetLibrarySchema()
        {
            _lastUpdated = DateTime.Now;
            _assets = new Dictionary<AssetId, AssetSchema>();
            _tags = new List<string>();
            _assetTypes = new List<string>();
        }

        #region Properties
        public DateTime LastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            private set => _lastUpdated = value;
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

        public IReadOnlyList<string> Tags => _tags ?? new List<string>();

        public IReadOnlyList<string> AssetTypes => _assetTypes ?? new List<string>();

        public int AssetCount => _assets?.Count ?? 0;

        public int TagsCount => _tags?.Count ?? 0;

        public int AssetTypeCount => _assetTypes?.Count ?? 0;
        #endregion
    }
}
