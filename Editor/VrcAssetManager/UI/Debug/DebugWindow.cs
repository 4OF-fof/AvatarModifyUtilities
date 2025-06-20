using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;

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
            _testFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities", "VrcAssetManager", "AssetLibrary.json");

            // ウィンドウを開いたときに自動でライブラリを読み込み
            _assetLibraryController.LoadAssetLibrary();

            ShowLibraryInfo();
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
                _assetLibraryController.InitializeLibrary();
                LogMessage("ライブラリ初期化: 成功");
            }

            if (GUILayout.Button("ライブラリを強制初期化"))
            {
                _assetLibraryController.ForceInitializeLibrary();
                LogMessage("ライブラリ強制初期化: 成功");
            }

            if (GUILayout.Button("ライブラリ情報を表示"))
            {
                ShowLibraryInfo();
            }
            EditorGUILayout.EndHorizontal();

            // Optimize関連ボタン追加
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("タグ最適化"))
            {
                _assetLibraryController.OptimizeTags();
                ShowLibraryInfo();
                LogMessage(_assetLibraryController.GetAllTags().Count == 0 ? "タグ最適化: 変更なしまたは全削除" : "タグ最適化: 完了");
            }
            if (GUILayout.Button("アセットタイプ最適化"))
            {
                _assetLibraryController.OptimizeAssetTypes();
                ShowLibraryInfo();
                LogMessage(_assetLibraryController.GetAllAssetTypes().Count == 0 ? "アセットタイプ最適化: 変更なしまたは全削除" : "アセットタイプ最適化: 完了");
            }
            if (GUILayout.Button("ライブラリ全体最適化"))
            {
                _assetLibraryController.OptimizeAssetLibrary();
                ShowLibraryInfo();
                LogMessage((_assetLibraryController.GetAllTags().Count == 0 && _assetLibraryController.GetAllAssetTypes().Count == 0) ? "ライブラリ全体最適化: 変更なしまたは全削除" : "ライブラリ全体最適化: 完了");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを読み込み"))
            {
                try
                {
                    _assetLibraryController.LoadAssetLibrary();
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
                    _assetLibraryController.ForceLoadAssetLibrary();
                    LogMessage($"ライブラリ強制読み込み ({Path.GetFileName(_testFilePath)}): 成功");
                    ShowLibraryInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"ライブラリ強制読み込み ({Path.GetFileName(_testFilePath)}): 失敗 - {ex.Message}");
                }
            }
            if (GUILayout.Button("データ同期"))
            {
                try
                {
                    _assetLibraryController.SyncAssetLibrary();
                    LogMessage($"データ同期 ({Path.GetFileName(_testFilePath)}): 成功");
                    ShowLibraryInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"データ同期 ({Path.GetFileName(_testFilePath)}): 失敗 - {ex.Message}");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを保存"))
            {
                try
                {
                    _assetLibraryController.SaveAssetLibrary();
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
                    _assetLibraryController.ForceSaveAssetLibrary();
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
                    var testAsset = new AssetSchema();
                    _assetLibraryController.AddAsset(testAsset);
                    LogMessage("テストアセット追加: 成功");
                    ShowLibraryInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"テストアセット追加: 失敗 - {ex.Message}");
                }
            }
            if (GUILayout.Button("テストタグを追加"))
            {
                try
                {
                    var testTags = new[] { "Test", "Debug", "Sample", "Development" };
                    foreach (var tag in testTags)
                    {
                        if (!_assetLibraryController.TagExists(tag))
                        {
                            _assetLibraryController.AddTag(tag);
                        }
                    }
                    LogMessage("テストタグ追加: 成功");
                    ShowLibraryInfo();
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
                    var testAssetTypes = new[] { "Avatar", "Accessory", "Cloth", "Animation" };
                    foreach (var assetType in testAssetTypes)
                    {
                        if (!_assetLibraryController.AssetTypeExists(assetType))
                        {
                            _assetLibraryController.AddAssetType(assetType);
                        }
                    }
                    LogMessage("テストアセットタイプ追加: 成功");
                    ShowLibraryInfo();
                }
                catch (Exception ex)
                {
                    LogMessage($"テストアセットタイプ追加: 失敗 - {ex.Message}");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 個別管理機能
            EditorGUILayout.LabelField("個別管理", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("アセットをクリア"))
            {
                if (EditorUtility.DisplayDialog("確認", "ライブラリの全アセットをクリアしますか？", "はい", "キャンセル"))
                {
                    try
                    {
                        _assetLibraryController.ClearAssets();
                        LogMessage("アセットクリア: 成功");
                        ShowLibraryInfo();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"アセットクリア: 失敗 - {ex.Message}");
                    }
                }
            }
            if (GUILayout.Button("タグをクリア"))
            {
                if (EditorUtility.DisplayDialog("確認", "全てのタグをクリアしますか？", "はい", "キャンセル"))
                {
                    try
                    {
                        _assetLibraryController.ClearTags();
                        LogMessage("タグクリア: 成功");
                        ShowLibraryInfo();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"タグクリア: 失敗 - {ex.Message}");
                    }
                }
            }

            if (GUILayout.Button("アセットタイプをクリア"))
            {
                if (EditorUtility.DisplayDialog("確認", "全てのアセットタイプをクリアしますか？", "はい", "キャンセル"))
                {
                    try
                    {
                        _assetLibraryController.ClearAssetTypes();
                        LogMessage("アセットタイプクリア: 成功");
                        ShowLibraryInfo();
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"アセットタイプクリア: 失敗 - {ex.Message}");
                    }
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
            try
            {
                var assets = _assetLibraryController.GetAllAssets();
                var tags = _assetLibraryController.GetAllTags();
                var assetTypes = _assetLibraryController.GetAllAssetTypes();
                LogMessage($"=== ライブラリ情報 ===");
                LogMessage($"アセット数: {assets?.Count ?? 0}");
                LogMessage($"タグ数: {tags?.Count ?? 0}");
                LogMessage($"アセットタイプ数: {assetTypes?.Count ?? 0}");

                if (assets != null && assets.Count > 0)
                {
                    LogMessage("アセット一覧:");
                    int count = 0;
                    foreach (var kvp in assets)
                    {
                        if (count >= 10) break; // 最大10件まで表示
                        var asset = kvp.Value;
                        LogMessage($"  {count + 1}. {asset.AssetId} (タイプ: {asset.Metadata.AssetType})");
                        count++;
                    }
                    if (assets.Count > 10)
                    {
                        LogMessage($"  ... および他 {assets.Count - 10} 件");
                    }
                }

                if (tags != null && tags.Count > 0)
                {
                    LogMessage($"タグ一覧: {string.Join(", ", tags)}");
                }

                if (assetTypes != null && assetTypes.Count > 0)
                {
                    LogMessage($"アセットタイプ一覧: {string.Join(", ", assetTypes)}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ライブラリ情報取得: 失敗 - {ex}");
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