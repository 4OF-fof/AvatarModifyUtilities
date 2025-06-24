using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.Helper
{
    /// <summary>
    /// アセットのインポートを行うユーティリティクラス
    /// </summary>
    public static class AssetImportUtility
    {
        /// <summary>
        /// Core_dirPathからの相対パスを受け取り、ファイルをUnityプロジェクトにインポートする
        /// </summary>
        /// <param name="relativePath">Core_dirPathからの相対パス</param>
        /// <param name="showImportDialog">インポートダイアログを表示するかどうか（UnityPackageの場合のみ有効）</param>
        /// <returns>インポートが成功した場合はtrue、失敗した場合はfalse</returns>
        public static bool ImportAsset(string relativePath, bool showImportDialog = true)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogWarning("[AssetImportUtility] Relative path is null or empty");
                return false;
            }

            try
            {
                // Core_dirPathを取得
                string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                if (string.IsNullOrEmpty(coreDir))
                {
                    Debug.LogError("[AssetImportUtility] Core_dirPath setting not found");
                    return false;
                }

                // フルパスを構築
                string fullPath = Path.Combine(coreDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                fullPath = Path.GetFullPath(fullPath);

                // ファイルが存在するかチェック
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"[AssetImportUtility] File not found: {fullPath}");
                    return false;
                }

                // 拡張子を取得
                string extension = Path.GetExtension(fullPath).ToLower();

                // UnityPackageかどうかで処理を分岐
                if (extension == ".unitypackage")
                {
                    return ImportUnityPackage(fullPath, showImportDialog);
                }
                else
                {
                    return ImportFileToAssets(fullPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import asset from relative path '{relativePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// UnityPackageファイルをインポートする
        /// </summary>
        /// <param name="packagePath">UnityPackageファイルのフルパス</param>
        /// <param name="showImportDialog">インポートダイアログを表示するかどうか</param>
        /// <returns>インポートが開始された場合はtrue、失敗した場合はfalse</returns>
        private static bool ImportUnityPackage(string packagePath, bool showImportDialog)
        {
            try
            {
                Debug.Log($"[AssetImportUtility] Importing Unity Package: {packagePath}");
                
                // showImportDialogの値をそのまま渡す
                AssetDatabase.ImportPackage(packagePath, showImportDialog);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import Unity Package '{packagePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 通常のファイルをAssetsフォルダにインポートする
        /// </summary>
        /// <param name="sourceFilePath">ソースファイルのフルパス</param>
        /// <returns>インポートが成功した場合はtrue、失敗した場合はfalse</returns>
        private static bool ImportFileToAssets(string sourceFilePath)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string targetPath = Path.Combine(Application.dataPath, fileName);
                string assetPath = "Assets/" + fileName;

                // ファイルが既に存在する場合
                if (File.Exists(targetPath))
                {
                    Debug.Log($"[AssetImportUtility] File already exists in Assets folder: {assetPath}");
                    
                    // 既存ファイルを選択状態にする
                    SelectAssetInProject(assetPath);
                    return true;
                }

                // ファイルをAssetsフォルダにコピー
                File.Copy(sourceFilePath, targetPath, true);

                // AssetDatabaseを更新
                AssetDatabase.Refresh();

                Debug.Log($"[AssetImportUtility] File imported to Assets folder: {assetPath}");

                // インポート後にファイルを選択状態にする
                SelectAssetInProject(assetPath);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetImportUtility] Failed to import file to Assets '{sourceFilePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// プロジェクトビューでアセットを選択状態にする
        /// </summary>
        /// <param name="assetPath">アセットのパス（Assets/から始まる相対パス）</param>
        private static void SelectAssetInProject(string assetPath)
        {
            EditorApplication.delayCall += () =>
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            };
        }

        /// <summary>
        /// ファイルがUnityPackageかどうかを判定する
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>UnityPackageの場合はtrue、それ以外はfalse</returns>
        public static bool IsUnityPackage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".unitypackage";
        }

        /// <summary>
        /// ファイルがインポート可能かどうかを判定する
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>インポート可能な場合はtrue、それ以外はfalse</returns>
        public static bool IsImportable(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            // UnityPackageの場合
            if (IsUnityPackage(filePath))
                return true;

            // その他のファイル形式をチェック
            string extension = Path.GetExtension(filePath).ToLower();
            
            // 除外する拡張子のリスト
            string[] excludedExtensions = { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2" };
            
            return !Array.Exists(excludedExtensions, ext => ext == extension);
        }

        /// <summary>
        /// Core_dirPathからの相対パスを受け取り、ファイルが存在するかチェックする
        /// </summary>
        /// <param name="relativePath">Core_dirPathからの相対パス</param>
        /// <returns>ファイルが存在する場合はtrue、それ以外はfalse</returns>
        public static bool FileExists(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;

            try
            {
                string coreDir = SettingAPI.GetSetting<string>("Core_dirPath");
                if (string.IsNullOrEmpty(coreDir))
                    return false;

                string fullPath = Path.Combine(coreDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                fullPath = Path.GetFullPath(fullPath);

                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
    }
}