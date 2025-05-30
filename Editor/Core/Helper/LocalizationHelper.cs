using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AMU.Data.Lang
{
    public static partial class LocalizationManager
    {
        private static Dictionary<string, string> _localizedTexts = new Dictionary<string, string>();
        public static string CurrentLanguage { get; private set; } = "ja_jp";

        public static void LoadLanguage(string languageCode)
        {
            var rootDir = Path.Combine(Application.dataPath, "AMU/Editor");
            var searchPattern = $"{languageCode}.json";
            var langFiles = Directory.GetFiles(rootDir, searchPattern, SearchOption.AllDirectories);
            var mergedDict = new Dictionary<string, string>();
            foreach (var path in langFiles)
            {
                var json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<LocalizationWrapper>(json);
                var dict = wrapper.ToDictionary();
                foreach (var kv in dict)
                {
                    mergedDict[kv.Key] = kv.Value;
                }
            }
            _localizedTexts = mergedDict;
            CurrentLanguage = languageCode;
        }

        [System.Serializable]
        private partial class LocalizationWrapper
        {
            public Dictionary<string, string> ToDictionary()
            {
                var dict = new Dictionary<string, string>();
                var fields = typeof(LocalizationWrapper).GetFields();
                foreach (var field in fields)
                {
                    var value = field.GetValue(this) as string;
                    if (value != null)
                    {
                        dict[field.Name] = value;
                    }
                }
                return dict;
            }
        }

        public static string GetText(string key)
        {
            if (_localizedTexts.TryGetValue(key, out var value))
                return value;
            return key;
        }
    }
}
