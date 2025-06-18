using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Schema;
using Newtonsoft.Json;

namespace AMU.Editor.VrcAssetManager.UI
{
    /// <summary>
    /// AssetLibraryのテスト用ウィンドウ
    /// AssetLibraryの作成・操作・保存・読み込みなどをテストできます
    /// </summary>
    public class AssetLibraryTestWindow : EditorWindow
    {
        private AssetLibrarySchema _currentLibrary;
        private string _libraryFilePath;
        private Vector2 _scrollPosition;
        private string _newAssetName = "New Asset";
        private AssetType _newAssetType = AssetType.Other;
        private string _newAssetFilePath = "";
        private string _newAssetAuthor = "Test Author";
        private string _selectedAssetId = "";
        private string _newGroupName = "New Group";
        private string _testMessage = "";
        private bool _showAssetDetails = false;
        private bool _showGroupDetails = false;

        [MenuItem("Tools/Asset Library Test Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetLibraryTestWindow>();
            window.minSize = new Vector2(1200, 800);
            window.maxSize = new Vector2(1200, 800);
            window.titleContent = new GUIContent("Asset Library Test");
            window.Show();
        }

        private void OnEnable()
        {
            _libraryFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "TestAssetLibrary.json"));
            if (_currentLibrary == null)
            {
                _currentLibrary = new AssetLibrarySchema();
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);
            DrawLibraryInfo();
            EditorGUILayout.Space(10);
            DrawFileOperations();
            EditorGUILayout.Space(10);
            DrawAssetOperations();
            EditorGUILayout.Space(10);
            DrawGroupOperations();
            EditorGUILayout.Space(10);
            DrawAssetList();
            EditorGUILayout.Space(10);
            DrawTestResults();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Asset Library Test Window", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("このウィンドウでAssetLibraryの基本機能をテストできます", EditorStyles.helpBox);
        }

        private void DrawLibraryInfo()
        {
            EditorGUILayout.LabelField("Library Information", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Version: {_currentLibrary.Version}");
                EditorGUILayout.LabelField($"Last Updated: {_currentLibrary.LastUpdated:yyyy/MM/dd HH:mm:ss}");
                EditorGUILayout.LabelField($"Asset Count: {_currentLibrary.AssetCount}");
                EditorGUILayout.LabelField($"Group Count: {_currentLibrary.GroupCount}");
                EditorGUILayout.LabelField($"Has Assets: {_currentLibrary.HasAssets}");
                EditorGUILayout.LabelField($"File Path: {_libraryFilePath}");
            }
        }

        private void DrawFileOperations()
        {
            EditorGUILayout.LabelField("File Operations", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("New Library"))
                    {
                        CreateNewLibrary();
                    }

                    if (GUILayout.Button("Save Library"))
                    {
                        SaveLibrary();
                    }

                    if (GUILayout.Button("Load Library"))
                    {
                        LoadLibrary();
                    }
                }

                EditorGUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("File Path:", GUILayout.Width(70));
                    _libraryFilePath = EditorGUILayout.TextField(_libraryFilePath);

                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        var path = EditorUtility.SaveFilePanel("Asset Library", Application.dataPath, "AssetLibrary", "json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            _libraryFilePath = path;
                        }
                    }
                }
            }
        }

        private void DrawAssetOperations()
        {
            EditorGUILayout.LabelField("Asset Operations", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // アセット追加UI
                EditorGUILayout.LabelField("Add New Asset:", EditorStyles.miniBoldLabel);
                _newAssetName = EditorGUILayout.TextField("Name:", _newAssetName);

                // アセットタイプの選択
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Type:", GUILayout.Width(50));
                    var typeOptions = new string[] { "Avatar", "Clothing", "Accessory", "Other" };
                    var selectedIndex = Array.IndexOf(typeOptions, _newAssetType.Value);
                    if (selectedIndex == -1) selectedIndex = 3; // Default to "Other"

                    selectedIndex = EditorGUILayout.Popup(selectedIndex, typeOptions);
                    _newAssetType = new AssetType(typeOptions[selectedIndex]);
                }

                _newAssetAuthor = EditorGUILayout.TextField("Author:", _newAssetAuthor);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _newAssetFilePath = EditorGUILayout.TextField("File Path:", _newAssetFilePath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        var path = EditorUtility.OpenFilePanel("Select Asset File", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            _newAssetFilePath = path;
                        }
                    }
                }

                EditorGUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Asset"))
                    {
                        AddTestAsset();
                    }

                    if (GUILayout.Button("Add Random Assets (5)"))
                    {
                        AddRandomAssets(5);
                    }

                    if (GUILayout.Button("Clear All Assets"))
                    {
                        ClearAllAssets();
                    }
                }

                EditorGUILayout.Space(10);

                // アセット操作UI
                EditorGUILayout.LabelField("Asset Operations:", EditorStyles.miniBoldLabel);
                _selectedAssetId = EditorGUILayout.TextField("Asset ID:", _selectedAssetId);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Remove Asset"))
                    {
                        RemoveAsset();
                    }

                    if (GUILayout.Button("Get Asset Info"))
                    {
                        GetAssetInfo();
                    }

                    if (GUILayout.Button("Toggle Favorite"))
                    {
                        ToggleAssetFavorite();
                    }
                }
            }
        }

        private void DrawGroupOperations()
        {
            EditorGUILayout.LabelField("Group Operations", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _newGroupName = EditorGUILayout.TextField("Group Name:", _newGroupName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Group"))
                    {
                        CreateGroup();
                    }

                    if (GUILayout.Button("Add Asset to Group"))
                    {
                        AddAssetToGroup();
                    }

                    if (GUILayout.Button("Remove from Group"))
                    {
                        RemoveAssetFromGroup();
                    }
                }
            }
        }

        private void DrawAssetList()
        {
            EditorGUILayout.LabelField("Asset List", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _showAssetDetails = EditorGUILayout.Foldout(_showAssetDetails, "Show Asset Details");

                if (_showAssetDetails)
                {
                    foreach (var kvp in _currentLibrary.Assets)
                    {
                        var asset = kvp.Value;
                        DrawAssetItem(asset);
                    }
                }

                EditorGUILayout.Space(5);

                _showGroupDetails = EditorGUILayout.Foldout(_showGroupDetails, "Show Group Details");

                if (_showGroupDetails)
                {
                    foreach (var kvp in _currentLibrary.Groups)
                    {
                        var group = kvp.Value;
                        DrawGroupItem(kvp.Key, group);
                    }
                }

                EditorGUILayout.Space(10);

                // 統計情報
                EditorGUILayout.LabelField("Statistics:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Visible Assets: {_currentLibrary.GetVisibleAssets().Count()}");
                EditorGUILayout.LabelField($"Favorite Assets: {_currentLibrary.GetFavoriteAssets().Count()}");
                EditorGUILayout.LabelField($"Avatar Assets: {_currentLibrary.GetAssetsByType(AssetType.Avatar).Count()}");
                EditorGUILayout.LabelField($"Clothing Assets: {_currentLibrary.GetAssetsByType(AssetType.Clothing).Count()}");
            }
        }

        private void DrawAssetItem(AssetSchema asset)
        {
            using (new EditorGUILayout.VerticalScope("helpBox"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"[{asset.AssetType}] {asset.Metadata.Name}", EditorStyles.boldLabel);
                    if (asset.State.IsFavorite)
                    {
                        EditorGUILayout.LabelField("★", GUILayout.Width(20));
                    }
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        _selectedAssetId = asset.Id.Value;
                    }
                }

                EditorGUILayout.LabelField($"ID: {asset.Id}");
                EditorGUILayout.LabelField($"Author: {asset.Metadata.AuthorName}");
                EditorGUILayout.LabelField($"File: {asset.FileInfo.FilePath}");
                EditorGUILayout.LabelField($"Created: {asset.Metadata.CreatedDate:yyyy/MM/dd}");

                if (asset.HasParentGroup)
                {
                    EditorGUILayout.LabelField($"Parent Group: {asset.ParentGroupId}");
                }
            }
        }

        private void DrawGroupItem(AssetId groupId, AssetGroupSchema group)
        {
            using (new EditorGUILayout.VerticalScope("helpBox"))
            {
                EditorGUILayout.LabelField($"Group: {group.GroupName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"ID: {groupId}");
                EditorGUILayout.LabelField($"Level: {group.GroupLevel}");
                EditorGUILayout.LabelField($"Children: {group.ChildAssetIds.Count}");

                if (group.HasParent)
                {
                    EditorGUILayout.LabelField($"Parent: {group.ParentGroupId}");
                }
            }
        }

        private void DrawTestResults()
        {
            EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (!string.IsNullOrEmpty(_testMessage))
                {
                    EditorGUILayout.TextArea(_testMessage, GUILayout.Height(80));
                }

                if (GUILayout.Button("Clear Messages"))
                {
                    _testMessage = "";
                }
            }
        }

        #region Operations

        private void CreateNewLibrary()
        {
            _currentLibrary = new AssetLibrarySchema();
            LogMessage("新しいライブラリを作成しました。");
        }

        private void SaveLibrary()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_currentLibrary, Formatting.Indented);
                File.WriteAllText(_libraryFilePath, json);
                LogMessage($"ライブラリを保存しました: {_libraryFilePath}");
            }
            catch (Exception e)
            {
                LogMessage($"保存エラー: {e.Message}");
            }
        }

        private void LoadLibrary()
        {
            try
            {
                if (File.Exists(_libraryFilePath))
                {
                    var json = File.ReadAllText(_libraryFilePath);
                    _currentLibrary = JsonConvert.DeserializeObject<AssetLibrarySchema>(json);
                    LogMessage($"ライブラリを読み込みました: {_libraryFilePath}");
                }
                else
                {
                    LogMessage("ファイルが見つかりません。");
                }
            }
            catch (Exception e)
            {
                LogMessage($"読み込みエラー: {e.Message}");
            }
        }

        private void AddTestAsset()
        {
            var asset = new AssetSchema(_newAssetName, _newAssetType, _newAssetFilePath);
            asset.Metadata.AuthorName = _newAssetAuthor;

            if (_currentLibrary.AddAsset(asset))
            {
                LogMessage($"アセットを追加しました: {asset.Metadata.Name} (ID: {asset.Id})");
            }
            else
            {
                LogMessage("アセットの追加に失敗しました。");
            }
        }

        private void AddRandomAssets(int count)
        {
            var assetTypes = new AssetType[] { AssetType.Avatar, AssetType.Clothing, AssetType.Accessory, AssetType.Other };
            var authors = new string[] { "Author A", "Author B", "Author C", "Test Creator" };

            for (int i = 0; i < count; i++)
            {
                var randomType = assetTypes[UnityEngine.Random.Range(0, assetTypes.Length)];
                var randomAuthor = authors[UnityEngine.Random.Range(0, authors.Length)];
                var assetName = $"Random Asset {i + 1}";
                var filePath = $"/path/to/asset_{i + 1}.unity";

                var asset = new AssetSchema(assetName, randomType, filePath);
                asset.Metadata.AuthorName = randomAuthor;
                asset.Metadata.Description = $"This is a test asset number {i + 1}";
                asset.AddTag("test");
                asset.AddTag(randomType.Value.ToLower());

                // ランダムでお気に入りに設定
                if (UnityEngine.Random.Range(0, 3) == 0)
                {
                    asset.State.IsFavorite = true;
                }

                _currentLibrary.AddAsset(asset);
            }

            LogMessage($"{count}個のランダムアセットを追加しました。");
        }

        private void ClearAllAssets()
        {
            _currentLibrary.ClearAssets();
            LogMessage("すべてのアセットをクリアしました。");
        }

        private void RemoveAsset()
        {
            if (string.IsNullOrEmpty(_selectedAssetId))
            {
                LogMessage("Asset IDが指定されていません。");
                return;
            }

            if (AssetId.TryParse(_selectedAssetId, out var assetId))
            {
                if (_currentLibrary.RemoveAsset(assetId))
                {
                    LogMessage($"アセットを削除しました: {assetId}");
                }
                else
                {
                    LogMessage($"アセットが見つかりません: {assetId}");
                }
            }
            else
            {
                LogMessage("無効なAsset IDです。");
            }
        }

        private void GetAssetInfo()
        {
            if (string.IsNullOrEmpty(_selectedAssetId))
            {
                LogMessage("Asset IDが指定されていません。");
                return;
            }

            if (AssetId.TryParse(_selectedAssetId, out var assetId))
            {
                var asset = _currentLibrary.GetAsset(assetId);
                if (asset != null)
                {
                    var info = $"Asset Info:\n" +
                              $"Name: {asset.Metadata.Name}\n" +
                              $"Type: {asset.AssetType}\n" +
                              $"Author: {asset.Metadata.AuthorName}\n" +
                              $"File: {asset.FileInfo.FilePath}\n" +
                              $"Favorite: {asset.State.IsFavorite}\n" +
                              $"Created: {asset.Metadata.CreatedDate}\n" +
                              $"Tags: {string.Join(", ", asset.Metadata.Tags)}";
                    LogMessage(info);
                }
                else
                {
                    LogMessage($"アセットが見つかりません: {assetId}");
                }
            }
            else
            {
                LogMessage("無効なAsset IDです。");
            }
        }

        private void ToggleAssetFavorite()
        {
            if (string.IsNullOrEmpty(_selectedAssetId))
            {
                LogMessage("Asset IDが指定されていません。");
                return;
            }

            if (AssetId.TryParse(_selectedAssetId, out var assetId))
            {
                var asset = _currentLibrary.GetAsset(assetId);
                if (asset != null)
                {
                    asset.State.IsFavorite = !asset.State.IsFavorite;
                    LogMessage($"お気に入り状態を切り替えました: {asset.Metadata.Name} -> {asset.State.IsFavorite}");
                }
                else
                {
                    LogMessage($"アセットが見つかりません: {assetId}");
                }
            }
            else
            {
                LogMessage("無効なAsset IDです。");
            }
        }

        private void CreateGroup()
        {
            var groupId = AssetId.NewId();
            var group = new AssetGroupSchema();
            group.GroupName = _newGroupName;

            _currentLibrary.AddGroup(groupId, group);
            LogMessage($"グループを作成しました: {_newGroupName} (ID: {groupId})");
        }

        private void AddAssetToGroup()
        {
            if (string.IsNullOrEmpty(_selectedAssetId))
            {
                LogMessage("Asset IDが指定されていません。");
                return;
            }

            // 最初のグループを取得（簡易実装）
            var firstGroup = _currentLibrary.Groups.FirstOrDefault();
            if (firstGroup.Key.Value == null)
            {
                LogMessage("グループが存在しません。先にグループを作成してください。");
                return;
            }

            if (AssetId.TryParse(_selectedAssetId, out var assetId))
            {
                var asset = _currentLibrary.GetAsset(assetId);
                if (asset != null)
                {
                    asset.SetParentGroup(firstGroup.Key);
                    firstGroup.Value.AddChildAsset(assetId);
                    LogMessage($"アセットをグループに追加しました: {asset.Metadata.Name} -> {firstGroup.Value.GroupName}");
                }
                else
                {
                    LogMessage($"アセットが見つかりません: {assetId}");
                }
            }
            else
            {
                LogMessage("無効なAsset IDです。");
            }
        }

        private void RemoveAssetFromGroup()
        {
            if (string.IsNullOrEmpty(_selectedAssetId))
            {
                LogMessage("Asset IDが指定されていません。");
                return;
            }

            if (AssetId.TryParse(_selectedAssetId, out var assetId))
            {
                var asset = _currentLibrary.GetAsset(assetId);
                if (asset != null && asset.HasParentGroup)
                {
                    var groupId = asset.ParentGroupId;
                    var group = _currentLibrary.GetGroup(groupId);

                    asset.RemoveFromParentGroup();
                    group?.RemoveChildAsset(assetId);

                    LogMessage($"アセットをグループから削除しました: {asset.Metadata.Name}");
                }
                else
                {
                    LogMessage("アセットがグループに属していません。");
                }
            }
            else
            {
                LogMessage("無効なAsset IDです。");
            }
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _testMessage += $"[{timestamp}] {message}\n";

            // メッセージが長くなりすぎないように制限
            var lines = _testMessage.Split('\n');
            if (lines.Length > 20)
            {
                _testMessage = string.Join("\n", lines.Skip(lines.Length - 20));
            }
        }

        #endregion
    }
}
