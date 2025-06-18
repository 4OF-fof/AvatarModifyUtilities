using System;
using System.IO;
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

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_libraryLoaded"), targetPath, library.AssetCount, library.GroupCount));
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
    }
}
