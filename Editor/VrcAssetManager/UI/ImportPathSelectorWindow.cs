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

        private bool _isProcessing = false;
        private string _statusMessage = "";
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized = false;

        public static void ShowWindow(Action<List<string>> onSelectionComplete, AssetSchema asset, List<string> initialSelectedFiles = null)
        {
            if (asset == null)
            {
                Debug.LogError(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_assetNull"));
                return;
            }

            if (string.IsNullOrEmpty(asset.fileInfo.filePath))
            {
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), LocalizationAPI.GetText("VrcAssetManager_ui_filePathNotSet"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                return;
            }

            if (!ZipFileUtility.IsZipFile(asset.fileInfo.filePath))
            {
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), LocalizationAPI.GetText("VrcAssetManager_ui_notZipFile"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                return;
            }

            var zipFiles = ZipFileUtility.GetZipFileList(asset.fileInfo.filePath);
            if (zipFiles.Count == 0)
            {
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), LocalizationAPI.GetText("VrcAssetManager_ui_noFilesInZip"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                return;
            }

            var window = GetWindow<ImportPathSelectorWindow>(LocalizationAPI.GetText("VrcAssetManager_ui_importPathSelector_title"));
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(800, 700);

            window._asset = asset;
            window._zipFiles = zipFiles;
            window._onSelectionComplete = onSelectionComplete;
            window._selectedFiles.Clear();

            if (initialSelectedFiles != null)
            {
                foreach (var selectedFile in initialSelectedFiles)
                {
                    var fileName = Path.GetFileName(selectedFile);
                    var matchingFile = zipFiles.FirstOrDefault(f => Path.GetFileName(f) == fileName);
                    if (!string.IsNullOrEmpty(matchingFile))
                    {
                        window._selectedFiles[matchingFile] = true;
                    }
                }
            }

            window.Show();
        }

        private void OnEnable()
        {
            if (_asset?.fileInfo?.importFiles != null)
            {
                foreach (var importFile in _asset.fileInfo.importFiles)
                {
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

            using (new GUILayout.VerticalScope(_boxStyle))
            {
                GUILayout.Label(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_zipFile"), Path.GetFileName(_asset.fileInfo.filePath)), _headerStyle);

                var filteredFiles = GetFilteredFiles();
                var selectedCount = _selectedFiles.Count(kvp => kvp.Value);
                GUILayout.Label(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_fileCount"), filteredFiles.Count, selectedCount), EditorStyles.miniLabel);
            }

            GUILayout.Space(5);

            using (new GUILayout.VerticalScope(_boxStyle))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_filter"), GUILayout.Width(50));
                    _searchFilter = EditorGUILayout.TextField(_searchFilter);
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_clear"), GUILayout.Width(60)))
                    {
                        _searchFilter = "";
                    }
                }

                GUILayout.Space(5);

                using (new GUILayout.HorizontalScope())
                {
                    var newShowAllFiles = EditorGUILayout.Toggle(LocalizationAPI.GetText("VrcAssetManager_ui_showAllFiles"), _showAllFiles);
                    if (newShowAllFiles != _showAllFiles)
                    {
                        _showAllFiles = newShowAllFiles;
                        RefreshSelectedFiles();
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_selectAll"), EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        SelectAllFiles(true);
                    }
                    if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_deselectAll"), EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        SelectAllFiles(false);
                    }
                }
            }

            GUILayout.Space(5);

            GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_selectFilesToImport"), EditorStyles.boldLabel);

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
                        GUILayout.Label(LocalizationAPI.GetText("VrcAssetManager_ui_noFilesMatch"), EditorStyles.centeredGreyMiniLabel);
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

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                using (new GUILayout.HorizontalScope(_boxStyle))
                {
                    GUILayout.Label(_statusMessage, _statusStyle);
                }
            }

            GUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                GUI.enabled = !_isProcessing;
                if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_cancel"), _buttonStyle, GUILayout.Width(100)))
                {
                    CancelAndClose();
                }

                var selectedFiles = _selectedFiles.Count(kvp => kvp.Value);
                GUI.enabled = !_isProcessing && selectedFiles > 0;
                var extractButtonText = _isProcessing ? LocalizationAPI.GetText("VrcAssetManager_ui_extracting") : LocalizationAPI.GetText("VrcAssetManager_ui_extractAndSet");
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
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), LocalizationAPI.GetText("VrcAssetManager_ui_noFileSelected"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
                return;
            }

            _isProcessing = true;
            SetStatus(LocalizationAPI.GetText("VrcAssetManager_ui_extractionInProgress"));

            var extractedPaths = new List<string>();
            string unzipDir = ZipFileUtility.GetUnzipDirectory();
            string zipFileName = Path.GetFileNameWithoutExtension(_asset.fileInfo.filePath);
            string assetUnzipDir = Path.Combine(unzipDir, zipFileName);

            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_targetDir"), assetUnzipDir));

            if (!Directory.Exists(assetUnzipDir))
            {
                Directory.CreateDirectory(assetUnzipDir);
                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_createdDir"), assetUnzipDir));
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

                    if (File.Exists(outputPath))
                    {
                        Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_fileExists"), outputPath));
                        string relativePath = $"VrcAssetManager/Unzip/{zipFileName}/{Path.GetFileName(outputPath)}";
                        extractedPaths.Add(relativePath);
                        successCount++;
                        continue;
                    }

                    Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_extractingFile"), file, outputPath));

                    if (ZipFileUtility.ExtractFileFromZip(_asset.fileInfo.filePath, file, outputPath))
                    {
                        if (File.Exists(outputPath))
                        {
                            string relativePath = $"VrcAssetManager/Unzip/{zipFileName}/{Path.GetFileName(outputPath)}";
                            extractedPaths.Add(relativePath);
                            successCount++;
                            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_extracted"), relativePath));
                        }
                        else
                        {
                            Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_fileNotCreated"), outputPath));
                        }
                    }
                    else
                    {
                        Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_extractFailed"), file));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_extractException"), file, ex.Message));
                }
            }

            _isProcessing = false;

            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_importPathSelector_completed"), successCount, selectedFiles.Count));

            if (extractedPaths.Count > 0)
            {
                SetStatus(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_extractionSuccess"), extractedPaths.Count));
                _onSelectionComplete?.Invoke(extractedPaths);
            }
            else
            {
                EditorUtility.DisplayDialog(LocalizationAPI.GetText("VrcAssetManager_ui_error"), LocalizationAPI.GetText("VrcAssetManager_ui_extractionFailed"), LocalizationAPI.GetText("VrcAssetManager_common_ok"));
            }

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
                _selectedFiles[file] = EditorGUILayout.Toggle(_selectedFiles[file], GUILayout.Width(20));

                var extension = Path.GetExtension(file).ToLower();
                var icon = GetFileIcon(extension);
                if (icon != null)
                {
                    GUILayout.Label(new GUIContent(icon), GUILayout.Width(20), GUILayout.Height(20));
                }

                var fileName = Path.GetFileName(file);
                var directoryPath = Path.GetDirectoryName(file);

                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(fileName, EditorStyles.boldLabel);

                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        GUILayout.Label(directoryPath, EditorStyles.miniLabel);
                    }
                }

                GUILayout.FlexibleSpace();

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
                    return LocalizationAPI.GetText("VrcAssetManager_ui_unityPackage");
                case ".cs":
                    return LocalizationAPI.GetText("VrcAssetManager_ui_csharpScript");
                case ".png":
                case ".jpg":
                case ".jpeg":
                    return LocalizationAPI.GetText("VrcAssetManager_ui_image");
                case ".fbx":
                case ".obj":
                    return LocalizationAPI.GetText("VrcAssetManager_ui_3dModel");
                case ".mat":
                    return LocalizationAPI.GetText("VrcAssetManager_ui_material");
                case ".txt":
                    return LocalizationAPI.GetText("VrcAssetManager_ui_text");
                case ".md":
                    return LocalizationAPI.GetText("VrcAssetManager_ui_markdown");
                case ".zip":
                    return LocalizationAPI.GetText("VrcAssetManager_ui_archive");
                default:
                    return extension.TrimStart('.').ToUpper();
            }
        }

        private void RefreshSelectedFiles()
        {
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
