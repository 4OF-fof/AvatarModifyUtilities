using System;

using UnityEngine;
using UnityEditor;

using AMU.Editor.Core.Controller;

namespace AMU.Editor.Core.Services
{
    [InitializeOnLoad]
    public static class InitializationService
    {
        static InitializationService()
        {
            EditorApplication.delayCall += Initialize;
        }

        public static void Initialize()
        {
            try
            {
                Debug.Log(LocalizationController.GetText("Core_message_info_initialization_starting"));

                InitializeEditorPrefs();

                InitializeLocalization();

                Debug.Log(LocalizationController.GetText("Core_message_success_initialization_completed"));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("Core_message_error_initialization_failed"), ex.Message));
            }
        }

        private static void InitializeEditorPrefs()
        {
            SettingsController.InitializeEditorPrefs();
        }

        private static void InitializeLocalization()
        {
            try
            {
                LocalizationController.LoadLanguage("en_us");
                Debug.Log(LocalizationController.GetText("Core_message_success_localization_initialized"));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("Core_message_error_localization_failed"), ex.Message));
            }
        }
    }
}
