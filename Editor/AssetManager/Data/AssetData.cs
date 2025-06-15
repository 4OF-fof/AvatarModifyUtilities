using System;
using System.Collections.Generic;
using UnityEngine;
using AMU.Data.TagType;

namespace AMU.AssetManager.Data
{
    [Serializable]
    public class AssetTypeManager
    {
        public static List<string> DefaultTypes
        {
            get
            {
                var types = new List<string>();
                var allTypes = TagTypeManager.GetVisibleTypes();
                foreach (var type in allTypes)
                {
                    types.Add(type.name);
                }
                return types;
            }
        }

        private static List<string> _customTypes = new List<string>();
        public static List<string> CustomTypes => new List<string>(_customTypes); public static List<string> AllTypes
        {
            get
            {
                var allTypes = DefaultTypes;
                allTypes.AddRange(_customTypes);
                return allTypes;
            }
        }
        public static void AddCustomType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return;

            typeName = typeName.Trim();
            var defaultTypes = DefaultTypes;
            if (!defaultTypes.Contains(typeName) && !_customTypes.Contains(typeName))
            {
                _customTypes.Add(typeName);
                SaveCustomTypes();
            }
        }

        public static void RemoveCustomType(string typeName)
        {
            if (_customTypes.Remove(typeName))
            {
                SaveCustomTypes();
            }
        }
        public static bool IsDefaultType(string typeName)
        {
            return DefaultTypes.Contains(typeName);
        }

        public static void SaveCustomTypes()
        {
            var json = JsonUtility.ToJson(new SerializableStringList { items = _customTypes });
            UnityEditor.EditorPrefs.SetString("AssetManager_CustomTypes", json);
        }

        public static void LoadCustomTypes()
        {
            var json = UnityEditor.EditorPrefs.GetString("AssetManager_CustomTypes", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<SerializableStringList>(json);
                    _customTypes = data.items ?? new List<string>();
                }
                catch
                {
                    _customTypes = new List<string>();
                }
            }
        }
        [Serializable]
        private class SerializableStringList
        {
            public List<string> items = new List<string>();
        }
    }
    [Serializable]
    public class BoothItem
    {
        public string boothItemUrl;
        public string boothfileName;
        public string boothDownloadUrl;

        public BoothItem()
        {
            boothItemUrl = null;
            boothfileName = null;
            boothDownloadUrl = null;
        }

        public BoothItem(string itemUrl, string fileName, string downloadUrl)
        {
            boothItemUrl = itemUrl;
            boothfileName = fileName;
            boothDownloadUrl = downloadUrl;
        }

        public BoothItem Clone()
        {
            return new BoothItem
            {
                boothItemUrl = this.boothItemUrl,
                boothfileName = this.boothfileName,
                boothDownloadUrl = this.boothDownloadUrl
            };
        }

        public bool HasData()
        {
            return !string.IsNullOrEmpty(boothItemUrl) ||
                   !string.IsNullOrEmpty(boothfileName) ||
                   !string.IsNullOrEmpty(boothDownloadUrl);
        }
    }
    [Serializable]
    public class AssetInfo
    {
        public string uid;
        public string name;
        public string description;
        public string assetType;
        public string filePath;
        public string thumbnailPath;
        public string authorName;
        public DateTime createdDate;
        public long fileSize; public List<string> tags;
        public List<string> dependencies;
        public bool isFavorite;
        public bool isHidden;
        public BoothItem boothItem;
        public string groupUid; // グループのUUID（グループに属さない場合はnullまたは空文字）

        public AssetInfo()
        {
            uid = Guid.NewGuid().ToString();
            name = "";
            description = "";
            assetType = "Other";
            filePath = "";
            thumbnailPath = "";
            authorName = "";
            createdDate = DateTime.Now;
            fileSize = 0;
            tags = new List<string>();
            dependencies = new List<string>();
            isFavorite = false;
            isHidden = false;
            boothItem = null; // デフォルトでは値が割り振られていない
            groupUid = null; // デフォルトではグループに属さない
        }

        public AssetInfo Clone()
        {
            return new AssetInfo
            {
                uid = this.uid,
                name = this.name,
                description = this.description,
                assetType = this.assetType,
                filePath = this.filePath,
                thumbnailPath = this.thumbnailPath,
                authorName = this.authorName,
                createdDate = this.createdDate,
                fileSize = this.fileSize,
                tags = new List<string>(this.tags),
                dependencies = new List<string>(this.dependencies),
                isFavorite = this.isFavorite,
                isHidden = this.isHidden,
                boothItem = this.boothItem?.Clone(),
                groupUid = this.groupUid,
            };
        }

        public bool IsGrouped()
        {
            return !string.IsNullOrEmpty(groupUid);
        }
    }
    [Serializable]
    public class AssetLibrary
    {
        public DateTime lastUpdated;
        public List<AssetInfo> assets;
        public List<GroupInfo> groups;

        public AssetLibrary()
        {
            lastUpdated = DateTime.Now;
            assets = new List<AssetInfo>();
            groups = new List<GroupInfo>();
        }
    }
    [Serializable]
    public class AssetTagManager
    {
        public static List<string> GetAllTags()
        {
            var tags = new List<string>();
            var allTags = TagTypeManager.GetVisibleTags();

            foreach (var tag in allTags)
            {
                tags.Add(tag.name);
            }

            return tags;
        }

        public static List<string> GetTagsByCategory(string category)
        {
            var tags = new List<string>();
            var categoryTags = TagTypeManager.GetTagsByCategory(category);

            foreach (var tag in categoryTags)
            {
                tags.Add(tag.name);
            }

            return tags;
        }

        public static Color GetTagColor(string tagName)
        {
            return TagTypeManager.GetTagColor(tagName);
        }
        public static bool AddCustomTag(string tagName, string color = "#CCCCCC")
        {
            if (string.IsNullOrWhiteSpace(tagName)) return false;

            tagName = tagName.Trim();
            var existingTag = TagTypeManager.GetTagByName(tagName);

            if (existingTag == null)
            {
                var newTag = new TagItem(tagName, color);
                TagTypeManager.AddTag(newTag);
                return true;
            }

            return false;
        }

        public static bool RemoveCustomTag(string tagName)
        {
            var tag = TagTypeManager.GetTagByName(tagName);
            if (tag != null)
            {
                TagTypeManager.RemoveTag(tag.id);
                return true;
            }

            return false;
        }
        public static List<string> GetTagCategories()
        {
            return TagTypeManager.GetTagCategories();
        }
    }
    [Serializable]
    public class GroupInfo
    {
        public string groupUid;
        public string groupName;
        public string description;
        public string thumbnailPath;
        public List<string> childAssetUids;
        public List<string> tags;
        public string authorName;
        public DateTime createdDate;
        public bool isFavorite;
        public bool isHidden;

        public GroupInfo()
        {
            groupUid = Guid.NewGuid().ToString();
            groupName = "";
            description = "";
            thumbnailPath = "";
            childAssetUids = new List<string>();
            tags = new List<string>();
            authorName = "";
            createdDate = DateTime.Now;
            isFavorite = false;
            isHidden = false;
        }

        public GroupInfo Clone()
        {
            return new GroupInfo
            {
                groupUid = this.groupUid,
                groupName = this.groupName,
                description = this.description,
                thumbnailPath = this.thumbnailPath,
                childAssetUids = new List<string>(this.childAssetUids),
                tags = new List<string>(this.tags),
                authorName = this.authorName,
                createdDate = this.createdDate,
                isFavorite = this.isFavorite,
                isHidden = this.isHidden
            };
        }

        public int GetChildCount()
        {
            return childAssetUids?.Count ?? 0;
        }

        public bool HasChild(string assetUid)
        {
            return childAssetUids != null && childAssetUids.Contains(assetUid);
        }

        public void AddChild(string assetUid)
        {
            if (childAssetUids == null)
                childAssetUids = new List<string>();

            if (!childAssetUids.Contains(assetUid))
                childAssetUids.Add(assetUid);
        }

        public void RemoveChild(string assetUid)
        {
            childAssetUids?.Remove(assetUid);
        }
    }
    [Serializable]
    public class GroupManager
    {
        public static GroupInfo CreateGroup(string groupName)
        {
            var group = new GroupInfo();
            group.groupName = groupName;
            return group;
        }

        public static GroupInfo CreateGroupFromAssets(string groupName, List<AssetInfo> assets)
        {
            var group = CreateGroup(groupName);

            foreach (var asset in assets)
            {
                group.AddChild(asset.uid);
                asset.groupUid = group.groupUid;
            }

            // 最初のアセットのサムネイルをグループのサムネイルとして使用
            if (assets.Count > 0 && !string.IsNullOrEmpty(assets[0].thumbnailPath))
            {
                group.thumbnailPath = assets[0].thumbnailPath;
            }

            return group;
        }

        public static void AddAssetToGroup(GroupInfo group, AssetInfo asset)
        {
            if (group == null || asset == null) return;

            // 既存のグループから削除
            if (!string.IsNullOrEmpty(asset.groupUid))
            {
                RemoveAssetFromGroup(asset);
            }

            group.AddChild(asset.uid);
            asset.groupUid = group.groupUid;
        }

        public static void RemoveAssetFromGroup(AssetInfo asset)
        {
            if (asset == null) return;
            asset.groupUid = null;
        }

        public static void RemoveAssetFromGroup(GroupInfo group, AssetInfo asset)
        {
            if (group == null || asset == null) return;

            group.RemoveChild(asset.uid);
            if (asset.groupUid == group.groupUid)
            {
                asset.groupUid = null;
            }
        }

        public static List<AssetInfo> GetGroupAssets(GroupInfo group, List<AssetInfo> allAssets)
        {
            if (group == null || allAssets == null) return new List<AssetInfo>();

            var groupAssets = new List<AssetInfo>();
            foreach (var asset in allAssets)
            {
                if (group.HasChild(asset.uid))
                {
                    groupAssets.Add(asset);
                }
            }
            return groupAssets;
        }

        public static void UngroupAssets(GroupInfo group, List<AssetInfo> allAssets)
        {
            if (group == null || allAssets == null) return;

            foreach (var asset in allAssets)
            {
                if (asset.groupUid == group.groupUid)
                {
                    asset.groupUid = null;
                }
            }

            group.childAssetUids.Clear();
        }

        public static bool IsValidGroup(GroupInfo group)
        {
            return group != null &&
                   !string.IsNullOrEmpty(group.groupUid) &&
                   !string.IsNullOrEmpty(group.groupName) &&
                   group.GetChildCount() > 0;
        }

        public static void CleanupEmptyGroups(AssetLibrary library)
        {
            if (library?.groups == null) return;

            for (int i = library.groups.Count - 1; i >= 0; i--)
            {
                var group = library.groups[i];
                var actualChildren = GetGroupAssets(group, library.assets);

                if (actualChildren.Count == 0)
                {
                    library.groups.RemoveAt(i);
                }
                else
                {
                    // グループの子リストを実際のアセットと同期
                    group.childAssetUids.Clear();
                    foreach (var asset in actualChildren)
                    {
                        group.childAssetUids.Add(asset.uid);
                    }
                }
            }
        }
    }
}
