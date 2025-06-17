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
                // ファイルサイズをチェック
                var sourceFileInfo = new FileInfo(sourcePath);
                if (!sourceFileInfo.Exists)
                {
                    Debug.LogError($"[DownloadFolderWatcher] Source file not found: {sourcePath}");
                    return;
                }

                long fileSize = sourceFileInfo.Length;

                // 移動先ドライブの空き容量をチェック
                string targetDrive = Path.GetPathRoot(targetPath);
                var driveInfo = new DriveInfo(targetDrive);
                if (driveInfo.AvailableFreeSpace < fileSize * 1.1) // 10%のマージンを確保
                {
                    Debug.LogError($"[DownloadFolderWatcher] Insufficient disk space. Required: {fileSize:N0} bytes, Available: {driveInfo.AvailableFreeSpace:N0} bytes");
                    return;
                }

                // 大きなファイルの場合は警告ログ
                if (fileSize > 1024 * 1024 * 100) // 100MB以上
                {
                    Debug.Log($"[DownloadFolderWatcher] Moving large file: {Path.GetFileName(sourcePath)} ({fileSize:N0} bytes)");
                }

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
                else
                {
                    asset.fileSize = fileSize; // FileManagerがない場合は直接設定
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
        /// ファイルのダウンロードが完了しているかをチェック
        /// </summary>
        private bool IsFileDownloadComplete(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return false;

                // ファイルサイズが0の場合は未完了
                if (fileInfo.Length == 0)
                    return false;

                // ファイルがロックされていないかチェック（排他アクセステスト）
                if (!TryAccessFile(filePath))
                    return false;

                // ブラウザの一時ファイル拡張子をチェック
                if (IsTemporaryFile(fileInfo.Name))
                    return false;

                // ファイルの最終更新時刻をチェック（直近で更新されていない）
                var timeSinceLastWrite = DateTime.Now - fileInfo.LastWriteTime;
                if (timeSinceLastWrite.TotalSeconds < 2) // 2秒以内に更新されている場合は待機
                    return false;

                // NTFS代替データストリーム（Zone.Identifier）のチェック
                // ダウンロード中のファイルには通常このストリームが存在する
                if (HasDownloadInProgressMarker(filePath))
                    return false;

                Debug.Log($"[DownloadFolderWatcher] File appears to be download complete: {Path.GetFileName(filePath)} ({fileInfo.Length:N0} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcher] Error checking file completion: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ダウンロード進行中マーカーをチェック
        /// </summary>
        private bool HasDownloadInProgressMarker(string filePath)
        {
            try
            {
                // Zone.Identifierストリームの存在をチェック
                string zoneIdentifierPath = filePath + ":Zone.Identifier";
                if (File.Exists(zoneIdentifierPath))
                {
                    // Zone.Identifierの内容をチェック
                    string content = File.ReadAllText(zoneIdentifierPath);
                    // ダウンロード中を示すマーカーがある場合
                    if (content.Contains("ReferrerUrl=") && content.Contains("HostUrl="))
                    {
                        // まだダウンロード中の可能性がある
                        return true;
                    }
                }

                // Chromeの.crdownloadファイルをチェック
                string crdownloadPath = filePath + ".crdownload";
                if (File.Exists(crdownloadPath))
                    return true;

                // Firefoxの.part.mozdownloadファイルをチェック
                string mozdownloadPath = filePath + ".part";
                if (File.Exists(mozdownloadPath))
                    return true;

                return false;
            }
            catch
            {
                // エラーの場合は安全側に倒してfalseを返す
                return false;
            }
        }

        /// <summary>
        /// ファイルのダウンロード完了を待機（非時間ベース）
        /// </summary>
        private bool WaitForFileCompletion(string filePath)
        {
            const int maxRetries = 30; // 最大試行回数
            const int baseDelayMs = 500; // 基本待機時間
            int retryCount = 0;

            Debug.Log($"[DownloadFolderWatcher] Checking download status: {Path.GetFileName(filePath)}");

            while (retryCount < maxRetries)
            {
                if (IsFileDownloadComplete(filePath))
                {
                    return true;
                }

                // 指数バックオフで待機時間を調整
                int delayMs = Math.Min(baseDelayMs * (int)Math.Pow(1.5, retryCount / 5), 5000);

                Debug.Log($"[DownloadFolderWatcher] File not ready, waiting... (attempt {retryCount + 1}/{maxRetries})");
                System.Threading.Thread.Sleep(delayMs);
                retryCount++;
            }

            Debug.LogWarning($"[DownloadFolderWatcher] File completion check failed after {maxRetries} attempts: {Path.GetFileName(filePath)}");
            return false;
        }

        /// <summary>
        /// ファイルがアクセス可能かテスト
        /// </summary>
        private bool TryAccessFile(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
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
