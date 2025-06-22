using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class BoothItemImportWindow : EditorWindow
    {
        private string _filePath;
        private List<BoothItemSchema> _boothItems = new List<BoothItemSchema>();
        private Vector2 _scrollPosition;
        private AssetLibraryController _controller;

        public static void ShowWindowWithFile(AssetLibraryController controller, string filePath)
        {
            var window = GetWindow<BoothItemImportWindow>("Boothアイテム一括登録");
            window._controller = controller;
            window._filePath = filePath;
            window.LoadBoothItems();
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void LoadBoothItems()
        {
            _boothItems.Clear();
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                return;
            try
            {
                var json = File.ReadAllText(_filePath);
                _boothItems = JsonConvert.DeserializeObject<List<BoothItemSchema>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"BoothItemの読み込みに失敗: {_filePath}\n{ex.Message}");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Boothアイテム一括登録", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            if (_boothItems == null || _boothItems.Count == 0)
            {
                EditorGUILayout.HelpBox("Boothアイテムが見つかりません。", MessageType.Info);
                return;
            }
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (var item in _boothItems)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("アイテム名", item.itemName);
                EditorGUILayout.LabelField("作者", item.authorName);
                EditorGUILayout.LabelField("URL", item.itemUrl);
                EditorGUILayout.LabelField("ファイル名", item.fileName);
                EditorGUILayout.LabelField("ダウンロードURL", item.downloadUrl);
                if (!string.IsNullOrEmpty(item.imageUrl))
                {
                    GUILayout.Label(item.imageUrl, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            if (GUILayout.Button("全てアセットとして登録"))
            {
                RegisterAllAsAssets();
            }
        }

        private void RegisterAllAsAssets()
        {
            if (_controller == null || _boothItems == null) return;
            int parentCount = 0;
            int childCount = 0;
            // itemUrlでグループ化
            var grouped = new Dictionary<string, List<BoothItemSchema>>();
            foreach (var item in _boothItems)
            {
                if (string.IsNullOrEmpty(item.itemUrl)) continue;
                if (!grouped.ContainsKey(item.itemUrl)) grouped[item.itemUrl] = new List<BoothItemSchema>();
                grouped[item.itemUrl].Add(item);
            }

            foreach (var kv in grouped)
            {
                var group = kv.Value;
                if (group.Count == 0) continue;
                // 親アセット作成（fileName, downloadUrl以外の情報のみ）
                var parentAsset = new AssetSchema();
                var first = group[0];
                parentAsset.SetBoothItem(new BoothItemSchema(
                    first.itemName,
                    first.authorName,
                    first.itemUrl,
                    first.imageUrl,
                    string.Empty, // fileNameなし
                    string.Empty  // downloadUrlなし
                ));
                parentAsset.metadata.SetName(first.itemName);
                parentAsset.metadata.SetAuthorName(first.authorName);
                // 必要に応じて他のメタ情報もセット
                // 子アセットを作成
                foreach (var boothItem in group)
                {
                    var childAsset = new AssetSchema();
                    childAsset.SetBoothItem(new BoothItemSchema(
                        boothItem.itemName,
                        boothItem.authorName,
                        boothItem.itemUrl,
                        boothItem.imageUrl,
                        boothItem.fileName,
                        boothItem.downloadUrl
                    ));
                    // fileName, downloadUrlのみセット → 全情報セットに修正
                    childAsset.metadata.SetName(boothItem.fileName); // 子の名前はfileNameで区別
                    childAsset.SetParentGroupId(parentAsset.assetId.ToString());
                    parentAsset.AddChildAssetId(childAsset.assetId.ToString());
                    _controller.AddAsset(childAsset);
                    childCount++;
                }
                _controller.AddAsset(parentAsset);
                parentCount++;
            }
            Debug.Log($"{parentCount}件の親アセットと{childCount}件の子アセットを登録しました。");
            Close();
        }
    }
}
