using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Controllers;

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
            
            EditorGUILayout.Space();

            // ライブラリ操作ボタン
            EditorGUILayout.LabelField("ライブラリ操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを初期化"))
            {
                bool result = _assetLibraryController.InitializeLibrary();
                LogMessage($"ライブラリ初期化: {(result ? "成功" : "失敗")}");
            }
            
            if (GUILayout.Button("ライブラリ情報を表示"))
            {
                ShowLibraryInfo();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを読み込み"))
            {
                bool result = _assetLibraryController.LoadAssetLibrary(_testFilePath);
                LogMessage($"ライブラリ読み込み ({Path.GetFileName(_testFilePath)}): {(result ? "成功" : "失敗")}");
                if (result) ShowLibraryInfo();
            }
            
            if (GUILayout.Button("強制読み込み"))
            {
                bool result = _assetLibraryController.ForceLoadAssetLibrary(_testFilePath);
                LogMessage($"ライブラリ強制読み込み ({Path.GetFileName(_testFilePath)}): {(result ? "成功" : "失敗")}");
                if (result) ShowLibraryInfo();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("ライブラリを保存"))
            {
                bool result = _assetLibraryController.SaveAssetLibrary(_testFilePath);
                LogMessage($"ライブラリ保存 ({Path.GetFileName(_testFilePath)}): {(result ? "成功" : "失敗")}");
            }
            
            if (GUILayout.Button("強制保存"))
            {
                bool result = _assetLibraryController.ForceSaveAssetLibrary(_testFilePath);
                LogMessage($"ライブラリ強制保存 ({Path.GetFileName(_testFilePath)}): {(result ? "成功" : "失敗")}");
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
                    LogMessage($"  {i + 1}. {asset.AssetId:D} (タイプ: {asset.Metadata.AssetType})");
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