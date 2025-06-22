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
        private List<BoothItemSchema> _filteredBoothItems = new List<BoothItemSchema>();
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
            _filteredBoothItems.Clear();
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                return;
            try
            {
                var json = File.ReadAllText(_filePath);
                _boothItems = JsonConvert.DeserializeObject<List<BoothItemSchema>>(json);
                // 既存アセットと重複するBoothItemを除外
                var existing = new HashSet<string>();
                if (_controller != null)
                {
                    foreach (var asset in _controller.GetAllAssets())
                    {
                        var b = asset.boothItem;
                        if (b == null) continue;
                        existing.Add(GetBoothItemKey(b));
                    }
                }
                foreach (var item in _boothItems)
                {
                    if (!existing.Contains(GetBoothItemKey(item)))
                    {
                        _filteredBoothItems.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"BoothItemの読み込みに失敗: {_filePath}\n{ex.Message}");
            }
        }

        // BoothItemSchemaの全プロパティで比較するためのキー生成
        private string GetBoothItemKey(BoothItemSchema b)
        {
            return $"{b.itemName}\0{b.authorName}\0{b.itemUrl}\0{b.imageUrl}\0{b.fileName}\0{b.downloadUrl}";
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Boothアイテム一括登録", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            if (_filteredBoothItems == null || _filteredBoothItems.Count == 0)
            {
                EditorGUILayout.HelpBox("Boothアイテムが見つかりません。", MessageType.Info);
                return;
            }
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (var item in _filteredBoothItems)
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
            if (_controller == null || _filteredBoothItems == null) return;
            int parentCount = 0;
            int childCount = 0;
            // itemUrlでグループ化
            var grouped = new Dictionary<string, List<BoothItemSchema>>();
            foreach (var item in _filteredBoothItems)
            {
                if (string.IsNullOrEmpty(item.itemUrl)) continue;
                if (!grouped.ContainsKey(item.itemUrl)) grouped[item.itemUrl] = new List<BoothItemSchema>();
                grouped[item.itemUrl].Add(item);
            }

            var allAssets = _controller.GetAllAssets();

            foreach (var kv in grouped)
            {
                var group = kv.Value;
                if (group.Count == 0) continue;
                string itemUrl = kv.Key;
                // 既存アセットで同じitemUrlを持つものを取得
                var existing = new List<AssetSchema>();
                foreach (var asset in allAssets)
                {
                    if (asset.boothItem != null && asset.boothItem.itemUrl == itemUrl)
                    {
                        existing.Add(asset);
                    }
                }
                if (existing.Count > 0)
                {
                    // 子アセットを持つ既存アセットを探す
                    var parent = existing.Find(a => a.hasChildAssets);
                    if (parent != null)
                    {
                        // 子アセットとして登録
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
                            childAsset.metadata.SetName(boothItem.fileName);
                            childAsset.SetParentGroupId(parent.assetId.ToString());
                            parent.AddChildAssetId(childAsset.assetId.ToString());
                            _controller.AddAsset(childAsset);
                            childCount++;
                        }
                        _controller.UpdateAsset(parent);
                        parentCount++;
                        continue;
                    }
                    else
                    {
                        // 新たに親グループを作成し、既存・新規両方を子として登録
                        var newParent = new AssetSchema();
                        var first = group[0];
                        newParent.SetBoothItem(new BoothItemSchema(
                            first.itemName,
                            first.authorName,
                            first.itemUrl,
                            first.imageUrl,
                            string.Empty,
                            string.Empty
                        ));
                        newParent.metadata.SetName(first.itemName);
                        newParent.metadata.SetAuthorName(first.authorName);
                        // 既存アセットを子に
                        foreach (var exist in existing)
                        {
                            // 既存アセットの名前をファイル名にリネーム
                            if (exist.boothItem != null && !string.IsNullOrEmpty(exist.boothItem.fileName))
                            {
                                exist.metadata.SetName(exist.boothItem.fileName);
                            }
                            exist.SetParentGroupId(newParent.assetId.ToString());
                            newParent.AddChildAssetId(exist.assetId.ToString());
                            _controller.UpdateAsset(exist);
                        }
                        // 新規アセットも子に
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
                            childAsset.metadata.SetName(boothItem.fileName);
                            childAsset.SetParentGroupId(newParent.assetId.ToString());
                            newParent.AddChildAssetId(childAsset.assetId.ToString());
                            _controller.AddAsset(childAsset);
                            childCount++;
                        }
                        _controller.AddAsset(newParent);
                        parentCount++;
                        continue;
                    }
                }
                // 既存がなければ従来通り
                if (group.Count == 1)
                {
                    var boothItem = group[0];
                    var asset = new AssetSchema();
                    asset.SetBoothItem(new BoothItemSchema(
                        boothItem.itemName,
                        boothItem.authorName,
                        boothItem.itemUrl,
                        boothItem.imageUrl,
                        boothItem.fileName,
                        boothItem.downloadUrl
                    ));
                    asset.metadata.SetName(boothItem.itemName);
                    asset.metadata.SetAuthorName(boothItem.authorName);
                    _controller.AddAsset(asset);
                    parentCount++;
                    continue;
                }
                // 2件以上の場合は親子アセット構造で登録
                var parentAsset = new AssetSchema();
                var firstNew = group[0];
                parentAsset.SetBoothItem(new BoothItemSchema(
                    firstNew.itemName,
                    firstNew.authorName,
                    firstNew.itemUrl,
                    firstNew.imageUrl,
                    string.Empty,
                    string.Empty
                ));
                parentAsset.metadata.SetName(firstNew.itemName);
                parentAsset.metadata.SetAuthorName(firstNew.authorName);
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
                    childAsset.metadata.SetName(boothItem.fileName);
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
