using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using AMU.Editor.VrcAssetManager.Controller;
using AMU.Editor.VrcAssetManager.Schema;
using System.Security.Cryptography;
using System.Net;
using AMU.Editor.Core.Api;
using AMU.Editor.VrcAssetManager.Helper;

namespace AMU.Editor.VrcAssetManager.UI
{
    public class BoothItemImportWindow : EditorWindow
    {
        private string _filePath;
        private List<BoothItemSchema> _boothItems = new List<BoothItemSchema>();
        private List<BoothItemSchema> _filteredBoothItems = new List<BoothItemSchema>();
        private Vector2 _scrollPosition;

        public static void ShowWindowWithFile(string filePath)
        {
            var window = GetWindow<BoothItemImportWindow>(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_title"));
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
                var existing = new HashSet<string>();
                foreach (var asset in AssetLibraryController.Instance.GetAllAssets())
                {
                    var b = asset.boothItem;
                    if (b == null) continue;
                    existing.Add(GetBoothItemKey(b));
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
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_boothImport_loadFailed"), _filePath, ex.Message));
            }
        }

        private string GetBoothItemKey(BoothItemSchema b)
        {
            return b.downloadUrl;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_targetCount"), _filteredBoothItems?.Count ?? 0), MessageType.Info);
            if (_filteredBoothItems == null || _filteredBoothItems.Count == 0)
            {
                EditorGUILayout.HelpBox(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_noItems"), MessageType.Info);
                return;
            }
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (var item in _filteredBoothItems)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_itemName"), item.itemName);
                EditorGUILayout.LabelField(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_author"), item.authorName);
                EditorGUILayout.LabelField(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_url"), item.itemUrl);
                EditorGUILayout.LabelField(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_fileName"), item.fileName);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            if (GUILayout.Button(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_registerAll")))
            {
                RegisterAllAsAssets();
            }
        }

        private AssetSchema CreateAssetFromBoothItem(BoothItemSchema boothItem, string parentGroupId = null, string thumbnailPath = null)
        {
            var asset = new AssetSchema();
            asset.SetBoothItem(new BoothItemSchema(
                boothItem.itemName,
                boothItem.authorName,
                boothItem.description,
                boothItem.itemUrl,
                boothItem.imageUrl,
                boothItem.fileName,
                boothItem.downloadUrl
            ));
            asset.metadata.SetName(boothItem.fileName);
            asset.metadata.SetDescription(boothItem.description);
            if (!string.IsNullOrEmpty(parentGroupId))
            {
                asset.SetParentGroupId(parentGroupId);
            }
            if (!string.IsNullOrEmpty(thumbnailPath))
            {
                asset.metadata.SetThumbnailPath(thumbnailPath);
            }
            return asset;
        }

        private AssetSchema CreateParentAssetFromBoothItem(BoothItemSchema boothItem, string thumbnailPath = null)
        {
            var asset = new AssetSchema();
            asset.SetBoothItem(new BoothItemSchema(
                boothItem.itemName,
                boothItem.authorName,
                boothItem.description,
                boothItem.itemUrl,
                boothItem.imageUrl,
                string.Empty,
                string.Empty
            ));
            asset.metadata.SetName(boothItem.itemName);
            asset.metadata.SetAuthorName(boothItem.authorName);
            asset.metadata.SetDescription(boothItem.description);
            if (!string.IsNullOrEmpty(thumbnailPath))
            {
                asset.metadata.SetThumbnailPath(thumbnailPath);
            }
            return asset;
        }

        private string GetThumbnailDirPath()
        {
            var rootDir = SettingAPI.GetSetting<string>("Core_dirPath");
            var dir = Path.Combine(rootDir, "VrcAssetManager", "BoothItem", "Thumbnail");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private string DownloadImageIfNeeded(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return null;
            var hash = HashUtility.GetHash(imageUrl, false);
            var dir = GetThumbnailDirPath();
            if (string.IsNullOrEmpty(dir)) return null;
            var ext = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
            if (string.IsNullOrEmpty(ext) || ext.Length > 5) ext = ".png";
            var filePath = Path.Combine(dir, hash + ext);
            if (File.Exists(filePath)) return filePath;
            try
            {
                EditorUtility.DisplayProgressBar(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_downloadingImage"), imageUrl, 0f);
                System.Threading.Thread.Sleep(100);
                using (var client = new WebClient())
                {
                    client.DownloadFile(imageUrl, filePath);
                }
                EditorUtility.ClearProgressBar();
                return filePath;
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_downloadImageFailed"), imageUrl, ex.Message));
                return null;
            }
        }

        private void RegisterAllAsAssets()
        {
            var _controller = AssetLibraryController.Instance;
            if (_controller == null || _filteredBoothItems == null) return;
            int total = _filteredBoothItems.Count;
            var imagePathDict = new Dictionary<BoothItemSchema, string>();
            for (int i = 0; i < total; i++)
            {
                var item = _filteredBoothItems[i];
                string localPath = null;
                if (!string.IsNullOrEmpty(item.imageUrl))
                {
                    float progress = (float)i / total;
                    EditorUtility.DisplayProgressBar(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_downloadingImage"), item.itemName, progress);
                    localPath = DownloadImageIfNeeded(item.imageUrl);
                    if (!string.IsNullOrEmpty(localPath))
                    {
                        var rootDir = SettingAPI.GetSetting<string>("Core_dirPath");
                        localPath = localPath.Replace("\\", "/");
                        var rootDirNormalized = rootDir.Replace("\\", "/");
                        if (localPath.StartsWith(rootDirNormalized))
                        {
                            localPath = localPath.Substring(rootDirNormalized.Length).TrimStart('/', '\\');
                        }
                        else
                        {
                            localPath = null;
                        }
                    }
                }
                imagePathDict[item] = localPath;
            }
            EditorUtility.ClearProgressBar();

            int parentCount = 0;
            int childCount = 0;

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
                    var parent = existing.Find(a => a.hasChildAssets);
                    if (parent != null)
                    {
                        foreach (var boothItem in group)
                        {
                            var childAsset = CreateAssetFromBoothItem(boothItem, parent.assetId.ToString(), imagePathDict.ContainsKey(boothItem) ? imagePathDict[boothItem] : null);
                            if (string.IsNullOrEmpty(childAsset.metadata.assetType) && !string.IsNullOrEmpty(parent.metadata.assetType))
                            {
                                childAsset.metadata.SetAssetType(parent.metadata.assetType);
                            }
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
                        var newParent = CreateParentAssetFromBoothItem(group[0], imagePathDict.ContainsKey(group[0]) ? imagePathDict[group[0]] : null);
                        foreach (var exist in existing)
                        {
                            if (exist.boothItem != null && !string.IsNullOrEmpty(exist.boothItem.fileName))
                            {
                                exist.metadata.SetName(exist.boothItem.fileName);
                            }
                            exist.SetParentGroupId(newParent.assetId.ToString());
                            newParent.AddChildAssetId(exist.assetId.ToString());
                            _controller.UpdateAsset(exist);
                        }
                        foreach (var boothItem in group)
                        {
                            var childAsset = CreateAssetFromBoothItem(boothItem, newParent.assetId.ToString(), imagePathDict.ContainsKey(boothItem) ? imagePathDict[boothItem] : null);
                            if (string.IsNullOrEmpty(childAsset.metadata.assetType) && !string.IsNullOrEmpty(newParent.metadata.assetType))
                            {
                                childAsset.metadata.SetAssetType(newParent.metadata.assetType);
                            }
                            newParent.AddChildAssetId(childAsset.assetId.ToString());
                            _controller.AddAsset(childAsset);
                            childCount++;
                        }
                        _controller.AddAsset(newParent);
                        parentCount++;
                        continue;
                    }
                }
                if (group.Count == 1)
                {
                    var boothItem = group[0];
                    var asset = CreateAssetFromBoothItem(boothItem, null, imagePathDict.ContainsKey(boothItem) ? imagePathDict[boothItem] : null);
                    asset.metadata.SetName(boothItem.itemName);
                    asset.metadata.SetAuthorName(boothItem.authorName);
                    asset.metadata.SetDescription(boothItem.description);
                    _controller.AddAsset(asset);
                    parentCount++;
                    continue;
                }
                var parentAsset = CreateParentAssetFromBoothItem(group[0], imagePathDict.ContainsKey(group[0]) ? imagePathDict[group[0]] : null);
                parentAsset.metadata.SetDescription(group[0].description);
                foreach (var boothItem in group)
                {
                    var childAsset = CreateAssetFromBoothItem(boothItem, parentAsset.assetId.ToString(), imagePathDict.ContainsKey(boothItem) ? imagePathDict[boothItem] : null);
                    if (string.IsNullOrEmpty(childAsset.metadata.assetType) && !string.IsNullOrEmpty(parentAsset.metadata.assetType))
                    {
                        childAsset.metadata.SetAssetType(parentAsset.metadata.assetType);
                    }
                    parentAsset.AddChildAssetId(childAsset.assetId.ToString());
                    _controller.AddAsset(childAsset);
                    childCount++;
                }
                _controller.AddAsset(parentAsset);
                parentCount++;
            }
            Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_ui_boothImport_registerResult"), parentCount, childCount));
            Close();
        }
    }
}
