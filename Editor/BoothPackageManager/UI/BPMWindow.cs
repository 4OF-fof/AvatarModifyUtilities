using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        }

        private BPMLibrary bpmLibrary;
        private Vector2 scrollPos;
        private string jsonPath = "Assets/AvatarModifyUtilities/Editor/Core/BPM/BPMLiblary.json"; // パスは後で調整可
        private bool triedLoad = false;
        private string loadError = null;
        private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();

        private string GetJsonPath()
        {
            // EditorPrefsからCore_dirPathを取得し、BPM/BPMlibrary.jsonを返す
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            return Path.Combine(coreDir, "BPM", "BPMlibrary.json");
        }

        private void LoadJsonIfNeeded()
        {
            if (bpmLibrary != null || triedLoad) return;
            triedLoad = true;
            try {
                jsonPath = GetJsonPath();
                if (!File.Exists(jsonPath)) {
                    loadError = $"ファイルが見つかりません: {jsonPath}";
                    return;
                }
                var json = File.ReadAllText(jsonPath);
                bpmLibrary = JsonConvert.DeserializeObject<BPMLibrary>(json);
            } catch (Exception ex) {
                loadError = ex.Message;
            }
        }

        private void OnEnable()
        {
            // ウィンドウが開かれたときのみロード
            bpmLibrary = null;
            triedLoad = false;
            loadError = null;
            LoadJsonIfNeeded();
        }

        private void OnGUI()
        {
            GUILayout.Label("Booth Package Manager", EditorStyles.boldLabel);
            GUILayout.Space(10);
            // LoadJsonIfNeeded(); ←ここは削除（OnEnableでのみロード）
            if (loadError != null) {
                EditorGUILayout.HelpBox(loadError, MessageType.Error);
                return;
            }
            if (bpmLibrary == null) {
                GUILayout.Label("読み込み中...", EditorStyles.helpBox);
                return;
            }
            GUILayout.Label($"最終更新: {bpmLibrary.lastUpdated}");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var author in bpmLibrary.authors) {
                GUILayout.Label(author.Key, EditorStyles.boldLabel);
                foreach (var pkg in author.Value) {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();
                    if (!string.IsNullOrEmpty(pkg.imageUrl)) {
                        var tex = LoadImageFromUrl(pkg.imageUrl);
                        if (tex != null)
                            GUILayout.Label(tex, GUILayout.Width(80), GUILayout.Height(80));
                        else
                            GUILayout.Label("No Image", GUILayout.Width(80), GUILayout.Height(80));
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

        // 画像URLからテクスチャをロード（キャッシュ対応）
        private Texture2D LoadImageFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (imageCache.TryGetValue(url, out var cached)) return cached;
            try {
                using (var wc = new System.Net.WebClient()) {
                    var bytes = wc.DownloadData(url);
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    imageCache[url] = tex;
                    return tex;
                }
            } catch {
                imageCache[url] = null;
                return null;
            }
        }
    }
}
