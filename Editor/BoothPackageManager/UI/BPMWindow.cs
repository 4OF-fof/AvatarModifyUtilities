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

        // データ構造
        [Serializable]
        public class BPMFileInfo {
            public string fileName;
            public string downloadLink;
        }
        [Serializable]
        public class BPMPackage {
            public string packageName;
            public string itemUrl;
            public string imageUrl;
            public List<BPMFileInfo> files;
        }
        [Serializable]
        public class BPMLibrary {
            public string lastUpdated;
            public Dictionary<string, List<BPMPackage>> authors;
        }        private BPMLibrary bpmLibrary;
        private Vector2 scrollPos;
        private bool triedLoad = false;
        private bool isLoading = false;
        private string loadError = null;
        private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
        private DateTime lastJsonWriteTime = DateTime.MinValue;
        private string cachedJsonPath = null;private string GetJsonPath()
        {
            // EditorPrefsからCore_dirPathを取得し、BPM/BPMlibrary.jsonを返す
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "BPM", "BPMlibrary.json");
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
        }        private void LoadJsonIfNeeded()
        {
            if (bpmLibrary != null || triedLoad || isLoading) return;
            
            string currentJsonPath = GetJsonPath();
            
            // ファイルの存在確認
            if (!File.Exists(currentJsonPath)) {
                loadError = $"ファイルが見つかりません: {currentJsonPath}";
                triedLoad = true;
                return;
            }
            
            // ファイルが変更されていない場合はスキップ
            DateTime currentWriteTime = File.GetLastWriteTime(currentJsonPath);
            if (bpmLibrary != null && 
                currentJsonPath == cachedJsonPath && 
                currentWriteTime == lastJsonWriteTime) {
                return;
            }
            
            LoadJsonAsync(currentJsonPath);
        }

        private async void LoadJsonAsync(string jsonPath)
        {
            if (isLoading) return;
            
            isLoading = true;
            triedLoad = true;
            loadError = null;
            
            try 
            {
                // 非同期でファイルを読み込み
                string json = await ReadFileAsync(jsonPath);
                
                // JSONのデシリアライズも非同期で実行
                var library = await Task.Run(() => 
                {
                    var settings = new JsonSerializerSettings
                    {
                        // パフォーマンス向上のための設定
                        CheckAdditionalContent = false,
                        DateParseHandling = DateParseHandling.None
                    };
                    return JsonConvert.DeserializeObject<BPMLibrary>(json, settings);
                });
                
                // メインスレッドで結果を設定
                EditorApplication.delayCall += () =>
                {
                    bpmLibrary = library;
                    cachedJsonPath = jsonPath;
                    lastJsonWriteTime = File.GetLastWriteTime(jsonPath);
                    isLoading = false;
                    Repaint(); // UIを更新
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
        }        private void OnEnable()
        {
            // 既存のデータをクリアして再読み込みを強制
            bpmLibrary = null;
            triedLoad = false;
            isLoading = false;
            loadError = null;
            cachedJsonPath = null;
            lastJsonWriteTime = DateTime.MinValue;
            LoadJsonIfNeeded();
        }        private void OnGUI()
        {
            GUILayout.Label("Booth Package Manager", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (loadError != null) {
                EditorGUILayout.HelpBox(loadError, MessageType.Error);
                if (GUILayout.Button("再読み込み"))
                {
                    triedLoad = false;
                    isLoading = false;
                    loadError = null;
                    LoadJsonIfNeeded();
                }
                return;
            }
            
            if (isLoading) {
                GUILayout.Label("読み込み中...", EditorStyles.helpBox);
                // プログレスバーを表示（オプション）
                Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, 0.5f, "JSONファイルを読み込み中...");
                return;
            }
            
            if (bpmLibrary == null) {
                GUILayout.Label("データが読み込まれていません", EditorStyles.helpBox);
                if (GUILayout.Button("読み込み"))
                {
                    LoadJsonIfNeeded();
                }
                return;
            }
              GUILayout.Label($"最終更新: {bpmLibrary.lastUpdated}");
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var author in bpmLibrary.authors) {
                GUILayout.Label(author.Key, EditorStyles.boldLabel);
                foreach (var pkg in author.Value) {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();                    if (!string.IsNullOrEmpty(pkg.imageUrl)) {
                        var tex = GetCachedImage(pkg.imageUrl);
                        if (tex != null)
                            GUILayout.Label(tex, GUILayout.Width(80), GUILayout.Height(80));
                        else
                        {
                            GUILayout.Label("読み込み中...", GUILayout.Width(80), GUILayout.Height(80));
                            // 非同期で画像読み込みを開始
                            if (!imageCache.ContainsKey(pkg.imageUrl))
                            {
                                LoadImageAsync(pkg.imageUrl);
                            }
                        }
                    } else {
                        GUILayout.Label("No Image", GUILayout.Width(80), GUILayout.Height(80));
                    }
                    GUILayout.BeginVertical();
                    GUILayout.Label(pkg.packageName, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(pkg.itemUrl))
                        if (GUILayout.Button("Boothページを開く", GUILayout.Width(120)))
                            Application.OpenURL(pkg.itemUrl);
                    GUILayout.Label("ファイル:");
                    foreach (var f in pkg.files) {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(f.fileName, GUILayout.Width(180));
                        if (!string.IsNullOrEmpty(f.downloadLink))
                            if (GUILayout.Button("DL", GUILayout.Width(40)))
                                Application.OpenURL(f.downloadLink);
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
            
            // プレースホルダーを設定して重複読み込みを防ぐ
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

                // ローカルファイルをチェック
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
                    // ローカルファイルから非同期読み込み
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
                    // ネットワークから非同期ダウンロード
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
                        // ファイルを保存
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
                
                // メインスレッドで結果を設定
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
            // テクスチャのクリーンアップ
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
