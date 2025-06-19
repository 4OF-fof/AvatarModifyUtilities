using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Controller;

namespace AvatarModifyUtilities.Editor.VrcAssetManager.UI.Debug
{
    public class DebugWindow : EditorWindow
    {
        private AssetLibraryController _assetLibraryController;
        private string _testFilePath = "";
        private Vector2 _scrollPosition;
        private string _logText = "";
        private bool _autoScroll = true;

        [MenuItem("Debug/AssetLibraryController Test Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<DebugWindow>("AssetLibraryController Debug");
            window.Show();
        }

        private void OnEnable()
        {
            _assetLibraryController = new AssetLibraryController();

            // デフォルトのテストファイルパスを設定
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _testFilePath = Path.Combine(documentsPath, "AvatarModifyUtilities", "TestAssetLibrary.json");

            LogMessage("AssetLibraryController テストウィンドウが初期化されました。");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("AssetLibraryController テストウィンドウ", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // ファイルパス設定
            EditorGUILayout.LabelField("テストファイルパス", EditorStyles.boldLabel);
            _testFilePath = EditorGUILayout.TextField("ファイルパス:", _testFilePath);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ファイルを選択"))
            {
                string selectedPath = EditorUtility.OpenFilePanel("AssetLibraryファイルを選択",
                    Path.GetDirectoryName(_testFilePath), "json");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _testFilePath = selectedPath;
                }
            }

            if (GUILayout.Button("フォルダを開く"))
            {
                string directory = Path.GetDirectoryName(_testFilePath);
                if (Directory.Exists(directory))
                {
                    EditorUtility.RevealInFinder(directory);
                }
                else
                {
                    LogMessage($"フォルダが存在しません: {directory}");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();            // ライブラリ操作ボタン
            EditorGUILayout.LabelField("ライブラリ操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを初期化"))
            {
                try
                {
                    _assetLibraryController.InitializeLibrary();
                    LogMessage("ライブラリ初期化: 成功");
                }
                catch (Exception ex)
                {
                    LogMessage($"ライブラリ初期化: 失敗 - {ex.Message}");
                }
            }

            if (GUILayout.Button("強制初期化"))
            {
                try
                {
                    _assetLibraryController.ForceInitializeLibrary();
                    LogMessage("ライブラリ強制初期化: 成功");
                }
                catch (Exception ex)
                {
                    LogMessage($"ライブラリ強制初期化: 失敗 - {ex.Message}");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリ情報を表示"))
            {
                ShowLibraryInfo();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを読み込み"))
            {
                try
                {
                    _assetLibraryController.LoadAssetLibrary(_testFilePath);
                    LogMessage($"ライブラリ読み込み ({Path.GetFileName(_testFilePath)}): 成功");
                    ShowLibraryInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"ライブラリ読み込み ({Path.GetFileName(_testFilePath)}): 失敗 - {ex.Message}");
                }
            }

            if (GUILayout.Button("強制読み込み"))
            {
                try
                {
                    _assetLibraryController.ForceLoadAssetLibrary(_testFilePath);
                    LogMessage($"ライブラリ強制読み込み ({Path.GetFileName(_testFilePath)}): 成功");
                    ShowLibraryInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"ライブラリ強制読み込み ({Path.GetFileName(_testFilePath)}): 失敗 - {ex.Message}");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを保存"))
            {
                try
                {
                    _assetLibraryController.SaveAssetLibrary(_testFilePath);
                    LogMessage($"ライブラリ保存 ({Path.GetFileName(_testFilePath)}): 成功");
                }
                catch (Exception ex)
                {
                    LogMessage($"ライブラリ保存 ({Path.GetFileName(_testFilePath)}): 失敗 - {ex.Message}");
                }
            }

            if (GUILayout.Button("強制保存"))
            {
                try
                {
                    _assetLibraryController.ForceSaveAssetLibrary(_testFilePath);
                    LogMessage($"ライブラリ強制保存 ({Path.GetFileName(_testFilePath)}): 成功");
                }
                catch (Exception ex)
                {
                    LogMessage($"ライブラリ強制保存 ({Path.GetFileName(_testFilePath)}): 失敗 - {ex.Message}");
                }
            }
            EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("テストアセットを追加"))
            {
                try
                {
                    _assetLibraryController.AddTestAsset();
                    LogMessage("テストアセット追加: 成功");
                    ShowLibraryInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"テストアセット追加: 失敗 - {ex.Message}");
                }
            }

            if (GUILayout.Button("ライブラリをクリア"))
            {
                if (EditorUtility.DisplayDialog("確認", "ライブラリの全アセットをクリアしますか？", "はい", "キャンセル"))
                {
                    try
                    {
                        _assetLibraryController.library?.ClearAssets();
                        LogMessage("ライブラリクリア: 成功");
                        ShowLibraryInfo();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"ライブラリクリア: 失敗 - {ex.Message}");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("テストタグを追加"))
            {
                try
                {
                    if (_assetLibraryController.library != null)
                    {
                        var testTags = new[] { "Test", "Debug", "Sample", "Development" };
                        foreach (var tag in testTags)
                        {
                            if (!_assetLibraryController.library.TagExists(tag))
                            {
                                _assetLibraryController.library.AddTag(tag);
                            }
                        }
                        LogMessage("テストタグ追加: 成功");
                        ShowLibraryInfo();
                    }
                    else
                    {
                        LogMessage("テストタグ追加: 失敗 - ライブラリが初期化されていません");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"テストタグ追加: 失敗 - {ex.Message}");
                }
            }

            if (GUILayout.Button("テストアセットタイプを追加"))
            {
                try
                {
                    if (_assetLibraryController.library != null)
                    {
                        var testAssetTypes = new[] { "Avatar", "Accessory", "Cloth", "Animation" };
                        foreach (var assetType in testAssetTypes)
                        {
                            if (!_assetLibraryController.library.AssetTypeExists(assetType))
                            {
                                _assetLibraryController.library.AddAssetType(assetType);
                            }
                        }
                        LogMessage("テストアセットタイプ追加: 成功");
                        ShowLibraryInfo();
                    }
                    else
                    {
                        LogMessage("テストアセットタイプ追加: 失敗 - ライブラリが初期化されていません");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"テストアセットタイプ追加: 失敗 - {ex.Message}");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // ログ表示エリア
            EditorGUILayout.LabelField("ログ", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _autoScroll = EditorGUILayout.Toggle("自動スクロール", _autoScroll);
            if (GUILayout.Button("ログをクリア"))
            {
                _logText = "";
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(_logText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (_autoScroll && Event.current.type == EventType.Repaint)
            {
                _scrollPosition.y = Mathf.Infinity;
            }
        }
        private void ShowLibraryInfo()
        {
            if (_assetLibraryController.library == null)
            {
                LogMessage("ライブラリが初期化されていません。");
                return;
            }

            var library = _assetLibraryController.library;
            LogMessage($"=== ライブラリ情報 ===");
            LogMessage($"最終更新: {library.LastUpdated:yyyy/MM/dd HH:mm:ss}");
            LogMessage($"アセット数: {library.AssetCount}");
            LogMessage($"タグ数: {library.TagsCount}");
            LogMessage($"アセットタイプ数: {library.AssetTypeCount}");

            if (library.AssetCount > 0)
            {
                LogMessage("アセット一覧:");
                var assets = library.GetAllAssets();
                for (int i = 0; i < Math.Min(assets.Count, 10); i++) // 最大10件まで表示
                {
                    var asset = assets[i];
                    LogMessage($"  {i + 1}. {asset.AssetId} (タイプ: {asset.Metadata.AssetType})");
                }
                if (assets.Count > 10)
                {
                    LogMessage($"  ... および他 {assets.Count - 10} 件");
                }
            }

            if (library.TagsCount > 0)
            {
                LogMessage($"タグ一覧: {string.Join(", ", library.GetAllTags())}");
            }

            if (library.AssetTypeCount > 0)
            {
                LogMessage($"アセットタイプ一覧: {string.Join(", ", library.GetAllAssetTypes())}");
            }
        }

        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logText += $"[{timestamp}] {message}\n";

            // ログが長すぎる場合は古い部分を削除
            if (_logText.Length > 10000)
            {
                int cutIndex = _logText.IndexOf('\n', 2000);
                if (cutIndex > 0)
                {
                    _logText = _logText.Substring(cutIndex + 1);
                }
            }

            Repaint();
        }
    }
}