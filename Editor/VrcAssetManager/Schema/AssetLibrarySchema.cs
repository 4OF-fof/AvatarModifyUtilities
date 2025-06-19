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

        /// <summary>
        /// タグをライブラリに追加します
        /// </summary>
        /// <param name="tag">追加するタグ</param>
        /// <returns>追加に成功した場合はtrue</returns>
        public bool AddTag(string tag)
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
        /// タグをライブラリから削除します
        /// </summary>
        /// <param name="tag">削除するタグ</param>
        /// <returns>削除に成功した場合はtrue</returns>
        public bool RemoveTag(string tag)
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
        /// アセットタイプをライブラリに追加します
        /// </summary>
        /// <param name="assetType">追加するアセットタイプ</param>
        /// <returns>追加に成功した場合はtrue</returns>
        public bool AddAssetType(string assetType)
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
        /// アセットタイプをライブラリから削除します
        /// </summary>
        /// <param name="assetType">削除するアセットタイプ</param>
        /// <returns>削除に成功した場合はtrue</returns>
        public bool RemoveAssetType(string assetType)
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

        /// <summary>
        /// アセット内で使用されているタグを収集してライブラリのタグリストに自動追加します
        /// </summary>
        public void SynchronizeTagsFromAssets()
        {
            if (_assets == null) return;

            _tags ??= new List<string>();
            var newTags = new HashSet<string>();

            foreach (var asset in _assets.Values)
            {
                foreach (var tag in asset.Metadata.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag) && !_tags.Contains(tag))
                    {
                        newTags.Add(tag.Trim());
                    }
                }
            }

            if (newTags.Count > 0)
            {
                _tags.AddRange(newTags);
                _lastUpdated = DateTime.Now;
            }
        }

        /// <summary>
        /// アセット内で使用されているアセットタイプを収集してライブラリのアセットタイプリストに自動追加します
        /// </summary>
        public void SynchronizeAssetTypesFromAssets()
        {
            if (_assets == null) return;

            _assetTypes ??= new List<string>();
            var newAssetTypes = new HashSet<string>();

            foreach (var asset in _assets.Values)
            {
                var assetType = asset.Metadata.AssetType;
                if (!string.IsNullOrWhiteSpace(assetType) && !_assetTypes.Contains(assetType))
                {
                    newAssetTypes.Add(assetType.Trim());
                }
            }

            if (newAssetTypes.Count > 0)
            {
                _assetTypes.AddRange(newAssetTypes);
                _lastUpdated = DateTime.Now;
            }
        }

        /// <summary>
        /// 未使用のタグをライブラリから削除します
        /// </summary>
        public void CleanupUnusedTags()
        {
            if (_assets == null || _tags == null) return;

            var usedTags = new HashSet<string>();
            foreach (var asset in _assets.Values)
            {
                foreach (var tag in asset.Metadata.Tags)
                {
                    usedTags.Add(tag);
                }
            }

            var tagsToRemove = _tags.Where(tag => !usedTags.Contains(tag)).ToList();
            foreach (var tag in tagsToRemove)
            {
                _tags.Remove(tag);
            }

            if (tagsToRemove.Count > 0)
            {
                _lastUpdated = DateTime.Now;
            }
        }

        /// <summary>
        /// 未使用のアセットタイプをライブラリから削除します
        /// </summary>
        public void CleanupUnusedAssetTypes()
        {
            if (_assets == null || _assetTypes == null) return;

            var usedAssetTypes = new HashSet<string>();
            foreach (var asset in _assets.Values)
            {
                if (!string.IsNullOrWhiteSpace(asset.Metadata.AssetType))
                {
                    usedAssetTypes.Add(asset.Metadata.AssetType);
                }
            }

            var assetTypesToRemove = _assetTypes.Where(assetType => !usedAssetTypes.Contains(assetType)).ToList();
            foreach (var assetType in assetTypesToRemove)
            {
                _assetTypes.Remove(assetType);
            }

            if (assetTypesToRemove.Count > 0)
            {
                _lastUpdated = DateTime.Now;
            }
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
        /// すべてのタグをクリアします
        /// </summary>
        public void ClearTags()
        {
            _tags?.Clear();
            _lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// すべてのアセットタイプをクリアします
        /// </summary>
        public void ClearAssetTypes()
        {
            _assetTypes?.Clear();
            _lastUpdated = DateTime.Now;
        }

        public void Optimize()
        {
            // アセットから自動的にタグとアセットタイプを同期
            SynchronizeTagsFromAssets();
            SynchronizeAssetTypesFromAssets();

            // 未使用のタグとアセットタイプを削除
            CleanupUnusedTags();
            CleanupUnusedAssetTypes();

            _lastUpdated = DateTime.Now;
        }
    }
}
