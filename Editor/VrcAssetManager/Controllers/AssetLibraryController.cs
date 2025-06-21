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
        public AssetLibrarySchema library { get; private set; }
        public FilterOptions filterOptions { get; set; } = new FilterOptions();
        public SortOptions sortOptions { get; set; } = new SortOptions();
        
        private int _columnsPerRow = 7;
        public int columnsPerRow 
        { 
            get => _columnsPerRow; 
            set => _columnsPerRow = Mathf.Clamp(value, 4, 13); 
        }

        #region Library Management

        private DateTime _lastUpdated;

        private string _libraryDir => Path.Combine(SettingsAPI.GetSetting<string>("Core_dirPath"), "VrcAssetManager");
        private string _libraryPath => Path.Combine(_libraryDir, "AssetLibrary.json");

        public void InitializeLibrary()
        {
            ForceLoadAssetLibrary();
            if (library != null){
                return;
            }
            library = new AssetLibrarySchema();
            _lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void ForceInitializeLibrary()
        {
            if (library != null)
            {
                Debug.LogWarning("Asset library is already initialized. Forcing re-initialization.");
            }
            library = new AssetLibrarySchema();
            _lastUpdated = DateTime.Now;
            ForceSaveAssetLibrary();
        }

        public void LoadAssetLibrary()
        {
            if (!File.Exists(_libraryPath))
            {
                Debug.LogError($"Asset library file not found at {_libraryPath}.");
                return;
            }

            if (File.GetLastWriteTime(_libraryPath) < _lastUpdated) return;

            ForceLoadAssetLibrary();
        }

        public void ForceLoadAssetLibrary()
        {
            if (!File.Exists(_libraryPath))
            {
                Debug.LogError($"Asset library file not found at {_libraryPath}.");
                return;
            }

            try
            {
                var json = File.ReadAllText(_libraryPath);
                library = JsonConvert.DeserializeObject<AssetLibrarySchema>(json);
                _lastUpdated = File.GetLastWriteTime(_libraryPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load asset library from {_libraryPath}: {ex.Message}");
            }
        }

        public void SaveAssetLibrary()
        {
            if (File.GetLastWriteTime(_libraryPath) > _lastUpdated)
            {
                Debug.LogWarning($"Asset library file at {_libraryPath} is newer than the current library. Skipping save.");
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

            if (!Directory.Exists(_libraryDir))
            {
                try
                {
                    Directory.CreateDirectory(_libraryDir);
                    Debug.Log($"Created library directory: {_libraryDir}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create library directory {_libraryDir}: {ex.Message}");
                    return;
                }
            }

            try
            {
                var json = JsonConvert.SerializeObject(library, Formatting.Indented);
                File.WriteAllText(_libraryPath, json);
                _lastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save asset library to {_libraryPath}: {ex.Message}");
            }
        }

        public void SyncAssetLibrary()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized. Cannot sync.");
                return;
            }

            if (!File.Exists(_libraryPath))
            {
                Debug.LogError($"Asset library file not found at {_libraryPath}.");
                return;
            }

            var lastWriteTime = File.GetLastWriteTime(_libraryPath);
            if (lastWriteTime > _lastUpdated)
            {
                Debug.Log($"Last write time of asset library: {lastWriteTime}, Last updated time: {_lastUpdated}");
                ForceLoadAssetLibrary();
            }
            else if (lastWriteTime < _lastUpdated)
            {
                Debug.Log($"Last write time of asset library: {lastWriteTime}, Last updated time: {_lastUpdated}");
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
            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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

        public bool HasUnCategorizedAssets()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get assets by name.");
                return false;
            }

            SyncAssetLibrary();
            return library.Assets.Values.Any(asset => string.IsNullOrEmpty(asset.Metadata.AssetType));
        }

        public IReadOnlyList<AssetSchema> GetFilteredAssets()
        {
            if (library == null)
            {
                Debug.LogWarning("Asset library is not initialized. Cannot get filtered assets.");
                return new List<AssetSchema>();
            }

            SyncAssetLibrary();

            if (filterOptions == null)
            {
                return library.GetAllAssets().Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived)).ToList();
            }

            var results = new List<List<AssetSchema>>();

            if (!string.IsNullOrEmpty(filterOptions.name))
            {
                results.Add(library.GetAssetsByName(filterOptions.name));
            }

            if (!string.IsNullOrEmpty(filterOptions.authorName))
            {
                results.Add(library.GetAssetsByAuthorName(filterOptions.authorName));
            }

            if (!string.IsNullOrEmpty(filterOptions.description))
            {
                results.Add(library.GetAssetsByDescription(filterOptions.description));
            }

            if (filterOptions.tags != null && filterOptions.tags.Count > 0)
            {
                var tagResults = new List<AssetSchema>();
                if (filterOptions.tagsAnd)
                {
                    tagResults = library.Assets.Values
                        .Where(asset => filterOptions.tags.All(tag => asset.Metadata.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                        .ToList();
                }
                else
                {
                    foreach (var tag in filterOptions.tags)
                    {
                        tagResults.AddRange(library.GetAssetsByTag(tag));
                    }
                }
                results.Add(tagResults.Distinct().ToList());
            }

            if (filterOptions.isFavorite.HasValue)
            {
                results.Add(library.GetAssetsByStateFavorite(filterOptions.isFavorite.Value));
            }
            
            if (results.Count == 0)
            {
                if (filterOptions.assetType == "UNCATEGORIZED")
                {
                    return library.Assets.Values
                        .Where(asset => string.IsNullOrEmpty(asset.Metadata.AssetType))
                        .Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived)).ToList();;
                }
                else if (!string.IsNullOrEmpty(filterOptions.assetType))
                {
                    return library.Assets.Values
                        .Where(asset => asset.Metadata.AssetType.Equals(filterOptions.assetType, StringComparison.OrdinalIgnoreCase))
                        .Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived)).ToList();;
                }
                return library.GetAllAssets().Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived)).ToList();
            }

            var filteredAssets = results[0];

            if (filterOptions.filterAnd)
            {
                for (int i = 1; i < results.Count; i++)
                {
                    filteredAssets = filteredAssets.Intersect(results[i]).ToList();
                }
            }
            else
            {
                for (int i = 1; i < results.Count; i++)
                {
                    filteredAssets = filteredAssets.Union(results[i]).ToList();
                }
            }

            if (filterOptions.assetType == "UNCATEGORIZED")
            {
                filteredAssets = filteredAssets
                    .Where(asset => string.IsNullOrEmpty(asset.Metadata.AssetType))
                    .ToList();
            }
            else if (!string.IsNullOrEmpty(filterOptions.assetType))
            {
                filteredAssets = filteredAssets
                    .Where(asset => asset.Metadata.AssetType.Equals(filterOptions.assetType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return filteredAssets.Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived)).ToList();;
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
            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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

            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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
            _lastUpdated = DateTime.Now;
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

            _lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }
        #endregion
    }
}