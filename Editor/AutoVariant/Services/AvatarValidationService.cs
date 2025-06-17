using UnityEngine;
using UnityEditor;
using AMU.Editor.Core.Helper;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.AutoVariant.Services
{
    /// <summary>
    /// アバター検証サービス
    /// アバターの状態確認と検証を行う
    /// </summary>
    public static class AvatarValidationService
    {
        /// <summary>
        /// アバター数を検証する
        /// </summary>
        /// <returns>検証が成功したかどうか</returns>
        public static bool ValidateAvatarCount()
        {
            var avatars = FindActiveAvatars();

            if (avatars.Length <= 1)
                return true;

            ShowMultipleAvatarsError();
            return false;
        }

        /// <summary>
        /// アクティブなアバターを検索する
        /// </summary>
        /// <returns>アクティブなアバターの配列</returns>
        public static GameObject[] FindActiveAvatars()
        {
            var allObjects = Object.FindObjectsOfType<GameObject>();
            var avatars = new System.Collections.Generic.List<GameObject>();

            foreach (var obj in allObjects)
            {
                if (obj.activeInHierarchy && PipelineManagerHelper.isVRCAvatar(obj))
                {
                    avatars.Add(obj);
                }
            }

            return avatars.ToArray();
        }

        /// <summary>
        /// 単一のアクティブアバターを取得する
        /// </summary>
        /// <returns>アクティブなアバター（複数ある場合はnull）</returns>
        public static GameObject GetSingleActiveAvatar()
        {
            var avatars = FindActiveAvatars();

            if (avatars.Length == 0)
            {
                Debug.LogWarning($"[AvatarValidationService] {LocalizationController.GetText("message_warning_no_active_avatars")}");
                return null;
            }

            if (avatars.Length > 1)
            {
                Debug.LogWarning($"[AvatarValidationService] {LocalizationController.GetText("message_warning_multiple_avatars")}");
                return null;
            }

            return avatars[0];
        }

        /// <summary>
        /// 指定されたGameObjectがVRCアバターかどうかを判定する
        /// </summary>
        /// <param name="obj">判定対象のGameObject</param>
        /// <returns>VRCアバターかどうか</returns>
        public static bool IsVRCAvatar(GameObject obj)
        {
            if (obj == null)
                return false;

            return PipelineManagerHelper.isVRCAvatar(obj);
        }
        private static void ShowMultipleAvatarsError()
        {
            var title = LocalizationController.GetText("message_error_build_cancelled_title");
            var message = LocalizationController.GetText("message_error_multiple_avatars_detected");

            EditorUtility.DisplayDialog(title, message, "OK");
        }
        private static (string title, string message) GetLocalizedErrorMessage(string language)
        {
            var title = LocalizationController.GetText("message_error_build_cancelled_title");
            var message = LocalizationController.GetText("message_error_multiple_avatars_detected");
            return (title, message);
        }
    }
}
