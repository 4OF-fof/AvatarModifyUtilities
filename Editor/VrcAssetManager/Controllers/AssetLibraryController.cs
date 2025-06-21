using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using Newtonsoft.Json;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.Controller
{
    public class AssetLibraryController
    {
        #region Library Management

        private DateTime lastUpdated;

        private string libraryDir => Path.Combine(SettingsAPI.GetSetting<string>("Core_dirPath"), "VrcAssetManager");
        private string libraryPath => Path.Combine(libraryDir, "AssetLibrary.json");

        public AssetLibrarySchema library { get; private set; }

        public void InitializeLibrary()
        {
            ForceLoadAssetLibrary();
            if (library != null){
                return;
            }
            library = new AssetLibrarySchema();
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void ForceInitializeLibrary()
        {
            if (library != null)
            {
                Debug.LogWarning("Asset library is already initialized. Forcing re-initialization.");
            }
            library = new AssetLibrarySchema();
            lastUpdated = DateTime.Now;
            ForceSaveAssetLibrary();
        }

        public void LoadAssetLibrary()
        {
            if (!File.Exists(libraryPath))
            {
                Debug.LogError($"Asset library file not found at {libraryPath}.");
                return;
            }

            if (File.GetLastWriteTime(libraryPath) < lastUpdated) return;

            ForceLoadAssetLibrary();
        }

        public void ForceLoadAssetLibrary()
        {
            if (!File.Exists(libraryPath))
            {
                Debug.LogError($"Asset library file not found at {libraryPath}.");
                return;
            }

            try
            {
                var json = File.ReadAllText(libraryPath);
                library = JsonConvert.DeserializeObject<AssetLibrarySchema>(json);
                lastUpdated = File.GetLastWriteTime(libraryPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load asset library from {libraryPath}: {ex.Message}");
            }
        }

        public void SaveAssetLibrary()
        {
            if (File.GetLastWriteTime(libraryPath) > lastUpdated)
            {
                Debug.LogWarning($"Asset library file at {libraryPath} is newer than the current library. Skipping save.");
                return;
            }

            ForceSaveAssetLibrary();
        }

        public void ForceSaveAssetLibrary()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot save.");
                return;
            }

            if (!Directory.Exists(libraryDir))
            {
                try
                {
                    Directory.CreateDirectory(libraryDir);
                    Debug.Log($"Created library directory: {libraryDir}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create library directory {libraryDir}: {ex.Message}");
                    return;
                }
            }

            try
            {
                var json = JsonConvert.SerializeObject(library, Formatting.Indented);
                File.WriteAllText(libraryPath, json);
                lastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save asset library to {libraryPath}: {ex.Message}");
            }
        }

        public void SyncAssetLibrary()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot sync.");
                return;
            }

            if (!File.Exists(libraryPath))
            {
                Debug.LogError($"Asset library file not found at {libraryPath}.");
                return;
            }

            var lastWriteTime = File.GetLastWriteTime(libraryPath);
            if (lastWriteTime > lastUpdated)
            {
                Debug.Log($"Last write time of asset library: {lastWriteTime}, Last updated time: {lastUpdated}");
                ForceLoadAssetLibrary();
            }
            else if (lastWriteTime < lastUpdated)
            {
                Debug.Log($"Last write time of asset library: {lastWriteTime}, Last updated time: {lastUpdated}");
                ForceSaveAssetLibrary();
            }
        }

        public void OptimizeAssetLibrary()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot optimize.");
                return;
            }
            OptimizeTags();
            OptimizeAssetTypes();
        }
        #endregion

        #region Asset Management
        public void AddAsset(AssetSchema asset)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot add asset.");
                return;
            }
            if (asset == null)
            {
                Debug.LogError("Asset is null. Cannot add asset.");
                return;
            }
            if (library.Assets.ContainsKey(asset.AssetId))
            {
                Debug.LogError($"Asset with ID {asset.AssetId} already exists in the library.");
                return;
            }

            SyncAssetLibrary();
            library.AddAsset(asset);
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void UpdateAsset(AssetSchema asset)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot update asset.");
                return;
            }
            if (asset == null)
            {
                Debug.LogError("Asset is null. Cannot update asset.");
                return;
            }
            if (!library.Assets.ContainsKey(asset.AssetId))
            {
                Debug.LogError($"Asset with ID {asset.AssetId} does not exist in the library.");
                return;
            }

            SyncAssetLibrary();
            library.UpdateAsset(asset);
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void RemoveAsset(Guid assetId)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot remove asset.");
                return;
            }
            if (assetId == Guid.Empty)
            {
                Debug.LogError("Asset ID is invalid. Cannot remove asset.");
                return;
            }
            if (!library.Assets.ContainsKey(assetId))
            {
                Debug.LogError($"Asset with ID {assetId} does not exist in the library.");
                return;
            }

            SyncAssetLibrary();
            library.RemoveAsset(assetId);
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public AssetSchema GetAsset(Guid assetId)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot get asset.");
                return null;
            }
            if (assetId == Guid.Empty)
            {
                Debug.LogError("Asset ID is invalid. Cannot get asset.");
                return null;
            }
            if (!library.Assets.ContainsKey(assetId))
            {
                Debug.LogError($"Asset with ID {assetId} does not exist in the library.");
                return null;
            }

            SyncAssetLibrary();
            return library.GetAsset(assetId);
        }

        public IReadOnlyList<AssetSchema> GetAllAssets()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get all assets.");
                return new List<AssetSchema>();
            }

            SyncAssetLibrary();
            return library.Assets.Values.ToList();
        }

        public void ClearAssets()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot clear assets.");
                return;
            }

            SyncAssetLibrary();
            library.ClearAssets();
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public int GetAssetCount()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get asset count.");
                return 0;
            }

            SyncAssetLibrary();
            return library.AssetCount;
        }
        #endregion

        #region Tag Management
        public void AddTag(string tag)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot add tag.");
                return;
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogError("Tag is null or empty. Cannot add tag.");
                return;
            }
            if (library.Tags.Contains(tag.Trim()))
            {
                Debug.LogError($"Tag '{tag}' already exists in the library.");
                return;
            }

            SyncAssetLibrary();
            library.AddTag(tag.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void RemoveTag(string tag)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot remove tag.");
                return;
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogError("Tag is null or empty. Cannot remove tag.");
                return;
            }
            if (!library.Tags.Contains(tag.Trim()))
            {
                Debug.LogError($"Tag '{tag}' does not exist in the library.");
                return;
            }

            SyncAssetLibrary();
            library.RemoveTag(tag.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public bool TagExists(string tag)
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot check tag existence.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogWarning("Tag is null or empty. Cannot check tag existence.");
                return false;
            }

            SyncAssetLibrary();
            return library.TagExists(tag.Trim());
        }

        public IReadOnlyList<string> GetAllTags()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get all tags.");
                return new List<string>();
            }

            SyncAssetLibrary();
            return library.Tags.ToList();
        }

        public void ClearTags()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot clear tags.");
                return;
            }

            SyncAssetLibrary();
            library.ClearTags();
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public int GetTagCount()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get tag count.");
                return 0;
            }

            SyncAssetLibrary();
            return library.TagCount;
        }

        public void OptimizeTags()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot optimize tags.");
                return;
            }

            SyncAssetLibrary();

            var unusedTags = library.Tags.Where(tag => !library.Assets.Values.Any(asset => asset.Metadata.Tags.Contains(tag))).ToList();
            foreach (var tag in unusedTags)
            {
                library.RemoveTag(tag);
            }

            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }
        #endregion

        #region AssetType Management
        public void AddAssetType(string assetType)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot add asset type.");
                return;
            }
            if (string.IsNullOrWhiteSpace(assetType))
            {
                Debug.LogError("Asset type is null or empty. Cannot add asset type.");
                return;
            }
            if (library.AssetTypes.Contains(assetType.Trim()))
            {
                Debug.LogError($"Asset type '{assetType}' already exists in the library.");
                return;
            }

            SyncAssetLibrary();
            library.AddAssetType(assetType.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void RemoveAssetType(string assetType)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot remove asset type.");
                return;
            }
            if (string.IsNullOrWhiteSpace(assetType))
            {
                Debug.LogError("Asset type is null or empty. Cannot remove asset type.");
                return;
            }
            if (!library.AssetTypes.Contains(assetType.Trim()))
            {
                Debug.LogError($"Asset type '{assetType}' does not exist in the library.");
                return;
            }

            SyncAssetLibrary();
            library.RemoveAssetType(assetType.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public bool AssetTypeExists(string assetType)
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot check asset type existence.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(assetType))
            {
                Debug.LogWarning("Asset type is null or empty. Cannot check asset type existence.");
                return false;
            }

            SyncAssetLibrary();
            return library.AssetTypeExists(assetType.Trim());
        }

        public IReadOnlyList<string> GetAllAssetTypes()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get all asset types.");
                return new List<string>();
            }

            SyncAssetLibrary();
            return library.AssetTypes.ToList();
        }

        public void ClearAssetTypes()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot clear asset types.");
                return;
            }

            SyncAssetLibrary();
            library.ClearAssetTypes();
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public int GetAssetTypeCount()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get asset type count.");
                return 0;
            }

            SyncAssetLibrary();
            return library.AssetTypeCount;
        }

        public void OptimizeAssetTypes()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot optimize asset types.");
                return;
            }

            SyncAssetLibrary();

            var unusedAssetTypes = library.AssetTypes.Where(type => !library.Assets.Values.Any(asset => asset.Metadata.AssetType == type)).ToList();
            foreach (var type in unusedAssetTypes)
            {
                library.RemoveAssetType(type);
            }

            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }
        #endregion
    }
}