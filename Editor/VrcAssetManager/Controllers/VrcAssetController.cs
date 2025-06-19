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
        }        /// <summary>
                 /// 名前でVRCアセットを取得します
                 /// </summary>
                 /// <param name="searchTerm">検索文字列</param>
                 /// <returns>検索条件に一致するVRCアセットのリスト</returns>
        public static List<AssetSchema> GetAssetsByName(string searchTerm)
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

        #region Asset Metadata Operations

        /// <summary>
        /// アセットのメタデータを更新します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="name">名前</param>
        /// <param name="description">説明</param>
        /// <param name="authorName">作者名</param>
        /// <param name="assetType">アセットタイプ</param>
        /// <returns>更新に成功した場合true</returns>
        public static bool UpdateMetadata(AssetId assetId, string name = null, string description = null,
            string authorName = null, string assetType = null)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var updatedMetadata = new AssetMetadata(
                    name ?? asset.Metadata.Name,
                    description ?? asset.Metadata.Description,
                    authorName ?? asset.Metadata.AuthorName,
                    assetType ?? asset.Metadata.AssetType,
                    new List<string>(asset.Metadata.Tags),
                    new List<string>(asset.Metadata.Dependencies),
                    asset.Metadata.CreatedDate,
                    DateTime.Now
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    updatedMetadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update metadata for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットにタグを追加します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="tag">追加するタグ</param>
        /// <returns>追加に成功した場合true</returns>
        public static bool AddTag(AssetId assetId, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var tags = new List<string>(asset.Metadata.Tags);
                var trimmedTag = tag.Trim();
                if (tags.Contains(trimmedTag)) return true; // 既に存在する場合は成功とする

                tags.Add(trimmedTag);

                var updatedMetadata = new AssetMetadata(
                    asset.Metadata.Name,
                    asset.Metadata.Description,
                    asset.Metadata.AuthorName,
                    asset.Metadata.AssetType,
                    tags,
                    new List<string>(asset.Metadata.Dependencies),
                    asset.Metadata.CreatedDate,
                    DateTime.Now
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    updatedMetadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add tag to asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットからタグを削除します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="tag">削除するタグ</param>
        /// <returns>削除に成功した場合true</returns>
        public static bool RemoveTag(AssetId assetId, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var tags = new List<string>(asset.Metadata.Tags);
                if (!tags.Remove(tag.Trim())) return true; // 存在しない場合は成功とする

                var updatedMetadata = new AssetMetadata(
                    asset.Metadata.Name,
                    asset.Metadata.Description,
                    asset.Metadata.AuthorName,
                    asset.Metadata.AssetType,
                    tags,
                    new List<string>(asset.Metadata.Dependencies),
                    asset.Metadata.CreatedDate,
                    DateTime.Now
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    updatedMetadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove tag from asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットのタグをクリアします
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>クリアに成功した場合true</returns>
        public static bool ClearTags(AssetId assetId)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var updatedMetadata = new AssetMetadata(
                    asset.Metadata.Name,
                    asset.Metadata.Description,
                    asset.Metadata.AuthorName,
                    asset.Metadata.AssetType,
                    new List<string>(),
                    new List<string>(asset.Metadata.Dependencies),
                    asset.Metadata.CreatedDate,
                    DateTime.Now
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    updatedMetadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear tags for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットが指定されたタグを持っているかを判定します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="tag">タグ</param>
        /// <returns>タグを持っている場合true</returns>
        public static bool HasTag(AssetId assetId, string tag)
        {
            var asset = GetAsset(assetId);
            return asset?.HasTag(tag) ?? false;
        }

        #endregion

        #region Asset Dependency Operations

        /// <summary>
        /// アセットに依存関係を追加します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="dependency">追加する依存関係</param>
        /// <returns>追加に成功した場合true</returns>
        public static bool AddDependency(AssetId assetId, string dependency)
        {
            if (string.IsNullOrWhiteSpace(dependency)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var dependencies = new List<string>(asset.Metadata.Dependencies);
                var trimmedDep = dependency.Trim();
                if (dependencies.Contains(trimmedDep)) return true; // 既に存在する場合は成功とする

                dependencies.Add(trimmedDep);

                var updatedMetadata = new AssetMetadata(
                    asset.Metadata.Name,
                    asset.Metadata.Description,
                    asset.Metadata.AuthorName,
                    asset.Metadata.AssetType,
                    new List<string>(asset.Metadata.Tags),
                    dependencies,
                    asset.Metadata.CreatedDate,
                    DateTime.Now
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    updatedMetadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add dependency to asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットから依存関係を削除します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="dependency">削除する依存関係</param>
        /// <returns>削除に成功した場合true</returns>
        public static bool RemoveDependency(AssetId assetId, string dependency)
        {
            if (string.IsNullOrWhiteSpace(dependency)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var dependencies = new List<string>(asset.Metadata.Dependencies);
                if (!dependencies.Remove(dependency.Trim())) return true; // 存在しない場合は成功とする

                var updatedMetadata = new AssetMetadata(
                    asset.Metadata.Name,
                    asset.Metadata.Description,
                    asset.Metadata.AuthorName,
                    asset.Metadata.AssetType,
                    new List<string>(asset.Metadata.Tags),
                    dependencies,
                    asset.Metadata.CreatedDate,
                    DateTime.Now
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    updatedMetadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove dependency from asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットの依存関係をクリアします
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>クリアに成功した場合true</returns>
        public static bool ClearDependencies(AssetId assetId)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var updatedMetadata = new AssetMetadata(
                    asset.Metadata.Name,
                    asset.Metadata.Description,
                    asset.Metadata.AuthorName,
                    asset.Metadata.AssetType,
                    new List<string>(asset.Metadata.Tags),
                    new List<string>(),
                    asset.Metadata.CreatedDate,
                    DateTime.Now
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    updatedMetadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear dependencies for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットが指定された依存関係を持っているかを判定します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="dependency">依存関係</param>
        /// <returns>依存関係を持っている場合true</returns>
        public static bool HasDependency(AssetId assetId, string dependency)
        {
            var asset = GetAsset(assetId);
            return asset?.HasDependency(dependency) ?? false;
        }

        #endregion

        #region Asset File Operations

        /// <summary>
        /// アセットのファイル情報を更新します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="thumbnailPath">サムネイルパス</param>
        /// <param name="fileSizeBytes">ファイルサイズ</param>
        /// <returns>更新に成功した場合true</returns>
        public static bool UpdateFileInfo(AssetId assetId, string filePath = null, string thumbnailPath = null, long? fileSizeBytes = null)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var updatedFileInfo = new AssetFileInfo(
                    filePath ?? asset.FileInfo.FilePath,
                    thumbnailPath ?? asset.FileInfo.ThumbnailPath,
                    fileSizeBytes ?? asset.FileInfo.FileSizeBytes,
                    new List<string>(asset.FileInfo.ImportFiles)
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    asset.Metadata,
                    updatedFileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update file info for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットにインポートファイルを追加します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="filePath">追加するファイルパス</param>
        /// <returns>追加に成功した場合true</returns>
        public static bool AddImportFile(AssetId assetId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var importFiles = new List<string>(asset.FileInfo.ImportFiles);
                var trimmedPath = filePath.Trim();
                if (importFiles.Contains(trimmedPath)) return true; // 既に存在する場合は成功とする

                importFiles.Add(trimmedPath);

                var updatedFileInfo = new AssetFileInfo(
                    asset.FileInfo.FilePath,
                    asset.FileInfo.ThumbnailPath,
                    asset.FileInfo.FileSizeBytes,
                    importFiles
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    asset.Metadata,
                    updatedFileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add import file to asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットからインポートファイルを削除します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="filePath">削除するファイルパス</param>
        /// <returns>削除に成功した場合true</returns>
        public static bool RemoveImportFile(AssetId assetId, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var importFiles = new List<string>(asset.FileInfo.ImportFiles);
                if (!importFiles.Remove(filePath.Trim())) return true; // 存在しない場合は成功とする

                var updatedFileInfo = new AssetFileInfo(
                    asset.FileInfo.FilePath,
                    asset.FileInfo.ThumbnailPath,
                    asset.FileInfo.FileSizeBytes,
                    importFiles
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    asset.Metadata,
                    updatedFileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove import file from asset {assetId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Asset State Operations

        /// <summary>
        /// アセットの状態を更新します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="isFavorite">お気に入り状態</param>
        /// <param name="isGroup">グループ状態</param>
        /// <param name="isArchived">アーカイブ状態</param>
        /// <returns>更新に成功した場合true</returns>
        public static bool UpdateState(AssetId assetId, bool? isFavorite = null, bool? isGroup = null, bool? isArchived = null)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var updatedState = new AssetState(
                    isFavorite ?? asset.State.IsFavorite,
                    isGroup ?? asset.State.IsGroup,
                    isArchived ?? asset.State.IsArchived
                );

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    asset.Metadata,
                    asset.FileInfo,
                    updatedState,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update state for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Asset Group Operations

        /// <summary>
        /// アセットに子アセットを追加します
        /// </summary>
        /// <param name="assetId">親アセットID</param>
        /// <param name="childAssetId">子アセットID</param>
        /// <returns>追加に成功した場合true</returns>
        public static bool AddChildAsset(AssetId assetId, string childAssetId)
        {
            if (string.IsNullOrWhiteSpace(childAssetId)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var childAssetIds = new List<string>(asset.ChildAssetIds);
                var trimmedId = childAssetId.Trim();
                if (childAssetIds.Contains(trimmedId)) return true; // 既に存在する場合は成功とする

                childAssetIds.Add(trimmedId);

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    asset.Metadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    childAssetIds
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add child asset to asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットから子アセットを削除します
        /// </summary>
        /// <param name="assetId">親アセットID</param>
        /// <param name="childAssetId">子アセットID</param>
        /// <returns>削除に成功した場合true</returns>
        public static bool RemoveChildAsset(AssetId assetId, string childAssetId)
        {
            if (string.IsNullOrWhiteSpace(childAssetId)) return false;

            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var childAssetIds = new List<string>(asset.ChildAssetIds);
                if (!childAssetIds.Remove(childAssetId.Trim())) return true; // 存在しない場合は成功とする

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    asset.Metadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    childAssetIds
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove child asset from asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットの子アセットをクリアします
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>クリアに成功した場合true</returns>
        public static bool ClearChildAssets(AssetId assetId)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var updatedAsset = new AssetSchema(
                    asset.ParentGroupId,
                    asset.Metadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>()
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear child assets for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットが指定された子アセットを持っているかを判定します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="childAssetId">子アセットID</param>
        /// <returns>子アセットを持っている場合true</returns>
        public static bool HasChildAsset(AssetId assetId, string childAssetId)
        {
            var asset = GetAsset(assetId);
            return asset?.HasChildAsset(childAssetId) ?? false;
        }

        /// <summary>
        /// アセットの親グループを設定します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <param name="parentGroupId">親グループID</param>
        /// <returns>設定に成功した場合true</returns>
        public static bool SetParentGroup(AssetId assetId, string parentGroupId)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                var updatedAsset = new AssetSchema(
                    parentGroupId?.Trim() ?? string.Empty,
                    asset.Metadata,
                    asset.FileInfo,
                    asset.State,
                    asset.BoothItem,
                    asset.LastAccessed,
                    new List<string>(asset.ChildAssetIds)
                );

                library.AddAsset(assetId, updatedAsset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set parent group for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// アセットを親グループから削除します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>削除に成功した場合true</returns>
        public static bool RemoveFromParentGroup(AssetId assetId)
        {
            return SetParentGroup(assetId, string.Empty);
        }

        /// <summary>
        /// アセットの最終アクセス日時を更新します
        /// </summary>
        /// <param name="assetId">アセットID</param>
        /// <returns>更新に成功した場合true</returns>
        public static bool UpdateLastAccessed(AssetId assetId)
        {
            try
            {
                var library = AssetLibraryController.LoadLibrary();
                if (library == null) return false;

                var asset = library.GetAsset(assetId);
                if (asset == null) return false;

                asset.UpdateLastAccessed();
                library.AddAsset(assetId, asset);
                return AssetLibraryController.SaveLibrary(library);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update last accessed for asset {assetId}: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
