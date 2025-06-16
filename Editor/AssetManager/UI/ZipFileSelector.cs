using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;
using AMU.AssetManager.Helper;

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

        public static void ShowWindow(AssetInfo asset, List<string> zipFiles, AssetFileManager fileManager, Action<List<string>> onSelectionComplete)
        {
            var window = GetWindow<ZipFileSelector>("Zip File Selector");
            window.minSize = new Vector2(500, 400);
            window.maxSize = new Vector2(500, 600);

            window._asset = asset;
            window._zipFiles = zipFiles;
            window._fileManager = fileManager;
            window._onSelectionComplete = onSelectionComplete;
            window._selectedFiles.Clear();

            window.Show();
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
            if (_asset == null || _zipFiles == null)
            {
                Close();
                return;
            }

            GUILayout.Label($"Zip File: {Path.GetFileName(_asset.filePath)}", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 検索フィルター
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Filter:", GUILayout.Width(50));
                _searchFilter = EditorGUILayout.TextField(_searchFilter);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    _searchFilter = "";
                }
            }

            // すべてのファイルを表示するトグル
            using (new GUILayout.HorizontalScope())
            {
                _showAllFiles = EditorGUILayout.Toggle("Show All Files", _showAllFiles);
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(10);

            // ファイルリスト
            GUILayout.Label("Select files to import:", EditorStyles.boldLabel);

            using (var scrollView = new GUILayout.ScrollViewScope(_scrollPosition, EditorStyles.helpBox))
            {
                _scrollPosition = scrollView.scrollPosition;

                var filteredFiles = GetFilteredFiles();

                GUILayout.Space(5);

                foreach (var file in filteredFiles)
                {
                    if (!_selectedFiles.ContainsKey(file))
                    {
                        _selectedFiles[file] = false;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        _selectedFiles[file] = EditorGUILayout.Toggle(_selectedFiles[file], GUILayout.Width(20));
                        GUILayout.Label(file);
                    }
                }
            }

            GUILayout.Space(10);

            // 実行ボタン
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(80)))
                {
                    // 一時ファイルをクリーンアップしてからウィンドウを閉じる
                    if (_asset != null && _fileManager != null)
                    {
                        string fullPath = _fileManager.GetFullPath(_asset.filePath);
                        _fileManager.CleanupTempExtraction(fullPath);
                    }
                    Close();
                }

                if (GUILayout.Button("Extract & Import", GUILayout.Width(120)))
                {
                    ExtractAndImportSelectedFiles();
                }
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
                EditorUtility.DisplayDialog("エラー", "ファイルが選択されていません。", "OK");
                return;
            }

            var extractedPaths = new List<string>();
            string unzipDir = _fileManager.GetUnzipDirectory();
            string assetUnzipDir = Path.Combine(unzipDir, _asset.uid);

            if (!Directory.Exists(assetUnzipDir))
            {
                Directory.CreateDirectory(assetUnzipDir);
            }

            foreach (var file in selectedFiles)
            {
                try
                {
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

                    if (_fileManager.ExtractFileFromZip(_asset.filePath, file, outputPath))
                    {
                        // AssetManager/unzip 以下の相対パスを保存
                        string relativePath = Path.Combine("AssetManager", "unzip", _asset.uid, Path.GetFileName(outputPath));
                        extractedPaths.Add(relativePath);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to extract file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error extracting file {file}: {ex.Message}");
                }
            }

            if (extractedPaths.Count > 0)
            {
                _onSelectionComplete?.Invoke(extractedPaths);
                EditorUtility.DisplayDialog("完了", $"{extractedPaths.Count}個のファイルを展開しました。", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "ファイルの展開に失敗しました。", "OK");
            }

            // 一時ファイルをクリーンアップ
            if (_asset != null && _fileManager != null)
            {
                string fullPath = _fileManager.GetFullPath(_asset.filePath);
                _fileManager.CleanupTempExtraction(fullPath);
            }

            Close();
        }
    }
}
