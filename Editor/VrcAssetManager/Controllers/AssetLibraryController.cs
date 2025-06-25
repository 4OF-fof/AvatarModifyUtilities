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
        private static AssetLibraryController _instance;
        public static AssetLibraryController Instance => _instance ??= new AssetLibraryController();
        private AssetLibraryController() {}

        public AssetLibrarySchema library { get; private set; }
        public FilterOptions filterOptions { get; set; } = new FilterOptions();
        public SortOptions sortOptions { get; set; } = new SortOptions();

        #region Library Management

        private DateTime _lastUpdated;

        private string _libraryDir => Path.Combine(SettingAPI.GetSetting<string>("Core_dirPath"), "VrcAssetManager");
        private string _libraryPath => Path.Combine(_libraryDir, "AssetLibrary.json");

        public void InitializeLibrary()
        {
            if (!File.Exists(_libraryPath))
            {
                Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryFileNotFound", _libraryPath));
                library = new AssetLibrarySchema();
                _lastUpdated = DateTime.Now;
                ForceSaveAssetLibrary();
                return;
            }
            ForceLoadAssetLibrary();
            if (library != null)
            {
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
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryAlreadyInitialized"));
            }
            library = new AssetLibrarySchema();
            _lastUpdated = DateTime.Now;
            ForceSaveAssetLibrary();
        }

        public void LoadAssetLibrary()
        {
            if (!File.Exists(_libraryPath))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryFileNotFound", _libraryPath));
                return;
            }

            if (File.GetLastWriteTime(_libraryPath) < _lastUpdated) return;

            ForceLoadAssetLibrary();
        }

        public void ForceLoadAssetLibrary()
        {
            if (!File.Exists(_libraryPath))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryFileNotFound", _libraryPath));
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
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryLoadFailed", _libraryPath, ex.Message));
            }
        }

        public void SaveAssetLibrary()
        {
            if (File.GetLastWriteTime(_libraryPath) > _lastUpdated)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryFileNewer", _libraryPath));
                return;
            }

            ForceSaveAssetLibrary();
        }

        public void ForceSaveAssetLibrary()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitialized"));
                return;
            }

            if (!Directory.Exists(_libraryDir))
            {
                try
                {
                    Directory.CreateDirectory(_libraryDir);
                    Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_success_libraryDirCreated", _libraryDir));
                }
                catch (Exception ex)
                {
                    Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryDirCreateFailed", _libraryDir, ex.Message));
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
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_librarySaveFailed", _libraryPath, ex.Message));
            }
        }

        public void SyncAssetLibrary()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForSync"));
                return;
            }

            if (!File.Exists(_libraryPath))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryFileNotFound", _libraryPath));
                return;
            }

            var lastWriteTime = File.GetLastWriteTime(_libraryPath);
            if (lastWriteTime > _lastUpdated)
            {
                Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_info_librarySyncLoad", lastWriteTime.ToString(), _lastUpdated.ToString()));
                ForceLoadAssetLibrary();
            }
            else if (lastWriteTime < _lastUpdated)
            {
                Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_info_librarySyncSave", lastWriteTime.ToString(), _lastUpdated.ToString()));
                ForceSaveAssetLibrary();
            }
        }

        public void OptimizeAssetLibrary()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForOptimize"));
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
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForAddAsset"));
                return;
            }
            if (asset == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetNull"));
                return;
            }
            if (library.assets.ContainsKey(asset.assetId))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetExists", asset.assetId));
                return;
            }

            SyncAssetLibrary();

            if (!string.IsNullOrEmpty(asset.parentGroupId) && Guid.TryParse(asset.parentGroupId, out var parentGuid))
            {
                if (library.assets.TryGetValue(parentGuid, out var parentGroup))
                {
                    if (!parentGroup.childAssetIds.Contains(asset.assetId.ToString()))
                    {
                        parentGroup.AddChildAssetId(asset.assetId.ToString());
                    }
                }
            }

            if (asset.childAssetIds != null)
            {
                foreach (var childId in asset.childAssetIds)
                {
                    if (Guid.TryParse(childId, out var childGuid) && library.assets.TryGetValue(childGuid, out var childAsset))
                    {
                        childAsset.SetParentGroupId(asset.assetId.ToString());
                    }
                }
            }
            library.AddAsset(asset);
            _lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public void UpdateAsset(AssetSchema asset)
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForUpdateAsset"));
                return;
            }
            if (asset == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetNull"));
                return;
            }
            if (!library.assets.ContainsKey(asset.assetId))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetNotFound", asset.assetId));
                return;
            }

            SyncAssetLibrary();

            var oldAsset = library.GetAsset(asset.assetId);
            var oldParentId = oldAsset.parentGroupId;
            var newParentId = asset.parentGroupId;
            Guid? oldParentGuidForCheck = null;
            
            if (oldParentId != newParentId)
            {
                if (!string.IsNullOrEmpty(oldParentId) && Guid.TryParse(oldParentId, out var oldParentGuid))
                {
                    if (library.assets.TryGetValue(oldParentGuid, out var oldParent))
                    {
                        oldParent.RemoveChildAssetId(asset.assetId.ToString());
                        oldParentGuidForCheck = oldParentGuid;
                    }
                }

                if (!string.IsNullOrEmpty(newParentId) && Guid.TryParse(newParentId, out var newParentGuid))
                {
                    if (library.assets.TryGetValue(newParentGuid, out var newParent))
                    {
                        if (!newParent.childAssetIds.Contains(asset.assetId.ToString()))
                        {
                            newParent.AddChildAssetId(asset.assetId.ToString());
                        }
                    }
                }
            }

            if (asset.childAssetIds != null)
            {
                foreach (var childId in asset.childAssetIds)
                {
                    if (Guid.TryParse(childId, out var childGuid) && library.assets.TryGetValue(childGuid, out var childAsset))
                    {
                        if (childAsset.parentGroupId != asset.assetId.ToString())
                        {
                            childAsset.SetParentGroupId(asset.assetId.ToString());
                        }
                    }
                }
            }
            library.UpdateAsset(asset);
            _lastUpdated = DateTime.Now;
            
            if (oldParentGuidForCheck.HasValue && library.assets.TryGetValue(oldParentGuidForCheck.Value, out var updatedOldParent))
            {
                if (!updatedOldParent.hasChildAssets)
                {
                    Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_info_autoRemoveOldParent", updatedOldParent.metadata.name));
                    RemoveAsset(oldParentGuidForCheck.Value);
                    return;
                }
            }
            
            SaveAssetLibrary();
        }

        public Guid CreateGroupAsset(List<AssetSchema> assets)
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForCreateGroupAsset"));
                return Guid.Empty;
            }
            if (assets == null || assets.Count == 0)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetListNullOrEmpty"));
                return Guid.Empty;
            }

            SyncAssetLibrary();

            var groupAsset = new AssetSchema();
            groupAsset.metadata.SetName(LocalizationAPI.GetText("VrcAssetManager_message_info_groupAssetName"));
            groupAsset.SetChildAssetIds(assets.Select(a => a.assetId.ToString()).ToList());

            var oldParentIdsToCheck = new List<Guid>();

            foreach (var asset in assets)
            {
                if (!string.IsNullOrEmpty(asset.parentGroupId) && asset.parentGroupId != groupAsset.assetId.ToString())
                {
                    if (Guid.TryParse(asset.parentGroupId, out var oldParentGuid))
                    {
                        var OldParentAsset = GetAsset(oldParentGuid);
                        if (OldParentAsset != null)
                        {
                            OldParentAsset.RemoveChildAssetId(asset.assetId.ToString());
                            if (!oldParentIdsToCheck.Contains(oldParentGuid))
                            {
                                oldParentIdsToCheck.Add(oldParentGuid);
                            }
                        }
                    }
                    Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_assetAlreadyInOtherGroup", asset.assetId, groupAsset.assetId));
                }
                asset.SetParentGroupId(groupAsset.assetId.ToString());
            }
            AddAsset(groupAsset);
            
            foreach (var oldParentId in oldParentIdsToCheck)
            {
                if (library.assets.TryGetValue(oldParentId, out var updatedOldParent))
                {
                    if (!updatedOldParent.hasChildAssets)
                    {
                        Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_info_autoRemoveOldParent", updatedOldParent.metadata.name));
                        RemoveAsset(oldParentId);
                    }
                }
            }
            
            return groupAsset.assetId;
        }

        public void RemoveAsset(Guid assetId)
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForRemoveAsset"));
                return;
            }
            if (assetId == Guid.Empty)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetIdInvalid"));
                return;
            }
            if (!library.assets.ContainsKey(assetId))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetNotFound", assetId));
                return;
            }

            var asset = library.GetAsset(assetId);
            Guid? parentGroupId = null;
            
            if (!string.IsNullOrEmpty(asset.parentGroupId) && library.assets.TryGetValue(Guid.Parse(asset.parentGroupId), out var parentGroup))
            {
                parentGroup.RemoveChildAssetId(assetId.ToString());
                parentGroupId = Guid.Parse(asset.parentGroupId);
            }

            foreach (var childId in asset.childAssetIds)
            {
                if (Guid.TryParse(childId, out var childGuid) && library.assets.TryGetValue(childGuid, out var childAsset))
                {
                    childAsset.SetParentGroupId("");
                }
            }

            foreach (var other in library.assets.Values)
            {
                if (other.metadata.dependencies.Contains(assetId.ToString()))
                {
                    other.metadata.RemoveDependency(assetId.ToString());
                }
            }

            SyncAssetLibrary();
            library.RemoveAsset(assetId);
            _lastUpdated = DateTime.Now;

            if (parentGroupId.HasValue && library.assets.TryGetValue(parentGroupId.Value, out var updatedParent))
            {
                if (!updatedParent.hasChildAssets)
                {
                    Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_info_autoRemoveParent", updatedParent.metadata.name));
                    RemoveAsset(parentGroupId.Value);
                    return;
                }
            }
            
            SaveAssetLibrary();
        }

        public void RemoveChildFromParent(Guid parentGroupId, Guid childAssetId)
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForRemoveChild"));
                return;
            }
            if (parentGroupId == Guid.Empty || childAssetId == Guid.Empty)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_parentOrChildIdInvalid"));
                return;
            }
            if (!library.assets.ContainsKey(parentGroupId) || !library.assets.ContainsKey(childAssetId))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_parentOrChildNotFound"));
                return;
            }

            SyncAssetLibrary();

            var parentAsset = library.GetAsset(parentGroupId);
            var childAsset = library.GetAsset(childAssetId);

            if (parentAsset.childAssetIds.Contains(childAssetId.ToString()))
            {
                parentAsset.RemoveChildAssetId(childAssetId.ToString());
            }
            
            childAsset.SetParentGroupId("");

            _lastUpdated = DateTime.Now;

            if (!parentAsset.hasChildAssets)
            {
                Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_info_autoRemoveParent", parentAsset.metadata.name));
                RemoveAsset(parentGroupId);
                return;
            }

            SaveAssetLibrary();
        }

        public AssetSchema GetAsset(Guid assetId)
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForGetAsset"));
                return null;
            }
            if (assetId == Guid.Empty)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetIdInvalidForGetAsset"));
                return null;
            }
            if (!library.assets.ContainsKey(assetId))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetNotFound", assetId));
                return null;
            }

            SyncAssetLibrary();
            return library.GetAsset(assetId);
        }

        public IReadOnlyList<AssetSchema> GetAllAssets()
        {
            if (library == null)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForGetAllAssets"));
                return new List<AssetSchema>();
            }

            SyncAssetLibrary();
            return library.assets.Values.ToList();
        }

        public bool HasUnCategorizedAssets()
        {
            if (library == null)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForHasUnCategorizedAssets"));
                return false;
            }

            SyncAssetLibrary();
            return library.assets.Values.Any(asset => string.IsNullOrEmpty(asset.metadata.assetType));
        }

        public IReadOnlyList<AssetSchema> GetFilteredAssets()
        {
            if (library == null)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForGetFilteredAssets"));
                return new List<AssetSchema>();
            }

            SyncAssetLibrary();

            bool ShowAsChild(AssetSchema asset)
            {
                return asset.hasParentGroup || (!asset.hasParentGroup && (asset.childAssetIds == null || asset.childAssetIds.Count == 0));
            }
            bool ShowAsParent(AssetSchema asset)
            {
                return !asset.hasParentGroup;
            }

            if (filterOptions == null)
            {
                return library.GetAllAssets()
                    .Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived))
                    .Where(ShowAsParent)
                    .ToList();
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

            if (!string.IsNullOrEmpty(filterOptions.parentGroupId))
            {
                var childAssets = library.assets.Values
                    .Where(asset => asset.parentGroupId == filterOptions.parentGroupId)
                    .ToList();
                results.Add(childAssets);
            }

            if (filterOptions.tags != null && filterOptions.tags.Count > 0)
            {
                var tagResults = new List<AssetSchema>();
                if (filterOptions.tagsAnd)
                {
                    tagResults = library.assets.Values
                        .Where(asset => filterOptions.tags.All(tag => asset.metadata.tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
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
                    return library.assets.Values
                        .Where(asset => string.IsNullOrEmpty(asset.metadata.assetType))
                        .Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived))
                        .Where(asset => filterOptions.isChildItem ? ShowAsChild(asset) : ShowAsParent(asset))
                        .ToList();
                }
                else if (!string.IsNullOrEmpty(filterOptions.assetType))
                {
                    return library.assets.Values
                        .Where(asset => asset.metadata.assetType.Equals(filterOptions.assetType, StringComparison.OrdinalIgnoreCase))
                        .Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived))
                        .Where(asset => filterOptions.isChildItem ? ShowAsChild(asset) : ShowAsParent(asset))
                        .ToList();
                }
                return library.GetAllAssets().Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived))
                    .Where(asset => filterOptions.isChildItem ? ShowAsChild(asset) : ShowAsParent(asset))
                    .ToList();
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
                    .Where(asset => string.IsNullOrEmpty(asset.metadata.assetType))
                    .ToList();
            }
            else if (!string.IsNullOrEmpty(filterOptions.assetType))
            {
                filteredAssets = filteredAssets
                    .Where(asset => asset.metadata.assetType.Equals(filterOptions.assetType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return filteredAssets.Intersect(library.GetAssetsByStateArchived(filterOptions.isArchived))
                .Where(asset => filterOptions.isChildItem ? ShowAsChild(asset) : ShowAsParent(asset))
                .ToList();
        }

        public void ClearAssets()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForClearAssets"));
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
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForGetAssetCount"));
                return 0;
            }

            SyncAssetLibrary();
            return library.assetCount;
        }
        #endregion

        #region Tag Management
        public void AddTag(string tag)
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForAddTag"));
                return;
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_tagNullOrEmpty"));
                return;
            }
            if (library.tags.Contains(tag.Trim()))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_tagExists", tag));
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
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForRemoveTag"));
                return;
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_tagNullOrEmpty"));
                return;
            }
            if (!library.tags.Contains(tag.Trim()))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_tagNotFound", tag));
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
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForTagExists"));
                return false;
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_tagNullOrEmptyForExists"));
                return false;
            }

            SyncAssetLibrary();
            return library.TagExists(tag.Trim());
        }

        public IReadOnlyList<string> GetAllTags()
        {
            if (library == null)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForGetAllTags"));
                return new List<string>();
            }

            SyncAssetLibrary();
            return library.tags.ToList();
        }

        public void ClearTags()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForClearTags"));
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
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForGetTagCount"));
                return 0;
            }

            SyncAssetLibrary();
            return library.tagCount;
        }

        public void OptimizeTags()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForOptimizeTags"));
                return;
            }

            SyncAssetLibrary();

            var unusedTags = library.tags.Where(tag => !library.assets.Values.Any(asset => asset.metadata.tags.Contains(tag))).ToList();
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
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForAddAssetType"));
                return;
            }
            if (string.IsNullOrWhiteSpace(assetType))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetTypeNullOrEmpty"));
                return;
            }
            if (library.assetTypes.Contains(assetType.Trim()))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetTypeExists", assetType));
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
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForRemoveAssetType"));
                return;
            }
            if (string.IsNullOrWhiteSpace(assetType))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetTypeNullOrEmpty"));
                return;
            }
            if (!library.assetTypes.Contains(assetType.Trim()))
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_assetTypeNotFound", assetType));
                return;
            }

            SyncAssetLibrary();

            var trimmedAssetType = assetType.Trim();
            var assetsWithType = library.assets.Values
                .Where(asset => asset.metadata.assetType == trimmedAssetType)
                .ToList();

            foreach (var asset in assetsWithType)
            {
                asset.metadata.SetAssetType("");
            }

            library.RemoveAssetType(trimmedAssetType);
            _lastUpdated = DateTime.Now;
            SaveAssetLibrary();
        }

        public bool AssetTypeExists(string assetType)
        {
            if (library == null)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForAssetTypeExists"));
                return false;
            }
            if (string.IsNullOrWhiteSpace(assetType))
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_assetTypeNullOrEmptyForExists"));
                return false;
            }

            SyncAssetLibrary();
            return library.AssetTypeExists(assetType.Trim());
        }

        public IReadOnlyList<string> GetAllAssetTypes()
        {
            if (library == null)
            {
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForGetAllAssetTypes"));
                return new List<string>();
            }

            SyncAssetLibrary();
            return library.assetTypes.ToList();
        }

        public void ClearAssetTypes()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForClearAssetTypes"));
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
                Debug.LogWarning(LocalizationAPI.GetText("VrcAssetManager_message_warning_libraryNotInitializedForGetAssetTypeCount"));
                return 0;
            }

            SyncAssetLibrary();
            return library.assetTypeCount;
        }

        public void OptimizeAssetTypes()
        {
            if (library == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_error_libraryNotInitializedForOptimizeAssetTypes"));
                return;
            }

            SyncAssetLibrary();

            var unusedAssetTypes = library.assetTypes.Where(type => !library.assets.Values.Any(asset => asset.metadata.assetType == type)).ToList();
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