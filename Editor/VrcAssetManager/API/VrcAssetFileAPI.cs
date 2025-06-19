using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using AMU.Editor.Core.Controller;

namespace AMU.Editor.VrcAssetManager.API
{
    /// <summary>
    /// VRCアセットファイルのAPI機能を提供します
    /// </summary>
    public static class VrcAssetFileAPI
    {
        /// <summary>
        /// 指定されたディレクトリ内のVRCアセットファイルをスキャンします
        /// </summary>
        /// <param name="directoryPath">スキャンするディレクトリパス</param>
        /// <param name="recursive">サブディレクトリも含めるかどうか</param>
        /// <returns>発見されたVRCアセットファイルのパスリスト</returns>
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

                // 全てのファイルを取得
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
