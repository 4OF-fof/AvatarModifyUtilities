using UnityEngine;
using UnityEditor;
using AMU.Data.TagType;

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

        [MenuItem("AMU/Initialize/Open Data Folder")]
        public static void OpenDataFolder()
        {
            var dataPath = TagTypeManager.GetDataFilePath();
            var folderPath = System.IO.Path.GetDirectoryName(dataPath);
            EditorUtility.RevealInFinder(folderPath);
        }
    }
}
