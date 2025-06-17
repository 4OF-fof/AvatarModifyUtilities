using UnityEngine;
using UnityEditor;
using AMU.Editor.Core.Helper;
using AMU.Data.Lang;

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
                Debug.LogWarning("[AvatarValidationService] No active avatars found");
                return null;
            }

            if (avatars.Length > 1)
            {
                Debug.LogWarning("[AvatarValidationService] Multiple active avatars found");
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
