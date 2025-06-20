using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using AMU.Editor.Core.Controller;

namespace AMU.Editor.Core.Api
{
    public static class LocalFileAPI
    {
        public static List<string> ScanDirectory(string directoryPath, bool recursive = true)
        {
            var foundFiles = new List<string>();

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_directoryNotFound"), directoryPath));
                    return foundFiles;
                }

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                var files = Directory.GetFiles(directoryPath, "*.*", searchOption);
                foundFiles.AddRange(files);

                Debug.Log(string.Format(LocalizationController.GetText("VrcAssetManager_message_success_scanCompleted"), foundFiles.Count, directoryPath));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationController.GetText("VrcAssetManager_message_error_scanFailed"), ex.Message));
            }

            return foundFiles;
        }
    }
}
