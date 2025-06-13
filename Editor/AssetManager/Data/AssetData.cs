using System;
using System.Collections.Generic;
using UnityEngine;

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
}
