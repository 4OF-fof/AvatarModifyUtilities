using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace AMU.BoothPackageManager.Helper
{
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

    public class BPMDataManager
    {
        private BPMLibrary bpmLibrary;
        private bool triedLoad = false;
        private bool isLoading = false;
        private string loadError = null;
        private DateTime lastJsonWriteTime = DateTime.MinValue;
        private string cachedJsonPath = null;

        public BPMLibrary Library => bpmLibrary;
        public bool IsLoading => isLoading;
        public string LoadError => loadError;
        public bool HasTriedLoad => triedLoad;

        public event Action OnDataLoaded;
        public event Action OnLoadError;

        public void LoadJsonIfNeeded()
        {
            if (bpmLibrary != null || triedLoad || isLoading) return;

            string currentJsonPath = BPMPathManager.GetJsonPath();

            CheckAndReplaceWithImportVersionAsync(currentJsonPath).ContinueWith(task =>
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (!File.Exists(currentJsonPath))
                    {
                        loadError = $"ファイルが見つかりません: {currentJsonPath}";
                        triedLoad = true;
                        OnLoadError?.Invoke();
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

                UnityEditor.EditorApplication.delayCall += () =>
                {
                    bpmLibrary = library;
                    cachedJsonPath = jsonPath;
                    lastJsonWriteTime = File.GetLastWriteTime(jsonPath);
                    isLoading = false;
                    OnDataLoaded?.Invoke();
                };
            }
            catch (Exception ex)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    loadError = ex.Message;
                    isLoading = false;
                    OnLoadError?.Invoke();
                };
            }
        }

        private async Task<bool> CheckAndReplaceWithImportVersionAsync(string mainJsonPath)
        {
            var candidateFiles = new List<string>();

            // Importフォルダ内のJSONを候補に追加
            string importJsonPath = BPMPathManager.GetImportJsonPath();
            if (File.Exists(importJsonPath))
            {
                candidateFiles.Add(importJsonPath);
            }

            // Downloadフォルダ検索が有効な場合、DownloadフォルダのJSONも候補に追加
            bool searchDownloadFolder = UnityEditor.EditorPrefs.GetBool("Setting.BPM_searchDownloadFolder", false);
            if (searchDownloadFolder)
            {
                string downloadJsonPath = BPMPathManager.GetDownloadJsonPath();
                if (File.Exists(downloadJsonPath))
                {
                    candidateFiles.Add(downloadJsonPath);
                }
            }

            if (candidateFiles.Count == 0)
            {
                return false;
            }

            try
            {
                // 候補ファイルの中から最新のものを探す
                BPMLibrary newestLibrary = null;
                string newestFilePath = null;
                string newestLastUpdated = null;

                foreach (string candidatePath in candidateFiles)
                {
                    string candidateJson = await ReadFileAsync(candidatePath);
                    var candidateLibrary = await Task.Run(() =>
                    {
                        var settings = new JsonSerializerSettings
                        {
                            CheckAdditionalContent = false,
                            DateParseHandling = DateParseHandling.None
                        };
                        return JsonConvert.DeserializeObject<BPMLibrary>(candidateJson, settings);
                    });

                    if (newestLibrary == null ||
                        string.Compare(candidateLibrary.lastUpdated, newestLastUpdated, StringComparison.Ordinal) > 0)
                    {
                        newestLibrary = candidateLibrary;
                        newestFilePath = candidatePath;
                        newestLastUpdated = candidateLibrary.lastUpdated;
                    }
                }

                // メインファイルと比較
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

                    if (string.Compare(newestLastUpdated, mainLibrary.lastUpdated, StringComparison.Ordinal) <= 0)
                    {
                        return false;
                    }
                }

                // 最新のファイルをメインファイルにコピー
                string bpmDir = Path.GetDirectoryName(mainJsonPath);
                if (!Directory.Exists(bpmDir))
                {
                    Directory.CreateDirectory(bpmDir);
                }
                await Task.Run(() => File.Copy(newestFilePath, mainJsonPath, true));

                string sourceFolder = newestFilePath == BPMPathManager.GetDownloadJsonPath() ? "Downloadフォルダ" : "Importフォルダ";
                Debug.Log($"{sourceFolder}からBPMlibrary.jsonを更新しました: {newestFilePath} -> {mainJsonPath}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"候補ファイルとの比較・置き換えに失敗: {ex.Message}");
                return false;
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

        public (string author, BPMPackage package) FindMatchingFileInDatabase(string fileName)
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

        public void ReloadData()
        {
            bpmLibrary = null;
            triedLoad = false;
            isLoading = false;
            loadError = null;
            cachedJsonPath = null;
            lastJsonWriteTime = DateTime.MinValue;
        }
    }
}
