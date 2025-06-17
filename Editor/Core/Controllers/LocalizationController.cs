using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace AMU.Data.Lang
{
    /// <summary>
    /// ローカライゼーション機能を管理するマネージャー（後方互換性のため）
    /// </summary>
    public static partial class LocalizationManager
    {
        /// <summary>
        /// 現在の言語コード
        /// </summary>
        public static string CurrentLanguage => AMU.Editor.Core.Controllers.LocalizationController.CurrentLanguage;

        /// <summary>
        /// 指定された言語コードの言語ファイルを読み込みます
        /// </summary>
        public static void LoadLanguage(string languageCode)
        {
            AMU.Editor.Core.Controllers.LocalizationController.LoadLanguage(languageCode);
        }

        /// <summary>
        /// 指定されたキーのローカライズされたテキストを取得します
        /// </summary>
        public static string GetText(string key)
        {
            return AMU.Editor.Core.Controllers.LocalizationController.GetText(key);
        }
    }
}

namespace AMU.Editor.Core.Controllers
{
    /// <summary>
    /// ローカライゼーション機能を管理するコントローラ
    /// </summary>
    public static class LocalizationController
    {
        private static Dictionary<string, string> _localizedTexts = new Dictionary<string, string>();

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

            Debug.Log($"[LocalizationController] Loaded {_localizedTexts.Count} localized texts for language: {languageCode}");
        }

        /// <summary>
        /// 指定されたキーのローカライズされたテキストを取得します
        /// </summary>
        /// <param name="key">テキストキー</param>
        /// <returns>ローカライズされたテキスト（見つからない場合はキーをそのまま返す）</returns>
        public static string GetText(string key)
        {
            if (_localizedTexts.TryGetValue(key, out var value))
                return value;
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
