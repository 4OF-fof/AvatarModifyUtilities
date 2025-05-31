using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        private string loadError = null; private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
        private DateTime lastJsonWriteTime = DateTime.MinValue;
        private string cachedJsonPath = null; private Dictionary<string, bool> fileExistenceCache = new Dictionary<string, bool>();
        private Dictionary<string, string> imagePathCache = new Dictionary<string, string>();
        private bool thumbnailDirectoryChecked = false;
        private HashSet<string> ensuredDirectories = new HashSet<string>(); private string GetJsonPath()
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

        private string GetImportDirectory()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "Import");
        }

        private string GetImageHash(string url)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private void UpdateImagePathCache()
        {
            imagePathCache.Clear();

            string thumbnailDir = GetThumbnailDirectory();
            if (!Directory.Exists(thumbnailDir)) return;

            try
            {
                var allFiles = Directory.GetFiles(thumbnailDir, "*", SearchOption.TopDirectoryOnly);
                string[] extensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

                foreach (var filePath in allFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string extension = Path.GetExtension(filePath);

                    if (extensions.Contains(extension.ToLower()))
                    {
                        imagePathCache[fileName] = filePath;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"画像パスキャッシュの更新に失敗: {ex.Message}");
            }
        }

        private string GetCachedImagePath(string imageHash)
        {
            return imagePathCache.TryGetValue(imageHash, out string path) ? path : null;
        }

        private void EnsureThumbnailDirectory()
        {
            if (thumbnailDirectoryChecked) return;

            string thumbnailDir = GetThumbnailDirectory();
            if (!Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }
            thumbnailDirectoryChecked = true;
        }

        private bool IsFileExistsCached(string filePath)
        {
            if (fileExistenceCache.TryGetValue(filePath, out bool exists))
            {
                return exists;
            }

            bool fileExists = File.Exists(filePath);
            fileExistenceCache[filePath] = fileExists;
            return fileExists;
        }

        private void UpdateFileExistenceCache()
        {
            fileExistenceCache.Clear();

            if (bpmLibrary?.authors == null) return;

            foreach (var authorKvp in bpmLibrary.authors)
            {
                foreach (var package in authorKvp.Value)
                {
                    if (package.files != null)
                    {
                        string fileDir = GetFileDirectory(authorKvp.Key, package.itemUrl);

                        foreach (var file in package.files)
                        {
                            string filePath = Path.Combine(fileDir, file.fileName);
                            fileExistenceCache[filePath] = File.Exists(filePath);
                        }
                    }
                }
            }
        }

        private void UpdateSingleFileExistenceCache(string filePath)
        {
            fileExistenceCache[filePath] = File.Exists(filePath);
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
            if (ensuredDirectories.Contains(directoryPath)) return;

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"ディレクトリを作成しました: {directoryPath}");
            }
            ensuredDirectories.Add(directoryPath);
        }

        private async Task CheckAndMoveImportFilesAsync()
        {
            if (bpmLibrary == null) return;

            string importDir = GetImportDirectory();
            if (!Directory.Exists(importDir)) return;

            var allFiles = Directory.GetFiles(importDir, "*", SearchOption.AllDirectories);

            foreach (var filePath in allFiles)
            {
                string fileName = Path.GetFileName(filePath);

                // BPMlibrary.jsonファイルはスキップ
                if (fileName.Equals("BPMlibrary.json", StringComparison.OrdinalIgnoreCase))
                    continue;                // データベース内でファイル名が一致するものを探す
                var matchedFile = FindMatchingFileInDatabase(fileName);
                if (matchedFile.author != null && matchedFile.package != null)
                {
                    try
                    {
                        string targetDir = GetFileDirectory(matchedFile.author, matchedFile.package.itemUrl);
                        EnsureDirectoryExists(targetDir);

                        string targetPath = Path.Combine(targetDir, fileName);

                        // ファイルが既に存在する場合はスキップ
                        if (File.Exists(targetPath))
                        {
                            Debug.Log($"ファイルは既に存在するためスキップしました: {targetPath}");
                            continue;
                        }                        // ファイルを移動
                        File.Move(filePath, targetPath);
                        Debug.Log($"ファイルを移動しました: {fileName} -> {targetPath}");

                        // キャッシュを更新
                        UpdateSingleFileExistenceCache(targetPath);

                        EditorUtility.DisplayDialog("ファイル移動完了",
                            $"Importフォルダからファイルを移動しました:\n{fileName}\n↓\n{targetPath}", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"ファイル移動エラー: {fileName}, エラー: {ex.Message}");
                    }
                }
            }
        }

        private (string author, BPMPackage package) FindMatchingFileInDatabase(string fileName)
        {
            if (bpmLibrary?.authors == null) return (null, null);

            foreach (var authorKvp in bpmLibrary.authors)
            {
                foreach (var package in authorKvp.Value)
                {
                    if (package.files != null)
                    {
                        foreach (var file in package.files)
                        {
                            if (string.Equals(file.fileName, fileName, StringComparison.OrdinalIgnoreCase))
                            {
                                return (authorKvp.Key, package);
                            }
                        }
                    }
                }
            }
            return (null, null);
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
                }); EditorApplication.delayCall += () =>
                {
                    bpmLibrary = library;
                    cachedJsonPath = jsonPath;
                    lastJsonWriteTime = File.GetLastWriteTime(jsonPath);
                    isLoading = false;
                    // ファイル存在キャッシュを更新
                    UpdateFileExistenceCache();

                    // 画像パスキャッシュを更新
                    UpdateImagePathCache();

                    Repaint();

                    // データベース読み込み完了後にImportフォルダをチェック
                    CheckAndMoveImportFilesAsync();
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
            fileExistenceCache.Clear();
            imagePathCache.Clear();
            thumbnailDirectoryChecked = false;
            ensuredDirectories.Clear();
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
                            string fileDir = GetFileDirectory(author.Key, pkg.itemUrl);
                            string filePath = Path.Combine(fileDir, f.fileName);
                            if (IsFileExistsCached(filePath))
                            {
                                if (GUILayout.Button("フォルダ", GUILayout.Width(60)))
                                {
                                    EnsureDirectoryExists(fileDir);
                                    EditorUtility.RevealInFinder(filePath);
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("DL", GUILayout.Width(40)))
                                {
                                    Application.OpenURL(f.downloadLink);
                                }
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
                EnsureThumbnailDirectory();

                string imageHash = GetImageHash(url);
                string localImagePath = GetCachedImagePath(imageHash);

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
                    using (var httpClient = new HttpClient())
                    {
                        byte[] bytes = await httpClient.GetByteArrayAsync(url);

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
                                extension = ".bmp"; string thumbnailDir = GetThumbnailDirectory();
                            string saveImagePath = Path.Combine(thumbnailDir, imageHash + extension);
                            await File.WriteAllBytesAsync(saveImagePath, bytes);

                            // 画像パスキャッシュを更新
                            imagePathCache[imageHash] = saveImagePath;
                        }
                        else
                        {
                            DestroyImmediate(tex);
                            tex = null;
                        }
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
