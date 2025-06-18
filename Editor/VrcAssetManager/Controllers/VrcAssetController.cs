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
    /// アセットライブラリ経由でアセットを管理し、キャッシュは AssetLibraryController に委任します
    /// </summary>
    public static class VrcAssetController
    {
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

                var library = AssetLibraryController.LoadLibrary();
                if (library == null)
                {
                    Debug.LogError("Failed to load asset library");
                    return false;
                }
                if (library.ContainsAsset(assetId))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_assetExists"), assetId));
                    return UpdateAsset(assetId, assetData);
                }

                library.AddAsset(assetId, assetData);
                bool saved = AssetLibraryController.SaveLibrary(library);

                if (saved)
                {
                    Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_assetAdded"), assetData.Metadata.Name));
                }

                return saved;
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
                var library = AssetLibraryController.LoadLibrary();
                if (library == null)
                {
                    Debug.LogError("Failed to load asset library");
                    return false;
                }

                if (!library.ContainsAsset(assetId))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_assetNotFound"), assetId));
                    return false;
                }
                library.AddAsset(assetId, assetData);
                bool saved = AssetLibraryController.SaveLibrary(library);

                if (saved)
                {
                    Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_assetUpdated"), assetData.Metadata.Name));
                }

                return saved;
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
                var library = AssetLibraryController.LoadLibrary();
                if (library == null)
                {
                    Debug.LogError("Failed to load asset library");
                    return false;
                }

                var assetData = library.GetAsset(assetId);
                if (assetData == null)
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_assetNotFound"), assetId));
                    return false;
                }
                library.RemoveAsset(assetId);
                bool saved = AssetLibraryController.SaveLibrary(library);

                if (saved)
                {
                    Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_assetRemoved"), assetData.Metadata.Name));
                }

                return saved;
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
            var library = AssetLibraryController.LoadLibrary();
            return library?.GetAsset(assetId);
        }

        /// <summary>
        /// 全てのVRCアセットを取得します
        /// </summary>
        /// <returns>全VRCアセットのリスト</returns>
        public static List<AssetSchema> GetAllAssets()
        {
            var library = AssetLibraryController.LoadLibrary();
            return library?.Assets?.Values.ToList() ?? new List<AssetSchema>();
        }

        /// <summary>
        /// 指定されたカテゴリのVRCアセットを取得します
        /// </summary>
        /// <param name="category">カテゴリ名</param>
        /// <returns>カテゴリに属するVRCアセットのリスト</returns>
        public static List<AssetSchema> GetAssetsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return new List<AssetSchema>();
            }

            var library = AssetLibraryController.LoadLibrary();
            if (library?.Assets == null)
            {
                return new List<AssetSchema>();
            }
            return library.Assets.Values
                   .Where(asset => string.Equals(asset.Metadata.AssetType, category, StringComparison.OrdinalIgnoreCase))
                   .ToList();
        }

        /// <summary>
        /// 指定された作者のVRCアセットを取得します
        /// </summary>
        /// <param name="author">作者名</param>
        /// <returns>作者のVRCアセットのリスト</returns>
        public static List<AssetSchema> GetAssetsByAuthor(string author)
        {
            if (string.IsNullOrEmpty(author))
            {
                return new List<AssetSchema>();
            }

            var library = AssetLibraryController.LoadLibrary();
            if (library?.Assets == null)
            {
                return new List<AssetSchema>();
            }

            return library.Assets.Values
                          .Where(asset => string.Equals(asset.Metadata.AuthorName, author, StringComparison.OrdinalIgnoreCase))
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

            var library = AssetLibraryController.LoadLibrary();
            if (library?.Assets == null)
            {
                return new List<AssetSchema>();
            }

            var searchTermLower = searchTerm.ToLower();
            return library.Assets.Values
                             .Where(asset => asset.Metadata.Name.ToLower().Contains(searchTermLower) ||
                                           asset.Metadata.Description.ToLower().Contains(searchTermLower) ||
                                           asset.Metadata.AuthorName.ToLower().Contains(searchTermLower))
                             .ToList();
        }

        /// <summary>
        /// 指定されたアセットがBoothアイテムを持っているかを判定します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>Boothアイテムがあればtrue</returns>
        public static bool HasBoothItem(AssetId assetId)
        {
            var asset = GetAsset(assetId);
            return asset != null && asset.BoothItem != null && asset.BoothItem.HasData;
        }

        /// <summary>
        /// 指定されたアセットがBoothアイテムを持っているかを判定します
        /// </summary>
        /// <param name="asset">アセットデータ</param>
        /// <returns>Boothアイテムがあればtrue</returns>
        public static bool HasBoothItem(AssetSchema asset)
        {
            return asset?.BoothItem != null && asset.BoothItem.HasData;
        }        /// <summary>
                 /// 指定されたアセットが親を持たないトップレベルのアイテムかを判定します
                 /// </summary>
                 /// <param name="assetId">アセットID</param>
                 /// <returns>トップレベルのアイテムならtrue</returns>
        public static bool IsTopLevel(AssetId assetId)
        {
            var asset = GetAsset(assetId);
            return asset != null && string.IsNullOrEmpty(asset.ParentGroupId);
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
        }        /// <summary>
                 /// 指定されたグループが子アセットを持っているかを判定します
                 /// </summary>
                 /// <param name="group">グループデータ</param>
                 /// <returns>子アセットがあればtrue</returns>
        public static bool HasChildren(AssetGroupSchema group)
        {
            return group?.ChildAssetIds?.Count > 0;
        }
    }
}
