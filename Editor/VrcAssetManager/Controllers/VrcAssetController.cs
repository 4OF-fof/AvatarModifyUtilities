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
        /// <param name="assetData">追加するVRCアセットデータ</param>
        /// <returns>追加に成功した場合true</returns>
        public static bool AddAsset(AssetSchema assetData)
        {
            try
            {
                if (assetData.Id == default(AssetId))
                {
                    Debug.LogError(LocalizationController.GetText("VrcAssetManager_message_error_invalidAssetId"));
                    return false;
                }

                if (_assetCache.ContainsKey(assetData.Id))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_assetExists"), assetData.Id));
                    return UpdateAsset(assetData);
                }

                _assetCache[assetData.Id] = assetData;
                UpdateIndices(assetData);

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
        /// <param name="assetData">更新するVRCアセットデータ</param>
        /// <returns>更新に成功した場合true</returns>
        public static bool UpdateAsset(AssetSchema assetData)
        {
            try
            {
                if (!_assetCache.ContainsKey(assetData.Id))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_assetNotFound"), assetData.Id));
                    return false;
                }

                var oldAssetData = _assetCache[assetData.Id];
                RemoveFromIndices(oldAssetData);

                _assetCache[assetData.Id] = assetData;
                UpdateIndices(assetData);

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

                RemoveFromIndices(assetData);
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
        /// 利用可能なカテゴリの一覧を取得します
        /// </summary>
        /// <returns>カテゴリ名のリスト</returns>
        public static List<string> GetAvailableCategories()
        {
            return _categoryIndex.Keys.ToList();
        }

        /// <summary>
        /// 利用可能な作者の一覧を取得します
        /// </summary>
        /// <returns>作者名のリスト</returns>
        public static List<string> GetAvailableAuthors()
        {
            return _authorIndex.Keys.ToList();
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
        /// キャッシュされているアセット数を取得します
        /// </summary>
        /// <returns>キャッシュされているアセット数</returns>
        public static int GetCachedAssetCount()
        {
            return _assetCache.Count;
        }

        /// <summary>
        /// インデックスを更新します
        /// </summary>
        /// <param name="assetData">更新対象のアセットデータ</param>
        private static void UpdateIndices(AssetSchema assetData)
        {
            // カテゴリインデックスの更新
            var category = assetData.Metadata.AssetType.Value;
            if (!string.IsNullOrEmpty(category))
            {
                if (!_categoryIndex.ContainsKey(category))
                {
                    _categoryIndex[category] = new List<AssetId>();
                }
                if (!_categoryIndex[category].Contains(assetData.Id))
                {
                    _categoryIndex[category].Add(assetData.Id);
                }
            }

            // 作者インデックスの更新
            if (!string.IsNullOrEmpty(assetData.Metadata.AuthorName))
            {
                if (!_authorIndex.ContainsKey(assetData.Metadata.AuthorName))
                {
                    _authorIndex[assetData.Metadata.AuthorName] = new List<AssetId>();
                }
                if (!_authorIndex[assetData.Metadata.AuthorName].Contains(assetData.Id))
                {
                    _authorIndex[assetData.Metadata.AuthorName].Add(assetData.Id);
                }
            }
        }

        /// <summary>
        /// インデックスからアセットを削除します
        /// </summary>
        /// <param name="assetData">削除対象のアセットデータ</param>
        private static void RemoveFromIndices(AssetSchema assetData)
        {
            // カテゴリインデックスから削除
            var category = assetData.Metadata.AssetType.Value;
            if (!string.IsNullOrEmpty(category) && _categoryIndex.ContainsKey(category))
            {
                _categoryIndex[category].Remove(assetData.Id);
                if (_categoryIndex[category].Count == 0)
                {
                    _categoryIndex.Remove(category);
                }
            }

            // 作者インデックスから削除
            if (!string.IsNullOrEmpty(assetData.Metadata.AuthorName) && _authorIndex.ContainsKey(assetData.Metadata.AuthorName))
            {
                _authorIndex[assetData.Metadata.AuthorName].Remove(assetData.Id);
                if (_authorIndex[assetData.Metadata.AuthorName].Count == 0)
                {
                    _authorIndex.Remove(assetData.Metadata.AuthorName);
                }
            }
        }
    }
}
