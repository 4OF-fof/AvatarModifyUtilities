using System;
using System.Collections.Generic;
using UnityEngine;
using AMU.Data.TagType;

namespace AMU.AssetManager.Data
{
    [Serializable]
    public class AssetTypeManager
    {
        private static List<string> _defaultTypes = new List<string>
        {
            "Avatar",
            "Clothing",
            "Accessory",
            "Texture",
            "Material",
            "Animation",
            "Shader",
            "Prefab",
            "Script",
            "Other"
        };

        public static List<string> DefaultTypes => new List<string>(_defaultTypes);

        private static List<string> _customTypes = new List<string>();
        public static List<string> CustomTypes => new List<string>(_customTypes);

        public static List<string> AllTypes
        {
            get
            {
                var allTypes = new List<string>(_defaultTypes);
                allTypes.AddRange(_customTypes);
                return allTypes;
            }
        }

        public static void AddCustomType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return;

            typeName = typeName.Trim();
            if (!_defaultTypes.Contains(typeName) && !_customTypes.Contains(typeName))
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
            return _defaultTypes.Contains(typeName);
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
        public static List<string> GetAllTypesFromTagTypeManager()
        {
            var types = new List<string>();
            var allTypes = TagTypeManager.GetVisibleTypes();

            foreach (var type in allTypes)
            {
                types.Add(type.name);
            }

            return types;
        }
        [Serializable]
        private class SerializableStringList
        {
            public List<string> items = new List<string>();
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
            };
        }
    }

    [Serializable]
    public class AssetLibrary
    {
        public string version;
        public DateTime lastUpdated;
        public List<AssetInfo> assets;

        public AssetLibrary()
        {
            version = "1.0";
            lastUpdated = DateTime.Now;
            assets = new List<AssetInfo>();
        }
    }

    [Serializable]
    public class AssetTagManager
    {
        // タグ管理用の新しいクラス
        public static List<string> GetAllTagsFromTagTypeManager()
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
