using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    /// <summary>
    /// AssetLibraryのJSONファイルの読み書きを担当するコントローラ
    /// ライブラリ全体をメモリにキャッシュしてファイルIOを削減します
    /// </summary>
    public static class AssetLibraryController
    {
        // キャッシュ関連
        private static AssetLibrarySchema _cachedLibrary = null;
        private static string _cachedFilePath = null;
        private static DateTime _cachedFileLastWrite = DateTime.MinValue;
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// デフォルトのAssetLibraryファイルパス
        /// EditorPrefsのCoreDir設定を使用します
        /// </summary>
        public static string DefaultLibraryPath
        {
            get
            {
                string coreDir = UnityEditor.EditorPrefs.GetString("Setting.Core_dirPath",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
                return Path.Combine(coreDir, "VrcAssetManager", "VrcAssetLibrary.json");
            }
        }

        /// <summary>
        /// キャッシュをクリアします
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _cachedLibrary = null;
                _cachedFilePath = null;
                _cachedFileLastWrite = DateTime.MinValue;
                Debug.Log(LocalizationController.GetText("VrcAssetManager_message_success_cacheCleared"));
            }
        }

        /// <summary>
        /// 指定されたファイルがキャッシュされているかを確認します
        /// </summary>
        /// <param name="filePath">確認するファイルパス</param>
        /// <returns>キャッシュされている場合true</returns>
        public static bool IsCached(string filePath = null)
        {
            var targetPath = filePath ?? DefaultLibraryPath;
            lock (_cacheLock)
            {
                return _cachedLibrary != null &&
                       _cachedFilePath == targetPath &&
                       IsFileUnchanged(targetPath);
            }
        }

        /// <summary>
        /// ファイルが変更されていないかを確認します
        /// </summary>
        /// <param name="filePath">確認するファイルパス</param>
        /// <returns>変更されていない場合true</returns>
        private static bool IsFileUnchanged(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return _cachedFileLastWrite == DateTime.MinValue;
                }

                var fileInfo = new FileInfo(filePath);
                return fileInfo.LastWriteTime <= _cachedFileLastWrite;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// キャッシュを更新します
        /// </summary>
        /// <param name="library">キャッシュするライブラリ</param>
        /// <param name="filePath">ファイルパス</param>
        private static void UpdateCache(AssetLibrarySchema library, string filePath)
        {
            lock (_cacheLock)
            {
                _cachedLibrary = library;
                _cachedFilePath = filePath;

                try
                {
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        _cachedFileLastWrite = fileInfo.LastWriteTime;
                    }
                    else
                    {
                        _cachedFileLastWrite = DateTime.Now;
                    }
                }
                catch
                {
                    _cachedFileLastWrite = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// 新しいAssetLibraryを作成します
        /// </summary>
        /// <returns>新しいAssetLibrarySchema</returns>
        public static AssetLibrarySchema CreateNewLibrary()
        {
            try
            {
                var library = new AssetLibrarySchema();
                Debug.Log(LocalizationController.GetText("VrcAssetManager_message_success_libraryCreated"));
                return library;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_libraryCreationFailed"), ex.Message));
                return null;
            }
        }

        /// <summary>
        /// JSONファイルからAssetLibraryを読み込みます
        /// キャッシュが有効な場合はキャッシュから返します
        /// </summary>
        /// <param name="filePath">読み込み元ファイルパス</param>
        /// <returns>読み込んだAssetLibrarySchema、失敗時はnull</returns>
        public static AssetLibrarySchema LoadLibrary(string filePath = null)
        {
            var targetPath = filePath ?? DefaultLibraryPath;

            // キャッシュが有効な場合はキャッシュから返す
            lock (_cacheLock)
            {
                if (_cachedLibrary != null &&
                    _cachedFilePath == targetPath &&
                    IsFileUnchanged(targetPath))
                {
                    Debug.Log(string.Format("Library loaded from cache: {0}", targetPath));
                    return _cachedLibrary;
                }
            }

            try
            {
                if (!File.Exists(targetPath))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_libraryFileNotFound"), targetPath));
                    var newLibrary = CreateNewLibrary();
                    if (newLibrary != null)
                    {
                        UpdateCache(newLibrary, targetPath);
                    }
                    return newLibrary;
                }

                // ファイルを読み込み
                var json = File.ReadAllText(targetPath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_emptyLibraryFile"), targetPath));
                    var newLibrary = CreateNewLibrary();
                    if (newLibrary != null)
                    {
                        UpdateCache(newLibrary, targetPath);
                    }
                    return newLibrary;
                }

                // JSONデシリアライズ
                var library = JsonConvert.DeserializeObject<AssetLibrarySchema>(json, new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Local
                });

                if (library == null)
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_libraryDeserializeFailed"), targetPath));
                    var newLibrary = CreateNewLibrary();
                    if (newLibrary != null)
                    {
                        UpdateCache(newLibrary, targetPath);
                    }
                    return newLibrary;
                }

                // キャッシュを更新
                UpdateCache(library, targetPath);

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_libraryLoaded"), targetPath, library.AssetCount));
                return library;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_libraryLoadFailed"), targetPath, ex.Message));
                var newLibrary = CreateNewLibrary();
                if (newLibrary != null)
                {
                    UpdateCache(newLibrary, targetPath);
                }
                return newLibrary;
            }
        }

        /// <summary>
        /// キャッシュを無視してライブラリを強制的に再読み込みします
        /// </summary>
        /// <param name="filePath">読み込み元ファイルパス</param>
        /// <returns>読み込んだAssetLibrarySchema、失敗時はnull</returns>
        public static AssetLibrarySchema ForceReloadLibrary(string filePath = null)
        {
            var targetPath = filePath ?? DefaultLibraryPath;

            // キャッシュをクリア
            lock (_cacheLock)
            {
                if (_cachedFilePath == targetPath)
                {
                    _cachedLibrary = null;
                    _cachedFilePath = null;
                    _cachedFileLastWrite = DateTime.MinValue;
                }
            }

            Debug.Log(string.Format("Force reloading library: {0}", targetPath));
            return LoadLibrary(targetPath);
        }

        /// <summary>
        /// ライブラリファイルが存在するかを確認します
        /// </summary>
        /// <param name="filePath">確認するファイルパス</param>
        /// <returns>ファイルが存在する場合true</returns>
        public static bool LibraryFileExists(string filePath = null)
        {
            var targetPath = filePath ?? DefaultLibraryPath;
            return File.Exists(targetPath);
        }

        /// <summary>
        /// ライブラリファイルの情報を取得します
        /// </summary>
        /// <param name="filePath">対象ファイルパス</param>
        /// <returns>ファイル情報、存在しない場合はnull</returns>
        public static FileInfo GetLibraryFileInfo(string filePath = null)
        {
            var targetPath = filePath ?? DefaultLibraryPath;

            if (!File.Exists(targetPath))
            {
                return null;
            }

            try
            {
                return new FileInfo(targetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_fileInfoFailed"), targetPath, ex.Message));
                return null;
            }
        }

        /// <summary>
        /// AssetLibraryをJSONファイルに非同期で保存します
        /// キャッシュは即座に更新され、ファイル書き込みはバックグラウンドで実行されます
        /// </summary>
        /// <param name="library">保存するAssetLibrarySchema</param>
        /// <param name="filePath">保存先ファイルパス</param>
        /// <returns>保存処理を開始できた場合true</returns>
        public static bool SaveLibrary(AssetLibrarySchema library, string filePath = null)
        {
            if (library == null)
            {
                Debug.LogError(LocalizationController.GetText("VrcAssetManager_message_error_libraryNull"));
                return false;
            }

            var targetPath = filePath ?? DefaultLibraryPath;

            try
            {
                // ライブラリの最終更新日時を設定
                library.LastUpdated = DateTime.Now;

                // キャッシュを即座に更新
                UpdateCache(library, targetPath);

                // 非同期で保存
                var saveTask = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        // ディレクトリが存在しない場合は作成
                        var directory = Path.GetDirectoryName(targetPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // JSONシリアライズ
                        var json = JsonConvert.SerializeObject(library, Formatting.Indented, new JsonSerializerSettings
                        {
                            DateFormatHandling = DateFormatHandling.IsoDateFormat,
                            DateTimeZoneHandling = DateTimeZoneHandling.Local
                        });

                        // ファイルに書き込み
                        File.WriteAllText(targetPath, json);

                        UnityEngine.Debug.Log(string.Format("Library saved asynchronously: {0}", targetPath));
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError(string.Format("Failed to save library asynchronously: {0} - {1}", targetPath, ex.Message));
                    }
                });

                Debug.Log(string.Format("Library save initiated asynchronously: {0}", targetPath));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_librarySaveFailed"), targetPath, ex.Message));
                return false;
            }
        }

        #region タグ管理

        /// <summary>
        /// ライブラリにタグを追加します
        /// </summary>
        /// <param name="tag">追加するタグ</param>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>追加に成功した場合はtrue</returns>
        public static bool AddTag(string tag, string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var result = library.InternalAddTag(tag);
            if (result)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("Tag added to library: {0}", tag));
            }
            return result;
        }

        /// <summary>
        /// ライブラリからタグを削除します
        /// </summary>
        /// <param name="tag">削除するタグ</param>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>削除に成功した場合はtrue</returns>
        public static bool RemoveTag(string tag, string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var result = library.InternalRemoveTag(tag);
            if (result)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("Tag removed from library: {0}", tag));
            }
            return result;
        }

        /// <summary>
        /// ライブラリにアセットタイプを追加します
        /// </summary>
        /// <param name="assetType">追加するアセットタイプ</param>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>追加に成功した場合はtrue</returns>
        public static bool AddAssetType(string assetType, string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var result = library.InternalAddAssetType(assetType);
            if (result)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("AssetType added to library: {0}", assetType));
            }
            return result;
        }

        /// <summary>
        /// ライブラリからアセットタイプを削除します
        /// </summary>
        /// <param name="assetType">削除するアセットタイプ</param>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>削除に成功した場合はtrue</returns>
        public static bool RemoveAssetType(string assetType, string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var result = library.InternalRemoveAssetType(assetType);
            if (result)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("AssetType removed from library: {0}", assetType));
            }
            return result;
        }

        /// <summary>
        /// ライブラリのすべてのタグをクリアします
        /// </summary>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>クリアに成功した場合はtrue</returns>
        public static bool ClearTags(string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            library.InternalClearTags();
            SaveLibrary(library, filePath);
            Debug.Log("All tags cleared from library");
            return true;
        }

        /// <summary>
        /// ライブラリのすべてのアセットタイプをクリアします
        /// </summary>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>クリアに成功した場合はtrue</returns>
        public static bool ClearAssetTypes(string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            library.InternalClearAssetTypes();
            SaveLibrary(library, filePath);
            Debug.Log("All asset types cleared from library");
            return true;
        }

        #endregion

        #region 同期・最適化機能

        /// <summary>
        /// アセット内で使用されているタグを収集してライブラリのタグリストに自動追加します
        /// </summary>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>同期に成功した場合はtrue</returns>
        public static bool SynchronizeTagsFromAssets(string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var initialTagCount = library.TagsCount;
            var newTags = new HashSet<string>();

            foreach (var asset in library.Assets.Values)
            {
                foreach (var tag in asset.Metadata.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag) && !library.HasTag(tag))
                    {
                        newTags.Add(tag.Trim());
                    }
                }
            }

            foreach (var tag in newTags)
            {
                library.InternalAddTag(tag);
            }

            if (newTags.Count > 0)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("Synchronized {0} tags from assets to library", newTags.Count));
            }

            return true;
        }

        /// <summary>
        /// アセット内で使用されているアセットタイプを収集してライブラリのアセットタイプリストに自動追加します
        /// </summary>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>同期に成功した場合はtrue</returns>
        public static bool SynchronizeAssetTypesFromAssets(string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var newAssetTypes = new HashSet<string>();

            foreach (var asset in library.Assets.Values)
            {
                var assetType = asset.Metadata.AssetType;
                if (!string.IsNullOrWhiteSpace(assetType) && !library.HasAssetType(assetType))
                {
                    newAssetTypes.Add(assetType.Trim());
                }
            }

            foreach (var assetType in newAssetTypes)
            {
                library.InternalAddAssetType(assetType);
            }

            if (newAssetTypes.Count > 0)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("Synchronized {0} asset types from assets to library", newAssetTypes.Count));
            }

            return true;
        }

        /// <summary>
        /// 未使用のタグをライブラリから削除します
        /// </summary>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>クリーンアップに成功した場合はtrue</returns>
        public static bool CleanupUnusedTags(string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var usedTags = new HashSet<string>();
            foreach (var asset in library.Assets.Values)
            {
                foreach (var tag in asset.Metadata.Tags)
                {
                    usedTags.Add(tag);
                }
            }

            var tagsToRemove = library.Tags.Where(tag => !usedTags.Contains(tag)).ToList();
            foreach (var tag in tagsToRemove)
            {
                library.InternalRemoveTag(tag);
            }

            if (tagsToRemove.Count > 0)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("Cleaned up {0} unused tags from library", tagsToRemove.Count));
            }

            return true;
        }

        /// <summary>
        /// 未使用のアセットタイプをライブラリから削除します
        /// </summary>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>クリーンアップに成功した場合はtrue</returns>
        public static bool CleanupUnusedAssetTypes(string filePath = null)
        {
            var library = LoadLibrary(filePath);
            if (library == null) return false;

            var usedAssetTypes = new HashSet<string>();
            foreach (var asset in library.Assets.Values)
            {
                if (!string.IsNullOrWhiteSpace(asset.Metadata.AssetType))
                {
                    usedAssetTypes.Add(asset.Metadata.AssetType);
                }
            }

            var assetTypesToRemove = library.AssetTypes.Where(assetType => !usedAssetTypes.Contains(assetType)).ToList();
            foreach (var assetType in assetTypesToRemove)
            {
                library.InternalRemoveAssetType(assetType);
            }

            if (assetTypesToRemove.Count > 0)
            {
                SaveLibrary(library, filePath);
                Debug.Log(string.Format("Cleaned up {0} unused asset types from library", assetTypesToRemove.Count));
            }

            return true;
        }

        /// <summary>
        /// ライブラリを最適化します（アセットからの同期と未使用項目のクリーンアップを実行）
        /// </summary>
        /// <param name="filePath">対象ライブラリファイルパス</param>
        /// <returns>最適化に成功した場合はtrue</returns>
        public static bool OptimizeLibrary(string filePath = null)
        {
            try
            {
                Debug.Log("Starting library optimization...");

                // アセットからタグとアセットタイプを同期
                SynchronizeTagsFromAssets(filePath);
                SynchronizeAssetTypesFromAssets(filePath);

                // 未使用のタグとアセットタイプを削除
                CleanupUnusedTags(filePath);
                CleanupUnusedAssetTypes(filePath);

                var library = LoadLibrary(filePath);
                if (library != null)
                {
                    library.Optimize();
                    SaveLibrary(library, filePath);
                }

                Debug.Log("Library optimization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Library optimization failed: {0}", ex.Message));
                return false;
            }
        }

        #endregion
    }
}
