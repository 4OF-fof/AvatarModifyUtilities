using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using AMU.AssetManager.Data;

namespace AMU.AssetManager.Helper
{
    public class AssetDataManager : IDisposable
    {
        private AssetLibrary _assetLibrary;
        private string _dataFilePath;
        private bool _isLoading = false;
        private DateTime _lastFileModified = DateTime.MinValue;
        private string _lastFileHash = "";

        // ファイルアクセス排他制御
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly object _saveLock = new object();
        private bool _isSaving = false;

        // キャッシュとインデックス
        private Dictionary<string, List<AssetInfo>> _searchCache = new Dictionary<string, List<AssetInfo>>();
        private Dictionary<string, List<AssetInfo>> _typeCache = new Dictionary<string, List<AssetInfo>>();
        private Dictionary<string, AssetInfo> _assetByIdIndex = new Dictionary<string, AssetInfo>();
        private List<string> _searchableText = new List<string>();
        private bool _indexNeedsUpdate = true;

        public AssetLibrary Library => _assetLibrary;
        public bool IsLoading => _isLoading;

        public event Action OnDataLoaded;
        public event Action OnDataChanged;

        public AssetDataManager()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            _dataFilePath = Path.Combine(coreDir, "AssetManager", "AssetLibrary.json");
        }
        public void LoadData()
        {
            if (_isLoading) return;

            _isLoading = true;

            // 非同期でデータを読み込み
            LoadDataAsync();
        }
        private async void LoadDataAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    _assetLibrary = new AssetLibrary();
                    await SaveDataAsync();
                }
                else
                {
                    // ファイルを非同期で読み込み
                    string json = await ReadFileAsync(_dataFilePath);

                    // JSONのデシリアライズを別スレッドで実行
                    _assetLibrary = await Task.Run(() =>
                        JsonConvert.DeserializeObject<AssetLibrary>(json) ?? new AssetLibrary());

                    UpdateFileTrackingInfo();
                    UpdateIndexes();
                }

                _isLoading = false;

                // メインスレッドでイベントを実行
                EditorApplication.delayCall += () => OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Failed to load data: {ex.Message}");
                _assetLibrary = new AssetLibrary();
                _isLoading = false;
                EditorApplication.delayCall += () => OnDataLoaded?.Invoke();
            }
            finally
            {
                _fileLock.Release();
            }
        }
        private async Task<string> ReadFileAsync(string filePath)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 100;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // 共有読み取りアクセスを許可してファイルを開く
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    // ファイルが使用中の場合は少し待ってからリトライ
                    Debug.LogWarning($"[AssetDataManager] File access retry {i + 1}/{maxRetries}: {ex.Message}");
                    await Task.Delay(retryDelayMs);
                }
            }

            // 最後の試行
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// JSONファイルが外部で変更されていないかチェックし、必要に応じて再読み込みを行う
        /// </summary>
        public bool CheckForExternalChanges()
        {
            if (!File.Exists(_dataFilePath)) return false;

            // 既に読み込み中または保存中の場合はスキップ
            if (_isLoading || _isSaving) return false;

            try
            {
                var fileInfo = new FileInfo(_dataFilePath);
                var currentModified = fileInfo.LastWriteTime;

                // ファイルの更新時刻が変わっていない場合はスキップ
                if (currentModified <= _lastFileModified) return false;

                // ファイルサイズをまずチェック（軽量）
                long currentSize = fileInfo.Length;

                // ハッシュ計算の頻度を減らすため、サイズが変わった場合のみ実行
                string currentHash = ComputeFileHashOptimized(_dataFilePath, currentSize);
                if (currentHash != _lastFileHash)
                {
                    // ファイルが変更されている場合は再読み込み
                    LoadData();
                    return true;
                }
            }
            catch (IOException ex)
            {
                // ファイルアクセスエラーは警告レベルで出力
                Debug.LogWarning($"[AssetDataManager] File access error during change check: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Failed to check file changes: {ex.Message}");
            }

            return false;
        }        /// <summary>
                 /// 最適化されたファイルハッシュ計算（一部のみ）
                 /// </summary>
        private string ComputeFileHashOptimized(string filePath, long fileSize)
        {
            try
            {
                // 小さなファイルの場合は全体をハッシュ
                if (fileSize < 1024 * 1024) // 1MB未満
                {
                    return ComputeFileHash(filePath);
                }

                // 大きなファイルの場合は先頭と末尾のみをハッシュ
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] buffer = new byte[8192]; // 8KB

                        // 先頭を読み込み
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            sha256.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                        }

                        // 末尾を読み込み
                        if (stream.Length > buffer.Length)
                        {
                            stream.Seek(-buffer.Length, SeekOrigin.End);
                            bytesRead = stream.Read(buffer, 0, buffer.Length); if (bytesRead > 0)
                            {
                                sha256.TransformFinalBlock(buffer, 0, bytesRead);
                            }
                        }

                        byte[] hash = sha256.Hash;
                        return Convert.ToBase64String(hash);
                    }
                }
            }
            catch (IOException)
            {
                // ファイルアクセスエラーの場合はフォールバックを試行
                return ComputeFileHash(filePath);
            }
            catch
            {
                return ComputeFileHash(filePath); // フォールバック
            }
        }
        public void SaveData()
        {
            lock (_saveLock)
            {
                if (_isSaving) return; // 既に保存中の場合はスキップ
                _isSaving = true;
            }

            _ = SaveDataAsync(); // 非同期メソッドを明示的に無視
        }

        private async Task SaveDataAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                EnsureDirectoryExists();
                _assetLibrary.lastUpdated = DateTime.Now;

                // JSONシリアライズを別スレッドで実行
                string json = await Task.Run(() =>
                    JsonConvert.SerializeObject(_assetLibrary, Formatting.Indented));

                // 一時ファイルに書き込んでから原子的に置き換え
                string tempFilePath = _dataFilePath + ".tmp";

                const int maxRetries = 3;
                const int retryDelayMs = 100;

                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        // 一時ファイルに書き込み
                        using (var writer = new StreamWriter(tempFilePath))
                        {
                            await writer.WriteAsync(json);
                        }

                        // 原子的にファイルを置き換え
                        if (File.Exists(_dataFilePath))
                        {
                            File.Replace(tempFilePath, _dataFilePath, null);
                        }
                        else
                        {
                            File.Move(tempFilePath, _dataFilePath);
                        }

                        break; // 成功した場合はループを抜ける
                    }
                    catch (IOException ex) when (i < maxRetries - 1)
                    {
                        Debug.LogWarning($"[AssetDataManager] Save retry {i + 1}/{maxRetries}: {ex.Message}");
                        await Task.Delay(retryDelayMs);

                        // 一時ファイルが残っている場合は削除
                        if (File.Exists(tempFilePath))
                        {
                            try { File.Delete(tempFilePath); } catch { }
                        }
                    }
                }

                UpdateFileTrackingInfo();
                UpdateIndexes();

                EditorApplication.delayCall += () => OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Failed to save data: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
                lock (_saveLock)
                {
                    _isSaving = false;
                }
            }
        }
        public void AddAsset(AssetInfo asset)
        {
            if (_assetLibrary?.assets == null)
                _assetLibrary = new AssetLibrary();

            _assetLibrary.assets.Add(asset);
            InvalidateCache();
            SaveData();
        }

        public void UpdateAsset(AssetInfo asset)
        {
            if (_assetLibrary?.assets == null) return;

            var existingAsset = _assetLibrary.assets.FirstOrDefault(a => a.uid == asset.uid);
            if (existingAsset != null)
            {
                var index = _assetLibrary.assets.IndexOf(existingAsset);
                _assetLibrary.assets[index] = asset;
                InvalidateCache();
                SaveData();
            }
        }

        public void RemoveAsset(string uid)
        {
            if (_assetLibrary?.assets == null) return;

            var asset = _assetLibrary.assets.FirstOrDefault(a => a.uid == uid);
            if (asset != null)
            {
                _assetLibrary.assets.Remove(asset);
                InvalidateCache();
                SaveData();
            }
        }
        public AssetInfo GetAsset(string uid)
        {
            if (_indexNeedsUpdate)
                UpdateIndexes();

            return _assetByIdIndex.TryGetValue(uid, out var asset) ? asset : null;
        }

        public AssetInfo GetAssetByName(string name)
        {
            return GetAllAssets().FirstOrDefault(a => a.name == name);
        }
        public List<AssetInfo> GetAllAssets()
        {
            return _assetLibrary?.assets ?? new List<AssetInfo>();
        }
        public List<AssetInfo> SearchAssets(string searchText, string filterType = null, bool? favoritesOnly = null, bool showHidden = false, bool? archivedOnly = null)
        {
            // インデックスの更新
            if (_indexNeedsUpdate)
                UpdateIndexes();

            // キャッシュキーを生成
            var cacheKey = $"{searchText}|{filterType}|{favoritesOnly}|{showHidden}|{archivedOnly}";

            // キャッシュから検索
            if (_searchCache.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            // 外部ファイル変更をチェック（頻度を削減）
            CheckForExternalChanges();

            var assets = GetAllAssets();

            // タイプフィルターを先に適用（最も効果的なフィルター）
            if (!string.IsNullOrEmpty(filterType))
            {
                if (_typeCache.TryGetValue(filterType, out var typeFiltered))
                {
                    assets = typeFiltered;
                }
                else
                {
                    assets = assets.Where(a => a.assetType == filterType).ToList();
                    _typeCache[filterType] = assets;
                }
            }

            // アーカイブフィルターを適用
            if (archivedOnly.HasValue && archivedOnly.Value)
            {
                assets = assets.Where(a => a.isHidden).ToList();
            }
            else if (!showHidden)
            {
                assets = assets.Where(a => !a.isHidden).ToList();
            }

            // お気に入りフィルターを適用
            if (favoritesOnly.HasValue && favoritesOnly.Value)
            {
                assets = assets.Where(a => a.isFavorite).ToList();
            }

            // テキスト検索を最後に適用（最も重い処理）
            if (!string.IsNullOrEmpty(searchText))
            {
                var searchLower = searchText.ToLower();
                assets = assets.Where(a =>
                    a.name.ToLower().Contains(searchLower) ||
                    a.description.ToLower().Contains(searchLower) ||
                    a.authorName.ToLower().Contains(searchLower) ||
                    a.tags.Any(tag => tag.ToLower().Contains(searchLower))
                ).ToList();
            }

            // 結果をキャッシュ（最大100件まで）
            if (_searchCache.Count >= 100)
            {
                var oldestKey = _searchCache.Keys.First();
                _searchCache.Remove(oldestKey);
            }
            _searchCache[cacheKey] = assets;

            return assets;
        }

        /// <summary>
        /// インデックスを更新してパフォーマンスを向上させる
        /// </summary>
        private void UpdateIndexes()
        {
            if (_assetLibrary?.assets == null) return;

            _assetByIdIndex.Clear();
            _typeCache.Clear();

            var typeGroups = new Dictionary<string, List<AssetInfo>>();

            foreach (var asset in _assetLibrary.assets)
            {
                // ID インデックス
                _assetByIdIndex[asset.uid] = asset;

                // タイプ別インデックス
                if (!typeGroups.ContainsKey(asset.assetType))
                {
                    typeGroups[asset.assetType] = new List<AssetInfo>();
                }
                typeGroups[asset.assetType].Add(asset);
            }

            // タイプキャッシュを更新
            foreach (var kvp in typeGroups)
            {
                _typeCache[kvp.Key] = kvp.Value;
            }

            _indexNeedsUpdate = false;
        }

        /// <summary>
        /// キャッシュを無効化する
        /// </summary>
        private void InvalidateCache()
        {
            _searchCache.Clear();
            _typeCache.Clear();
            _indexNeedsUpdate = true;
        }

        private void UpdateFileTrackingInfo()
        {
            if (File.Exists(_dataFilePath))
            {
                var fileInfo = new FileInfo(_dataFilePath);
                _lastFileModified = fileInfo.LastWriteTime;
                _lastFileHash = ComputeFileHash(_dataFilePath);
            }
        }
        private string ComputeFileHash(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] hash = sha256.ComputeHash(stream);
                        return Convert.ToBase64String(hash);
                    }
                }
            }
            catch (IOException)
            {
                // ファイルアクセスエラーの場合は空文字列を返す
                return "";
            }
            catch
            {
                return "";
            }
        }


        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Dispose()
        {
            _fileLock?.Dispose();
        }
    }
}
