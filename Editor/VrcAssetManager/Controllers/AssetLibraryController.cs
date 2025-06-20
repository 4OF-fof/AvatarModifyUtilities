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
            if (library != null)
            {
                Debug.LogWarning("Asset library is already initialized.");
                return;
            }

            library = new AssetLibrarySchema();
            lastUpdated = DateTime.Now;
        }

        public void ForceInitializeLibrary()
        {
            library = new AssetLibrarySchema();
            lastUpdated = DateTime.Now;
        }

        public void LoadAssetLibrary()
        {
            if (!File.Exists(libraryPath))
                throw new FileNotFoundException($"Asset library file not found at {libraryPath}");

            if (File.GetLastWriteTime(libraryPath) < lastUpdated) return;

            ForceLoadAssetLibrary();
        }

        public void ForceLoadAssetLibrary()
        {
            if (!File.Exists(libraryPath))
                throw new FileNotFoundException($"Asset library file not found at {libraryPath}");

            try
            {
                var json = File.ReadAllText(libraryPath);
                library = JsonConvert.DeserializeObject<AssetLibrarySchema>(json);
                lastUpdated = File.GetLastWriteTime(libraryPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load asset library from {libraryPath}: {ex.Message}", ex);
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
                throw new InvalidOperationException("Asset library is not initialized.");

            try
            {
                var json = JsonConvert.SerializeObject(library, Formatting.Indented);
                File.WriteAllText(libraryPath, json);
                lastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save asset library to {libraryPath}: {ex.Message}", ex);
            }
        }

        public void SyncAssetLibrary()
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            if (!File.Exists(libraryPath))
                throw new FileNotFoundException($"Asset library file not found at {libraryPath}");

            var lastWriteTime = File.GetLastWriteTime(libraryPath);
            Debug.Log($"Last write time of asset library: {lastWriteTime}, Last updated time: {lastUpdated}");
            if (lastWriteTime < lastUpdated)
            {
                ForceSaveAssetLibrary();
            }
            else
            {
                ForceLoadAssetLibrary();
            }
        }
        #endregion

        #region Asset Management
        public void AddAsset(AssetSchema asset)
        {
            if (library == null || asset == null || library.Assets.ContainsKey(asset.AssetId))
                throw new ArgumentException("Asset is null or already exists in the library.");

            SyncAssetLibrary();
            library.AddAsset(asset);
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void UpdateAsset(AssetSchema asset)
        {
            if (library == null || asset == null || !library.Assets.ContainsKey(asset.AssetId))
                throw new ArgumentException("Asset is null or does not exist in the library.");

            SyncAssetLibrary();
            library.UpdateAsset(asset);
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void RemoveAsset(Guid assetId)
        {
            if (library == null || assetId == Guid.Empty || !library.Assets.ContainsKey(assetId))
                throw new ArgumentException("Asset ID is invalid or does not exist in the library.");

            SyncAssetLibrary();
            library.RemoveAsset(assetId);
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public AssetSchema GetAsset(Guid assetId)
        {
            if (library == null || assetId == Guid.Empty || !library.Assets.ContainsKey(assetId))
                throw new ArgumentException("Asset ID is invalid or does not exist in the library.");

            SyncAssetLibrary();
            return library.GetAsset(assetId);
        }

        public IReadOnlyList<AssetSchema> GetAllAssets()
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            SyncAssetLibrary();
            return library.Assets.Values.ToList();
        }

        public void ClearAssets()
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            SyncAssetLibrary();
            library.ClearAssets();
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }
        #endregion

        #region Tag Management
        public void AddTag(string tag)
        {
            if (library == null || string.IsNullOrWhiteSpace(tag) || library.Tags.Contains(tag))
                throw new ArgumentException("Tag is null, empty, or already exists in the library.");

            SyncAssetLibrary();
            library.AddTag(tag.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void RemoveTag(string tag)
        {
            if (library == null || string.IsNullOrWhiteSpace(tag) || !library.Tags.Contains(tag))
                throw new ArgumentException("Tag is null, empty, or does not exist in the library.");

            SyncAssetLibrary();
            library.RemoveTag(tag.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public bool TagExists(string tag)
        {
            if (library == null || string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag is null or empty.");

            SyncAssetLibrary();
            return library.TagExists(tag.Trim());
        }

        public IReadOnlyList<string> GetAllTags()
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            SyncAssetLibrary();
            return library.Tags.ToList();
        }

        public void ClearTags()
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            SyncAssetLibrary();
            library.ClearTags();
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }
        #endregion

        #region AssetType Management
        public void AddAssetType(string assetType)
        {
            if (library == null || string.IsNullOrWhiteSpace(assetType) || library.AssetTypes.Contains(assetType))
                throw new ArgumentException("Asset type is null, empty, or already exists in the library.");

            SyncAssetLibrary();
            library.AddAssetType(assetType.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void RemoveAssetType(string assetType)
        {
            if (library == null || string.IsNullOrWhiteSpace(assetType) || !library.AssetTypes.Contains(assetType))
                throw new ArgumentException("Asset type is null, empty, or does not exist in the library.");

            SyncAssetLibrary();
            library.RemoveAssetType(assetType.Trim());
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public bool AssetTypeExists(string assetType)
        {
            if (library == null || string.IsNullOrWhiteSpace(assetType))
                throw new ArgumentException("Asset type is null or empty.");

            SyncAssetLibrary();
            return library.AssetTypeExists(assetType.Trim());
        }

        public IReadOnlyList<string> GetAllAssetTypes()
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            SyncAssetLibrary();
            return library.AssetTypes.ToList();
        }

        public void ClearAssetTypes()
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            SyncAssetLibrary();
            library.ClearAssetTypes();
            lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }
        #endregion
    }
}