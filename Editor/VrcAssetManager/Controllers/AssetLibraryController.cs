using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using Newtonsoft.Json;

namespace AMU.Editor.VrcAssetManager.Controller
{
    public class AssetLibraryController
    {
        #region Library Management

        private DateTime lastUpdated;
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

        public void LoadAssetLibrary(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Asset library path cannot be null or empty.");
            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException($"Asset library file not found at {path}");

            if (File.GetLastWriteTime(path) < lastUpdated) return;

            ForceLoadAssetLibrary(path);
        }

        public void ForceLoadAssetLibrary(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Asset library path cannot be null or empty.");
            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException($"Asset library file not found at {path}");

            try
            {
                var json = System.IO.File.ReadAllText(path);
                library = JsonConvert.DeserializeObject<AssetLibrarySchema>(json);
                lastUpdated = File.GetLastWriteTime(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load asset library from {path}: {ex.Message}", ex);
            }
        }

        public void SaveAssetLibrary(string path)
        {
            if (File.GetLastWriteTime(path) > lastUpdated)
            {
                Debug.LogWarning($"Asset library file at {path} is newer than the current library. Skipping save.");
                return;
            }

            ForceSaveAssetLibrary(path);
        }

        public void ForceSaveAssetLibrary(string path)
        {
            if (library == null)
                throw new InvalidOperationException("Asset library is not initialized.");

            try
            {
                var json = JsonConvert.SerializeObject(library, Formatting.Indented);
                System.IO.File.WriteAllText(path, json);
                lastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save asset library to {path}: {ex.Message}", ex);
            }
        }
        #endregion

        #region Asset Management
        public void AddAsset(AssetSchema asset)
        {
            if (library == null || asset == null || library.Assets.ContainsKey(asset.AssetId))
                throw new ArgumentException("Asset is null or already exists in the library.");

            library.AddAsset(asset);
        }

        public void AddTestAsset()
        {
            var testAsset = new AssetSchema();

            AddAsset(testAsset);
        }
        #endregion
    }
}