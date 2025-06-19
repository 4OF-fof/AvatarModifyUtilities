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

            _showLibraryDetails = EditorGUILayout.Foldout(_showLibraryDetails, "Show Library Details");

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
            if (_showLibraryDetails && _currentLibrary != null)
            {
                EditorGUILayout.LabelField("Library Details", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");

                var allAssets = VrcAssetController.GetAllAssets();
                EditorGUILayout.LabelField($"Asset Count: {allAssets.Count}");

                var library = AssetLibraryController.LoadLibrary();
                if (library != null)
                {
                    EditorGUILayout.LabelField($"Last Updated: {library.LastUpdated}");
                }

                _showAssetDetails = EditorGUILayout.Foldout(_showAssetDetails, "Asset List");
                if (_showAssetDetails)
                {
                    var assets = VrcAssetController.GetAllAssets();
                    foreach (var asset in assets.Take(10)) // 最初の10件のみ表示
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Name: {asset.Metadata.Name}");
                        EditorGUILayout.LabelField($"Type: {asset.Metadata.AssetType}");
                        EditorGUILayout.EndHorizontal();
                    }

                    if (assets.Count > 10)
                    {
                        EditorGUILayout.LabelField($"... and {assets.Count - 10} more assets");
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
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
