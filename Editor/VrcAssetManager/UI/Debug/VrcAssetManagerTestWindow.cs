using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

using AMU.Editor.VrcAssetManager.Controllers;
using AMU.Editor.VrcAssetManager.Schema;
using AMU.Editor.Core.Controllers;

namespace AMU.Editor.VrcAssetManager.UI.Debug
{
    /// <summary>
    /// VrcAssetManagerのコントローラー機能をテストするためのデバッグウィンドウ
    /// </summary>
    public class VrcAssetManagerTestWindow : EditorWindow
    {
        #region Window Management

        [MenuItem("Debug/VrcAssetManager Test Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<VrcAssetManagerTestWindow>("VrcAssetManager Test");
            window.minSize = new Vector2(1200, 800);
            window.maxSize = new Vector2(1200, 800);
            window.Show();
        }

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;
        private string _testAssetName = "TestAsset";
        private string _testAssetType = "Test";
        private string _testFilePath = "";
        private string _testAssetId = "";
        private string _logOutput = "";
        private bool _showLibraryDetails = false;
        private bool _showAssetDetails = false;
        private AssetLibrarySchema _currentLibrary;
        private readonly List<string> _logEntries = new List<string>();

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            LogMessage("VrcAssetManager Test Window Opened");
            RefreshLibrary();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawAssetLibraryControllerTests();
            DrawVrcAssetControllerTests();
            DrawVrcAssetFileControllerTests();
            DrawLibraryDetails();
            DrawLogOutput();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region GUI Drawing Methods

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("VrcAssetManager Controller Test Window", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Clear Log", GUILayout.Height(25)))
            {
                ClearLog();
            }

            if (GUILayout.Button("Refresh Library", GUILayout.Height(25)))
            {
                RefreshLibrary();
            }

            EditorGUILayout.Space();
        }

        private void DrawAssetLibraryControllerTests()
        {
            EditorGUILayout.LabelField("AssetLibraryController Tests", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Default Library Path: {AssetLibraryController.DefaultLibraryPath}");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Library"))
            {
                TestLoadLibrary();
            }
            if (GUILayout.Button("Save Library"))
            {
                TestSaveLibrary();
            }
            if (GUILayout.Button("Clear Cache"))
            {
                TestClearCache();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Get All Assets"))
            {
                TestGetAllAssets();
            }
            if (GUILayout.Button("Get Asset Count"))
            {
                TestGetAssetCount();
            }
            EditorGUILayout.EndHorizontal();

            _showLibraryDetails = EditorGUILayout.Foldout(_showLibraryDetails, "Show Asset Library Status");

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawVrcAssetControllerTests()
        {
            EditorGUILayout.LabelField("VrcAssetController Tests", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            _testAssetName = EditorGUILayout.TextField("Asset Name:", _testAssetName);
            _testAssetType = EditorGUILayout.TextField("Asset Type:", _testAssetType);
            _testAssetId = EditorGUILayout.TextField("Asset ID (for get/update/delete):", _testAssetId);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Test Asset"))
            {
                TestAddAsset();
            }
            if (GUILayout.Button("Get Asset"))
            {
                TestGetAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Asset"))
            {
                TestUpdateAsset();
            }
            if (GUILayout.Button("Delete Asset"))
            {
                TestDeleteAsset();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Search Assets by Type"))
            {
                TestSearchAssetsByType();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawVrcAssetFileControllerTests()
        {
            EditorGUILayout.LabelField("VrcAssetFileController Tests", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            _testFilePath = EditorGUILayout.TextField("File Path:", _testFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select File", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _testFilePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Import Asset File"))
            {
                TestImportAssetFile();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawLibraryDetails()
        {
            if (_showLibraryDetails)
            {
                EditorGUILayout.LabelField("Asset Library Status", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");

                // ライブラリファイル情報
                DrawLibraryFileInfo();
                EditorGUILayout.Space();

                // ライブラリの基本情報
                DrawLibraryBasicInfo();
                EditorGUILayout.Space();

                // アセット統計情報
                DrawAssetStatistics();
                EditorGUILayout.Space();

                // アセット詳細リスト
                DrawAssetDetailsList();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawLibraryFileInfo()
        {
            EditorGUILayout.LabelField("Library File Information", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            string libraryPath = AssetLibraryController.DefaultLibraryPath;
            EditorGUILayout.LabelField($"Library Path: {libraryPath}");

            if (File.Exists(libraryPath))
            {
                var fileInfo = new FileInfo(libraryPath);
                EditorGUILayout.LabelField($"File Size: {GetFileSizeString(fileInfo.Length)}");
                EditorGUILayout.LabelField($"Last Modified: {fileInfo.LastWriteTime}");
                EditorGUILayout.LabelField($"File Exists: Yes");
            }
            else
            {
                EditorGUILayout.LabelField("File Exists: No", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLibraryBasicInfo()
        {
            EditorGUILayout.LabelField("Library Basic Information", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            var library = AssetLibraryController.LoadLibrary();
            if (library != null)
            {
                _currentLibrary = library;
                EditorGUILayout.LabelField($"Last Updated: {library.LastUpdated}");
                EditorGUILayout.LabelField($"Asset Count: {library.AssetCount}");
                EditorGUILayout.LabelField($"Tags Count: {library.TagsCount}");
                EditorGUILayout.LabelField($"Asset Types Count: {library.AssetTypeCount}");
            }
            else
            {
                EditorGUILayout.LabelField("Library: Not loaded or not found", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetStatistics()
        {
            EditorGUILayout.LabelField("Asset Statistics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            var allAssets = VrcAssetController.GetAllAssets();
            EditorGUILayout.LabelField($"Total Assets: {allAssets.Count}");

            if (allAssets.Count > 0)
            {
                // アセットタイプ別統計
                var typeGroups = allAssets.GroupBy(a => a.Metadata.AssetType)
                                         .OrderByDescending(g => g.Count())
                                         .ToList();

                EditorGUILayout.LabelField("Assets by Type:", EditorStyles.miniBoldLabel);
                foreach (var group in typeGroups.Take(5)) // 上位5タイプを表示
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  {group.Key}:", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"{group.Count()} assets");
                    EditorGUILayout.EndHorizontal();
                }

                if (typeGroups.Count > 5)
                {
                    EditorGUILayout.LabelField($"  ... and {typeGroups.Count - 5} more types");
                }

                // 最近追加されたアセット
                var recentAssets = allAssets
                    .Where(a => a.Metadata.CreatedDate != default)
                    .OrderByDescending(a => a.Metadata.CreatedDate)
                    .Take(3)
                    .ToList();

                if (recentAssets.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Recently Added Assets:", EditorStyles.miniBoldLabel);
                    foreach (var asset in recentAssets)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  {asset.Metadata.Name}", GUILayout.Width(200));
                        EditorGUILayout.LabelField($"({asset.Metadata.AssetType})", GUILayout.Width(100));
                        EditorGUILayout.LabelField($"{asset.Metadata.CreatedDate:MM/dd HH:mm}");
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetDetailsList()
        {
            _showAssetDetails = EditorGUILayout.Foldout(_showAssetDetails, "Asset Details List");
            if (_showAssetDetails)
            {
                EditorGUILayout.BeginVertical("box");

                var library = AssetLibraryController.LoadLibrary();
                if (library == null || library.AssetCount == 0)
                {
                    EditorGUILayout.LabelField("No assets found in library", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    var assetPairs = library.Assets.Take(10).ToList();
                    EditorGUILayout.LabelField($"Showing first 10 of {library.AssetCount} assets:");
                    EditorGUILayout.Space();

                    // ヘッダー
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField("ID", EditorStyles.boldLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField("Size", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField("Created", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    foreach (var kvp in assetPairs)
                    {
                        var assetId = kvp.Key;
                        var asset = kvp.Value;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(asset.Metadata.Name, GUILayout.Width(150));
                        EditorGUILayout.LabelField(asset.Metadata.AssetType, GUILayout.Width(100));
                        EditorGUILayout.LabelField(assetId.Value.Substring(0, Math.Min(8, assetId.Value.Length)), GUILayout.Width(100));

                        string sizeText = "N/A";
                        if (asset.FileInfo.FileSizeBytes > 0)
                        {
                            sizeText = GetFileSizeString(asset.FileInfo.FileSizeBytes);
                        }
                        EditorGUILayout.LabelField(sizeText, GUILayout.Width(80));

                        string dateText = asset.Metadata.CreatedDate != default
                            ? asset.Metadata.CreatedDate.ToString("MM/dd HH:mm")
                            : "N/A";
                        EditorGUILayout.LabelField(dateText);
                        EditorGUILayout.EndHorizontal();
                    }

                    if (library.AssetCount > 10)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField($"... and {library.AssetCount - 10} more assets", EditorStyles.centeredGreyMiniLabel);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private string GetFileSizeString(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        private void DrawLogOutput()
        {
            EditorGUILayout.LabelField("Log Output", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box", GUILayout.Height(200));

            var logStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };

            _logOutput = string.Join("\n", _logEntries);
            EditorGUILayout.TextArea(_logOutput, logStyle, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Test Methods

        private void TestLoadLibrary()
        {
            try
            {
                LogMessage("Testing AssetLibraryController.LoadLibrary()...");
                var library = AssetLibraryController.LoadLibrary();
                if (library != null)
                {
                    _currentLibrary = library;
                    LogMessage($"SUCCESS: Loaded library with {library.AssetCount} assets");
                }
                else
                {
                    LogMessage("WARNING: LoadLibrary returned null");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestSaveLibrary()
        {
            try
            {
                LogMessage("Testing AssetLibraryController.SaveLibrary()...");
                if (_currentLibrary == null)
                {
                    LogMessage("No library loaded. Loading first...");
                    _currentLibrary = AssetLibraryController.LoadLibrary();
                }

                bool result = AssetLibraryController.SaveLibrary(_currentLibrary);
                LogMessage($"Save result: {result}");
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestClearCache()
        {
            try
            {
                LogMessage("Testing AssetLibraryController.ClearCache()...");
                AssetLibraryController.ClearCache();
                LogMessage("Cache cleared successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }
        private void TestGetAllAssets()
        {
            try
            {
                LogMessage("Testing VrcAssetController.GetAllAssets()...");
                var assets = VrcAssetController.GetAllAssets();
                LogMessage($"Retrieved {assets.Count} assets");

                if (assets.Count > 0)
                {
                    var firstAsset = assets.First();
                    LogMessage($"First asset: Name={firstAsset.Metadata.Name}, AssetType={firstAsset.Metadata.AssetType}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestGetAssetCount()
        {
            try
            {
                LogMessage("Testing VrcAssetController.GetAllAssets().Count...");
                var assets = VrcAssetController.GetAllAssets();
                int count = assets.Count;
                LogMessage($"Asset count: {count}");
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestAddAsset()
        {
            try
            {
                LogMessage($"Testing VrcAssetController.AddAsset() with name: {_testAssetName}...");

                var assetId = AssetId.NewId();
                var assetData = new AssetSchema(_testAssetName, _testAssetType, "");

                bool result = VrcAssetController.AddAsset(assetId, assetData);
                LogMessage($"Add asset result: {result}, Asset ID: {assetId}");

                if (result)
                {
                    _testAssetId = assetId;
                    RefreshLibrary();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestGetAsset()
        {
            try
            {
                if (string.IsNullOrEmpty(_testAssetId))
                {
                    LogMessage("Please specify an Asset ID");
                    return;
                }

                LogMessage($"Testing VrcAssetController.GetAsset() with ID: {_testAssetId}...");

                if (AssetId.TryParse(_testAssetId, out AssetId assetId))
                {
                    var asset = VrcAssetController.GetAsset(assetId);
                    if (asset != null)
                    {
                        LogMessage($"Found asset: Name={asset.Metadata.Name}, AssetType={asset.Metadata.AssetType}");
                    }
                    else
                    {
                        LogMessage("Asset not found");
                    }
                }
                else
                {
                    LogMessage("Invalid Asset ID format");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestUpdateAsset()
        {
            try
            {
                if (string.IsNullOrEmpty(_testAssetId))
                {
                    LogMessage("Please specify an Asset ID");
                    return;
                }

                LogMessage($"Testing VrcAssetController.UpdateAsset() with ID: {_testAssetId}...");

                if (AssetId.TryParse(_testAssetId, out AssetId assetId))
                {
                    var updatedData = new AssetSchema($"{_testAssetName}_Updated", _testAssetType, "");
                    updatedData.Metadata.Description = "Updated by test window";

                    bool result = VrcAssetController.UpdateAsset(assetId, updatedData);
                    LogMessage($"Update asset result: {result}");

                    if (result)
                    {
                        RefreshLibrary();
                    }
                }
                else
                {
                    LogMessage("Invalid Asset ID format");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestDeleteAsset()
        {
            try
            {
                if (string.IsNullOrEmpty(_testAssetId))
                {
                    LogMessage("Please specify an Asset ID");
                    return;
                }

                LogMessage($"Testing VrcAssetController.RemoveAsset() with ID: {_testAssetId}...");

                if (AssetId.TryParse(_testAssetId, out AssetId assetId))
                {
                    bool result = VrcAssetController.RemoveAsset(assetId);
                    LogMessage($"Remove asset result: {result}");

                    if (result)
                    {
                        _testAssetId = "";
                        RefreshLibrary();
                    }
                }
                else
                {
                    LogMessage("Invalid Asset ID format");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }
        private void TestSearchAssetsByType()
        {
            try
            {
                LogMessage($"Testing VrcAssetController.GetAssetsByCategory() with asset type: {_testAssetType}...");

                var searchResults = VrcAssetController.GetAssetsByCategory(_testAssetType);

                LogMessage($"Search returned {searchResults.Count} assets");

                foreach (var asset in searchResults.Take(5)) // 最初の5件のみ表示
                {
                    LogMessage($"  - Name: {asset.Metadata.Name}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        private void TestImportAssetFile()
        {
            try
            {
                if (string.IsNullOrEmpty(_testFilePath))
                {
                    LogMessage("Please specify a file path");
                    return;
                }

                LogMessage($"Testing VrcAssetFileController.ImportAssetFile() with path: {_testFilePath}...");

                var assetData = VrcAssetFileController.ImportAssetFile(_testFilePath);

                if (assetData != null && !string.IsNullOrEmpty(assetData.Metadata.Name))
                {
                    LogMessage($"Import successful: Name={assetData.Metadata.Name}, Size={assetData.FileInfo.FileSizeBytes} bytes");

                    // 自動的にライブラリに追加
                    var assetId = AssetId.NewId();
                    bool addResult = VrcAssetController.AddAsset(assetId, assetData);
                    LogMessage($"Auto-add to library result: {addResult}, Asset ID: {assetId}");

                    if (addResult)
                    {
                        RefreshLibrary();
                    }
                }
                else
                {
                    LogMessage("Import failed or returned invalid data");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void RefreshLibrary()
        {
            try
            {
                _currentLibrary = AssetLibraryController.LoadLibrary();
                LogMessage("Library refreshed");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to refresh library: {ex.Message}");
            }
        }

        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";

            _logEntries.Add(logEntry);

            // ログが多くなりすぎないよう、最新の100件のみ保持
            if (_logEntries.Count > 100)
            {
                _logEntries.RemoveAt(0);
            }

            UnityEngine.Debug.Log(logEntry);
            Repaint();
        }

        private void ClearLog()
        {
            _logEntries.Clear();
            _logOutput = "";
            LogMessage("Log cleared");
        }

        #endregion
    }
}
