using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    /// <summary>
    /// AssetLibraryのJSONファイルの読み書きを担当するコントローラ
    /// </summary>
    public static class AssetLibraryController
    {
        /// <summary>
        /// デフォルトのAssetLibraryファイルパス
        /// </summary>
        public static string DefaultLibraryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "AssetLibrary.json"));

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
        /// AssetLibraryをJSONファイルに保存します
        /// </summary>
        /// <param name="library">保存するAssetLibrarySchema</param>
        /// <param name="filePath">保存先ファイルパス</param>
        /// <returns>保存に成功した場合true</returns>
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
                // ディレクトリが存在しない場合は作成
                var directory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // ライブラリの最終更新日時を設定
                library.LastUpdated = DateTime.Now;

                // JSONシリアライズ
                var json = JsonConvert.SerializeObject(library, Formatting.Indented, new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateTimeZoneHandling = DateTimeZoneHandling.Local
                });

                // ファイルに書き込み
                File.WriteAllText(targetPath, json);

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_librarySaved"), targetPath));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_librarySaveFailed"), targetPath, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// JSONファイルからAssetLibraryを読み込みます
        /// </summary>
        /// <param name="filePath">読み込み元ファイルパス</param>
        /// <returns>読み込んだAssetLibrarySchema、失敗時はnull</returns>
        public static AssetLibrarySchema LoadLibrary(string filePath = null)
        {
            var targetPath = filePath ?? DefaultLibraryPath;

            try
            {
                if (!File.Exists(targetPath))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_libraryFileNotFound"), targetPath));
                    return CreateNewLibrary();
                }

                // ファイルを読み込み
                var json = File.ReadAllText(targetPath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning(string.Format(LocalizationController.GetText("VrcAssetManager_message_warning_emptyLibraryFile"), targetPath));
                    return CreateNewLibrary();
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
                    return CreateNewLibrary();
                }

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_libraryLoaded"), targetPath, library.AssetCount, library.GroupCount));
                return library;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_libraryLoadFailed"), targetPath, ex.Message));
                return CreateNewLibrary();
            }
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
        /// ライブラリファイルのバリデーションを行います
        /// </summary>
        /// <param name="filePath">検証するファイルパス</param>
        /// <returns>有効なライブラリファイルの場合true</returns>
        public static bool ValidateLibraryFile(string filePath = null)
        {
            var targetPath = filePath ?? DefaultLibraryPath;

            try
            {
                if (!File.Exists(targetPath))
                {
                    return false;
                }

                var json = File.ReadAllText(targetPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                // JSON構文の検証
                var library = JsonConvert.DeserializeObject<AssetLibrarySchema>(json);
                return library != null;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_libraryValidationFailed"), targetPath, ex.Message));
                return false;
            }
        }
    }
}
