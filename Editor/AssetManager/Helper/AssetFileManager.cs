using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using System.IO.Compression;
using System.Text;

namespace AMU.AssetManager.Helper
{
    public class AssetFileManager
    {
        private Dictionary<string, string> _tempExtractionDirs = new Dictionary<string, string>();
        public void OpenFileLocation(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
            {
                Debug.LogWarning("[AssetFileManager] Invalid asset or file path");
                return;
            }

            string fullPath = GetFullPath(asset.filePath);
            if (File.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                Debug.LogWarning($"[AssetFileManager] File not found: {fullPath}");
            }
        }

        public long GetFileSize(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                if (File.Exists(fullPath))
                {
                    return new FileInfo(fullPath).Length;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to get file size for {filePath}: {ex.Message}");
                return 0;
            }
        }



        public bool FileExists(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
        public AssetInfo CreateAssetFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            // ファイルをCoreDirに移動
            string coreDirFilePath = MoveAssetToCoreDir(filePath);

            var asset = new AssetInfo
            {
                name = Path.GetFileNameWithoutExtension(filePath),
                filePath = GetRelativePath(coreDirFilePath),
                assetType = "Other",
                fileSize = GetFileSize(coreDirFilePath),
                createdDate = DateTime.Now,
            };

            return asset;
        }



        public List<string> GetAssetDependencies(string assetPath)
        {
            try
            {
                if (assetPath.StartsWith("Assets/"))
                {
                    return AssetDatabase.GetDependencies(assetPath, true).ToList();
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to get dependencies for {assetPath}: {ex.Message}");
                return new List<string>();
            }
        }

        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            if (relativePath.StartsWith("Assets/"))
            {
                return Path.Combine(Application.dataPath, relativePath.Substring(7));
            }

            // CoreDirからの相対パスの場合
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            return Path.Combine(coreDir, relativePath.Replace('/', '\\'));
        }
        private string GetRelativePath(string fullPath)
        {
            // CoreDirからの相対パスを優先して計算
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

            if (fullPath.StartsWith(coreDir))
            {
                return fullPath.Substring(coreDir.Length + 1).Replace('\\', '/');
            }

            if (fullPath.StartsWith(Application.dataPath))
            {
                return "Assets" + fullPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            }
            return fullPath.Replace('\\', '/');
        }        /// <summary>
                 /// UnityPackageファイルをプロジェクトにインポートする
                 /// </summary>
        public void ImportUnityPackage(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
            {
                Debug.LogWarning("[AssetFileManager] Invalid asset or file path");
                return;
            }

            string fullPath = GetFullPath(asset.filePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[AssetFileManager] File not found: {fullPath}");
                return;
            }

            string extension = Path.GetExtension(fullPath).ToLower();
            if (extension != ".unitypackage")
            {
                Debug.LogWarning($"[AssetFileManager] File is not a Unity Package: {fullPath}");
                return;
            }

            try
            {
                Debug.Log($"[AssetFileManager] Importing Unity Package: {asset.name}");
                AssetDatabase.ImportPackage(fullPath, true);

                // インポート後、ファイルがCoreDirに存在しない場合は移動
                string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

                if (!fullPath.StartsWith(coreDir))
                {
                    string newPath = MoveAssetToCoreDir(fullPath);
                    asset.filePath = GetRelativePath(newPath);
                    Debug.Log($"[AssetFileManager] Unity Package moved to CoreDir: {newPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to import Unity Package {asset.name}: {ex.Message}");
            }
        }

        /// <summary>
        /// ファイルがUnityPackageかどうかを判定する
        /// </summary>
        public bool IsUnityPackage(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
                return false;

            return Path.GetExtension(asset.filePath).ToLower() == ".unitypackage";
        }
        /// <summary>
        /// ファイルがインポート可能かどうかを判定する
        /// </summary>
        public bool IsImportable(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
                return false;

            string extension = Path.GetExtension(asset.filePath).ToLower();            // 設定から除外する拡張子を取得
            string excludedExtensions = EditorPrefs.GetString("Setting.AssetManager_excludedImportExtensions", ".zip .rar .7z\n.tar .gz .bz2\npsd blend");

            // カンマ、スペース、改行で分割
            var separators = new char[] { ',', ' ', '\n', '\r', '\t' };
            var excludedList = excludedExtensions.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().ToLower())
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Select(ext => ext.StartsWith(".") ? ext : "." + ext) // ドットが無い場合は追加
                .ToArray();

            // 除外リストに含まれている場合はインポート不可
            return !excludedList.Contains(extension);
        }

        /// <summary>
        /// インポートボタンを表示すべきかどうかを判定する
        /// </summary>
        public bool ShouldShowImportButton(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
                return false;

            // ファイルが存在しない場合は表示しない
            if (!FileExists(asset.filePath))
                return false;

            // UnityPackageまたはその他のインポート可能ファイルの場合に表示
            return IsUnityPackage(asset) || IsImportable(asset);
        }

        /// <summary>
        /// ファイルをUnityプロジェクトにインポートする（拡張版）
        /// </summary>
        public void ImportAsset(AssetInfo asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.filePath))
            {
                Debug.LogWarning("[AssetFileManager] Invalid asset or file path");
                return;
            }

            string fullPath = GetFullPath(asset.filePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[AssetFileManager] File not found: {fullPath}");
                return;
            }

            try
            {
                if (IsUnityPackage(asset))
                {
                    // UnityPackageの場合は既存のメソッドを使用
                    ImportUnityPackage(asset);
                }
                else if (IsImportable(asset))
                {
                    // その他のファイルの場合はAssetsフォルダ直下にコピー
                    ImportFileToAssets(asset, fullPath);
                }
                else
                {
                    Debug.LogWarning($"[AssetFileManager] File type not supported for import: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to import asset {asset.name}: {ex.Message}");
            }
        }

        /// <summary>
        /// ファイルをAssetsフォルダ直下にインポートする
        /// </summary>
        private void ImportFileToAssets(AssetInfo asset, string sourceFilePath)
        {
            string fileName = Path.GetFileName(sourceFilePath);
            string targetPath = Path.Combine(Application.dataPath, fileName);

            // Assetsフォルダでのパスを取得
            string assetPath = "Assets/" + Path.GetFileName(targetPath);

            // ファイルが既に存在する場合は、既存ファイルを参照
            if (File.Exists(targetPath))
            {
                Debug.Log($"[AssetFileManager] File already exists in Assets folder, using existing file: {targetPath}");
                Debug.Log($"[AssetFileManager] Using existing file in Assets folder: {assetPath}");

                // 既存ファイルを選択状態にする
                EditorApplication.delayCall += () =>
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                };
                return;
            }

            // ファイルをコピー
            File.Copy(sourceFilePath, targetPath);

            // AssetDatabaseを更新
            AssetDatabase.Refresh();

            Debug.Log($"[AssetFileManager] File imported to Assets folder: {assetPath}");

            // インポート後にファイルを選択状態にする
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
        /// アセットファイルをCoreDirに移動する
        /// </summary>
        public string MoveAssetToCoreDir(string originalFilePath)
        {
            if (string.IsNullOrEmpty(originalFilePath) || !File.Exists(originalFilePath))
            {
                Debug.LogWarning($"[AssetFileManager] Invalid file path or file not found: {originalFilePath}");
                return originalFilePath;
            }

            try
            {
                // CoreDirパスを取得
                string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));

                // AssetManagerディレクトリを作成
                string assetManagerDir = Path.Combine(coreDir, "AssetManager", "Files");
                if (!Directory.Exists(assetManagerDir))
                {
                    Directory.CreateDirectory(assetManagerDir);
                }                // ファイル名を取得し、重複を確認
                string fileName = Path.GetFileName(originalFilePath);
                string targetPath = Path.Combine(assetManagerDir, fileName);

                // ファイルが既に存在する場合は、既存ファイルのパスを返す
                if (File.Exists(targetPath))
                {
                    Debug.Log($"[AssetFileManager] File already exists in CoreDir, using existing file: {targetPath}");
                    return targetPath;
                }

                // ファイルをコピー（移動ではなくコピーで安全性を確保）
                File.Copy(originalFilePath, targetPath);

                Debug.Log($"[AssetFileManager] Asset file copied to CoreDir: {targetPath}");
                return targetPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to move asset to CoreDir: {ex.Message}");
                return originalFilePath;
            }
        }

        public bool IsZipFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".zip";
        }

        public bool IsZipFile(AssetInfo asset)
        {
            return IsZipFile(asset?.filePath);
        }
        public List<string> GetZipFileList(string zipFilePath)
        {
            var fileList = new List<string>();

            try
            {
                string fullPath = GetFullPath(zipFilePath);
                if (!File.Exists(fullPath) || !IsZipFile(fullPath))
                {
                    return fileList;
                }

                // システムのTempディレクトリに一時展開
                string tempDir = ExtractZipToTemp(fullPath);
                if (string.IsNullOrEmpty(tempDir))
                {
                    return fileList;
                }

                // 展開されたファイル一覧を取得（日本語ファイル名も正しく取得される）
                string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    // tempDirからの相対パスを計算
                    string relativePath = Path.GetRelativePath(tempDir, file);
                    fileList.Add(relativePath.Replace('\\', '/'));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to read zip file {zipFilePath}: {ex.Message}");
            }

            return fileList;
        }
        public bool ExtractFileFromZip(string zipFilePath, string entryPath, string outputPath)
        {
            try
            {
                string fullZipPath = GetFullPath(zipFilePath);
                if (!File.Exists(fullZipPath) || !IsZipFile(fullZipPath))
                {
                    Debug.LogError($"[AssetFileManager] Zip file not found or invalid: {fullZipPath}");
                    return false;
                }

                // 一時展開ディレクトリから該当ファイルを検索
                string tempDir = GetTempExtractionDir(fullZipPath);
                if (string.IsNullOrEmpty(tempDir))
                {
                    Debug.LogError($"[AssetFileManager] Failed to get temp extraction directory for: {fullZipPath}");
                    return false;
                }

                // パスの正規化とファイル検索
                string normalizedEntryPath = entryPath.Replace('/', Path.DirectorySeparatorChar);
                string sourceFile = Path.Combine(tempDir, normalizedEntryPath);

                // ファイルが見つからない場合、ディレクトリ内を再帰的に検索
                if (!File.Exists(sourceFile))
                {
                    string fileName = Path.GetFileName(entryPath);
                    var foundFiles = Directory.GetFiles(tempDir, fileName, SearchOption.AllDirectories);

                    if (foundFiles.Length > 0)
                    {
                        sourceFile = foundFiles[0]; // 最初に見つかったファイルを使用
                        Debug.Log($"[AssetFileManager] Found file at alternative path: {sourceFile}");
                    }
                    else
                    {
                        Debug.LogError($"[AssetFileManager] Source file not found in temp directory: {sourceFile}");
                        Debug.LogError($"[AssetFileManager] Temp directory contents: {string.Join(", ", Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))}");
                        return false;
                    }
                }

                // 出力ディレクトリが存在しない場合は作成
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // ファイルをコピー
                File.Copy(sourceFile, outputPath, true);
                Debug.Log($"[AssetFileManager] Successfully extracted file: {entryPath} -> {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to extract file {entryPath} from {zipFilePath}: {ex.Message}");
                Debug.LogError($"[AssetFileManager] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public string GetUnzipDirectory()
        {
            string coreDir = GetCoreDirectory();
            string unzipDir = Path.Combine(coreDir, "AssetManager", "unzip");

            if (!Directory.Exists(unzipDir))
            {
                Directory.CreateDirectory(unzipDir);
            }

            return unzipDir;
        }
        private string GetCoreDirectory()
        {
            return EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
        }
        private string ExtractZipToTemp(string zipFilePath)
        {
            try
            {
                // 既に展開済みの場合はそのパスを返す
                if (_tempExtractionDirs.TryGetValue(zipFilePath, out string existingTempDir))
                {
                    if (Directory.Exists(existingTempDir))
                    {
                        Debug.Log($"[AssetFileManager] Using existing temp directory: {existingTempDir}");
                        return existingTempDir;
                    }
                    else
                    {
                        _tempExtractionDirs.Remove(zipFilePath);
                    }
                }

                // 新しい一時ディレクトリを作成
                string tempDir = Path.Combine(Path.GetTempPath(), "AMU_ZipExtract", Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                Debug.Log($"[AssetFileManager] Created temp directory: {tempDir}");

                int extractedCount = 0;
                // Shift_JISエンコーディングでZIPファイルを展開
                using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, false, Encoding.GetEncoding("Shift_JIS")))
                    {
                        Debug.Log($"[AssetFileManager] Archive contains {archive.Entries.Count} entries");

                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                // ディレクトリエントリをスキップ
                                continue;
                            }

                            try
                            {
                                string entryPath = Path.Combine(tempDir, entry.FullName);
                                string entryDir = Path.GetDirectoryName(entryPath);

                                if (!Directory.Exists(entryDir))
                                {
                                    Directory.CreateDirectory(entryDir);
                                }

                                entry.ExtractToFile(entryPath, true);
                                extractedCount++;
                                Debug.Log($"[AssetFileManager] Extracted: {entry.FullName} -> {entryPath}");
                            }
                            catch (Exception entryEx)
                            {
                                Debug.LogWarning($"[AssetFileManager] Failed to extract entry {entry.FullName}: {entryEx.Message}");
                            }
                        }
                    }
                }
                Debug.Log($"[AssetFileManager] Successfully extracted {extractedCount} files to temp directory");
                _tempExtractionDirs[zipFilePath] = tempDir;
                return tempDir;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetFileManager] Failed to extract zip to temp: {ex.Message}");
                Debug.LogError($"[AssetFileManager] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private string GetTempExtractionDir(string zipFilePath)
        {
            if (_tempExtractionDirs.TryGetValue(zipFilePath, out string tempDir))
            {
                if (Directory.Exists(tempDir))
                {
                    return tempDir;
                }
                else
                {
                    _tempExtractionDirs.Remove(zipFilePath);
                }
            }

            // まだ展開されていない場合は展開を実行
            return ExtractZipToTemp(zipFilePath);
        }

        public void CleanupTempExtractions()
        {
            foreach (var kvp in _tempExtractionDirs.ToList())
            {
                try
                {
                    if (Directory.Exists(kvp.Value))
                    {
                        Directory.Delete(kvp.Value, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AssetFileManager] Failed to cleanup temp directory {kvp.Value}: {ex.Message}");
                }
            }
            _tempExtractionDirs.Clear();
        }

        public void CleanupTempExtraction(string zipFilePath)
        {
            if (_tempExtractionDirs.TryGetValue(zipFilePath, out string tempDir))
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AssetFileManager] Failed to cleanup temp directory {tempDir}: {ex.Message}");
                }
                _tempExtractionDirs.Remove(zipFilePath);
            }
        }

        /// <summary>
        /// 複数のUnityPackageファイルを順次インポートする
        /// </summary>
        public void ImportUnityPackagesSequentially(List<AssetInfo> unityPackageAssets)
        {
            if (unityPackageAssets == null || unityPackageAssets.Count == 0)
            {
                Debug.LogWarning("[AssetFileManager] No Unity Package assets to import");
                return;
            }

            ImportUnityPackagesSequentiallyInternal(unityPackageAssets, 0);
        }

        /// <summary>
        /// UnityPackageファイルを順次インポートする内部メソッド
        /// </summary>
        private void ImportUnityPackagesSequentiallyInternal(List<AssetInfo> unityPackageAssets, int currentIndex)
        {
            if (currentIndex >= unityPackageAssets.Count)
            {
                Debug.Log($"[AssetFileManager] All Unity packages imported successfully. Total: {unityPackageAssets.Count}");
                return;
            }
            var currentAsset = unityPackageAssets[currentIndex]; Debug.Log($"[AssetFileManager] Importing Unity Package {currentIndex + 1}/{unityPackageAssets.Count}: {currentAsset.name}");

            // インポート完了を監視するコールバックを登録
            AssetDatabase.ImportPackageCallback importCompleteCallback = null;
            AssetDatabase.ImportPackageCallback importCancelledCallback = null;
            AssetDatabase.ImportPackageFailedCallback importFailedCallback = null;

            System.Action nextImport = () =>
            {
                // コールバックを解除
                if (importCompleteCallback != null)
                    AssetDatabase.importPackageCompleted -= importCompleteCallback;
                if (importCancelledCallback != null)
                    AssetDatabase.importPackageCancelled -= importCancelledCallback;
                if (importFailedCallback != null)
                    AssetDatabase.importPackageFailed -= importFailedCallback;

                // 次のファイルをインポート
                EditorApplication.delayCall += () =>
                {
                    ImportUnityPackagesSequentiallyInternal(unityPackageAssets, currentIndex + 1);
                };
            };

            importCompleteCallback = (packageName) => nextImport();
            importCancelledCallback = (packageName) => nextImport();
            importFailedCallback = (packageName, errorMessage) => nextImport();

            AssetDatabase.importPackageCompleted += importCompleteCallback;
            AssetDatabase.importPackageCancelled += importCancelledCallback;
            AssetDatabase.importPackageFailed += importFailedCallback;

            // UnityPackageをインポート
            ImportUnityPackage(currentAsset);
        }
    }
}
