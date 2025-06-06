using UnityEngine;
using UnityEditor;
using AMU.Editor.Core.Helper;
using AMU.Data.Lang;

namespace AMU.Editor.AutoVariant.Watcher
{
    public static class AvatarValidator
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
                if (obj.activeInHierarchy && PipelineManagerHelper.isVRCAvatar(obj))
                {
                    avatars.Add(obj);
                }
            }

            return avatars.ToArray();
        }

        private static void ShowMultipleAvatarsError()
        {
            var lang = EditorPrefs.GetString("Setting.Core_language", "en_us");
            var (title, message) = GetLocalizedErrorMessage(lang);
            
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static (string title, string message) GetLocalizedErrorMessage(string language)
        {
            return language switch
            {
                "ja_jp" => ("ビルド中止", "Hierarchy内に複数のアバターが検出されました。1体のみがアクティブな状態にしてください。"),
                _ => ("Build Cancelled", "Multiple avatars detected. Please activate only one avatar.")
            };
        }
    }
}
