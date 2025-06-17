using UnityEditor;
using AMU.Editor.AutoVariant.Schema;

namespace AMU.Editor.AutoVariant.Controllers
{
    /// <summary>
    /// AutoVariant設定の永続データ管理とアクセス制御
    /// </summary>
    public static class AutoVariantController
    {
        /// <summary>
        /// AutoVariant設定を初期化する
        /// </summary>
        public static void InitializeSettings()
        {
            // デフォルト値が設定されていない場合は設定
            if (!EditorPrefs.HasKey("Setting.AutoVariant_enableAutoVariant"))
            {
                SetAutoVariantEnabled(false);
            }

            if (!EditorPrefs.HasKey("Setting.AutoVariant_enablePrebuild"))
            {
                SetPrebuildEnabled(true);
            }

            if (!EditorPrefs.HasKey("Setting.AutoVariant_includeAllAssets"))
            {
                SetIncludeAllAssets(true);
            }
        }

        /// <summary>
        /// AutoVariant機能の有効/無効を設定する
        /// </summary>
        /// <param name="enabled">有効にするかどうか</param>
        public static void SetAutoVariantEnabled(bool enabled)
        {
            EditorPrefs.SetBool("Setting.AutoVariant_enableAutoVariant", enabled);
        }

        /// <summary>
        /// AutoVariant機能が有効かどうかを取得する
        /// </summary>
        /// <returns>有効かどうか</returns>
        public static bool IsAutoVariantEnabled()
        {
            return PrebuildSettings.IsAutoVariantEnabled;
        }

        /// <summary>
        /// Prebuild処理の有効/無効を設定する
        /// </summary>
        /// <param name="enabled">有効にするかどうか</param>
        public static void SetPrebuildEnabled(bool enabled)
        {
            EditorPrefs.SetBool("Setting.AutoVariant_enablePrebuild", enabled);
        }

        /// <summary>
        /// Prebuild処理が有効かどうかを取得する
        /// </summary>
        /// <returns>有効かどうか</returns>
        public static bool IsPrebuildEnabled()
        {
            return PrebuildSettings.IsOptimizationEnabled;
        }

        /// <summary>
        /// すべてのアセットを含める設定を変更する
        /// </summary>
        /// <param name="include">含めるかどうか</param>
        public static void SetIncludeAllAssets(bool include)
        {
            EditorPrefs.SetBool("Setting.AutoVariant_includeAllAssets", include);
        }

        /// <summary>
        /// すべてのアセットを含める設定を取得する
        /// </summary>
        /// <returns>含めるかどうか</returns>
        public static bool GetIncludeAllAssets()
        {
            return PrebuildSettings.IncludeAllAssets;
        }

        /// <summary>
        /// 設定をデフォルト値にリセットする
        /// </summary>
        public static void ResetToDefaults()
        {
            SetAutoVariantEnabled(false);
            SetPrebuildEnabled(true);
            SetIncludeAllAssets(true);
        }

        /// <summary>
        /// すべての設定値を検証する
        /// </summary>
        /// <returns>設定が有効かどうか</returns>
        public static bool ValidateSettings()
        {
            // 基本的な設定の整合性をチェック
            var baseDir = PrebuildSettings.BaseDirectoryPath;
            if (string.IsNullOrEmpty(baseDir))
            {
                UnityEngine.Debug.LogWarning("[AutoVariantController] Base directory path is not set");
                return false;
            }

            return true;
        }
    }
}
