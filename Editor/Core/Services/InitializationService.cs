using UnityEngine;
using UnityEditor;
using AMU.Editor.Core.Controller;

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
                Debug.Log(LocalizationController.GetText("message_info_initialization_starting"));

                // EditorPrefsの初期化
                InitializeEditorPrefs();

                // ローカライゼーションの初期化
                InitializeLocalization();

                Debug.Log(LocalizationController.GetText("message_success_initialization_completed"));
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("message_error_initialization_failed"), ex.Message));
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
        /// ローカライゼーションの初期化を実行します
        /// </summary>
        private static void InitializeLocalization()
        {
            try
            {
                // デフォルト言語の読み込み
                LocalizationController.LoadLanguage("ja_jp");
                Debug.Log(LocalizationController.GetText("message_success_localization_initialized"));
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("message_error_localization_failed"), ex.Message));
            }
        }
    }
}
