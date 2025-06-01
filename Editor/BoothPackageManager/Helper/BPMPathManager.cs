using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace AMU.BoothPackageManager.Helper
{
    public static class BPMPathManager
    {
        public static string GetJsonPath()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "BPM", "BPMlibrary.json");
        }

        public static string GetImportJsonPath()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "Import", "BPMlibrary.json");
        }

        public static string GetDownloadJsonPath()
        {
            return Path.Combine(GetDownloadDirectory(), "BPMlibrary.json");
        }

        public static string GetThumbnailDirectory()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "BPM", "thumbnail");
        }

        public static string GetImportDirectory()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "Import");
        }

        public static string GetDownloadDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        public static string GetImageHash(string url)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public static string ExtractItemIdFromUrl(string itemUrl)
        {
            if (string.IsNullOrEmpty(itemUrl)) return "unknown";

            var uri = new Uri(itemUrl);
            var segments = uri.Segments;

            if (segments.Length > 0)
            {
                string lastSegment = segments[segments.Length - 1].Trim('/');
                return lastSegment;
            }

            return "unknown";
        }

        public static string GetFileDirectory(string author, string itemUrl)
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            string itemId = ExtractItemIdFromUrl(itemUrl);
            return Path.Combine(coreDir, "BPM", "file", author, itemId);
        }
    }
}
