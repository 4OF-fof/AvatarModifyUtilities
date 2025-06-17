using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using System.Security.Cryptography;

namespace AMU.AssetManager.Helper
{
    /// <summary>
    /// BPMライブラリファイルの読み込み機能を提供する独立クラス
    /// BoothPackageManagerに依存せずにBPMLibraryからのインポートをサポート
    /// </summary>
    public class BPMHelper
    {
        /// <summary>
        /// 指定されたパスからBPMLibraryを読み込む
        /// </summary>
        public static async Task<Data.BPMLibrary> LoadBPMLibraryAsync(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            {
                return null;
            }

            try
            {
                string json = await ReadFileAsync(jsonPath);
                var settings = new JsonSerializerSettings
                {
                    CheckAdditionalContent = false,
                    DateParseHandling = DateParseHandling.None
                };
                return await Task.Run(() => JsonConvert.DeserializeObject<Data.BPMLibrary>(json, settings));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load BPM Library from {jsonPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// BPMLibraryファイルを検索する
        /// </summary>
        public static List<string> FindBPMLibraryFiles()
        {
            var files = new List<string>();

            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            // 主要なBPMlibrary.jsonファイル
            string mainJsonPath = Path.Combine(coreDir, "BPM", "BPMlibrary.json");
            if (File.Exists(mainJsonPath))
            {
                files.Add(mainJsonPath);
            }

            // Importディレクトリ内のBPMlibrary.jsonファイル
            string importDir = Path.Combine(coreDir, "Import");
            if (Directory.Exists(importDir))
            {
                var importFiles = Directory.GetFiles(importDir, "BPMlibrary.json", SearchOption.AllDirectories);
                files.AddRange(importFiles);
            }

            // Downloadディレクトリ内のBPMlibrary.jsonファイル
            string downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadDir))
            {
                var downloadFiles = Directory.GetFiles(downloadDir, "BPMlibrary.json", SearchOption.TopDirectoryOnly);
                files.AddRange(downloadFiles);
            }

            return files;
        }

        /// <summary>
        /// 最新のBPMLibraryファイルを見つける
        /// </summary>
        public static async Task<(string filePath, Data.BPMLibrary library)> FindLatestBPMLibraryAsync()
        {
            var candidateFiles = FindBPMLibraryFiles();
            if (candidateFiles.Count == 0)
            {
                return (null, null);
            }

            Data.BPMLibrary latestLibrary = null;
            string latestFilePath = null;
            string latestUpdated = null;

            foreach (string filePath in candidateFiles)
            {
                try
                {
                    var library = await LoadBPMLibraryAsync(filePath);
                    if (library != null)
                    {
                        if (latestLibrary == null ||
                            string.Compare(library.lastUpdated, latestUpdated, StringComparison.Ordinal) > 0)
                        {
                            latestLibrary = library;
                            latestFilePath = filePath;
                            latestUpdated = library.lastUpdated;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to load BPM Library from {filePath}: {ex.Message}");
                }
            }

            return (latestFilePath, latestLibrary);
        }

        /// <summary>
        /// Booth画像のハッシュを生成
        /// </summary>
        public static string GetImageHash(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Boothアイテムのサムネイルディレクトリのパスを取得
        /// </summary>
        public static string GetBoothThumbnailDirectory()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "AssetManager", "BoothItem", "Thumbnail");
        }

        /// <summary>
        /// ファイルを非同期で読み込む
        /// </summary>
        private static async Task<string> ReadFileAsync(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// BPMPackageからファイル名で検索
        /// </summary>
        public static (string author, Data.BPMPackage package) FindPackageByFileName(Data.BPMLibrary library, string fileName)
        {
            if (library?.authors == null || string.IsNullOrEmpty(fileName))
                return (null, null);

            foreach (var authorKvp in library.authors)
            {
                foreach (var package in authorKvp.Value)
                {
                    if (package.files != null)
                    {
                        foreach (var file in package.files)
                        {
                            if (string.Equals(file.fileName, fileName, StringComparison.OrdinalIgnoreCase))
                            {
                                return (authorKvp.Key, package);
                            }
                        }
                    }
                }
            }
            return (null, null);
        }
    }
}
