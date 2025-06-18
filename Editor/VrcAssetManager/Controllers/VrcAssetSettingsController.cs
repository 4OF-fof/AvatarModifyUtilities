using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Controllers;
using AMU.Editor.Core.Schema;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    /// <summary>
    /// VRCアセットマネージャーの設定を管理するコントローラ
    /// </summary>
    public static class VrcAssetSettingsController
    {
        private const string SettingsPrefix = "VrcAssetManager_";

        /// <summary>
        /// VRCアセットマネージャーの設定を初期化します
        /// </summary>
        public static void InitializeSettings()
        {
            try
            {
                // 設定項目の初期化
                if (!SettingsController.HasSetting($"{SettingsPrefix}autoScanOnStartup"))
                {
                    SettingsController.SetSetting($"{SettingsPrefix}autoScanOnStartup", false);
                }

                if (!SettingsController.HasSetting($"{SettingsPrefix}defaultScanDirectory"))
                {
                    SettingsController.SetSetting($"{SettingsPrefix}defaultScanDirectory", "Assets");
                }

                if (!SettingsController.HasSetting($"{SettingsPrefix}enableRecursiveScan"))
                {
                    SettingsController.SetSetting($"{SettingsPrefix}enableRecursiveScan", true);
                }

                if (!SettingsController.HasSetting($"{SettingsPrefix}maxCacheSize"))
                {
                    SettingsController.SetSetting($"{SettingsPrefix}maxCacheSize", 1000);
                }

                if (!SettingsController.HasSetting($"{SettingsPrefix}enableAutoRefresh"))
                {
                    SettingsController.SetSetting($"{SettingsPrefix}enableAutoRefresh", true);
                }

                Debug.Log(LocalizationController.GetText("VrcAssetManager_message_success_settingsInitialized"));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_settingsInitFailed"), ex.Message));
            }
        }

        /// <summary>
        /// 起動時の自動スキャンが有効かどうかを取得します
        /// </summary>
        /// <returns>自動スキャンが有効な場合true</returns>
        public static bool GetAutoScanOnStartup()
        {
            return SettingsController.GetSetting($"{SettingsPrefix}autoScanOnStartup", false);
        }

        /// <summary>
        /// 起動時の自動スキャンの設定を変更します
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public static void SetAutoScanOnStartup(bool enabled)
        {
            SettingsController.SetSetting($"{SettingsPrefix}autoScanOnStartup", enabled);
        }

        /// <summary>
        /// デフォルトのスキャンディレクトリを取得します
        /// </summary>
        /// <returns>デフォルトのスキャンディレクトリパス</returns>
        public static string GetDefaultScanDirectory()
        {
            return SettingsController.GetSetting($"{SettingsPrefix}defaultScanDirectory", "Assets");
        }

        /// <summary>
        /// デフォルトのスキャンディレクトリを設定します
        /// </summary>
        /// <param name="directoryPath">ディレクトリパス</param>
        public static void SetDefaultScanDirectory(string directoryPath)
        {
            SettingsController.SetSetting($"{SettingsPrefix}defaultScanDirectory", directoryPath ?? "Assets");
        }

        /// <summary>
        /// 再帰的スキャンが有効かどうかを取得します
        /// </summary>
        /// <returns>再帰的スキャンが有効な場合true</returns>
        public static bool GetEnableRecursiveScan()
        {
            return SettingsController.GetSetting($"{SettingsPrefix}enableRecursiveScan", true);
        }

        /// <summary>
        /// 再帰的スキャンの設定を変更します
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public static void SetEnableRecursiveScan(bool enabled)
        {
            SettingsController.SetSetting($"{SettingsPrefix}enableRecursiveScan", enabled);
        }

        /// <summary>
        /// 最大キャッシュサイズを取得します
        /// </summary>
        /// <returns>最大キャッシュサイズ</returns>
        public static int GetMaxCacheSize()
        {
            return SettingsController.GetSetting($"{SettingsPrefix}maxCacheSize", 1000);
        }

        /// <summary>
        /// 最大キャッシュサイズを設定します
        /// </summary>
        /// <param name="maxSize">最大キャッシュサイズ</param>
        public static void SetMaxCacheSize(int maxSize)
        {
            var validMaxSize = Mathf.Max(100, maxSize); // 最小値を100に制限
            SettingsController.SetSetting($"{SettingsPrefix}maxCacheSize", validMaxSize);
        }

        /// <summary>
        /// 自動リフレッシュが有効かどうかを取得します
        /// </summary>
        /// <returns>自動リフレッシュが有効な場合true</returns>
        public static bool GetEnableAutoRefresh()
        {
            return SettingsController.GetSetting($"{SettingsPrefix}enableAutoRefresh", true);
        }

        /// <summary>
        /// 自動リフレッシュの設定を変更します
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public static void SetEnableAutoRefresh(bool enabled)
        {
            SettingsController.SetSetting($"{SettingsPrefix}enableAutoRefresh", enabled);
        }

        /// <summary>
        /// フィルタリング用のカテゴリ設定を取得します
        /// </summary>
        /// <returns>有効なカテゴリのリスト</returns>
        public static List<string> GetEnabledCategories()
        {
            var categoriesString = SettingsController.GetSetting($"{SettingsPrefix}enabledCategories", "");
            if (string.IsNullOrEmpty(categoriesString))
            {
                return new List<string> { "Prefabs", "Models", "Textures", "Materials", "Shaders", "Scripts", "Scenes", "Packages", "Other" };
            }

            return categoriesString.Split(';').Where(c => !string.IsNullOrEmpty(c)).ToList();
        }

        /// <summary>
        /// フィルタリング用のカテゴリ設定を変更します
        /// </summary>
        /// <param name="enabledCategories">有効にするカテゴリのリスト</param>
        public static void SetEnabledCategories(List<string> enabledCategories)
        {
            var categoriesString = enabledCategories != null ? string.Join(";", enabledCategories) : "";
            SettingsController.SetSetting($"{SettingsPrefix}enabledCategories", categoriesString);
        }

        /// <summary>
        /// 除外するファイル拡張子の設定を取得します
        /// </summary>
        /// <returns>除外するファイル拡張子のリスト</returns>
        public static List<string> GetExcludedFileExtensions()
        {
            var extensionsString = SettingsController.GetSetting($"{SettingsPrefix}excludedExtensions", ".meta;.tmp");
            return extensionsString.Split(';').Where(e => !string.IsNullOrEmpty(e)).ToList();
        }

        /// <summary>
        /// 除外するファイル拡張子の設定を変更します
        /// </summary>
        /// <param name="excludedExtensions">除外するファイル拡張子のリスト</param>
        public static void SetExcludedFileExtensions(List<string> excludedExtensions)
        {
            var extensionsString = excludedExtensions != null ? string.Join(";", excludedExtensions) : "";
            SettingsController.SetSetting($"{SettingsPrefix}excludedExtensions", extensionsString);
        }

        /// <summary>
        /// 検索履歴の最大保持数を取得します
        /// </summary>
        /// <returns>検索履歴の最大保持数</returns>
        public static int GetMaxSearchHistory()
        {
            return SettingsController.GetSetting($"{SettingsPrefix}maxSearchHistory", 20);
        }

        /// <summary>
        /// 検索履歴の最大保持数を設定します
        /// </summary>
        /// <param name="maxHistory">検索履歴の最大保持数</param>
        public static void SetMaxSearchHistory(int maxHistory)
        {
            var validMaxHistory = Mathf.Max(5, maxHistory); // 最小値を5に制限
            SettingsController.SetSetting($"{SettingsPrefix}maxSearchHistory", validMaxHistory);
        }

        /// <summary>
        /// 全ての設定をデフォルト値にリセットします
        /// </summary>
        public static void ResetToDefaults()
        {
            try
            {
                SetAutoScanOnStartup(false);
                SetDefaultScanDirectory("Assets");
                SetEnableRecursiveScan(true);
                SetMaxCacheSize(1000);
                SetEnableAutoRefresh(true);
                SetEnabledCategories(new List<string> { "Prefabs", "Models", "Textures", "Materials", "Shaders", "Scripts", "Scenes", "Packages", "Other" });
                SetExcludedFileExtensions(new List<string> { ".meta", ".tmp" });
                SetMaxSearchHistory(20);

                Debug.Log(LocalizationController.GetText("VrcAssetManager_message_success_settingsReset"));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_settingsResetFailed"), ex.Message));
            }
        }

        /// <summary>
        /// 現在の設定値を文字列として出力します（デバッグ用）
        /// </summary>
        /// <returns>設定値の文字列表現</returns>
        public static string GetSettingsString()
        {
            var settings = new Dictionary<string, object>
            {
                { "AutoScanOnStartup", GetAutoScanOnStartup() },
                { "DefaultScanDirectory", GetDefaultScanDirectory() },
                { "EnableRecursiveScan", GetEnableRecursiveScan() },
                { "MaxCacheSize", GetMaxCacheSize() },
                { "EnableAutoRefresh", GetEnableAutoRefresh() },
                { "EnabledCategories", string.Join(", ", GetEnabledCategories()) },
                { "ExcludedExtensions", string.Join(", ", GetExcludedFileExtensions()) },
                { "MaxSearchHistory", GetMaxSearchHistory() }
            };

            var settingsText = string.Join("\n", settings.Select(kv => $"{kv.Key}: {kv.Value}"));
            return settingsText;
        }
    }
}
