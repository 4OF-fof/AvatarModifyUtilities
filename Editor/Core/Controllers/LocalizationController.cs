using System.IO;
using System.Collections.Generic;

using UnityEngine;
using Newtonsoft.Json;

namespace AMU.Editor.Core.Controller
{
    /// <summary>
    /// ローカライゼーション機能を管理するコントローラ
    /// </summary>
    public static class LocalizationController
    {
        private static Dictionary<string, string> _localizedTexts = new Dictionary<string, string>();
        private static Dictionary<string, string> _fallbackTexts = new Dictionary<string, string>();

        /// <summary>
        /// 現在の言語コード
        /// </summary>
        public static string CurrentLanguage { get; private set; } = "ja_jp";

        /// <summary>
        /// 指定された言語コードの言語ファイルを読み込みます
        /// </summary>
        /// <param name="languageCode">言語コード (例: ja_jp, en_us)</param>
        public static void LoadLanguage(string languageCode)
        {
            var rootDir = Path.Combine(Application.dataPath, "AvatarModifyUtilities/Editor");

            // 指定された言語のテキストを読み込み
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
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to load language file: {path}. Error: {ex.Message}");
                }
            }

            _localizedTexts = mergedDict;
            CurrentLanguage = languageCode;

            // 英語のフォールバックテキストを読み込み（指定された言語が英語でない場合）
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

        /// <summary>
        /// 英語のフォールバックテキストを読み込みます
        /// </summary>
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
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to load fallback language file: {path}. Error: {ex.Message}");
                }
            }

            _fallbackTexts = mergedDict;
        }

        /// <summary>
        /// 指定されたキーのローカライズされたテキストを取得します
        /// </summary>
        /// <param name="key">テキストキー</param>
        /// <returns>ローカライズされたテキスト（見つからない場合は英語のフォールバックテキスト、それも見つからない場合はキーをそのまま返す）</returns>
        public static string GetText(string key)
        {
            if (_localizedTexts.TryGetValue(key, out var value))
                return value;

            // フォールバックテキスト（英語）を試す
            if (_fallbackTexts.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return key;
        }

        /// <summary>
        /// 現在読み込まれているローカライズテキストの数を取得します
        /// </summary>
        /// <returns>ローカライズテキストの数</returns>
        public static int GetLoadedTextCount()
        {
            return _localizedTexts.Count;
        }

        /// <summary>
        /// 現在読み込まれているフォールバックテキストの数を取得します
        /// </summary>
        /// <returns>フォールバックテキストの数</returns>
        public static int GetFallbackTextCount()
        {
            return _fallbackTexts.Count;
        }

        /// <summary>
        /// 指定されたキーが存在するかどうかを確認します
        /// </summary>
        /// <param name="key">確認するキー</param>
        /// <returns>キーが存在する場合true</returns>
        public static bool HasKey(string key)
        {
            return _localizedTexts.ContainsKey(key);
        }
    }
}
