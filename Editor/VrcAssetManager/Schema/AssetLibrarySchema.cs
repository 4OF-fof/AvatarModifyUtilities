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
        [SerializeField]
        private List<string> _tags;
        [SerializeField]
        private List<string> _assetTypes;

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

        /// <summary>
        /// ライブラリ内で使用可能なタグのリスト
        /// </summary>
        public IReadOnlyList<string> Tags => _tags ?? new List<string>();

        /// <summary>
        /// ライブラリ内で使用可能なアセットタイプのリスト
        /// </summary>
        public IReadOnlyList<string> AssetTypes => _assetTypes ?? new List<string>();

        /// <summary>
        /// タグの数
        /// </summary>
        public int TagsCount => _tags?.Count ?? 0;

        /// <summary>
        /// アセットタイプの数
        /// </summary>
        public int AssetTypeCount => _assetTypes?.Count ?? 0;

        public AssetLibrarySchema()
        {
            _lastUpdated = DateTime.Now;
            _assets = new Dictionary<string, AssetSchema>();
            _tags = new List<string>();
            _assetTypes = new List<string>();
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

        /// <summary>
        /// 内部使用：タグを直接追加します（Controllerからのみ使用）
        /// </summary>
        internal bool InternalAddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;

            _tags ??= new List<string>();
            var trimmedTag = tag.Trim();
            if (!_tags.Contains(trimmedTag))
            {
                _tags.Add(trimmedTag);
                _lastUpdated = DateTime.Now;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 内部使用：タグを直接削除します（Controllerからのみ使用）
        /// </summary>
        internal bool InternalRemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;

            var removed = _tags?.Remove(tag.Trim()) ?? false;
            if (removed)
            {
                _lastUpdated = DateTime.Now;
            }
            return removed;
        }

        /// <summary>
        /// 内部使用：アセットタイプを直接追加します（Controllerからのみ使用）
        /// </summary>
        internal bool InternalAddAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType)) return false;

            _assetTypes ??= new List<string>();
            var trimmedType = assetType.Trim();
            if (!_assetTypes.Contains(trimmedType))
            {
                _assetTypes.Add(trimmedType);
                _lastUpdated = DateTime.Now;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 内部使用：アセットタイプを直接削除します（Controllerからのみ使用）
        /// </summary>
        internal bool InternalRemoveAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType)) return false;

            var removed = _assetTypes?.Remove(assetType.Trim()) ?? false;
            if (removed)
            {
                _lastUpdated = DateTime.Now;
            }
            return removed;
        }

        /// <summary>
        /// 内部使用：すべてのタグをクリアします（Controllerからのみ使用）
        /// </summary>
        internal void InternalClearTags()
        {
            _tags?.Clear();
            _lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// 内部使用：すべてのアセットタイプをクリアします（Controllerからのみ使用）
        /// </summary>
        internal void InternalClearAssetTypes()
        {
            _assetTypes?.Clear();
            _lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// 指定されたタグがライブラリに存在するかどうかを確認します
        /// </summary>
        /// <param name="tag">確認するタグ</param>
        /// <returns>存在する場合はtrue</returns>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            return _tags?.Contains(tag.Trim()) ?? false;
        }

        /// <summary>
        /// 指定されたアセットタイプがライブラリに存在するかどうかを確認します
        /// </summary>
        /// <param name="assetType">確認するアセットタイプ</param>
        /// <returns>存在する場合はtrue</returns>
        public bool HasAssetType(string assetType)
        {
            if (string.IsNullOrWhiteSpace(assetType)) return false;
            return _assetTypes?.Contains(assetType.Trim()) ?? false;
        }

        public void Optimize()
        {
            _lastUpdated = DateTime.Now;
        }
    }
}
