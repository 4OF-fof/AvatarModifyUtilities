using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace AMU.Data.TagType
{
    /// <summary>
    /// タグとタイプの一覧をJSONで管理するマネージャークラス
    /// </summary>
    [Serializable]
    public class TagTypeData
    {
        public string version = "1.0";
        public DateTime lastUpdated = DateTime.Now;
        public List<TagItem> tags = new List<TagItem>();
        public List<TypeItem> types = new List<TypeItem>();
    }

    [Serializable]
    public class TagItem
    {
        public string id;
        public string name;
        public string color; // HEX色コード
        public DateTime createdDate = DateTime.Now;

        public TagItem()
        {
            id = Guid.NewGuid().ToString();
        }

        public TagItem(string name, string color = "#FFFFFF")
        {
            id = Guid.NewGuid().ToString();
            this.name = name;
            this.color = color;
        }
    }

    [Serializable]
    public class TypeItem
    {
        public string id;
        public string name;
        public string description;
        public bool isDefault = false;
        public bool isVisible = true;
        public int sortOrder = 0;
        public DateTime createdDate = DateTime.Now;

        public TypeItem()
        {
            id = Guid.NewGuid().ToString();
        }

        public TypeItem(string name, string description = "", bool isDefault = false)
        {
            id = Guid.NewGuid().ToString();
            this.name = name;
            this.description = description;
            this.isDefault = isDefault;
        }
    }

    public static class TagTypeManager
    {
        private static TagTypeData _data;
        private static string _filePath;
        private static DateTime _lastLoadTime;

        public static event System.Action OnDataChanged;

        static TagTypeManager()
        {
            InitializeFilePath();
            LoadData();
        }

        private static void InitializeFilePath()
        {
            string dataDir = UnityEditor.EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            dataDir = Path.Combine(dataDir, "AssetManager");

            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            _filePath = Path.Combine(dataDir, "TagTypeData.json");
        }

        public static TagTypeData Data => _data ?? new TagTypeData();

        public static void LoadData()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _data = JsonConvert.DeserializeObject<TagTypeData>(json);
                    _lastLoadTime = File.GetLastWriteTime(_filePath);
                }
                else
                {
                    _data = CreateDefaultData();
                    SaveData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"TagTypeData読み込みエラー: {ex.Message}");
                _data = CreateDefaultData();
            }
        }

        public static void SaveData()
        {
            try
            {
                if (_data == null)
                {
                    _data = CreateDefaultData();
                }
                _data.lastUpdated = DateTime.Now;
                var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(_filePath, json);

                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"TagTypeData保存エラー: {ex.Message}");
            }
        }

        private static TagTypeData CreateDefaultData()
        {
            var data = new TagTypeData();

            // デフォルトタイプ
            var defaultTypes = new[]
            {
                new TypeItem("Avatar", "アバター関連", true),
                new TypeItem("Clothing", "衣装・服", true),
                new TypeItem("Accessory", "アクセサリー", true),
                new TypeItem("Other", "その他", true)
            };

            for (int i = 0; i < defaultTypes.Length; i++)
            {
                defaultTypes[i].sortOrder = i;
            }

            data.types.AddRange(defaultTypes);

            return data;
        }

        public static void CheckAndReloadIfNeeded()
        {
            if (File.Exists(_filePath))
            {
                var currentWriteTime = File.GetLastWriteTime(_filePath);
                if (currentWriteTime != _lastLoadTime)
                {
                    LoadData();
                }
            }
        }

        // タグ関連メソッド
        public static List<TagItem> GetAllTags()
        {
            CheckAndReloadIfNeeded();
            return new List<TagItem>(Data.tags);
        }

        public static List<TagItem> GetVisibleTags()
        {
            CheckAndReloadIfNeeded();
            return new List<TagItem>(Data.tags);
        }

        public static TagItem GetTagById(string id)
        {
            CheckAndReloadIfNeeded();
            return Data.tags.Find(t => t.id == id);
        }

        public static TagItem GetTagByName(string name)
        {
            CheckAndReloadIfNeeded();
            return Data.tags.Find(t => t.name == name);
        }

        public static void AddTag(TagItem tag)
        {
            if (tag == null || GetTagByName(tag.name) != null) return;

            Data.tags.Add(tag);
            SaveData();
        }

        public static void UpdateTag(TagItem tag)
        {
            if (tag == null) return;

            var existingTag = GetTagById(tag.id);
            if (existingTag != null)
            {
                var index = Data.tags.IndexOf(existingTag);
                Data.tags[index] = tag;
                SaveData();
            }
        }

        public static void RemoveTag(string id)
        {
            var tag = GetTagById(id);
            if (tag != null)
            {
                Data.tags.Remove(tag);
                SaveData();
            }
        }

        // タイプ関連メソッド
        public static List<TypeItem> GetAllTypes()
        {
            CheckAndReloadIfNeeded();
            return new List<TypeItem>(Data.types);
        }

        public static List<TypeItem> GetVisibleTypes()
        {
            CheckAndReloadIfNeeded();
            return Data.types.FindAll(t => t.isVisible);
        }

        public static TypeItem GetTypeById(string id)
        {
            CheckAndReloadIfNeeded();
            return Data.types.Find(t => t.id == id);
        }

        public static TypeItem GetTypeByName(string name)
        {
            CheckAndReloadIfNeeded();
            return Data.types.Find(t => t.name == name);
        }

        public static void AddType(TypeItem type)
        {
            if (type == null || GetTypeByName(type.name) != null) return;

            Data.types.Add(type);
            SaveData();
        }

        public static void UpdateType(TypeItem type)
        {
            if (type == null) return;

            var existingType = GetTypeById(type.id);
            if (existingType != null)
            {
                var index = Data.types.IndexOf(existingType);
                Data.types[index] = type;
                SaveData();
            }
        }

        public static void RemoveType(string id)
        {
            var type = GetTypeById(id);
            if (type != null && !type.isDefault)
            {
                Data.types.Remove(type);
                SaveData();
            }
        }

        // ユーティリティメソッド
        public static List<string> GetTagCategories()
        {
            CheckAndReloadIfNeeded();
            return new List<string>();
        }

        public static List<TagItem> GetTagsByCategory(string category)
        {
            CheckAndReloadIfNeeded();
            return new List<TagItem>(Data.tags);
        }

        public static Color GetTagColor(string tagName)
        {
            var tag = GetTagByName(tagName);
            if (tag != null && ColorUtility.TryParseHtmlString(tag.color, out Color color))
            {
                return color;
            }
            return Color.white;
        }

        public static void ResetToDefaults()
        {
            _data = CreateDefaultData();
            SaveData();
        }

        public static string GetDataFilePath()
        {
            return _filePath;
        }
    }
}
