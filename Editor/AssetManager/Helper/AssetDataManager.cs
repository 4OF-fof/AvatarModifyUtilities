using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using AMU.AssetManager.Data;

namespace AMU.AssetManager.Helper
{
    public class AssetDataManager
    {
        private AssetLibrary _assetLibrary;
        private string _dataFilePath;
        private bool _isLoading = false;
        private DateTime _lastFileModified = DateTime.MinValue;
        private string _lastFileHash = "";

        public AssetLibrary Library => _assetLibrary;
        public bool IsLoading => _isLoading;

        public event Action OnDataLoaded;
        public event Action OnDataChanged;

        public AssetDataManager()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            _dataFilePath = Path.Combine(coreDir, "AssetManager", "AssetLibrary.json");
        }
        public void LoadData()
        {
            if (_isLoading) return;

            _isLoading = true;

            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    _assetLibrary = new AssetLibrary();
                    SaveData();
                }
                else
                {
                    string json = File.ReadAllText(_dataFilePath);
                    _assetLibrary = JsonConvert.DeserializeObject<AssetLibrary>(json) ?? new AssetLibrary();
                    UpdateFileTrackingInfo();
                }

                _isLoading = false;
                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Failed to load data: {ex.Message}");
                _assetLibrary = new AssetLibrary();
                _isLoading = false;
                OnDataLoaded?.Invoke();
            }
        }

        /// <summary>
        /// JSONファイルが外部で変更されていないかチェックし、必要に応じて再読み込みを行う
        /// </summary>
        public bool CheckForExternalChanges()
        {
            if (!File.Exists(_dataFilePath)) return false;

            var fileInfo = new FileInfo(_dataFilePath);
            var currentModified = fileInfo.LastWriteTime;

            // ファイルの更新時刻が変わっていない場合はスキップ
            if (currentModified <= _lastFileModified) return false;

            try
            {
                string currentHash = ComputeFileHash(_dataFilePath);
                if (currentHash != _lastFileHash)
                {
                    // ファイルが変更されている場合は再読み込み
                    LoadData();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Failed to check file changes: {ex.Message}");
            }

            return false;
        }
        public void SaveData()
        {
            try
            {
                EnsureDirectoryExists();
                _assetLibrary.lastUpdated = DateTime.Now;
                string json = JsonConvert.SerializeObject(_assetLibrary, Formatting.Indented);
                File.WriteAllText(_dataFilePath, json);
                UpdateFileTrackingInfo();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Failed to save data: {ex.Message}");
            }
        }

        public void AddAsset(AssetInfo asset)
        {
            if (_assetLibrary?.assets == null)
                _assetLibrary = new AssetLibrary();

            _assetLibrary.assets.Add(asset);
            SaveData();
        }

        public void UpdateAsset(AssetInfo asset)
        {
            if (_assetLibrary?.assets == null) return;

            var existingAsset = _assetLibrary.assets.FirstOrDefault(a => a.uid == asset.uid);
            if (existingAsset != null)
            {
                var index = _assetLibrary.assets.IndexOf(existingAsset);
                _assetLibrary.assets[index] = asset;
                SaveData();
            }
        }

        public void RemoveAsset(string uid)
        {
            if (_assetLibrary?.assets == null) return;

            var asset = _assetLibrary.assets.FirstOrDefault(a => a.uid == uid);
            if (asset != null)
            {
                _assetLibrary.assets.Remove(asset);
                SaveData();
            }
        }

        public AssetInfo GetAsset(string uid)
        {
            return _assetLibrary?.assets?.FirstOrDefault(a => a.uid == uid);
        }

        public AssetInfo GetAssetByName(string name)
        {
            return _assetLibrary?.assets?.FirstOrDefault(a => a.name == name);
        }
        public List<AssetInfo> GetAllAssets()
        {
            return _assetLibrary?.assets ?? new List<AssetInfo>();
        }
        public List<AssetInfo> SearchAssets(string searchText, string filterType = null, bool? favoritesOnly = null, bool showHidden = false)
        {
            // 外部ファイル変更をチェック
            CheckForExternalChanges();

            var assets = GetAllAssets();

            if (!showHidden)
            {
                assets = assets.Where(a => !a.isHidden).ToList();
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                searchText = searchText.ToLower();
                assets = assets.Where(a =>
                    a.name.ToLower().Contains(searchText) ||
                    a.description.ToLower().Contains(searchText) ||
                    a.authorName.ToLower().Contains(searchText) ||
                    a.tags.Any(tag => tag.ToLower().Contains(searchText))
                ).ToList();
            }

            if (!string.IsNullOrEmpty(filterType))
            {
                assets = assets.Where(a => a.assetType == filterType).ToList();
            }

            if (favoritesOnly.HasValue && favoritesOnly.Value)
            {
                assets = assets.Where(a => a.isFavorite).ToList();
            }

            return assets;
        }

        private void UpdateFileTrackingInfo()
        {
            if (File.Exists(_dataFilePath))
            {
                var fileInfo = new FileInfo(_dataFilePath);
                _lastFileModified = fileInfo.LastWriteTime;
                _lastFileHash = ComputeFileHash(_dataFilePath);
            }
        }

        private string ComputeFileHash(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] hash = sha256.ComputeHash(stream);
                        return Convert.ToBase64String(hash);
                    }
                }
            }
            catch
            {
                return "";
            }
        }


        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
