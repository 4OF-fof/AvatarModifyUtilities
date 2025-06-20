using UnityEngine;
using UnityEditor;

using AMU.Editor.Core.Api;

namespace AMU.Editor.AutoVariant.Services
{
    public static class AvatarValidationService
    {
        public static bool ValidateAvatarCount()
        {
            var avatars = FindActiveAvatars();

            if (avatars.Length <= 1)
                return true;

            ShowMultipleAvatarsError();
            return false;
        }

        public static GameObject[] FindActiveAvatars()
        {
            var allObjects = Object.FindObjectsOfType<GameObject>();
            var avatars = new System.Collections.Generic.List<GameObject>();

            foreach (var obj in allObjects)
            {
                if (obj.activeInHierarchy && VRChatAPI.IsVRCAvatar(obj))
                {
                    avatars.Add(obj);
                }
            }

            return avatars.ToArray();
        }

        public static GameObject GetSingleActiveAvatar()
        {
            var avatars = FindActiveAvatars();

            if (avatars.Length == 0)
            {
                Debug.LogWarning($"[AvatarValidationService] {LocalizationAPI.GetText("message_warning_no_active_avatars")}");
                return null;
            }

            if (avatars.Length > 1)
            {
                Debug.LogWarning($"[AvatarValidationService] {LocalizationAPI.GetText("message_warning_multiple_avatars")}");
                return null;
            }

            return avatars[0];
        }

        public static bool IsVRCAvatar(GameObject obj)
        {
            if (obj == null)
                return false;

            return VRChatAPI.IsVRCAvatar(obj);
        }

        private static void ShowMultipleAvatarsError()
        {
            var title = LocalizationAPI.GetText("message_error_build_cancelled_title");
            var message = LocalizationAPI.GetText("message_error_multiple_avatars_detected");

            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static (string title, string message) GetLocalizedErrorMessage(string language)
        {
            var title = LocalizationAPI.GetText("message_error_build_cancelled_title");
            var message = LocalizationAPI.GetText("message_error_multiple_avatars_detected");
            return (title, message);
        }
    }
}
