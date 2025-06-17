using UnityEngine;
using UnityEditor;
using AMU.Data.TagType;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.Core.Services
{
    /// <summary>
    /// AMUの初期化処理を管理するサービス
    /// </summary>
    [InitializeOnLoad]
    public static class InitializationService
    {
        static InitializationService()
        {
            // エディター起動時の初期化
            EditorApplication.delayCall += Initialize;
        }

        /// <summary>
        /// AMUの初期化処理を実行します
        /// </summary>
        public static void Initialize()
        {
            try
            {
                Debug.Log("[InitializationService] Starting AMU initialization...");

                // EditorPrefsの初期化
                InitializeEditorPrefs();

                // TagTypeManagerの初期化
                InitializeTagTypeManager();

                // ローカライゼーションの初期化
                InitializeLocalization();

                Debug.Log("[InitializationService] AMU initialization completed successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InitializationService] AMU initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// EditorPrefsの初期化を実行します
        /// </summary>
        private static void InitializeEditorPrefs()
        {
            SettingsController.InitializeEditorPrefs();
        }

        /// <summary>
        /// TagTypeManagerの初期化を実行します
        /// </summary>
        private static void InitializeTagTypeManager()
        {
            try
            {
                // TagTypeManagerのデータを読み込み
                TagTypeManager.LoadData();
                Debug.Log("[InitializationService] TagTypeManager initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InitializationService] TagTypeManager initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// ローカライゼーションの初期化を実行します
        /// </summary>
        private static void InitializeLocalization()
        {
            try
            {
                // デフォルト言語の読み込み
                LocalizationController.LoadLanguage("ja_jp");
                Debug.Log("[InitializationService] Localization initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InitializationService] Localization initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定されたコンポーネントの初期化を再実行します
        /// </summary>
        /// <param name="component">再初期化するコンポーネント</param>
        public static void Reinitialize(InitializationComponent component)
        {
            switch (component)
            {
                case InitializationComponent.EditorPrefs:
                    InitializeEditorPrefs();
                    break;
                case InitializationComponent.TagTypeManager:
                    InitializeTagTypeManager();
                    break;
                case InitializationComponent.Localization:
                    InitializeLocalization();
                    break;
                case InitializationComponent.All:
                    Initialize();
                    break;
            }
        }
    }

    /// <summary>
    /// 初期化可能なコンポーネントの種類
    /// </summary>
    public enum InitializationComponent
    {
        EditorPrefs,
        TagTypeManager,
        Localization,
        All
    }
}
