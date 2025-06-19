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

        public bool InitializeLibrary()
        {
            if (library != null) return true;

            library = new AssetLibrarySchema();
            lastUpdated = DateTime.Now;
            return true;
        }

        public bool LoadAssetLibrary(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                Debug.LogError($"Asset library file not found at {path}");
                return false;
            }

            if (File.GetLastWriteTime(path) < lastUpdated) return true;

            return ForceLoadAssetLibrary(path);
        }

        public bool ForceLoadAssetLibrary(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                Debug.LogError($"Asset library file not found at {path}");
                return false;
            }

            try
            {
                var json = System.IO.File.ReadAllText(path);
                library = JsonConvert.DeserializeObject<AssetLibrarySchema>(json);
                lastUpdated = File.GetLastWriteTime(path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to force load asset library: {ex.Message}");
                return false;
            }
        }

        public bool SaveAssetLibrary(string path)
        {
            if (File.GetLastWriteTime(path) > lastUpdated)
            {
                Debug.LogWarning($"Asset library file at {path} is newer than the current library. Skipping save.");
                return false;
            }

            return ForceSaveAssetLibrary(path);
        }

        public bool ForceSaveAssetLibrary(string path)
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized.");
                return false;
            }

            try
            {
                var json = JsonConvert.SerializeObject(library, Formatting.Indented);
                System.IO.File.WriteAllText(path, json);
                lastUpdated = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to force save asset library: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Asset Management
        public bool AddAsset(AssetSchema asset)
        {
            if (library == null || asset == null || library.Assets.ContainsKey(asset.AssetId))
            {
                Debug.LogError("Invalid asset or library not initialized.");
                return false;
            }

            library.AddAsset(asset);
            return true;
        }

        public bool AddTestAsset()
        {
            if (library == null)
            {
                Debug.LogError("Asset library is not initialized.");
                return false;
            }

            var testAsset = new AssetSchema();

            return library.AddAsset(testAsset);
        }
        #endregion
    }
}