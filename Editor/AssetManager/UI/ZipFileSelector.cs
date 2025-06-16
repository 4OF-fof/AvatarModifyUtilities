using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;
using AMU.Data.Lang;

namespace AMU.AssetManager.UI
{
    public class ZipFileSelector : EditorWindow
    {
        private AssetInfo _asset;
        private List<string> _zipFiles;
        private AssetFileManager _fileManager;
        private Action<List<string>> _onSelectionComplete;
        private Vector2 _scrollPosition;
        private Dictionary<string, bool> _selectedFiles = new Dictionary<string, bool>();
        private string _searchFilter = "";
        private bool _showAllFiles = false;

        // UI state
        private bool _isProcessing = false;
        private string _statusMessage = "";
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized = false;

        public static void ShowWindow(AssetInfo asset, List<string> zipFiles, AssetFileManager fileManager, Action<List<string>> onSelectionComplete)
        {
            var window = GetWindow<ZipFileSelector>(LocalizationManager.GetText("ZipFileSelector_windowTitle"));
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(800, 700);

            window._asset = asset;
            window._zipFiles = zipFiles;
            window._fileManager = fileManager;
            window._onSelectionComplete = onSelectionComplete;
            window._selectedFiles.Clear();

            window.Show();
        }

        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                padding = new RectOffset(10, 10, 10, 10)
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(15, 15, 8, 8)
            };

            _statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };

            _stylesInitialized = true;
        }

        private void OnDestroy()
        {
            // ウィンドウが閉じられる際に一時ファイルをクリーンアップ
            if (_asset != null && _fileManager != null)
            {
                string fullPath = _fileManager.GetFullPath(_asset.filePath);
                _fileManager.CleanupTempExtraction(fullPath);
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (_asset == null || _zipFiles == null)
            {
                Close();
                return;
            }

            // Header
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label($"{LocalizationManager.GetText("ZipFileSelector_zipFile")}: {Path.GetFileName(_asset.filePath)}", _headerStyle);

                // File count info
                var filteredFiles = GetFilteredFiles();
                var selectedCount = _selectedFiles.Count(kvp => kvp.Value);
                GUILayout.Label($"{LocalizationManager.GetText("ZipFileSelector_fileCount").Replace("{0}", filteredFiles.Count.ToString())} | {LocalizationManager.GetText("ZipFileSelector_selectedCount").Replace("{0}", selectedCount.ToString())}", EditorStyles.miniLabel);
            }

            GUILayout.Space(5);

            // Filter section
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationManager.GetText("ZipFileSelector_filter"), GUILayout.Width(50));
                    _searchFilter = EditorGUILayout.TextField(_searchFilter);
                    if (GUILayout.Button(LocalizationManager.GetText("Common_clear"), GUILayout.Width(60)))
                    {
                        _searchFilter = "";
                    }
                }

                GUILayout.Space(5);

                // File type toggles
                using (new GUILayout.HorizontalScope())
                {
                    var newShowAllFiles = EditorGUILayout.Toggle(LocalizationManager.GetText("ZipFileSelector_showAllFiles"), _showAllFiles);
                    if (newShowAllFiles != _showAllFiles)
                    {
                        _showAllFiles = newShowAllFiles;
                        RefreshSelectedFiles();
                    }

                    GUILayout.FlexibleSpace();

                    // Bulk selection buttons
                    if (GUILayout.Button(LocalizationManager.GetText("ZipFileSelector_selectAll"), EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        SelectAllFiles(true);
                    }
                    if (GUILayout.Button(LocalizationManager.GetText("ZipFileSelector_selectNone"), EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        SelectAllFiles(false);
                    }
                }
            }

            GUILayout.Space(5);

            // File list
            GUILayout.Label(LocalizationManager.GetText("ZipFileSelector_selectFiles"), EditorStyles.boldLabel);

            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, _boxStyle))
            {
                _scrollPosition = scrollView.scrollPosition;

                var filteredFiles = GetFilteredFiles();

                if (filteredFiles.Count == 0)
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(LocalizationManager.GetText("ZipFileSelector_noFilesFoundFiltered"), EditorStyles.centeredGreyMiniLabel);
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    foreach (var file in filteredFiles)
                    {
                        DrawFileItem(file);
                    }
                }
            }

            // Status message
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                using (new GUILayout.HorizontalScope(_boxStyle))
                {
                    GUILayout.Label(_statusMessage, _statusStyle);
                }
            }

            GUILayout.Space(10);

            // Action buttons
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                GUI.enabled = !_isProcessing;
                if (GUILayout.Button(LocalizationManager.GetText("Common_cancel"), _buttonStyle, GUILayout.Width(100)))
                {
                    CancelAndClose();
                }

                var selectedFiles = _selectedFiles.Count(kvp => kvp.Value);
                GUI.enabled = !_isProcessing && selectedFiles > 0;

                var extractButtonText = _isProcessing ? LocalizationManager.GetText("ZipFileSelector_extractingFiles") : LocalizationManager.GetText("ZipFileSelector_extractImport");
                if (GUILayout.Button(extractButtonText, _buttonStyle, GUILayout.Width(150)))
                {
                    ExtractAndImportSelectedFiles();
                }
                GUI.enabled = true;
            }
        }

        private List<string> GetFilteredFiles()
        {
            var files = _zipFiles;

            // デフォルトでunitypackageのみ表示（_showAllFilesがfalseの場合）
            if (!_showAllFiles)
            {
                files = files.Where(f => f.ToLower().EndsWith(".unitypackage")).ToList();
            }

            if (string.IsNullOrEmpty(_searchFilter))
            {
                return files;
            }

            return files.Where(f => f.ToLower().Contains(_searchFilter.ToLower())).ToList();
        }
        private void ExtractAndImportSelectedFiles()
        {
            var selectedFiles = _selectedFiles.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

            if (selectedFiles.Count == 0)
            {
                EditorUtility.DisplayDialog(LocalizationManager.GetText("Common_error"), LocalizationManager.GetText("ZipFileSelector_noFilesSelected"), LocalizationManager.GetText("Common_ok"));
                return;
            }

            _isProcessing = true;
            SetStatus(LocalizationManager.GetText("ZipFileSelector_extractingFiles"));

            var extractedPaths = new List<string>();
            string unzipDir = _fileManager.GetUnzipDirectory();
            // zipファイル名（拡張子なし）をディレクトリ名として使用
            string zipFileName = Path.GetFileNameWithoutExtension(_asset.filePath);
            string assetUnzipDir = Path.Combine(unzipDir, zipFileName);

            Debug.Log($"[ZipFileSelector] Target extraction directory: {assetUnzipDir}");

            if (!Directory.Exists(assetUnzipDir))
            {
                Directory.CreateDirectory(assetUnzipDir);
                Debug.Log($"[ZipFileSelector] Created extraction directory: {assetUnzipDir}");
            }

            int processedCount = 0;
            int successCount = 0;
            foreach (var file in selectedFiles)
            {
                try
                {
                    processedCount++;
                    SetStatus(string.Format(LocalizationManager.GetText("ZipFileSelector_extractingFile"), Path.GetFileName(file)) +
                             $" ({processedCount}/{selectedFiles.Count})");

                    string fileName = Path.GetFileName(file);
                    string outputPath = Path.Combine(assetUnzipDir, fileName);

                    // 同名ファイルが存在する場合は番号を付加
                    int counter = 1;
                    string originalOutputPath = outputPath;
                    while (File.Exists(outputPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(originalOutputPath);
                        string extension = Path.GetExtension(originalOutputPath);
                        string directory = Path.GetDirectoryName(originalOutputPath);
                        outputPath = Path.Combine(directory, $"{nameWithoutExt}_{counter}{extension}");
                        counter++;
                    }

                    Debug.Log($"[ZipFileSelector] Extracting file: {file} -> {outputPath}");

                    if (_fileManager.ExtractFileFromZip(_asset.filePath, file, outputPath))
                    {
                        // ファイルが実際に作成されたかを確認
                        if (File.Exists(outputPath))
                        {
                            // AssetManager/unzip 以下の相対パスを保存（スラッシュ区切りで）
                            string relativePath = $"AssetManager/unzip/{zipFileName}/{Path.GetFileName(outputPath)}";
                            extractedPaths.Add(relativePath);
                            successCount++;
                            Debug.Log($"[ZipFileSelector] Successfully extracted: {relativePath}");
                        }
                        else
                        {
                            Debug.LogError($"[ZipFileSelector] File was not created after extraction: {outputPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ZipFileSelector] Failed to extract file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ZipFileSelector] Error extracting file {file}: {ex.Message}");
                    Debug.LogError($"[ZipFileSelector] Stack trace: {ex.StackTrace}");
                }
            }

            _isProcessing = false;

            Debug.Log($"[ZipFileSelector] Extraction completed. Success: {successCount}/{selectedFiles.Count}");

            if (extractedPaths.Count > 0)
            {
                SetStatus($"Successfully extracted {extractedPaths.Count} files");
                _onSelectionComplete?.Invoke(extractedPaths);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    LocalizationManager.GetText("Common_error"),
                    LocalizationManager.GetText("ZipFileSelector_extractFailed"),
                    LocalizationManager.GetText("Common_ok"));
            }

            // 一時ファイルをクリーンアップ
            if (_asset != null && _fileManager != null)
            {
                string fullPath = _fileManager.GetFullPath(_asset.filePath);
                _fileManager.CleanupTempExtraction(fullPath);
            }

            Close();
        }

        private void DrawFileItem(string file)
        {
            if (!_selectedFiles.ContainsKey(file))
            {
                _selectedFiles[file] = false;
            }

            using (new GUILayout.HorizontalScope(GUILayout.Height(22)))
            {
                // Checkbox
                _selectedFiles[file] = EditorGUILayout.Toggle(_selectedFiles[file], GUILayout.Width(20));

                // File icon based on extension
                var extension = Path.GetExtension(file).ToLower();
                var icon = GetFileIcon(extension);
                if (icon != null)
                {
                    GUILayout.Label(new GUIContent(icon), GUILayout.Width(20), GUILayout.Height(20));
                }

                // File name with proper formatting
                var fileName = Path.GetFileName(file);
                var directoryPath = Path.GetDirectoryName(file);

                using (new GUILayout.VerticalScope())
                {
                    // File name (bold)
                    GUILayout.Label(fileName, EditorStyles.boldLabel);

                    // Directory path (smaller, gray)
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        GUILayout.Label(directoryPath, EditorStyles.miniLabel);
                    }
                }

                GUILayout.FlexibleSpace();

                // File size info (if available)
                // Note: Getting file size from zip would require additional zip reading functionality
                // For now, we'll just show the file type
                var typeLabel = GetFileTypeLabel(extension);
                GUILayout.Label(typeLabel, EditorStyles.miniLabel, GUILayout.Width(100));
            }
        }

        private Texture2D GetFileIcon(string extension)
        {
            switch (extension)
            {
                case ".unitypackage":
                    return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                case ".cs":
                    return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                case ".png":
                case ".jpg":
                case ".jpeg":
                    return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
                case ".fbx":
                case ".obj":
                    return EditorGUIUtility.IconContent("Mesh Icon").image as Texture2D;
                case ".mat":
                    return EditorGUIUtility.IconContent("Material Icon").image as Texture2D;
                case ".txt":
                case ".md":
                    return EditorGUIUtility.IconContent("TextAsset Icon").image as Texture2D;
                default:
                    return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
            }
        }

        private string GetFileTypeLabel(string extension)
        {
            switch (extension.ToLower())
            {
                case ".unitypackage":
                    return "Unity Package";
                case ".cs":
                    return "C# Script";
                case ".png":
                case ".jpg":
                case ".jpeg":
                    return "Image";
                case ".fbx":
                case ".obj":
                    return "3D Model";
                case ".mat":
                    return "Material";
                case ".txt":
                    return "Text";
                case ".md":
                    return "Markdown";
                case ".zip":
                    return "Archive";
                default:
                    return extension.TrimStart('.').ToUpper();
            }
        }

        private void RefreshSelectedFiles()
        {
            // Clear selections for files that are no longer visible
            var filteredFiles = GetFilteredFiles();
            var toRemove = _selectedFiles.Keys.Where(key => !filteredFiles.Contains(key)).ToList();
            foreach (var key in toRemove)
            {
                _selectedFiles.Remove(key);
            }
        }

        private void SelectAllFiles(bool selected)
        {
            var filteredFiles = GetFilteredFiles();
            foreach (var file in filteredFiles)
            {
                _selectedFiles[file] = selected;
            }
        }

        private void CancelAndClose()
        {
            if (_asset != null && _fileManager != null)
            {
                string fullPath = _fileManager.GetFullPath(_asset.filePath);
                _fileManager.CleanupTempExtraction(fullPath);
            }
            Close();
        }

        private void SetStatus(string message)
        {
            _statusMessage = message;
            Repaint();
        }
    }
}
