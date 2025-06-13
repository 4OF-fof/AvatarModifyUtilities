using UnityEngine;
using UnityEditor;
using AMU.Data.TagType;
using AMU.AssetManager.Data;

namespace AMU.Editor.Initializer
{
    /// <summary>
    /// AMUの初期化処理を行うクラス
    /// </summary>
    [InitializeOnLoad]
    public static class AMUInitializer
    {
        static AMUInitializer()
        {
            // エディター起動時の初期化
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            // TagTypeManagerの初期化
            InitializeTagTypeManager();

            // データマイグレーション
            PerformDataMigration();
        }

        private static void InitializeTagTypeManager()
        {
            try
            {
                // TagTypeManagerのデータを読み込み
                TagTypeManager.LoadData();
                Debug.Log("[AMU] TagTypeManager initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AMU] TagTypeManager initialization failed: {ex.Message}");
            }
        }

        private static void PerformDataMigration()
        {
            try
            {
                // 既存のAssetManagerのカスタムタイプを新しいシステムに移行
                AssetTypeManager.MigrateToTagTypeManager();
                Debug.Log("[AMU] Data migration completed successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AMU] Data migration failed: {ex.Message}");
            }
        }

        [MenuItem("AMU/Initialize/Reset Tag & Type Data")]
        public static void ResetTagTypeData()
        {
            if (EditorUtility.DisplayDialog("確認",
                "タグとタイプのデータをデフォルト状態にリセットしますか？\n" +
                "カスタムタグとタイプは失われます。",
                "リセット", "キャンセル"))
            {
                TagTypeManager.ResetToDefaults();
                Debug.Log("[AMU] Tag & Type data has been reset to defaults.");
            }
        }

        [MenuItem("AMU/Initialize/Migrate Asset Data")]
        public static void MigrateAssetData()
        {
            if (EditorUtility.DisplayDialog("確認",
                "既存のアセットデータを新しいシステムに移行しますか？",
                "移行", "キャンセル"))
            {
                AssetTypeManager.MigrateToTagTypeManager();
                Debug.Log("[AMU] Asset data migration completed.");
            }
        }

        [MenuItem("AMU/Initialize/Open Data Folder")]
        public static void OpenDataFolder()
        {
            var dataPath = TagTypeManager.GetDataFilePath();
            var folderPath = System.IO.Path.GetDirectoryName(dataPath);
            EditorUtility.RevealInFinder(folderPath);
        }
    }
}
