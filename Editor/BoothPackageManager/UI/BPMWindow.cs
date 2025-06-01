using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.BoothPackageManager.Helper;

namespace AMU.BoothPackageManager.UI
{
    public class BoothPackageManagerWindow : EditorWindow
    {        [MenuItem("AMU/Booth Package Manager", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<BoothPackageManagerWindow>("Booth Package Manager");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private BPMDataManager dataManager;
        private BPMFileManager fileManager;
        private BPMImageManager imageManager;        private Vector2 scrollPos;

        private void InitializeManagers()
        {
            if (dataManager == null)
            {
                dataManager = new BPMDataManager();
                dataManager.OnDataLoaded += OnDataLoaded;
                dataManager.OnLoadError += OnLoadError;
            }

            if (fileManager == null)
            {
                fileManager = new BPMFileManager();
            }

            if (imageManager == null)
            {
                imageManager = new BPMImageManager();
                imageManager.OnImageLoaded += () => Repaint();
            }
        }

        private void OnDataLoaded()
        {
            fileManager.UpdateFileExistenceCache(dataManager.Library);
            imageManager.UpdateImagePathCache();
            Repaint();

            // データベース読み込み完了後にImportフォルダをチェック
            fileManager.CheckAndMoveImportFilesAsync(dataManager);
        }

        private void OnLoadError()
        {
            Repaint();
        }        private void ReloadData()
        {
            dataManager?.ReloadData();
            fileManager?.ClearCaches();
            imageManager?.UpdateImagePathCache();
            dataManager?.LoadJsonIfNeeded();
        }        private void OnEnable()
        {
            InitializeManagers();
            ReloadData();
        }        private void OnGUI()
        {
            GUILayout.Label("Booth Package Manager", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (dataManager.LoadError != null)
            {
                EditorGUILayout.HelpBox(dataManager.LoadError, MessageType.Error);
                return;
            }

            if (dataManager.IsLoading)
            {
                GUILayout.Label("読み込み中...", EditorStyles.helpBox);
                Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, 0.5f, "JSONファイルを読み込み中...");
                return;
            }

            if (dataManager.Library == null)
            {
                GUILayout.Label("データが読み込まれていません", EditorStyles.helpBox);
                return;
            }

            GUILayout.Label($"最終更新: {dataManager.Library.lastUpdated}");

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var author in dataManager.Library.authors)
            {
                GUILayout.Label(author.Key, EditorStyles.boldLabel);
                foreach (var pkg in author.Value)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();

                    if (!string.IsNullOrEmpty(pkg.imageUrl))
                    {
                        var tex = imageManager.GetCachedImage(pkg.imageUrl);
                        if (tex != null)
                            GUILayout.Label(tex, GUILayout.Width(80), GUILayout.Height(80));
                        else
                        {
                            GUILayout.Label("読み込み中...", GUILayout.Width(80), GUILayout.Height(80));
                            imageManager.LoadImageAsync(pkg.imageUrl);
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
                            string fileDir = BPMPathManager.GetFileDirectory(author.Key, pkg.itemUrl);
                            string filePath = Path.Combine(fileDir, f.fileName);
                            if (fileManager.IsFileExistsCached(filePath))
                            {
                                if (GUILayout.Button("フォルダ", GUILayout.Width(60)))
                                {
                                    fileManager.EnsureDirectoryExists(fileDir);
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

        private void OnDisable()
        {
            imageManager?.ClearCaches();
        }
    }
}
