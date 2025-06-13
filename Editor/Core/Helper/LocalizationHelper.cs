using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace AMU.Data.Lang
{
    public static partial class LocalizationManager
    {
        private static Dictionary<string, string> _localizedTexts = new Dictionary<string, string>();
        public static string CurrentLanguage { get; private set; } = "ja_jp"; public static void LoadLanguage(string languageCode)
        {
            var rootDir = Path.Combine(Application.dataPath, "AvatarModifyUtilities/Editor");
            var searchPattern = $"{languageCode}.json";
            var langFiles = Directory.GetFiles(rootDir, searchPattern, SearchOption.AllDirectories);
            var mergedDict = new Dictionary<string, string>();
            foreach (var path in langFiles)
            {
                var json = File.ReadAllText(path);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        mergedDict[kv.Key] = kv.Value;
                    }
                }
            }
            _localizedTexts = mergedDict;
            CurrentLanguage = languageCode;
        }
        public static string GetText(string key)
        {
            if (_localizedTexts.TryGetValue(key, out var value))
                return value;
            return key;
        }
    }
}
