using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public void SaveData()
        {
            try
            {
                EnsureDirectoryExists();
                _assetLibrary.lastUpdated = DateTime.Now;
                string json = JsonConvert.SerializeObject(_assetLibrary, Formatting.Indented);
                File.WriteAllText(_dataFilePath, json);
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

        public List<AssetInfo> GetAllAssets()
        {
            return _assetLibrary?.assets ?? new List<AssetInfo>();
        }

        public List<AssetInfo> SearchAssets(string searchText, AssetType? filterType = null, bool? favoritesOnly = null, bool showHidden = false)
        {
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

            if (filterType.HasValue)
            {
                assets = assets.Where(a => a.assetType == filterType.Value).ToList();
            }

            if (favoritesOnly.HasValue && favoritesOnly.Value)
            {
                assets = assets.Where(a => a.isFavorite).ToList();
            }

            return assets;
        }

        public AssetInfo DuplicateAsset(AssetInfo originalAsset)
        {
            var duplicate = originalAsset.Clone();
            duplicate.uid = Guid.NewGuid().ToString();
            duplicate.name = $"{originalAsset.name} (Copy)";
            duplicate.createdDate = DateTime.Now;

            AddAsset(duplicate);
            return duplicate;
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
