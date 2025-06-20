using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using Newtonsoft.Json;

namespace AMU.Editor.Core.Controller
{
    public static class LocalizationController
    {
        private static Dictionary<string, string> _localizedTexts = new Dictionary<string, string>();
        private static Dictionary<string, string> _fallbackTexts = new Dictionary<string, string>();

        public static string CurrentLanguage { get; private set; } = "ja_jp";

        public static void LoadLanguage(string languageCode)
        {
            var rootDir = Path.Combine(Application.dataPath, "AvatarModifyUtilities/Editor");

            var searchPattern = $"{languageCode}.json";
            var langFiles = Directory.GetFiles(rootDir, searchPattern, SearchOption.AllDirectories);
            var mergedDict = new Dictionary<string, string>();

            foreach (var path in langFiles)
            {
                try
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
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load language file: {path}. Error: {ex.Message}");
                }
            }

            _localizedTexts = mergedDict;
            CurrentLanguage = languageCode;

            if (languageCode != "en_us")
            {
                LoadFallbackTexts();
            }
            else
            {
                _fallbackTexts = new Dictionary<string, string>(_localizedTexts);
            }

            Debug.Log($"[LocalizationController] Loaded {_localizedTexts.Count} localized texts for language: {languageCode}");
            if (_fallbackTexts.Count > 0)
            {
                Debug.Log($"[LocalizationController] Loaded {_fallbackTexts.Count} fallback texts (en_us)");
            }
        }

        private static void LoadFallbackTexts()
        {
            var rootDir = Path.Combine(Application.dataPath, "AvatarModifyUtilities/Editor");
            var searchPattern = "en_us.json";
            var langFiles = Directory.GetFiles(rootDir, searchPattern, SearchOption.AllDirectories);
            var mergedDict = new Dictionary<string, string>();

            foreach (var path in langFiles)
            {
                try
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
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load fallback language file: {path}. Error: {ex.Message}");
                }
            }

            _fallbackTexts = mergedDict;
        }

        public static string GetText(string key)
        {
            if (_localizedTexts.TryGetValue(key, out var value))
                return value;

            if (_fallbackTexts.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return key;
        }

        public static int GetLoadedTextCount()
        {
            return _localizedTexts.Count;
        }

        public static int GetFallbackTextCount()
        {
            return _fallbackTexts.Count;
        }

        public static bool HasKey(string key)
        {
            return _localizedTexts.ContainsKey(key);
        }
    }
}
