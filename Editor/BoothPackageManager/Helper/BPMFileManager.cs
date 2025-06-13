using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AMU.BoothPackageManager.Helper
{
    public class BPMFileManager
    {
        private Dictionary<string, bool> fileExistenceCache = new Dictionary<string, bool>();
        private HashSet<string> ensuredDirectories = new HashSet<string>();

        public bool IsFileExistsCached(string filePath)
        {
            if (fileExistenceCache.TryGetValue(filePath, out bool exists))
            {
                return exists;
            }

            bool fileExists = File.Exists(filePath);
            fileExistenceCache[filePath] = fileExists;
            return fileExists;
        }

        public void UpdateFileExistenceCache(BPMLibrary bpmLibrary)
        {
            fileExistenceCache.Clear();

            if (bpmLibrary?.authors == null) return;

            foreach (var authorKvp in bpmLibrary.authors)
            {
                foreach (var package in authorKvp.Value)
                {
                    if (package.files != null)
                    {
                        string fileDir = BPMPathManager.GetFileDirectory(authorKvp.Key, package.itemUrl);

                        foreach (var file in package.files)
                        {
                            string filePath = Path.Combine(fileDir, file.fileName);
                            fileExistenceCache[filePath] = File.Exists(filePath);
                        }
                    }
                }
            }
        }

        public void UpdateSingleFileExistenceCache(string filePath)
        {
            fileExistenceCache[filePath] = File.Exists(filePath);
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (ensuredDirectories.Contains(directoryPath)) return;

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"ディレクトリを作成しました: {directoryPath}");
            }
            ensuredDirectories.Add(directoryPath);
        }

        public Task CheckAndMoveImportFilesAsync(BPMDataManager dataManager)
        {
            if (dataManager.Library == null) return Task.CompletedTask;

            var searchDirectories = new List<string>();

            // Importディレクトリを常に検索対象に含める
            string importDir = BPMPathManager.GetImportDirectory();
            if (Directory.Exists(importDir))
            {
                searchDirectories.Add(importDir);
            }

            // 設定でDownloadフォルダ検索が有効な場合、Downloadディレクトリも検索対象に含める
            bool searchDownloadFolder = EditorPrefs.GetBool("Setting.BPM_searchDownloadFolder", false);
            if (searchDownloadFolder)
            {
                string downloadDir = BPMPathManager.GetDownloadDirectory();
                if (Directory.Exists(downloadDir))
                {
                    searchDirectories.Add(downloadDir);
                }
            }

            if (searchDirectories.Count == 0) return Task.CompletedTask;

            foreach (string searchDir in searchDirectories)
            {
                var allFiles = Directory.GetFiles(searchDir, "*", SearchOption.AllDirectories);

                foreach (var filePath in allFiles)
                {
                    string fileName = Path.GetFileName(filePath);

                    // BPMlibrary.jsonファイルはスキップ
                    if (fileName.Equals("BPMlibrary.json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // データベース内でファイル名が一致するものを探す
                    var matchedFile = dataManager.FindMatchingFileInDatabase(fileName);
                    if (matchedFile.author != null && matchedFile.package != null)
                    {
                        try
                        {
                            string targetDir = BPMPathManager.GetFileDirectory(matchedFile.author, matchedFile.package.itemUrl);
                            EnsureDirectoryExists(targetDir);

                            string targetPath = Path.Combine(targetDir, fileName);

                            // ファイルが既に存在する場合はスキップ
                            if (File.Exists(targetPath))
                            {
                                Debug.Log($"ファイルは既に存在するためスキップしました: {targetPath}");
                                continue;
                            }

                            // ファイルを移動
                            File.Move(filePath, targetPath);
                            Debug.Log($"ファイルを移動しました: {fileName} -> {targetPath}");

                            // キャッシュを更新
                            UpdateSingleFileExistenceCache(targetPath);

                            string sourceFolder = searchDir == BPMPathManager.GetDownloadDirectory() ? "Downloadフォルダ" : "Importフォルダ";
                            EditorUtility.DisplayDialog("ファイル移動完了",
                                $"{sourceFolder}からファイルを移動しました:\n{fileName}\n↓\n{targetPath}", "OK");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"ファイル移動エラー: {fileName}, エラー: {ex.Message}");
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void ClearCaches()
        {
            fileExistenceCache.Clear();
            ensuredDirectories.Clear();
        }
    }
}
