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
        public long fileSize;
        public List<string> tags;
        public List<string> dependencies;
        public bool isFavorite;
        public bool isHidden;
        public BoothItem boothItem;
        public List<string> importFiles;  // インポートファイルのパスリスト

        // グループ機能用の追加プロパティ
        public string parentGroupId;  // 親グループのUID
        public List<string> childAssetIds;  // 子アセットのUIDリスト（このアセットがグループの場合）
        public bool isGroup;  // このアセットがグループかどうか
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
            importFiles = new List<string>();  // インポートファイルリストの初期化

            // グループ機能用プロパティの初期化
            parentGroupId = null;
            childAssetIds = new List<string>();
            isGroup = false;
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
                importFiles = new List<string>(this.importFiles),

                // グループ機能用プロパティのコピー
                parentGroupId = this.parentGroupId,
                childAssetIds = new List<string>(this.childAssetIds),
                isGroup = this.isGroup,
            };
        }

        // グループ機能用のヘルパーメソッド
        public bool HasParent()
        {
            return !string.IsNullOrEmpty(parentGroupId);
        }

        public bool HasChildren()
        {
            return childAssetIds != null && childAssetIds.Count > 0;
        }

        public void AddChildAsset(string childAssetId)
        {
            if (childAssetIds == null)
                childAssetIds = new List<string>();

            if (!childAssetIds.Contains(childAssetId))
            {
                childAssetIds.Add(childAssetId);
                isGroup = true; // 子要素を持つ場合はグループとして扱う
            }
        }

        public void RemoveChildAsset(string childAssetId)
        {
            if (childAssetIds != null)
            {
                childAssetIds.Remove(childAssetId);
                if (childAssetIds.Count == 0)
                {
                    isGroup = false; // 子要素がなくなったらグループではない
                }
            }
        }

        public void SetParentGroup(string parentId)
        {
            parentGroupId = parentId;
        }

        public void RemoveFromParentGroup()
        {
            parentGroupId = null;
        }

        public bool IsVisibleInList()
        {
            // 親グループが存在するアセットは非表示
            return !HasParent();
        }
    }
    [Serializable]
    public class AssetLibrary
    {
        public DateTime lastUpdated;
        public List<AssetInfo> assets;

        public AssetLibrary()
        {
            lastUpdated = DateTime.Now;
            assets = new List<AssetInfo>();
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
}
