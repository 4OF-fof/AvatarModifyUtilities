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
    /// <summary>
    /// 改善されたアセットデータマネージャー
    /// - シングルトンパターンでデータを共有
    /// - 明示的なリフレッシュのみサポート
    /// - 高速化されたキャッシュシステム
    /// </summary>
    public class AssetDataManager : IDisposable
    {
        private static AssetDataManager _instance;
        private static readonly object _lockObject = new object();

        private AssetLibrary _assetLibrary;
        private string _dataFilePath;
        private bool _isLoading = false;
        private DateTime _lastFileModified = DateTime.MinValue;

        // ファイルアクセス排他制御
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly object _saveLock = new object();
        private bool _isSaving = false;

        // 高速化されたインデックスシステム
        private Dictionary<string, AssetInfo> _assetByIdIndex = new Dictionary<string, AssetInfo>();
        private Dictionary<string, List<AssetInfo>> _assetsByTypeIndex = new Dictionary<string, List<AssetInfo>>();
        private Dictionary<string, List<AssetInfo>> _favoriteAssetsIndex = new Dictionary<string, List<AssetInfo>>();
        private Dictionary<string, List<AssetInfo>> _hiddenAssetsIndex = new Dictionary<string, List<AssetInfo>>();
        private bool _indexNeedsUpdate = true;

        // 検索キャッシュ（LRUキャッシュとして実装）
        private readonly Dictionary<int, SearchResult> _searchCache = new Dictionary<int, SearchResult>();
        private readonly Queue<int> _searchCacheKeys = new Queue<int>();
        private const int MaxCacheSize = 50;

        public AssetLibrary Library => _assetLibrary;
        public bool IsLoading => _isLoading;

        public event Action OnDataLoaded;
        public event Action OnDataChanged;

        /// <summary>
        /// シングルトンインスタンスを取得
        /// </summary>
        public static AssetDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new AssetDataManager();
                        }
                    }
                }
                return _instance;
            }
        }
        private AssetDataManager()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            _dataFilePath = Path.Combine(coreDir, "AssetManager", "AssetLibrary.json");

            // 初期化時にディレクトリを確保
            EnsureDirectoryExists();
        }

        /// <summary>
        /// インスタンスを初期化（初回のみデータロード）
        /// </summary>
        public void Initialize()
        {
            // まず同期的にファイルの存在確認と作成を行う
            EnsureLibraryFileExists();

            if (_assetLibrary == null && !_isLoading)
            {
                LoadData();
            }
        }

        /// <summary>
        /// AssetLibrary.jsonファイルが存在することを保証（同期処理）
        /// </summary>
        public void EnsureLibraryFileExists()
        {
            EnsureDirectoryExists();

            if (!File.Exists(_dataFilePath))
            {
                Debug.Log($"[AssetDataManager] AssetLibrary.json not found. Creating new file at: {_dataFilePath}");

                try
                {
                    var defaultLibrary = CreateDefaultAssetLibrary();
                    string json = JsonConvert.SerializeObject(defaultLibrary, Formatting.Indented);
                    File.WriteAllText(_dataFilePath, json);

                    Debug.Log($"[AssetDataManager] Successfully created new AssetLibrary.json");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AssetDataManager] Failed to create AssetLibrary.json: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// データをロード（初回または明示的なリフレッシュ時のみ）
        /// </summary>
        public void LoadData()
        {
            if (_isLoading) return;

            _isLoading = true;

            // 非同期でデータを読み込み
            LoadDataAsync();
        }

        /// <summary>
        /// データを強制的にリフレッシュ（ユーザーが更新ボタンを押した時など）
        /// </summary>
        public void ForceRefresh()
        {
            _isLoading = true;
            InvalidateCache();
            LoadDataAsync();
        }
        private async void LoadDataAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                // ディレクトリが存在しない場合は作成
                EnsureDirectoryExists();

                if (!File.Exists(_dataFilePath))
                {
                    // AssetLibrary.jsonが存在しない場合、デフォルトの空のライブラリを作成
                    Debug.Log($"[AssetDataManager] AssetLibrary.json not found. Creating new file at: {_dataFilePath}");
                    _assetLibrary = CreateDefaultAssetLibrary();
                    await SaveDataAsync();
                    Debug.Log($"[AssetDataManager] Successfully created new AssetLibrary.json");
                }
                else
                {
                    // ファイルを非同期で読み込み
                    string json = await ReadFileAsync(_dataFilePath);

                    // JSONのデシリアライズを別スレッドで実行
                    _assetLibrary = await Task.Run(() =>
                        JsonConvert.DeserializeObject<AssetLibrary>(json) ?? CreateDefaultAssetLibrary());

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
                Debug.Log($"[AssetDataManager] Creating default AssetLibrary due to error");
                _assetLibrary = CreateDefaultAssetLibrary();
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
        }        /// <summary>
                 /// 外部からの変更チェック機能を削除
                 /// データの変更は明示的なリフレッシュまたは内部操作でのみ行う
                 /// </summary>
        public bool CheckForExternalChanges()
        {
            // 明示的なリフレッシュのみサポートするため、常にfalseを返す
            return false;
        }

        /// <summary>
        /// 検索結果のキャッシュエントリ
        /// </summary>
        private class SearchResult
        {
            public List<AssetInfo> Assets { get; set; }
            public DateTime CreatedAt { get; set; }
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
            {
                Debug.Log("[AssetDataManager] AssetLibrary not initialized. Creating new instance.");
                _assetLibrary = CreateDefaultAssetLibrary();
            }

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
        }        /// <summary>
                 /// 高速化された検索機能
                 /// </summary>
        public List<AssetInfo> SearchAssets(string searchText, string filterType = null, bool? favoritesOnly = null, bool showHidden = false, bool? archivedOnly = null)
        {
            // インデックスの更新
            if (_indexNeedsUpdate)
                UpdateIndexes();

            // 検索パラメータのハッシュを計算（高速化）
            int searchHash = ComputeSearchHash(searchText, filterType, favoritesOnly, showHidden, archivedOnly);

            // キャッシュから検索
            if (_searchCache.TryGetValue(searchHash, out var cachedResult))
            {
                // キャッシュの有効期限チェック（5分）
                if (DateTime.Now - cachedResult.CreatedAt < TimeSpan.FromMinutes(5))
                {
                    return cachedResult.Assets;
                }
                else
                {
                    // 期限切れのキャッシュを削除
                    _searchCache.Remove(searchHash);
                    _searchCacheKeys.Dequeue();
                }
            }

            var assets = GetFilteredAssets(filterType, favoritesOnly, showHidden, archivedOnly);

            // テキスト検索（最も重い処理を最後に）
            if (!string.IsNullOrEmpty(searchText))
            {
                assets = ApplyTextSearch(assets, searchText);
            }

            // 結果をキャッシュ（LRU方式）
            CacheSearchResult(searchHash, assets);

            return assets;
        }

        /// <summary>
        /// フィルタを適用してアセットを取得
        /// </summary>
        private List<AssetInfo> GetFilteredAssets(string filterType, bool? favoritesOnly, bool showHidden, bool? archivedOnly)
        {
            List<AssetInfo> assets;

            // タイプフィルターを最初に適用（最も効果的）
            if (!string.IsNullOrEmpty(filterType))
            {
                assets = _assetsByTypeIndex.TryGetValue(filterType, out var typeFiltered) ?
                    new List<AssetInfo>(typeFiltered) : new List<AssetInfo>();
            }
            else
            {
                assets = GetAllAssets();
            }

            // お気に入りフィルター
            if (favoritesOnly.HasValue && favoritesOnly.Value)
            {
                assets = assets.Where(a => a.isFavorite).ToList();
            }

            // アーカイブフィルター
            if (archivedOnly.HasValue && archivedOnly.Value)
            {
                assets = assets.Where(a => a.isHidden).ToList();
            }
            else if (!showHidden)
            {
                assets = assets.Where(a => !a.isHidden).ToList();
            }

            return assets;
        }

        /// <summary>
        /// テキスト検索を適用
        /// </summary>
        private List<AssetInfo> ApplyTextSearch(List<AssetInfo> assets, string searchText)
        {
            var searchLower = searchText.ToLower();
            return assets.Where(a =>
                a.name.ToLower().Contains(searchLower) ||
                a.description.ToLower().Contains(searchLower) ||
                a.authorName.ToLower().Contains(searchLower) ||
                a.tags.Any(tag => tag.ToLower().Contains(searchLower))
            ).ToList();
        }

        /// <summary>
        /// 検索パラメータのハッシュを計算
        /// </summary>
        private int ComputeSearchHash(string searchText, string filterType, bool? favoritesOnly, bool showHidden, bool? archivedOnly)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (searchText?.GetHashCode() ?? 0);
                hash = hash * 23 + (filterType?.GetHashCode() ?? 0);
                hash = hash * 23 + (favoritesOnly?.GetHashCode() ?? 0);
                hash = hash * 23 + showHidden.GetHashCode();
                hash = hash * 23 + (archivedOnly?.GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <summary>
        /// 検索結果をキャッシュ（LRU方式）
        /// </summary>
        private void CacheSearchResult(int searchHash, List<AssetInfo> assets)
        {
            // キャッシュサイズ制限
            while (_searchCache.Count >= MaxCacheSize)
            {
                var oldestKey = _searchCacheKeys.Dequeue();
                _searchCache.Remove(oldestKey);
            }

            _searchCache[searchHash] = new SearchResult
            {
                Assets = assets,
                CreatedAt = DateTime.Now
            };
            _searchCacheKeys.Enqueue(searchHash);
        }        /// <summary>
                 /// 高速化されたインデックス更新
                 /// </summary>
        private void UpdateIndexes()
        {
            if (_assetLibrary?.assets == null) return;

            _assetByIdIndex.Clear();
            _assetsByTypeIndex.Clear();
            _favoriteAssetsIndex.Clear();
            _hiddenAssetsIndex.Clear();

            foreach (var asset in _assetLibrary.assets)
            {
                // ID インデックス
                _assetByIdIndex[asset.uid] = asset;

                // タイプ別インデックス
                if (!_assetsByTypeIndex.ContainsKey(asset.assetType))
                {
                    _assetsByTypeIndex[asset.assetType] = new List<AssetInfo>();
                }
                _assetsByTypeIndex[asset.assetType].Add(asset);

                // お気に入りインデックス
                if (asset.isFavorite)
                {
                    if (!_favoriteAssetsIndex.ContainsKey(asset.assetType))
                    {
                        _favoriteAssetsIndex[asset.assetType] = new List<AssetInfo>();
                    }
                    _favoriteAssetsIndex[asset.assetType].Add(asset);
                }

                // 非表示アセットインデックス
                if (asset.isHidden)
                {
                    if (!_hiddenAssetsIndex.ContainsKey(asset.assetType))
                    {
                        _hiddenAssetsIndex[asset.assetType] = new List<AssetInfo>();
                    }
                    _hiddenAssetsIndex[asset.assetType].Add(asset);
                }
            }

            _indexNeedsUpdate = false;
        }

        /// <summary>
        /// キャッシュを無効化する
        /// </summary>
        private void InvalidateCache()
        {
            _searchCache.Clear();
            _searchCacheKeys.Clear();
            _indexNeedsUpdate = true;
        }
        private void UpdateFileTrackingInfo()
        {
            if (File.Exists(_dataFilePath))
            {
                var fileInfo = new FileInfo(_dataFilePath);
                _lastFileModified = fileInfo.LastWriteTime;
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

        /// <summary>
        /// デフォルトのAssetLibraryを作成
        /// </summary>
        private AssetLibrary CreateDefaultAssetLibrary()
        {
            return new AssetLibrary
            {
                version = "1.0",
                lastUpdated = DateTime.Now,
                assets = new List<AssetInfo>()
            };
        }

        public void Dispose()
        {
            _fileLock?.Dispose();

            // シングルトンインスタンスをクリア
            lock (_lockObject)
            {
                if (_instance == this)
                {
                    _instance = null;
                }
            }
        }
    }
}
