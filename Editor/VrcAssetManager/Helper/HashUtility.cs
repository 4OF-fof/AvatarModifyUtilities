using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.Helper
{
    public static class HashUtility
    {
        public static string GetHash(string input, bool isFile)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes;
                if (isFile)
                {
                    if (!File.Exists(input)) throw new FileNotFoundException(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_hashUtility_fileNotFound"), input));
                    bytes = File.ReadAllBytes(input);
                }
                else
                {
                    bytes = Encoding.UTF8.GetBytes(input);
                }
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
