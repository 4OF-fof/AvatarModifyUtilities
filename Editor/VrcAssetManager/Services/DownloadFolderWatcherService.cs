using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Helper;

namespace AMU.Editor.VrcAssetManager.Services
{
    public class DownloadFolderWatcherService : IDisposable
    {
        private FileSystemWatcher _fileWatcher;
        private readonly HashSet<string> _processedFiles = new HashSet<string>();
        private bool _isEnabled = false;
        private string _downloadFolderPath;

        public event Action<string, string> OnFileProcessed;

        public DownloadFolderWatcherService()
        {
            _downloadFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            UpdateWatcherState();
        }

        public void UpdateWatcherState()
        {
            bool shouldEnable = true;
            if (shouldEnable && !_isEnabled)
            {
                StartWatching();
            }
            else if (!shouldEnable && _isEnabled)
            {
                StopWatching();
            }
        }

        private void StartWatching()
        {
            if (_isEnabled || !Directory.Exists(_downloadFolderPath))
                return;
            try
            {
                _fileWatcher = new FileSystemWatcher(_downloadFolderPath)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                _fileWatcher.Created += OnFileCreated;
                _fileWatcher.Renamed += OnFileRenamed;
                _isEnabled = true;
                Debug.Log($"[DownloadFolderWatcherService] Started watching: {_downloadFolderPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcherService] Failed to start watching: {ex.Message}");
            }
        }

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
                Debug.Log("[DownloadFolderWatcherService] Stopped watching download folder");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcherService] Error stopping watcher: {ex.Message}");
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            EditorApplication.delayCall += () => ProcessFile(e.FullPath);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            EditorApplication.delayCall += () => ProcessFile(e.FullPath);
        }

        private void ProcessFile(string filePath)
        {
            if (!_isEnabled)
                return;
            try
            {
                string fileName = Path.GetFileName(filePath);
                Debug.Log($"[DownloadFolderWatcherService] ProcessFile called: {filePath}");

                string originalFileName = System.Text.RegularExpressions.Regex.Replace(fileName, @" ?\([0-9]+\)(?=\.[^.]+$)", "");
                string assetFileName = originalFileName != fileName ? Path.GetFileName(originalFileName) : fileName;
                if (_processedFiles.Contains(assetFileName))
                {
                    Debug.Log($"[DownloadFolderWatcherService] Already processed: {assetFileName}");
                    return;
                }
                if (!File.Exists(filePath))
                {
                    Debug.Log($"[DownloadFolderWatcherService] File does not exist: {filePath}");
                    return;
                }
                if (IsTemporaryFile(assetFileName))
                {
                    Debug.Log($"[DownloadFolderWatcherService] Temporary file, skipping: {assetFileName}");
                    return;
                }
                var asset = AssetLibraryController.Instance.GetAllAssets()
                    .FirstOrDefault(a => a.boothItem != null && a.boothItem.fileName == assetFileName);
                if (asset == null)
                {
                    Debug.Log($"[DownloadFolderWatcherService] No matching asset for: {assetFileName}");
                    return;
                }
                if (!string.IsNullOrEmpty(asset.fileInfo.filePath))
                {
                    Debug.Log($"[DownloadFolderWatcherService] Asset '{asset.metadata.name}' already has a file path, skipping: {asset.fileInfo.filePath}");
                    _processedFiles.Add(assetFileName);
                    return;
                }
                string coreDir = AMU.Editor.Core.Api.SettingAPI.GetSetting<string>("Core_dirPath");
                if (string.IsNullOrEmpty(coreDir))
                {
                    coreDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities");
                }

                string relativePath = AssetFileUtility.MoveToCoreSubDirectory(filePath, "VrcAssetManager/BoothItem/Package", assetFileName);
                string targetPath = Path.Combine(Path.GetFullPath(AMU.Editor.Core.Api.SettingAPI.GetSetting<string>("Core_dirPath")), relativePath.Replace('/', Path.DirectorySeparatorChar));
                Debug.Log($"[DownloadFolderWatcherService] File processed: {filePath} -> {targetPath}");
                try
                {
                    asset.fileInfo.GetType().GetProperty("filePath").SetValue(asset.fileInfo, relativePath);
                    Debug.Log($"[DownloadFolderWatcherService] Set fileInfo.filePath: {relativePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DownloadFolderWatcherService] Failed to set fileInfo.filePath: {ex.Message}");
                }
                try
                {
                    AssetLibraryController.Instance.UpdateAsset(asset);
                    Debug.Log($"[DownloadFolderWatcherService] UpdateAsset called for: {asset.metadata.name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DownloadFolderWatcherService] UpdateAsset failed: {ex.Message}");
                }
                OnFileProcessed?.Invoke(fileName, targetPath);
                _processedFiles.Add(fileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DownloadFolderWatcherService] Error processing file {filePath}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private bool IsTemporaryFile(string fileName)
        {
            return fileName.EndsWith(".tmp") || fileName.StartsWith("~") || fileName.EndsWith(".crdownload");
        }

        private string GetRelativePath(string fullPath, string coreDir)
        {
            if (fullPath.StartsWith(coreDir))
            {
                return fullPath.Substring(coreDir.Length + 1).Replace('\\', '/');
            }
            return fullPath.Replace('\\', '/');
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
