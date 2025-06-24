using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.Helper
{
    public static class ZipFileUtility
    {
        private static Dictionary<string, string> _tempExtractionDirs = new Dictionary<string, string>();

        /// <summary>
        /// zipファイルが有効かどうかチェック
        /// </summary>
        public static bool IsZipFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".zip";
        }

        /// <summary>
        /// zipファイル内のファイル一覧を取得
        /// </summary>
        public static List<string> GetZipFileList(string zipFilePath)
        {
            var fileList = new List<string>();

            try
            {
                string fullPath = GetFullPath(zipFilePath);
                if (!File.Exists(fullPath) || !IsZipFile(fullPath))
                {
                    return fileList;
                }

                // システムのTempディレクトリに一時展開
                string tempDir = ExtractZipToTemp(fullPath);
                if (string.IsNullOrEmpty(tempDir))
                {
                    return fileList;
                }

                // 展開されたファイル一覧を取得（日本語ファイル名も正しく取得される）
                string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    // tempDirからの相対パスを計算
                    string relativePath = Path.GetRelativePath(tempDir, file);
                    fileList.Add(relativePath.Replace('\\', '/'));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ZipFileUtility] Failed to read zip file {zipFilePath}: {ex.Message}");
            }

            return fileList;
        }

        /// <summary>
        /// zipファイルから指定されたファイルを展開
        /// </summary>
        public static bool ExtractFileFromZip(string zipFilePath, string entryPath, string outputPath)
        {
            try
            {
                string fullZipPath = GetFullPath(zipFilePath);
                if (!File.Exists(fullZipPath) || !IsZipFile(fullZipPath))
                {
                    Debug.LogError($"[ZipFileUtility] Zip file not found or invalid: {fullZipPath}");
                    return false;
                }

                // 一時展開ディレクトリから該当ファイルを検索
                string tempDir = GetTempExtractionDir(fullZipPath);
                if (string.IsNullOrEmpty(tempDir))
                {
                    Debug.LogError($"[ZipFileUtility] Failed to get temp extraction directory for: {fullZipPath}");
                    return false;
                }

                // パスの正規化とファイル検索
                string normalizedEntryPath = entryPath.Replace('/', Path.DirectorySeparatorChar);
                string sourceFile = Path.Combine(tempDir, normalizedEntryPath);

                // ファイルが見つからない場合、ディレクトリ内を再帰的に検索
                if (!File.Exists(sourceFile))
                {
                    string fileName = Path.GetFileName(entryPath);
                    var foundFiles = Directory.GetFiles(tempDir, fileName, SearchOption.AllDirectories);

                    if (foundFiles.Length > 0)
                    {
                        sourceFile = foundFiles[0]; // 最初に見つかったファイルを使用
                        Debug.Log($"[ZipFileUtility] Found file at alternative path: {sourceFile}");
                    }
                    else
                    {
                        Debug.LogError($"[ZipFileUtility] Source file not found in temp directory: {sourceFile}");
                        return false;
                    }
                }

                // 出力ディレクトリが存在しない場合は作成
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // ファイルをコピー
                File.Copy(sourceFile, outputPath, true);
                Debug.Log($"[ZipFileUtility] Successfully extracted file: {entryPath} -> {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ZipFileUtility] Failed to extract file {entryPath} from {zipFilePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// VrcAssetManager用の展開先ディレクトリを取得
        /// </summary>
        public static string GetUnzipDirectory()
        {
            string coreDir = GetCoreDirectory();
            string unzipDir = Path.Combine(coreDir, "VrcAssetManager", "Unzip");

            if (!Directory.Exists(unzipDir))
            {
                Directory.CreateDirectory(unzipDir);
            }

            return unzipDir;
        }

        /// <summary>
        /// 相対パスから絶対パスを取得
        /// </summary>
        public static string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            string coreDir = GetCoreDirectory();
            return Path.Combine(coreDir, relativePath);
        }

        /// <summary>
        /// Coreディレクトリを取得
        /// </summary>
        private static string GetCoreDirectory()
        {
            return SettingAPI.GetSetting<string>("Core_dirPath");
        }

        /// <summary>
        /// zipファイルを一時ディレクトリに展開
        /// </summary>
        private static string ExtractZipToTemp(string zipFilePath)
        {
            try
            {
                // 既に展開済みの場合はそのパスを返す
                if (_tempExtractionDirs.TryGetValue(zipFilePath, out string existingTempDir))
                {
                    if (Directory.Exists(existingTempDir))
                    {
                        Debug.Log($"[ZipFileUtility] Using existing temp directory: {existingTempDir}");
                        return existingTempDir;
                    }
                    else
                    {
                        _tempExtractionDirs.Remove(zipFilePath);
                    }
                }

                // 新しい一時ディレクトリを作成
                string tempDir = Path.Combine(Path.GetTempPath(), "VrcAssetManager_ZipExtract", Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                Debug.Log($"[ZipFileUtility] Created temp directory: {tempDir}");

                int extractedCount = 0;

                // Shift_JISエンコーディングでZIPファイルを展開
                using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, false, Encoding.GetEncoding("Shift_JIS")))
                    {
                        Debug.Log($"[ZipFileUtility] Archive contains {archive.Entries.Count} entries");

                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                // ディレクトリエントリをスキップ
                                continue;
                            }

                            try
                            {
                                string entryPath = Path.Combine(tempDir, entry.FullName);
                                string entryDir = Path.GetDirectoryName(entryPath);

                                if (!Directory.Exists(entryDir))
                                {
                                    Directory.CreateDirectory(entryDir);
                                }

                                entry.ExtractToFile(entryPath, true);
                                extractedCount++;
                                Debug.Log($"[ZipFileUtility] Extracted: {entry.FullName} -> {entryPath}");
                            }
                            catch (Exception entryEx)
                            {
                                Debug.LogWarning($"[ZipFileUtility] Failed to extract entry {entry.FullName}: {entryEx.Message}");
                            }
                        }
                    }
                }
                Debug.Log($"[ZipFileUtility] Successfully extracted {extractedCount} files to temp directory");
                _tempExtractionDirs[zipFilePath] = tempDir;
                return tempDir;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ZipFileUtility] Failed to extract zip to temp: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 一時展開ディレクトリを取得
        /// </summary>
        private static string GetTempExtractionDir(string zipFilePath)
        {
            if (_tempExtractionDirs.TryGetValue(zipFilePath, out string tempDir))
            {
                if (Directory.Exists(tempDir))
                {
                    return tempDir;
                }
                else
                {
                    _tempExtractionDirs.Remove(zipFilePath);
                }
            }

            // まだ展開されていない場合は展開を実行
            return ExtractZipToTemp(zipFilePath);
        }

        /// <summary>
        /// 一時展開ディレクトリをクリーンアップ
        /// </summary>
        public static void CleanupTempExtraction(string zipFilePath)
        {
            if (_tempExtractionDirs.TryGetValue(zipFilePath, out string tempDir))
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ZipFileUtility] Failed to cleanup temp directory {tempDir}: {ex.Message}");
                }
                _tempExtractionDirs.Remove(zipFilePath);
            }
        }

        /// <summary>
        /// 全ての一時展開ディレクトリをクリーンアップ
        /// </summary>
        public static void CleanupAllTempExtractions()
        {
            foreach (var kvp in _tempExtractionDirs.Keys.ToList())
            {
                CleanupTempExtraction(kvp);
            }
        }
    }
}
