using UnityEngine;
using UnityEditor;

namespace AMU.Editor.AutoVariant.Schema
{
    /// <summary>
    /// Prebuildプロセスの設定スキーマ定義
    /// </summary>
    public static class PrebuildSettings
    {
        /// <summary>
        /// 最適化処理が有効かどうか
        /// </summary>
        public static bool IsOptimizationEnabled =>
            EditorPrefs.GetBool("Setting.AutoVariant_enablePrebuild", true);

        /// <summary>
        /// すべてのアセットを含めるかどうか
        /// </summary>
        public static bool IncludeAllAssets =>
            EditorPrefs.GetBool("Setting.AutoVariant_includeAllAssets", true);

        /// <summary>
        /// 現在の言語設定
        /// </summary>
        public static string CurrentLanguage =>
            EditorPrefs.GetString("Setting.Core_language", "en_us");

        /// <summary>
        /// ベースディレクトリパス
        /// </summary>
        public static string BaseDirectoryPath =>
            EditorPrefs.GetString("Setting.Core_dirPath",
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

        /// <summary>
        /// AutoVariant機能が有効かどうか
        /// </summary>
        public static bool IsAutoVariantEnabled =>
            EditorPrefs.GetBool("Setting.AutoVariant_enableAutoVariant", false);
    }
}
