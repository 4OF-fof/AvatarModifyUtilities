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
    }
}
