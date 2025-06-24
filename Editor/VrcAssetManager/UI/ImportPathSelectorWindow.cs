using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.VrcAssetManager.Helper;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class ImportPathSelectorWindow : EditorWindow
    {
        private AssetSchema _asset;
        private List<string> _zipFiles;
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

        public static void ShowWindow(AssetSchema asset, Action<List<string>> onSelectionComplete)
        {
            if (asset == null)
            {
                Debug.LogError("[ImportPathSelectorWindow] Asset is null");
                return;
            }

            if (string.IsNullOrEmpty(asset.fileInfo.filePath))
            {
                EditorUtility.DisplayDialog("エラー", "ファイルパスが設定されていません。", "OK");
                return;
            }

            if (!ZipFileUtility.IsZipFile(asset.fileInfo.filePath))
            {
                EditorUtility.DisplayDialog("エラー", "選択されたファイルはZipファイルではありません。", "OK");
                return;
            }

            var zipFiles = ZipFileUtility.GetZipFileList(asset.fileInfo.filePath);
            if (zipFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "Zipファイル内にファイルが見つかりません。", "OK");
                return;
            }

            var window = GetWindow<ImportPathSelectorWindow>("Import Path選択");
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(800, 700);

            window._asset = asset;
            window._zipFiles = zipFiles;
            window._onSelectionComplete = onSelectionComplete;
            window._selectedFiles.Clear();

            window.Show();
        }

        private void OnEnable()
        {
            // 既存のimportFilesを選択状態にする
            if (_asset?.fileInfo?.importFiles != null)
            {
                foreach (var importFile in _asset.fileInfo.importFiles)
                {
                    // VrcAssetManager/Unzip以下の相対パスから元のzip内パスを推定
                    var fileName = Path.GetFileName(importFile);
                    var matchingFile = _zipFiles?.FirstOrDefault(f => Path.GetFileName(f) == fileName);
                    if (!string.IsNullOrEmpty(matchingFile))
                    {
                        _selectedFiles[matchingFile] = true;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // ウィンドウが閉じられる際に一時ファイルをクリーンアップ
            if (_asset != null)
            {
                string fullPath = ZipFileUtility.GetFullPath(_asset.fileInfo.filePath);
                ZipFileUtility.CleanupTempExtraction(fullPath);
            }
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
                GUILayout.Label($"Zipファイル: {Path.GetFileName(_asset.fileInfo.filePath)}", _headerStyle);

                // File count info
                var filteredFiles = GetFilteredFiles();
                var selectedCount = _selectedFiles.Count(kvp => kvp.Value);
                GUILayout.Label($"ファイル数: {filteredFiles.Count} | 選択中: {selectedCount}", EditorStyles.miniLabel);
            }

            GUILayout.Space(5);

            // Filter section
            using (new GUILayout.VerticalScope(_boxStyle))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("フィルター", GUILayout.Width(50));
                    _searchFilter = EditorGUILayout.TextField(_searchFilter);
                    if (GUILayout.Button("クリア", GUILayout.Width(60)))
                    {
                        _searchFilter = "";
                    }
                }

                GUILayout.Space(5);

                // File type toggles
                using (new GUILayout.HorizontalScope())
                {
                    var newShowAllFiles = EditorGUILayout.Toggle("全てのファイルを表示", _showAllFiles);
                    if (newShowAllFiles != _showAllFiles)
                    {
                        _showAllFiles = newShowAllFiles;
                        RefreshSelectedFiles();
                    }

                    GUILayout.FlexibleSpace();

                    // Bulk selection buttons
                    if (GUILayout.Button("全て選択", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        SelectAllFiles(true);
                    }
                    if (GUILayout.Button("選択解除", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        SelectAllFiles(false);
                    }
                }
            }

            GUILayout.Space(5);

            // File list
            GUILayout.Label("インポートするファイルを選択", EditorStyles.boldLabel);

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
                        GUILayout.Label("条件に一致するファイルが見つかりません", EditorStyles.centeredGreyMiniLabel);
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
                if (GUILayout.Button("キャンセル", _buttonStyle, GUILayout.Width(100)))
                {
                    CancelAndClose();
                }

                var selectedFiles = _selectedFiles.Count(kvp => kvp.Value);
                GUI.enabled = !_isProcessing && selectedFiles > 0;
                var extractButtonText = _isProcessing ? "展開中..." : "展開して設定";
                if (GUILayout.Button(extractButtonText, _buttonStyle, GUILayout.Width(150)))
                {
                    ExtractAndSetImportPaths();
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

        private void ExtractAndSetImportPaths()
        {
            var selectedFiles = _selectedFiles.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

            if (selectedFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("エラー", "ファイルが選択されていません。", "OK");
                return;
            }

            _isProcessing = true;
            SetStatus("ファイルを展開中...");

            var extractedPaths = new List<string>();
            string unzipDir = ZipFileUtility.GetUnzipDirectory();
            // zipファイル名（拡張子なし）をディレクトリ名として使用
            string zipFileName = Path.GetFileNameWithoutExtension(_asset.fileInfo.filePath);
            string assetUnzipDir = Path.Combine(unzipDir, zipFileName);

            Debug.Log($"[ImportPathSelectorWindow] Target extraction directory: {assetUnzipDir}");

            if (!Directory.Exists(assetUnzipDir))
            {
                Directory.CreateDirectory(assetUnzipDir);
                Debug.Log($"[ImportPathSelectorWindow] Created extraction directory: {assetUnzipDir}");
            }

            int processedCount = 0;
            int successCount = 0;
            foreach (var file in selectedFiles)
            {
                try
                {
                    processedCount++;
                    SetStatus($"ファイルを展開中: {Path.GetFileName(file)} ({processedCount}/{selectedFiles.Count})");

                    string fileName = Path.GetFileName(file);
                    string outputPath = Path.Combine(assetUnzipDir, fileName);

                    // 同名ファイルが存在する場合は既存ファイルを参照
                    if (File.Exists(outputPath))
                    {
                        Debug.Log($"[ImportPathSelectorWindow] File already exists, using existing file: {outputPath}");
                        // VrcAssetManager/Unzip 以下の相対パスを保存（スラッシュ区切りで）
                        string relativePath = $"VrcAssetManager/Unzip/{zipFileName}/{Path.GetFileName(outputPath)}";
                        extractedPaths.Add(relativePath);
                        successCount++;
                        continue;
                    }

                    Debug.Log($"[ImportPathSelectorWindow] Extracting file: {file} -> {outputPath}");

                    if (ZipFileUtility.ExtractFileFromZip(_asset.fileInfo.filePath, file, outputPath))
                    {
                        // ファイルが実際に作成されたかを確認
                        if (File.Exists(outputPath))
                        {
                            // VrcAssetManager/Unzip 以下の相対パスを保存（スラッシュ区切りで）
                            string relativePath = $"VrcAssetManager/Unzip/{zipFileName}/{Path.GetFileName(outputPath)}";
                            extractedPaths.Add(relativePath);
                            successCount++;
                            Debug.Log($"[ImportPathSelectorWindow] Successfully extracted: {relativePath}");
                        }
                        else
                        {
                            Debug.LogError($"[ImportPathSelectorWindow] File was not created after extraction: {outputPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ImportPathSelectorWindow] Failed to extract file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ImportPathSelectorWindow] Error extracting file {file}: {ex.Message}");
                }
            }

            _isProcessing = false;

            Debug.Log($"[ImportPathSelectorWindow] Extraction completed. Success: {successCount}/{selectedFiles.Count}");

            if (extractedPaths.Count > 0)
            {
                SetStatus($"Successfully extracted {extractedPaths.Count} files");
                _onSelectionComplete?.Invoke(extractedPaths);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "ファイルの展開に失敗しました。", "OK");
            }

            // 一時ファイルをクリーンアップ
            if (_asset != null)
            {
                string fullPath = ZipFileUtility.GetFullPath(_asset.fileInfo.filePath);
                ZipFileUtility.CleanupTempExtraction(fullPath);
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

                // File type
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
            if (_asset != null)
            {
                string fullPath = ZipFileUtility.GetFullPath(_asset.fileInfo.filePath);
                ZipFileUtility.CleanupTempExtraction(fullPath);
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
