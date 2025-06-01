using System.IO;
using UnityEditor;
using UnityEngine;
using AMU.BoothPackageManager.Helper;
using AMU.Data.Lang;

namespace AMU.BoothPackageManager.UI
{
    public class BoothPackageManagerWindow : EditorWindow
    {
        [MenuItem("AMU/Booth Package Manager", priority = 0)]
        public static void ShowWindow()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            var window = GetWindow<BoothPackageManagerWindow>(LocalizationManager.GetText("BPM_windowTitle"));
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private BPMDataManager dataManager;
        private BPMFileManager fileManager;
        private BPMImageManager imageManager; private Vector2 scrollPos;

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

            _ = fileManager.CheckAndMoveImportFilesAsync(dataManager);
        }

        private void OnLoadError()
        {
            Repaint();
        }
        private void ReloadData()
        {
            dataManager?.ReloadData();
            fileManager?.ClearCaches();
            imageManager?.UpdateImagePathCache();
            dataManager?.LoadJsonIfNeeded();
        }
        private void OnEnable()
        {
            var language = EditorPrefs.GetString("Setting.Core_language", "ja_jp");
            LocalizationManager.LoadLanguage(language);

            InitializeManagers();
            ReloadData();
        }
        private void OnGUI()
        {
            GUILayout.Label(LocalizationManager.GetText("BPM_windowTitle"), EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button(LocalizationManager.GetText("BPM_reloadButton"), GUILayout.Width(100)))
            {
                ReloadData();
            }
            GUILayout.Space(5);

            if (dataManager.LoadError != null)
            {
                EditorGUILayout.HelpBox(dataManager.LoadError, MessageType.Error);
                return;
            }

            if (dataManager.IsLoading)
            {
                GUILayout.Label(LocalizationManager.GetText("BPM_loading"), EditorStyles.helpBox);
                Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, 0.5f, LocalizationManager.GetText("BPM_loadingJson"));
                return;
            }

            if (dataManager.Library == null)
            {
                GUILayout.Label(LocalizationManager.GetText("BPM_noDataLoaded"), EditorStyles.helpBox);
                return;
            }

            GUILayout.Label($"{LocalizationManager.GetText("BPM_lastUpdated")} {dataManager.Library.lastUpdated}");

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var author in dataManager.Library.authors)
            {
                GUILayout.Label(author.Key, EditorStyles.boldLabel);
                foreach (var pkg in author.Value)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            if (!string.IsNullOrEmpty(pkg.imageUrl))
                            {
                                var tex = imageManager.GetCachedImage(pkg.imageUrl);
                                if (tex != null)
                                    GUILayout.Label(tex, GUILayout.Width(80), GUILayout.Height(80));
                                else
                                {
                                    GUILayout.Label(LocalizationManager.GetText("BPM_loading"), GUILayout.Width(80), GUILayout.Height(80));
                                    imageManager.LoadImageAsync(pkg.imageUrl);
                                }
                            }
                            else
                            {
                                GUILayout.Label(LocalizationManager.GetText("BPM_noImage"), GUILayout.Width(80), GUILayout.Height(80));
                            }

                            using (new GUILayout.VerticalScope())
                            {
                                GUILayout.Label(pkg.packageName, EditorStyles.boldLabel);
                                if (!string.IsNullOrEmpty(pkg.itemUrl))
                                    if (GUILayout.Button(LocalizationManager.GetText("BPM_openBoothPage"), GUILayout.Width(120)))
                                        Application.OpenURL(pkg.itemUrl);

                                GUILayout.Label($"{LocalizationManager.GetText("BPM_files")}");
                                foreach (var f in pkg.files)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label(f.fileName, GUILayout.Width(180));
                                        if (!string.IsNullOrEmpty(f.downloadLink))
                                        {
                                            string fileDir = BPMPathManager.GetFileDirectory(author.Key, pkg.itemUrl);
                                            string filePath = Path.Combine(fileDir, f.fileName);
                                            if (fileManager.IsFileExistsCached(filePath))
                                            {
                                                if (GUILayout.Button(LocalizationManager.GetText("BPM_openFile"), GUILayout.Width(60)))
                                                {
                                                    fileManager.EnsureDirectoryExists(fileDir);
                                                    EditorUtility.RevealInFinder(filePath);
                                                }
                                            }
                                            else
                                            {
                                                if (GUILayout.Button(LocalizationManager.GetText("BPM_download"), GUILayout.Width(80)))
                                                {
                                                    Application.OpenURL(f.downloadLink);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
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
