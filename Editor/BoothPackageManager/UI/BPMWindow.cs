using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AMU.BoothPackageManager.UI
{
    public class BoothPackageManagerWindow : EditorWindow
    {
        [MenuItem("AMU/Booth Package Manager", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<BoothPackageManagerWindow>("Booth Package Manager");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        [Serializable]
        public class BPMFileInfo
        {
            public string fileName;
            public string downloadLink;
        }
        [Serializable]
        public class BPMPackage
        {
            public string packageName;
            public string itemUrl;
            public string imageUrl;
            public List<BPMFileInfo> files;
        }
        [Serializable]
        public class BPMLibrary
        {
            public string lastUpdated;
            public Dictionary<string, List<BPMPackage>> authors;
        }
        private BPMLibrary bpmLibrary;
        private Vector2 scrollPos;
        private bool triedLoad = false;
        private bool isLoading = false;
        private string loadError = null;
        private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
        private DateTime lastJsonWriteTime = DateTime.MinValue;
        private string cachedJsonPath = null; private string GetJsonPath()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "BPM", "BPMlibrary.json");
        }

        private string GetImportJsonPath()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "Import", "BPMlibrary.json");
        }

        private string GetThumbnailDirectory()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "BPM", "thumbnail");
        }

        private string GetImageHash(string url)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private string ExtractItemIdFromUrl(string itemUrl)
        {
            if (string.IsNullOrEmpty(itemUrl)) return "unknown";

            // BoothのURLパターン: https://booth.pm/ja/items/12345 など
            var uri = new Uri(itemUrl);
            var segments = uri.Segments;

            // 最後のセグメントからIDを取得
            if (segments.Length > 0)
            {
                string lastSegment = segments[segments.Length - 1].Trim('/');
                return lastSegment;
            }

            return "unknown";
        }

        private string GetFileDirectory(string author, string itemUrl)
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            string itemId = ExtractItemIdFromUrl(itemUrl);
            return Path.Combine(coreDir, "BPM", "file", author, itemId);
        }

        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"ディレクトリを作成しました: {directoryPath}");
            }
        }

        private async void DownloadFileAsync(string downloadUrl, string fileName, string destinationPath)
        {
            try
            {
                EnsureDirectoryExists(destinationPath);
                string filePath = Path.Combine(destinationPath, fileName);

                Debug.Log($"ファイルをダウンロード中: {fileName}");

                byte[] fileData = await Task.Run(() =>
                {
                    using (var webClient = new System.Net.WebClient())
                    {
                        return webClient.DownloadData(downloadUrl);
                    }
                });

                await Task.Run(() => File.WriteAllBytes(filePath, fileData));

                Debug.Log($"ダウンロード完了: {filePath}");
                EditorUtility.DisplayDialog("ダウンロード完了", $"ファイルをダウンロードしました:\n{filePath}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ダウンロードエラー: {ex.Message}");
                EditorUtility.DisplayDialog("ダウンロードエラー", $"ファイルのダウンロードに失敗しました:\n{ex.Message}", "OK");
            }
        }

        private async Task<bool> CheckAndReplaceWithImportVersionAsync(string mainJsonPath)
        {
            string importJsonPath = GetImportJsonPath();

            if (!File.Exists(importJsonPath))
            {
                return false;
            }

            try
            {
                string importJson = await ReadFileAsync(importJsonPath);
                var importLibrary = await Task.Run(() =>
                {
                    var settings = new JsonSerializerSettings
                    {
                        CheckAdditionalContent = false,
                        DateParseHandling = DateParseHandling.None
                    };
                    return JsonConvert.DeserializeObject<BPMLibrary>(importJson, settings);
                });

                if (File.Exists(mainJsonPath))
                {
                    string mainJson = await ReadFileAsync(mainJsonPath);
                    var mainLibrary = await Task.Run(() =>
                    {
                        var settings = new JsonSerializerSettings
                        {
                            CheckAdditionalContent = false,
                            DateParseHandling = DateParseHandling.None
                        };
                        return JsonConvert.DeserializeObject<BPMLibrary>(mainJson, settings);
                    });

                    if (string.Compare(importLibrary.lastUpdated, mainLibrary.lastUpdated, StringComparison.Ordinal) <= 0)
                    {
                        return false;
                    }
                }

                string bpmDir = Path.GetDirectoryName(mainJsonPath);
                if (!Directory.Exists(bpmDir))
                {
                    Directory.CreateDirectory(bpmDir);
                }
                await Task.Run(() => File.Copy(importJsonPath, mainJsonPath, true));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Importファイルとの比較・置き換えに失敗: {ex.Message}");
                return false;
            }
        }
        private void LoadJsonIfNeeded()
        {
            if (bpmLibrary != null || triedLoad || isLoading) return;

            string currentJsonPath = GetJsonPath();

            CheckAndReplaceWithImportVersionAsync(currentJsonPath).ContinueWith(task =>
            {
                EditorApplication.delayCall += () =>
                {
                    if (!File.Exists(currentJsonPath))
                    {
                        loadError = $"ファイルが見つかりません: {currentJsonPath}";
                        triedLoad = true;
                        return;
                    }

                    DateTime currentWriteTime = File.GetLastWriteTime(currentJsonPath);
                    if (bpmLibrary != null &&
                        currentJsonPath == cachedJsonPath &&
                        currentWriteTime == lastJsonWriteTime)
                    {
                        return;
                    }

                    LoadJsonAsync(currentJsonPath);
                };
            });
        }

        private async void LoadJsonAsync(string jsonPath)
        {
            if (isLoading) return;

            isLoading = true;
            triedLoad = true;
            loadError = null;

            try
            {
                string json = await ReadFileAsync(jsonPath);

                var library = await Task.Run(() =>
                {
                    var settings = new JsonSerializerSettings
                    {
                        CheckAdditionalContent = false,
                        DateParseHandling = DateParseHandling.None
                    };
                    return JsonConvert.DeserializeObject<BPMLibrary>(json, settings);
                });

                EditorApplication.delayCall += () =>
                {
                    bpmLibrary = library;
                    cachedJsonPath = jsonPath;
                    lastJsonWriteTime = File.GetLastWriteTime(jsonPath);
                    isLoading = false;
                    Repaint();
                };
            }
            catch (Exception ex)
            {
                EditorApplication.delayCall += () =>
                {
                    loadError = ex.Message;
                    isLoading = false;
                    Repaint();
                };
            }
        }

        private async Task<string> ReadFileAsync(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private void ReloadData()
        {
            bpmLibrary = null;
            triedLoad = false;
            isLoading = false;
            loadError = null;
            cachedJsonPath = null;
            lastJsonWriteTime = DateTime.MinValue;
            LoadJsonIfNeeded();
        }
        private void OnEnable()
        {
            ReloadData();
        }
        private void OnGUI()
        {
            GUILayout.Label("Booth Package Manager", EditorStyles.boldLabel);
            GUILayout.Space(10); if (loadError != null)
            {
                EditorGUILayout.HelpBox(loadError, MessageType.Error);
                return;
            }

            if (isLoading)
            {
                GUILayout.Label("読み込み中...", EditorStyles.helpBox);
                Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, 0.5f, "JSONファイルを読み込み中...");
                return;
            }
            if (bpmLibrary == null)
            {
                GUILayout.Label("データが読み込まれていません", EditorStyles.helpBox);
                return;
            }
            GUILayout.Label($"最終更新: {bpmLibrary.lastUpdated}");

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var author in bpmLibrary.authors)
            {
                GUILayout.Label(author.Key, EditorStyles.boldLabel);
                foreach (var pkg in author.Value)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal(); if (!string.IsNullOrEmpty(pkg.imageUrl))
                    {
                        var tex = GetCachedImage(pkg.imageUrl);
                        if (tex != null)
                            GUILayout.Label(tex, GUILayout.Width(80), GUILayout.Height(80));
                        else
                        {
                            GUILayout.Label("読み込み中...", GUILayout.Width(80), GUILayout.Height(80));
                            if (!imageCache.ContainsKey(pkg.imageUrl))
                            {
                                LoadImageAsync(pkg.imageUrl);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("No Image", GUILayout.Width(80), GUILayout.Height(80));
                    }
                    GUILayout.BeginVertical();
                    GUILayout.Label(pkg.packageName, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(pkg.itemUrl))
                        if (GUILayout.Button("Boothページを開く", GUILayout.Width(120)))
                            Application.OpenURL(pkg.itemUrl);
                    GUILayout.Label("ファイル:");
                    foreach (var f in pkg.files)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(f.fileName, GUILayout.Width(180));
                        if (!string.IsNullOrEmpty(f.downloadLink))
                        {
                            if (GUILayout.Button("DL", GUILayout.Width(40)))
                            {
                                string fileDir = GetFileDirectory(author.Key, pkg.itemUrl);
                                DownloadFileAsync(f.downloadLink, f.fileName, fileDir);
                            }
                            if (GUILayout.Button("フォルダ", GUILayout.Width(60)))
                            {
                                string fileDir = GetFileDirectory(author.Key, pkg.itemUrl);
                                EnsureDirectoryExists(fileDir);
                                EditorUtility.RevealInFinder(fileDir);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(8);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private Texture2D GetCachedImage(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            return imageCache.TryGetValue(url, out var cached) ? cached : null;
        }

        private async void LoadImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url) || imageCache.ContainsKey(url)) return;

            imageCache[url] = null;

            try
            {
                string thumbnailDir = GetThumbnailDirectory();
                if (!Directory.Exists(thumbnailDir))
                {
                    Directory.CreateDirectory(thumbnailDir);
                }

                string imageHash = GetImageHash(url);
                string[] extensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
                string localImagePath = null;

                foreach (string ext in extensions)
                {
                    string testPath = Path.Combine(thumbnailDir, imageHash + ext);
                    if (File.Exists(testPath))
                    {
                        localImagePath = testPath;
                        break;
                    }
                }

                Texture2D tex = null;

                if (!string.IsNullOrEmpty(localImagePath))
                {
                    byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(localImagePath));
                    tex = new Texture2D(2, 2);
                    if (!tex.LoadImage(fileBytes))
                    {
                        DestroyImmediate(tex);
                        tex = null;
                    }
                }
                else
                {
                    byte[] bytes = await Task.Run(() =>
                    {
                        using (var wc = new System.Net.WebClient())
                        {
                            return wc.DownloadData(url);
                        }
                    });

                    tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        string extension = ".png";
                        string urlLower = url.ToLower();
                        if (urlLower.Contains(".jpg") || urlLower.Contains(".jpeg"))
                            extension = ".jpg";
                        else if (urlLower.Contains(".gif"))
                            extension = ".gif";
                        else if (urlLower.Contains(".bmp"))
                            extension = ".bmp";

                        string saveImagePath = Path.Combine(thumbnailDir, imageHash + extension);
                        await Task.Run(() => File.WriteAllBytes(saveImagePath, bytes));
                    }
                    else
                    {
                        DestroyImmediate(tex);
                        tex = null;
                    }
                }

                EditorApplication.delayCall += () =>
                {
                    imageCache[url] = tex;
                    Repaint();
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"画像の読み込みに失敗: {url}, エラー: {ex.Message}");
                EditorApplication.delayCall += () =>
                {
                    imageCache[url] = null;
                };
            }
        }

        private void OnDisable()
        {
            foreach (var kvp in imageCache)
            {
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value);
                }
            }
            imageCache.Clear();
        }
    }
}
