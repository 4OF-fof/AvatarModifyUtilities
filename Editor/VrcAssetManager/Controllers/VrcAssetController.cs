using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    /// <summary>
    /// VRCアセットの管理を行うコントローラ
    /// </summary>
    public static class VrcAssetController
    {
        private static readonly Dictionary<AssetId, AssetSchema> _assetCache = new Dictionary<AssetId, AssetSchema>();
        private static readonly Dictionary<string, List<AssetId>> _categoryIndex = new Dictionary<string, List<AssetId>>();
        private static readonly Dictionary<string, List<AssetId>> _authorIndex = new Dictionary<string, List<AssetId>>();

        /// <summary>
        /// VRCアセットを追加します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="assetData">追加するVRCアセットデータ</param>
        /// <returns>追加に成功した場合true</returns>
        public static bool AddAsset(AssetId assetId, AssetSchema assetData)
        {
            try
            {
                if (assetId == default(AssetId))
                {
                    Debug.LogError(LocalizationController.GetText("VrcAssetManager_message_error_invalidAssetId"));
                    return false;
                }

                if (_assetCache.ContainsKey(assetId))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_assetExists"), assetId));
                    return UpdateAsset(assetId, assetData);
                }

                _assetCache[assetId] = assetData;
                UpdateIndices(assetId, assetData);

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_assetAdded"), assetData.Metadata.Name));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_addAssetFailed"), ex.Message));
                return false;
            }
        }

        /// <summary>
        /// VRCアセットを更新します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="assetData">更新するVRCアセットデータ</param>
        /// <returns>更新に成功した場合true</returns>
        public static bool UpdateAsset(AssetId assetId, AssetSchema assetData)
        {
            try
            {
                if (!_assetCache.ContainsKey(assetId))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_assetNotFound"), assetId));
                    return false;
                }

                var oldAssetData = _assetCache[assetId];
                RemoveFromIndices(assetId, oldAssetData);

                _assetCache[assetId] = assetData;
                UpdateIndices(assetId, assetData);

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_assetUpdated"), assetData.Metadata.Name));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_updateAssetFailed"), ex.Message));
                return false;
            }
        }

        /// <summary>
        /// VRCアセットを削除します
        /// </summary>
        /// <param name="assetId">削除するアセットID</param>
        /// <returns>削除に成功した場合true</returns>
        public static bool RemoveAsset(AssetId assetId)
        {
            try
            {
                if (!_assetCache.TryGetValue(assetId, out var assetData))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_assetNotFound"), assetId));
                    return false;
                }

                RemoveFromIndices(assetId, assetData);
                _assetCache.Remove(assetId);

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_assetRemoved"), assetData.Metadata.Name));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_removeAssetFailed"), ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 指定されたIDのVRCアセットを取得します
        /// </summary>
        /// <param name="assetId">取得するアセットID</param>
        /// <returns>VRCアセットデータ、見つからない場合はnull</returns>
        public static AssetSchema GetAsset(AssetId assetId)
        {
            return _assetCache.TryGetValue(assetId, out var assetData) ? assetData : default(AssetSchema);
        }

        /// <summary>
        /// 全てのVRCアセットを取得します
        /// </summary>
        /// <returns>全VRCアセットのリスト</returns>
        public static List<AssetSchema> GetAllAssets()
        {
            return _assetCache.Values.ToList();
        }

        /// <summary>
        /// 指定されたカテゴリのVRCアセットを取得します
        /// </summary>
        /// <param name="category">カテゴリ名</param>
        /// <returns>カテゴリに属するVRCアセットのリスト</returns>
        public static List<AssetSchema> GetAssetsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || !_categoryIndex.TryGetValue(category, out var assetIds))
            {
                return new List<AssetSchema>();
            }

            return assetIds.Where(id => _assetCache.ContainsKey(id))
                          .Select(id => _assetCache[id])
                          .ToList();
        }

        /// <summary>
        /// 指定された作者のVRCアセットを取得します
        /// </summary>
        /// <param name="author">作者名</param>
        /// <returns>作者のVRCアセットのリスト</returns>
        public static List<AssetSchema> GetAssetsByAuthor(string author)
        {
            if (string.IsNullOrEmpty(author) || !_authorIndex.TryGetValue(author, out var assetIds))
            {
                return new List<AssetSchema>();
            }

            return assetIds.Where(id => _assetCache.ContainsKey(id))
                          .Select(id => _assetCache[id])
                          .ToList();
        }

        /// <summary>
        /// 名前でVRCアセットを検索します
        /// </summary>
        /// <param name="searchTerm">検索文字列</param>
        /// <returns>検索条件に一致するVRCアセットのリスト</returns>
        public static List<AssetSchema> SearchAssets(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return GetAllAssets();
            }

            var searchTermLower = searchTerm.ToLower();
            return _assetCache.Values
                             .Where(asset => asset.Metadata.Name.ToLower().Contains(searchTermLower) ||
                                           asset.Metadata.Description.ToLower().Contains(searchTermLower) ||
                                           asset.Metadata.AuthorName.ToLower().Contains(searchTermLower))
                             .ToList();
        }

        /// <summary>
        /// アセットキャッシュをクリアします
        /// </summary>
        public static void ClearCache()
        {
            _assetCache.Clear();
            _categoryIndex.Clear();
            _authorIndex.Clear();

            Debug.Log(LocalizationController.GetText("VrcAssetManager_message_success_cacheCleared"));
        }

        /// <summary>
        /// 指定されたアセットがBoothアイテムを持っているかを判定します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>Boothアイテムがあればtrue</returns>
        public static bool HasBoothItem(AssetId assetId)
        {
            if (!_assetCache.TryGetValue(assetId, out var asset))
            {
                return false;
            }
            return asset.BoothItem != null && asset.BoothItem.HasData;
        }

        /// <summary>
        /// 指定されたアセットがBoothアイテムを持っているかを判定します
        /// </summary>
        /// <param name="asset">アセットデータ</param>
        /// <returns>Boothアイテムがあればtrue</returns>
        public static bool HasBoothItem(AssetSchema asset)
        {
            return asset?.BoothItem != null && asset.BoothItem.HasData;
        }

        /// <summary>
        /// 指定されたアセットが親を持たないトップレベルのアイテムかを判定します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>トップレベルのアイテムならtrue</returns>
        public static bool IsTopLevel(AssetId assetId)
        {
            if (!_assetCache.TryGetValue(assetId, out var asset))
            {
                return false;
            }
            return string.IsNullOrEmpty(asset.ParentGroupId);
        }

        /// <summary>
        /// 指定されたアセットが親を持たないトップレベルのアイテムかを判定します
        /// </summary>
        /// <param name="asset">アセットデータ</param>
        /// <returns>トップレベルのアイテムならtrue</returns>
        public static bool IsTopLevel(AssetSchema asset)
        {
            return asset != null && string.IsNullOrEmpty(asset.ParentGroupId);
        }

        /// <summary>
        /// 指定されたグループが親グループを持っているかを判定します
        /// </summary>
        /// <param name="group">グループデータ</param>
        /// <returns>親グループがあればtrue</returns>
        public static bool HasParent(AssetGroupSchema group)
        {
            return group != null && !string.IsNullOrEmpty(group.ParentGroupId);
        }

        /// <summary>
        /// 指定されたグループが子アセットを持っているかを判定します
        /// </summary>
        /// <param name="group">グループデータ</param>
        /// <returns>子アセットがあればtrue</returns>
        public static bool HasChildren(AssetGroupSchema group)
        {
            return group?.ChildAssetIds?.Count > 0;
        }

        /// <summary>
        /// インデックスを更新します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="assetData">更新対象のアセットデータ</param>
        private static void UpdateIndices(AssetId assetId, AssetSchema assetData)
        {
            // カテゴリインデックスの更新
            var category = assetData.Metadata.AssetType.Value;
            if (!string.IsNullOrEmpty(category))
            {
                if (!_categoryIndex.ContainsKey(category))
                {
                    _categoryIndex[category] = new List<AssetId>();
                }
                if (!_categoryIndex[category].Contains(assetId))
                {
                    _categoryIndex[category].Add(assetId);
                }
            }

            // 作者インデックスの更新
            if (!string.IsNullOrEmpty(assetData.Metadata.AuthorName))
            {
                if (!_authorIndex.ContainsKey(assetData.Metadata.AuthorName))
                {
                    _authorIndex[assetData.Metadata.AuthorName] = new List<AssetId>();
                }
                if (!_authorIndex[assetData.Metadata.AuthorName].Contains(assetId))
                {
                    _authorIndex[assetData.Metadata.AuthorName].Add(assetId);
                }
            }
        }

        /// <summary>
        /// インデックスからアセットを削除します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="assetData">削除対象のアセットデータ</param>
        private static void RemoveFromIndices(AssetId assetId, AssetSchema assetData)
        {
            // カテゴリインデックスから削除
            var category = assetData.Metadata.AssetType.Value;
            if (!string.IsNullOrEmpty(category) && _categoryIndex.ContainsKey(category))
            {
                _categoryIndex[category].Remove(assetId);
                if (_categoryIndex[category].Count == 0)
                {
                    _categoryIndex.Remove(category);
                }
            }

            // 作者インデックスから削除
            if (!string.IsNullOrEmpty(assetData.Metadata.AuthorName) && _authorIndex.ContainsKey(assetData.Metadata.AuthorName))
            {
                _authorIndex[assetData.Metadata.AuthorName].Remove(assetId);
                if (_authorIndex[assetData.Metadata.AuthorName].Count == 0)
                {
                    _authorIndex.Remove(assetData.Metadata.AuthorName);
                }
            }
        }
    }
}
