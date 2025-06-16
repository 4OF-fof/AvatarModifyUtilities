using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;

namespace AMU.AssetManager.Helper
{
    /// <summary>
    /// ダウンロードフォルダを監視して、boothfileNameと一致するファイルを自動的に移動・登録するクラス
    /// </summary>
    public class DownloadFolderWatcher : IDisposable
    {
        private FileSystemWatcher _fileWatcher;
        private AssetDataManager _assetDataManager;
        private AssetFileManager _fileManager;
        private readonly HashSet<string> _processedFiles = new HashSet<string>();
        private bool _isEnabled = false;
        private string _downloadFolderPath;

        public event Action<string, string> OnFileProcessed; // fileName, targetPath

        public DownloadFolderWatcher(AssetDataManager assetDataManager, AssetFileManager fileManager)
        {
            _assetDataManager = assetDataManager;
            _fileManager = fileManager;
            _downloadFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            // 設定変更を監視
            UpdateWatcherState();
        }

        /// <summary>
        /// 設定に基づいて監視状態を更新
        /// </summary>
        public void UpdateWatcherState()
        {
            bool shouldEnable = EditorPrefs.GetBool("Setting.AssetManager_watchDownloadFolder", false);
            
            if (shouldEnable && !_isEnabled)
            {
                StartWatching();
            }
            else if (!shouldEnable && _isEnabled)
            {
                StopWatching();
            }
        }

        /// <summary>
        /// ファイル監視を開始
        /// </summary>
        private void StartWatching()
        {
            if (_isEnabled || !Directory.Exists(_downloadFolderPath))
                return;

            try
            {
                _fileWatcher = new FileSystemWatcher(_downloadFolderPath)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false, // サブディレクトリは監視しない
                    EnableRaisingEvents = true
                };

                _fileWatcher.Created += OnFileCreated;
                _fileWatcher.Renamed += OnFileRenamed;

                _isEnabled = true;
                Debug.Log($"[DownloadFolderWatcher] Started watching: {_downloadFolderPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcher] Failed to start watching: {ex.Message}");
            }
        }

        /// <summary>
        /// ファイル監視を停止
        /// </summary>
        private void StopWatching()
        {
            if (!_isEnabled)
                return;

            try
            {
                _fileWatcher?.Dispose();
                _fileWatcher = null;
                _isEnabled = false;
                _processedFiles.Clear();
                Debug.Log("[DownloadFolderWatcher] Stopped watching download folder");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcher] Error stopping watcher: {ex.Message}");
            }
        }

        /// <summary>
        /// ファイル作成イベント
        /// </summary>
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            EditorApplication.delayCall += () => ProcessFile(e.FullPath);
        }

        /// <summary>
        /// ファイル名変更イベント（ダウンロード完了時に発生することがある）
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            EditorApplication.delayCall += () => ProcessFile(e.FullPath);
        }

        /// <summary>
        /// ファイルを処理する
        /// </summary>
        private void ProcessFile(string filePath)
        {
            if (!_isEnabled || _assetDataManager?.Library == null)
                return;

            try
            {
                string fileName = Path.GetFileName(filePath);

                // 既に処理済みのファイルはスキップ
                if (_processedFiles.Contains(fileName))
                    return;

                // ファイルが存在しない場合（削除された等）はスキップ
                if (!File.Exists(filePath))
                    return;

                // 一時ファイルなどはスキップ
                if (IsTemporaryFile(fileName))
                    return;

                // boothfileNameと一致するアセットを検索
                var matchingAsset = FindAssetByBoothFileName(fileName);
                if (matchingAsset == null)
                    return;

                // 既にファイルパスが設定されている場合はスキップ
                if (!string.IsNullOrEmpty(matchingAsset.filePath))
                {
                    Debug.Log($"[DownloadFolderWatcher] Asset '{matchingAsset.name}' already has a file path, skipping: {matchingAsset.filePath}");
                    _processedFiles.Add(fileName);
                    return;
                }

                // 移動先パスを生成
                string targetPath = GenerateTargetPath(matchingAsset, fileName);
                if (string.IsNullOrEmpty(targetPath))
                    return;

                // 既にファイルが存在する場合はスキップ
                if (File.Exists(targetPath))
                {
                    Debug.Log($"[DownloadFolderWatcher] File already exists, skipping: {targetPath}");
                    _processedFiles.Add(fileName);
                    return;
                }

                // ファイルが完全にダウンロードされるまで少し待機
                if (!WaitForFileCompletion(filePath))
                    return;

                // ファイルを移動
                MoveFileToTarget(filePath, targetPath, matchingAsset);

                _processedFiles.Add(fileName);

            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcher] Error processing file {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// boothfileNameからアセットを検索
        /// </summary>
        private AssetInfo FindAssetByBoothFileName(string fileName)
        {
            return _assetDataManager.GetAssetByBoothFileName(fileName);
        }

        /// <summary>
        /// 移動先パスを生成
        /// </summary>
        private string GenerateTargetPath(AssetInfo asset, string fileName)
        {
            try
            {
                string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
                
                string packageDir = Path.Combine(coreDir, "AssetManager", "BoothItem", "package");
                
                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(packageDir))
                {
                    Directory.CreateDirectory(packageDir);
                }

                return Path.Combine(packageDir, fileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcher] Error generating target path: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ファイルを移動してアセットに登録
        /// </summary>
        private void MoveFileToTarget(string sourcePath, string targetPath, AssetInfo asset)
        {
            try
            {
                // ファイルを移動
                File.Move(sourcePath, targetPath);
                Debug.Log($"[DownloadFolderWatcher] Moved file: {sourcePath} -> {targetPath}");

                // アセットにファイルパスを設定
                string relativePath = GetRelativePath(targetPath);
                asset.filePath = relativePath;
                
                // ファイルサイズを更新
                if (_fileManager != null)
                {
                    asset.fileSize = _fileManager.GetFileSize(relativePath);
                }

                // データを保存
                _assetDataManager.SaveData();
                
                Debug.Log($"[DownloadFolderWatcher] Set file path for asset '{asset.name}': {relativePath}");

                OnFileProcessed?.Invoke(Path.GetFileName(sourcePath), targetPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcher] Error moving file {sourcePath} to {targetPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 相対パスを取得
        /// </summary>
        private string GetRelativePath(string fullPath)
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            
            if (fullPath.StartsWith(coreDir))
            {
                return fullPath.Substring(coreDir.Length + 1).Replace('\\', '/');
            }
            
            return fullPath.Replace('\\', '/');
        }

        /// <summary>
        /// ファイルが一時ファイルかどうかを判定
        /// </summary>
        private bool IsTemporaryFile(string fileName)
        {
            string lowerName = fileName.ToLower();
            return lowerName.EndsWith(".tmp") ||
                   lowerName.EndsWith(".crdownload") ||
                   lowerName.EndsWith(".part") ||
                   lowerName.StartsWith("~") ||
                   lowerName.Contains(".tmp.");
        }

        /// <summary>
        /// ファイルのダウンロードが完了するまで待機
        /// </summary>
        private bool WaitForFileCompletion(string filePath)
        {
            const int maxAttempts = 10;
            const int delayMs = 500;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    // ファイルがアクセス可能かチェック
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true; // ファイルが正常にアクセスできる
                    }
                }
                catch (IOException)
                {
                    // ファイルがまだ使用中の場合は待機
                    System.Threading.Thread.Sleep(delayMs);
                }
                catch (Exception)
                {
                    return false; // 予期しないエラー
                }
            }

            return false; // タイムアウト
        }

        /// <summary>
        /// 手動でダウンロードフォルダをスキャンして処理
        /// </summary>
        public void ScanDownloadFolder()
        {
            if (!Directory.Exists(_downloadFolderPath) || _assetDataManager?.Library == null)
                return;

            try
            {
                var files = Directory.GetFiles(_downloadFolderPath, "*", SearchOption.TopDirectoryOnly);
                
                foreach (string filePath in files)
                {
                    ProcessFile(filePath);
                }
                
                Debug.Log($"[DownloadFolderWatcher] Scanned {files.Length} files in download folder");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcher] Error scanning download folder: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
