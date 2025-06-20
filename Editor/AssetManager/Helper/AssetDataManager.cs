using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using AMU.AssetManager.Data;
using AMU.Data.TagType;

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
        private DateTime _lastFileModified = DateTime.MinValue;        // ファイルアクセス排他制御
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly object _saveLock = new object();
        private bool _isSaving = false;

        // HTTP関連の制御
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly SemaphoreSlim _httpSemaphore = new SemaphoreSlim(3, 3); // 同時HTTP要求数を制限
        private const int HTTP_TIMEOUT_SECONDS = 10;

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

        // ダウンロードフォルダ監視
        private DownloadFolderWatcher _downloadWatcher;

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

            // HttpClientの設定
            _httpClient.Timeout = TimeSpan.FromSeconds(HTTP_TIMEOUT_SECONDS);
        }        /// <summary>
                 /// インスタンスを初期化（初回のみデータロード）
                 /// </summary>
        public void Initialize()
        {
            // まず同期的にファイルの存在確認と作成を行う
            EnsureLibraryFileExists();

            // TagTypeManagerの初期化（AssetManagerが管理するタグタイプ）
            InitializeTagTypeManager();

            if (_assetLibrary == null && !_isLoading)
            {
                LoadData();
            }

            // DownloadFolderWatcherを初期化
            InitializeDownloadWatcher();
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
        /// TagTypeManagerの初期化を実行します
        /// AssetManagerで管理するタグタイプシステムの初期化
        /// </summary>
        private void InitializeTagTypeManager()
        {
            try
            {
                // TagTypeManagerのデータを読み込み
                AMU.Data.TagType.TagTypeManager.LoadData();
                Debug.Log("[AssetDataManager] TagTypeManager initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AssetDataManager] TagTypeManager initialization failed: {ex.Message}");
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

                    // グループの整合性をチェック・修復
                    RepairOrphanedAssets();
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

                // シリアライゼーション用にアセットライブラリのスナップショットを作成
                AssetLibrary librarySnapshot;
                lock (_assetLibrary.assets)
                {
                    librarySnapshot = new AssetLibrary
                    {
                        assets = new List<AssetInfo>(_assetLibrary.assets),
                        lastUpdated = _assetLibrary.lastUpdated
                    };
                }

                // JSONシリアライズを別スレッドで実行
                string json = await Task.Run(() =>
                    JsonConvert.SerializeObject(librarySnapshot, Formatting.Indented));

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

            lock (_assetLibrary.assets)
            {
                _assetLibrary.assets.Add(asset);
            }
            InvalidateCache();
            SaveData();
        }

        public void UpdateAsset(AssetInfo asset)
        {
            if (_assetLibrary?.assets == null) return;

            lock (_assetLibrary.assets)
            {
                var existingAsset = _assetLibrary.assets.FirstOrDefault(a => a.uid == asset.uid);
                if (existingAsset != null)
                {
                    var index = _assetLibrary.assets.IndexOf(existingAsset);
                    _assetLibrary.assets[index] = asset;
                }
            }
            InvalidateCache();
            SaveData();
        }

        public void RemoveAsset(string uid)
        {
            if (_assetLibrary?.assets == null) return;

            lock (_assetLibrary.assets)
            {
                var asset = _assetLibrary.assets.FirstOrDefault(a => a.uid == uid);
                if (asset != null)
                {
                    _assetLibrary.assets.Remove(asset);
                }
            }
            InvalidateCache();
            SaveData();
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

        /// <summary>
        /// boothfileNameでアセットを検索
        /// </summary>
        public AssetInfo GetAssetByBoothFileName(string boothFileName)
        {
            if (string.IsNullOrEmpty(boothFileName))
                return null;

            return GetAllAssets().FirstOrDefault(a =>
                a.boothItem != null &&
                !string.IsNullOrEmpty(a.boothItem.boothfileName) &&
                string.Equals(a.boothItem.boothfileName, boothFileName, StringComparison.OrdinalIgnoreCase));
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
        }        /// <summary>
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

            // グループ機能：表示対象のアセットのみをフィルタリング
            // （親グループを持つアセットは除外）
            assets = assets.Where(a => a.IsVisibleInList()).ToList();

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
        }

        /// <summary>
        /// 高速化されたインデックス更新
        /// </summary>
        private void UpdateIndexes()
        {
            if (_assetLibrary?.assets == null) return;

            _assetByIdIndex.Clear();
            _assetsByTypeIndex.Clear();
            _favoriteAssetsIndex.Clear();
            _hiddenAssetsIndex.Clear();

            // コレクションのスナップショットを作成して安全に列挙
            List<AssetInfo> assetsSnapshot;
            lock (_assetLibrary.assets)
            {
                assetsSnapshot = new List<AssetInfo>(_assetLibrary.assets);
            }

            foreach (var asset in assetsSnapshot)
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

        private AssetLibrary CreateDefaultAssetLibrary()
        {
            return new AssetLibrary
            {
                lastUpdated = DateTime.Now,
                assets = new List<AssetInfo>()
            };
        }        /// <summary>
                 /// DownloadFolderWatcherを初期化
                 /// </summary>
        private void InitializeDownloadWatcher()
        {
            if (_downloadWatcher == null)
            {
                var fileManager = new AssetFileManager();
                _downloadWatcher = new DownloadFolderWatcher(this, fileManager);

                // ファイル処理イベントを購読
                _downloadWatcher.OnFileProcessed += (fileName, targetPath) =>
                {
                    Debug.Log($"[AssetDataManager] Auto-moved file: {fileName} -> {targetPath}");
                    EditorApplication.delayCall += () => OnDataChanged?.Invoke();
                };
            }

            // 監視状態を更新
            _downloadWatcher.UpdateWatcherState();
        }

        /// <summary>
        /// ダウンロードフォルダ監視の設定を更新
        /// </summary>
        public void UpdateDownloadWatcherSettings()
        {
            _downloadWatcher?.UpdateWatcherState();
        }

        /// <summary>
        /// ダウンロードフォルダを手動スキャン
        /// </summary>
        public void ScanDownloadFolder()
        {
            _downloadWatcher?.ScanDownloadFolder();
        }

        public void Dispose()
        {
            _downloadWatcher?.Dispose();
            _fileLock?.Dispose();
            _httpClient?.Dispose();

            // シングルトンインスタンスをクリア
            lock (_lockObject)
            {
                if (_instance == this)
                {
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// 詳細検索機能
        /// </summary>
        public List<AssetInfo> AdvancedSearchAssets(AdvancedSearchCriteria criteria, string filterType = null, bool? favoritesOnly = null, bool showHidden = false, bool? archivedOnly = null)
        {
            if (criteria == null || !criteria.HasCriteria())
            {
                return SearchAssets("", filterType, favoritesOnly, showHidden, archivedOnly);
            }

            // インデックスの更新
            if (_indexNeedsUpdate)
                UpdateIndexes();

            var assets = GetFilteredAssets(filterType, favoritesOnly, showHidden, archivedOnly);

            // 詳細検索の適用
            assets = ApplyAdvancedSearch(assets, criteria);

            return assets;
        }        /// <summary>
                 /// 詳細検索条件を適用
                 /// </summary>
        private List<AssetInfo> ApplyAdvancedSearch(List<AssetInfo> assets, AdvancedSearchCriteria criteria)
        {
            return assets.Where(asset =>
            {
                bool matchesName = string.IsNullOrEmpty(criteria.nameQuery) ||
                                   asset.name.ToLower().Contains(criteria.nameQuery.ToLower());

                bool matchesDescription = string.IsNullOrEmpty(criteria.descriptionQuery) ||
                                          asset.description.ToLower().Contains(criteria.descriptionQuery.ToLower());

                bool matchesAuthor = string.IsNullOrEmpty(criteria.authorQuery) ||
                                     asset.authorName.ToLower().Contains(criteria.authorQuery.ToLower());

                bool matchesTags = criteria.selectedTags.Count == 0 || CheckTagMatch(asset, criteria); return matchesName && matchesDescription && matchesAuthor && matchesTags;
            }).ToList();
        }

        /// <summary>
        /// タグマッチング処理
        /// </summary>
        private bool CheckTagMatch(AssetInfo asset, AdvancedSearchCriteria criteria)
        {
            if (criteria.selectedTags.Count == 0) return true;

            if (criteria.useAndLogicForTags)
            {
                // AND ロジック: 選択されたすべてのタグが含まれている必要がある
                return criteria.selectedTags.All(selectedTag =>
                    asset.tags.Any(assetTag => assetTag.ToLower().Contains(selectedTag.ToLower())));
            }
            else
            {
                // OR ロジック: 選択されたタグのいずれかが含まれていれば良い
                return criteria.selectedTags.Any(selectedTag =>
                    asset.tags.Any(assetTag => assetTag.ToLower().Contains(selectedTag.ToLower())));
            }
        }        /// <summary>
                 /// BPMLibraryからアセットをインポート（同じitemUrlのアイテムは自動グループ化）
                 /// </summary>
        public async Task<List<AssetInfo>> ImportFromBPMLibraryAsync(string defaultAssetType, List<string> defaultTags = null)
        {
            var (filePath, bmpLibrary) = await BPMHelper.FindLatestBPMLibraryAsync();
            if (bmpLibrary == null)
            {
                Debug.LogWarning("[AssetDataManager] BPMLibrary is not loaded or empty");
                return new List<AssetInfo>();
            }

            return ImportFromBPMLibraryInternal(bmpLibrary, defaultAssetType, defaultTags);
        }
        private List<AssetInfo> ImportFromBPMLibraryInternal(Data.BPMLibrary bmpLibrary, string defaultAssetType, List<string> defaultTags = null)
        {
            var importedAssets = new List<AssetInfo>();
            var existingDownloadUrls = GetExistingDownloadUrls();
            var packageGroups = new Dictionary<string, AssetInfo>(); // itemUrl -> グループアセット

            // BPMLibraryのlastUpdatedを取得してパース
            DateTime bmpLastUpdated = DateTime.Now; // デフォルト値
            if (!string.IsNullOrEmpty(bmpLibrary.lastUpdated))
            {
                if (!DateTime.TryParse(bmpLibrary.lastUpdated, out bmpLastUpdated))
                {
                    // パースに失敗した場合は現在時刻を使用
                    bmpLastUpdated = DateTime.Now;
                    Debug.LogWarning($"[AssetDataManager] Failed to parse BPM lastUpdated: {bmpLibrary.lastUpdated}");
                }
            }

            foreach (var author in bmpLibrary.authors)
            {
                string authorName = author.Key;
                foreach (var package in author.Value)
                {
                    if (package.files?.Count > 0)
                    {
                        // 同じitemUrlの複数ファイルがある場合、グループ化の対象となる
                        bool needsGrouping = package.files.Count > 1;
                        AssetInfo groupAsset = null;

                        if (needsGrouping)
                        {
                            // グループが既に存在するかチェック
                            if (!packageGroups.TryGetValue(package.itemUrl, out groupAsset))
                            {
                                // 新しいグループを作成
                                groupAsset = new AssetInfo
                                {
                                    uid = Guid.NewGuid().ToString(),
                                    name = package.packageName ?? "Unknown Package",
                                    description = "",
                                    assetType = defaultAssetType,
                                    isGroup = true,
                                    filePath = "", // グループは物理ファイルを持たない
                                    thumbnailPath = "",
                                    authorName = authorName,
                                    createdDate = bmpLastUpdated,
                                    fileSize = 0,
                                    tags = defaultTags != null ? new List<string>(defaultTags) : new List<string>(),
                                    dependencies = new List<string>(),
                                    isFavorite = false,
                                    isHidden = false,
                                    parentGroupId = null,
                                    childAssetIds = new List<string>(),
                                    boothItem = new BoothItem
                                    {
                                        boothItemUrl = package.itemUrl,
                                        boothfileName = "",
                                        boothDownloadUrl = ""
                                    }
                                };

                                packageGroups[package.itemUrl] = groupAsset;
                                importedAssets.Add(groupAsset);
                            }
                        }

                        foreach (var file in package.files)
                        {
                            // 既に同じダウンロードリンクが存在する場合はスキップ
                            if (existingDownloadUrls.Contains(file.downloadLink))
                            {
                                Debug.Log($"[AssetDataManager] Skipping duplicate download link: {file.downloadLink}");
                                continue;
                            }

                            var assetInfo = CreateAssetFromBPMPackage(package, file, authorName, defaultAssetType, defaultTags, bmpLastUpdated);

                            if (needsGrouping && groupAsset != null)
                            {
                                // グループの子アセットとして設定
                                assetInfo.SetParentGroup(groupAsset.uid);
                                groupAsset.AddChildAsset(assetInfo.uid);
                            }

                            importedAssets.Add(assetInfo);
                        }
                    }
                }
            }

            // インポートしたアセットをライブラリに追加
            if (importedAssets.Count > 0)
            {
                if (_assetLibrary?.assets == null)
                {
                    _assetLibrary = CreateDefaultAssetLibrary();
                }
                lock (_assetLibrary.assets)
                {
                    _assetLibrary.assets.AddRange(importedAssets);
                }
                InvalidateCache();
                SaveData();

                // 画像を非同期で取得してサムネイルとして設定
                _ = ProcessThumbnailsFromBPMAsync(importedAssets, bmpLibrary);

                Debug.Log($"[AssetDataManager] Imported {importedAssets.Count} assets from BPMLibrary (with auto-grouping)");
            }

            return importedAssets;
        }        /// <summary>
                 /// BPMLibraryからアセットをインポート（個別設定使用）
                 /// </summary>
        public async Task<List<AssetInfo>> ImportFromBPMLibraryWithIndividualSettingsAsync(
            Dictionary<string, AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings> packageSettings,
            Dictionary<string, AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings> fileSettings,
            Dictionary<string, List<Data.BPMFileInfo>> unregisteredAssets = null)
        {
            var (filePath, bmpLibrary) = await BPMHelper.FindLatestBPMLibraryAsync();
            if (bmpLibrary == null)
            {
                Debug.LogWarning("[AssetDataManager] BPMLibrary is not loaded or empty");
                return new List<AssetInfo>();
            }

            return ImportFromBPMLibraryWithIndividualSettingsInternal(bmpLibrary, packageSettings, fileSettings, unregisteredAssets);
        }

        private List<AssetInfo> ImportFromBPMLibraryWithIndividualSettingsInternal(
            Data.BPMLibrary bmpLibrary,
            Dictionary<string, AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings> packageSettings,
            Dictionary<string, AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings> fileSettings,
            Dictionary<string, List<Data.BPMFileInfo>> unregisteredAssets = null)
        {
            var importedAssets = new List<AssetInfo>();
            var packageGroups = new Dictionary<string, AssetInfo>(); // itemUrl -> グループアセット

            // BPMLibraryのlastUpdatedを取得してパース
            DateTime bmpLastUpdated = DateTime.Now;
            if (!string.IsNullOrEmpty(bmpLibrary.lastUpdated))
            {
                if (!DateTime.TryParse(bmpLibrary.lastUpdated, out bmpLastUpdated))
                {
                    bmpLastUpdated = DateTime.Now;
                    Debug.LogWarning($"[AssetDataManager] Failed to parse BPM lastUpdated: {bmpLibrary.lastUpdated}");
                }
            }
            foreach (var author in bmpLibrary.authors)
            {
                string authorName = author.Key;
                foreach (var package in author.Value)
                {
                    string packageKey = $"{authorName}|{package.itemUrl}";                    // 未登録アセットのフィルタリングが指定されている場合、そのリストのみを処理
                    List<Data.BPMFileInfo> filesToProcess;
                    if (unregisteredAssets != null)
                    {
                        if (!unregisteredAssets.TryGetValue(packageKey, out filesToProcess))
                        {
                            continue; // このパッケージには未登録ファイルがない
                        }
                    }
                    else
                    {
                        filesToProcess = package.files ?? new List<Data.BPMFileInfo>();
                    }

                    if (filesToProcess?.Count > 0)
                    {
                        bool needsGrouping = filesToProcess.Count > 1;
                        AssetInfo groupAsset = null;

                        if (needsGrouping)
                        {
                            // グループが既に存在するかチェック
                            if (!packageGroups.TryGetValue(package.itemUrl, out groupAsset))
                            {
                                // パッケージ設定を取得
                                var packageSetting = packageSettings.ContainsKey(packageKey)
                                    ? packageSettings[packageKey]
                                    : new AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings();

                                // 新しいグループを作成
                                groupAsset = new AssetInfo
                                {
                                    uid = Guid.NewGuid().ToString(),
                                    name = package.packageName ?? "Unknown Package",
                                    description = "",
                                    assetType = packageSetting.assetType,
                                    isGroup = true,
                                    filePath = "",
                                    thumbnailPath = "",
                                    authorName = authorName,
                                    createdDate = bmpLastUpdated,
                                    fileSize = 0,
                                    tags = new List<string>(),
                                    dependencies = new List<string>(),
                                    isFavorite = false,
                                    isHidden = false,
                                    parentGroupId = null,
                                    childAssetIds = new List<string>(),
                                    boothItem = new BoothItem
                                    {
                                        boothItemUrl = package.itemUrl,
                                        boothfileName = "",
                                        boothDownloadUrl = ""
                                    }
                                };

                                packageGroups[package.itemUrl] = groupAsset;
                                importedAssets.Add(groupAsset);
                            }
                        }
                        foreach (var file in filesToProcess)
                        {
                            // ファイル設定を取得
                            AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings fileSetting;
                            if (needsGrouping)
                            {
                                // グループ化される場合はパッケージ設定を使用
                                fileSetting = packageSettings.ContainsKey(packageKey)
                                    ? packageSettings[packageKey]
                                    : new AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings();
                            }
                            else
                            {
                                // 個別ファイルの場合は個別設定を使用
                                string fileKey = $"{authorName}|{package.itemUrl}|{file.fileName}";
                                fileSetting = fileSettings.ContainsKey(fileKey)
                                    ? fileSettings[fileKey]
                                    : new AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings();
                            }

                            var assetInfo = CreateAssetFromBPMPackageWithSettings(package, file, authorName, fileSetting, bmpLastUpdated);

                            if (needsGrouping && groupAsset != null)
                            {
                                // グループの子アセットとして設定
                                assetInfo.SetParentGroup(groupAsset.uid);
                                groupAsset.AddChildAsset(assetInfo.uid);
                            }

                            importedAssets.Add(assetInfo);
                        }
                    }
                }
            }

            // インポートしたアセットをライブラリに追加
            if (importedAssets.Count > 0)
            {
                if (_assetLibrary?.assets == null)
                {
                    _assetLibrary = CreateDefaultAssetLibrary();
                }
                lock (_assetLibrary.assets)
                {
                    _assetLibrary.assets.AddRange(importedAssets);
                }
                InvalidateCache();
                SaveData();                // 画像を非同期で取得してサムネイルとして設定
                _ = ProcessThumbnailsFromBPMAsync(importedAssets, bmpLibrary);

                Debug.Log($"[AssetDataManager] Imported {importedAssets.Count} unregistered assets from BPMLibrary with individual settings");
            }

            return importedAssets;
        }

        /// <summary>
        /// BPMPackageからAssetInfoを作成
        /// </summary>
        private AssetInfo CreateAssetFromBPMPackage(Data.BPMPackage package, Data.BPMFileInfo file, string authorName, string assetType, List<string> tags, DateTime bmpLastUpdated)
        {
            var asset = new AssetInfo
            {
                uid = Guid.NewGuid().ToString(),
                name = file.fileName ?? "Unknown File",
                description = "",
                assetType = assetType,
                filePath = "",
                thumbnailPath = "",
                authorName = authorName,
                createdDate = bmpLastUpdated,
                fileSize = 0,
                tags = tags != null ? new List<string>(tags) : new List<string>(),
                dependencies = new List<string>(),
                isFavorite = false,
                isHidden = false,
                boothItem = new BoothItem
                {
                    boothItemUrl = package.itemUrl,
                    boothfileName = file.fileName,
                    boothDownloadUrl = file.downloadLink
                }
            };

            return asset;
        }

        /// <summary>
        /// BPMPackageからAssetInfoを作成（設定付き）
        /// </summary>
        private AssetInfo CreateAssetFromBPMPackageWithSettings(Data.BPMPackage package, Data.BPMFileInfo file, string authorName, AMU.AssetManager.UI.BPMImportWindow.AssetImportSettings settings, DateTime bmpLastUpdated)
        {
            var asset = new AssetInfo
            {
                uid = Guid.NewGuid().ToString(),
                name = file.fileName ?? "Unknown File",
                description = "",
                assetType = settings.assetType,
                filePath = "",
                thumbnailPath = "",
                authorName = authorName,
                createdDate = bmpLastUpdated,
                fileSize = 0,
                tags = new List<string>(),
                dependencies = new List<string>(),
                isFavorite = false,
                isHidden = false,
                boothItem = new BoothItem
                {
                    boothItemUrl = package.itemUrl,
                    boothfileName = file.fileName,
                    boothDownloadUrl = file.downloadLink
                }
            };

            return asset;
        }        /// <summary>
                 /// BPMインポート後のサムネイル処理を非同期で実行
                 /// </summary>
        private async Task ProcessThumbnailsFromBPMAsync(List<AssetInfo> importedAssets, Data.BPMLibrary bmpLibrary)
        {
            try
            {
                // グループアセットと個別アセットを分離
                var groupAssets = importedAssets.Where(a => a.isGroup).ToList();
                var individualAssets = importedAssets.Where(a => !a.isGroup).ToList();

                // 処理済みのimageUrlを記録（重複回避）
                var processedImageUrls = new HashSet<string>();

                // グループアセットのサムネイル処理
                foreach (var groupAsset in groupAssets)
                {
                    if (!string.IsNullOrEmpty(groupAsset.boothItem?.boothItemUrl))
                    {
                        var packageInfo = FindPackageByItemUrl(bmpLibrary, groupAsset.boothItem.boothItemUrl);
                        if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.imageUrl) &&
                            !processedImageUrls.Contains(packageInfo.imageUrl))
                        {
                            processedImageUrls.Add(packageInfo.imageUrl);
                            await DownloadAndSetThumbnailAsync(groupAsset, packageInfo.imageUrl);

                            // サーバー負荷軽減のため、リクエスト間に遅延を追加
                            await Task.Delay(500);
                        }
                    }
                }

                // 個別アセット（グループでないアセット）のサムネイル処理
                foreach (var asset in individualAssets)
                {
                    if (!string.IsNullOrEmpty(asset.boothItem?.boothItemUrl))
                    {
                        var packageInfo = FindPackageByItemUrl(bmpLibrary, asset.boothItem.boothItemUrl);
                        if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.imageUrl) &&
                            !processedImageUrls.Contains(packageInfo.imageUrl))
                        {
                            processedImageUrls.Add(packageInfo.imageUrl);
                            await DownloadAndSetThumbnailAsync(asset, packageInfo.imageUrl);

                            // サーバー負荷軽減のため、リクエスト間に遅延を追加
                            await Task.Delay(500);
                        }
                    }
                }

                Debug.Log($"[AssetDataManager] Processed thumbnails for {processedImageUrls.Count} items from BMP import");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Failed to process thumbnails from BMP: {ex.Message}");
            }
        }        /// <summary>
                 /// itemUrlからBPMPackage情報を検索
                 /// </summary>
        private Data.BPMPackage FindPackageByItemUrl(Data.BPMLibrary bmpLibrary, string itemUrl)
        {
            if (bmpLibrary?.authors == null || string.IsNullOrEmpty(itemUrl))
                return null;

            foreach (var author in bmpLibrary.authors)
            {
                foreach (var package in author.Value)
                {
                    if (package.itemUrl == itemUrl)
                    {
                        return package;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 画像をダウンロードしてサムネイルとして設定
        /// </summary>
        private async Task DownloadAndSetThumbnailAsync(AssetInfo asset, string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || asset == null)
                return;
            await _httpSemaphore.WaitAsync(); try
            {
                // BoothItem専用のサムネイルディレクトリを使用
                string boothThumbnailDir = BPMHelper.GetBoothThumbnailDirectory();

                if (!Directory.Exists(boothThumbnailDir))
                {
                    Directory.CreateDirectory(boothThumbnailDir);
                }

                // imageURLのハッシュを使用してファイル名を生成（同じURLなら同じ名前）
                string imageHash = BPMHelper.GetImageHash(imageUrl);

                // 既存ファイルをチェック（拡張子違いも考慮）
                string[] possibleExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
                string existingFilePath = null;

                foreach (string ext in possibleExtensions)
                {
                    string checkPath = Path.Combine(boothThumbnailDir, $"{imageHash}{ext}");
                    if (File.Exists(checkPath))
                    {
                        existingFilePath = checkPath;
                        break;
                    }
                }

                // ファイルが既に存在する場合はダウンロードをスキップ
                if (!string.IsNullOrEmpty(existingFilePath))
                {
                    Debug.Log($"[AssetDataManager] Using existing thumbnail for {asset.name}: {existingFilePath}");                    // Booth自動取得の場合はコピーを作成せずに直接パスを設定
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            AssetThumbnailManager.Instance.SetBoothThumbnailDirect(asset, existingFilePath);
                            Debug.Log($"[AssetDataManager] Booth thumbnail path set for {asset.name}: {existingFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[AssetDataManager] Failed to set booth thumbnail for {asset.name}: {ex.Message}");
                        }
                    };
                    return;
                }

                Debug.Log($"[AssetDataManager] Downloading thumbnail for {asset.name}: {imageUrl}");

                byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                if (imageBytes?.Length > 0)
                {
                    // ファイル拡張子を推測
                    string extension = GetImageExtension(imageUrl, imageBytes);
                    string fileName = $"{imageHash}{extension}";
                    string filePath = Path.Combine(boothThumbnailDir, fileName);

                    // 画像ファイルを保存
                    await File.WriteAllBytesAsync(filePath, imageBytes);                    // Booth自動取得の場合はコピーを作成せずに直接パスを設定
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            AssetThumbnailManager.Instance.SetBoothThumbnailDirect(asset, filePath);
                            Debug.Log($"[AssetDataManager] Booth thumbnail downloaded and set for {asset.name}: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[AssetDataManager] Failed to set downloaded booth thumbnail for {asset.name}: {ex.Message}");
                        }
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.LogWarning($"[AssetDataManager] Failed to download thumbnail for {asset.name}: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                Debug.LogWarning($"[AssetDataManager] Thumbnail download timeout for {asset.name}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetDataManager] Unexpected error downloading thumbnail for {asset.name}: {ex.Message}");
            }
            finally
            {
                _httpSemaphore.Release();
            }
        }

        /// <summary>
        /// 画像のファイル拡張子を推測
        /// </summary>
        private string GetImageExtension(string url, byte[] imageBytes)
        {
            // URLから拡張子を推測
            string urlLower = url.ToLower();
            if (urlLower.Contains(".jpg") || urlLower.Contains(".jpeg"))
                return ".jpg";
            if (urlLower.Contains(".png"))
                return ".png";
            if (urlLower.Contains(".gif"))
                return ".gif";
            if (urlLower.Contains(".bmp"))
                return ".bmp";
            if (urlLower.Contains(".webp"))
                return ".webp";

            // バイト配列からファイル形式を判定
            if (imageBytes?.Length >= 8)
            {
                // PNG signature
                if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                    return ".png";

                // JPEG signature
                if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                    return ".jpg";

                // GIF signature
                if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
                    return ".gif";

                // WebP signature
                if (imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46 &&
                    imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && imageBytes[10] == 0x42 && imageBytes[11] == 0x50)
                    return ".webp";
            }

            // デフォルトはPNG
            return ".png";
        }

        #region グループ管理機能

        /// <summary>
        /// 新しいグループを作成
        /// </summary>
        public AssetInfo CreateGroup(string groupName, string description = "")
        {
            var groupAsset = new AssetInfo
            {
                uid = Guid.NewGuid().ToString(),
                name = groupName,
                description = description,
                assetType = "Other", // グループには特定のassetTypeを設定しない
                isGroup = true,
                filePath = "", // グループは物理ファイルを持たない
                thumbnailPath = "",
                authorName = "",
                createdDate = DateTime.Now,
                fileSize = 0,
                tags = new List<string>(),
                dependencies = new List<string>(),
                isFavorite = false,
                isHidden = false,
                parentGroupId = null,
                childAssetIds = new List<string>()
            };

            AddAsset(groupAsset);
            return groupAsset;
        }

        /// <summary>
        /// アセットをグループに追加
        /// </summary>
        public bool AddAssetToGroup(string assetId, string groupId)
        {
            var asset = GetAsset(assetId);
            var group = GetAsset(groupId);

            if (asset == null || group == null || !group.isGroup)
                return false;

            // 既に別のグループに属している場合は先に削除
            if (asset.HasParent())
            {
                RemoveAssetFromGroup(assetId);
            }

            // グループに追加
            group.AddChildAsset(assetId);
            asset.SetParentGroup(groupId);

            InvalidateCache();
            SaveData();

            return true;
        }

        /// <summary>
        /// アセットをグループから削除
        /// </summary>
        public bool RemoveAssetFromGroup(string assetId)
        {
            var asset = GetAsset(assetId);
            if (asset == null || !asset.HasParent())
                return false;

            var group = GetAsset(asset.parentGroupId);
            if (group != null)
            {
                group.RemoveChildAsset(assetId);
            }

            asset.RemoveFromParentGroup();

            InvalidateCache();
            SaveData();

            return true;
        }

        /// <summary>
        /// グループを解散（子アセットをすべて独立させる）
        /// </summary>
        public bool DisbandGroup(string groupId)
        {
            var group = GetAsset(groupId);
            if (group == null || !group.isGroup)
                return false;

            // グループ解散前に詳細ウィンドウを閉じる
            CloseDetailWindowsForAsset(groupId);

            // 子アセットをすべて独立させる
            var childIds = new List<string>(group.childAssetIds);
            foreach (var childId in childIds)
            {
                RemoveAssetFromGroup(childId);
            }

            // グループ自体を削除
            RemoveAsset(groupId);

            return true;
        }

        /// <summary>
        /// 指定されたアセットの詳細ウィンドウを閉じる
        /// </summary>
        private void CloseDetailWindowsForAsset(string assetId)
        {
            // 現在開いているすべてのAssetDetailWindowを取得
            var detailWindows = UnityEngine.Resources.FindObjectsOfTypeAll<UI.AssetDetailWindow>();

            foreach (var window in detailWindows)
            {
                // プライベートフィールドにアクセスするためにリフレクションを使用
                var assetField = typeof(UI.AssetDetailWindow).GetField("_asset",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (assetField != null)
                {
                    var windowAsset = assetField.GetValue(window) as Data.AssetInfo;
                    if (windowAsset != null && windowAsset.uid == assetId)
                    {
                        window.Close();
                    }
                }
            }
        }

        /// <summary>
        /// グループの子アセットを取得
        /// </summary>
        public List<AssetInfo> GetGroupChildren(string groupId)
        {
            var group = GetAsset(groupId);
            if (group == null || !group.isGroup)
                return new List<AssetInfo>();

            var children = new List<AssetInfo>();
            foreach (var childId in group.childAssetIds)
            {
                var child = GetAsset(childId);
                if (child != null)
                {
                    children.Add(child);
                }
            }

            return children;
        }

        /// <summary>
        /// 表示対象のアセットのみを取得（親グループを持つアセットは除外）
        /// </summary>
        public List<AssetInfo> GetVisibleAssets()
        {
            if (_assetLibrary?.assets == null)
                return new List<AssetInfo>();

            return _assetLibrary.assets.Where(asset => asset.IsVisibleInList()).ToList();
        }

        /// <summary>
        /// すべてのグループアセットを取得
        /// </summary>
        public List<AssetInfo> GetGroupAssets()
        {
            if (_assetLibrary?.assets == null)
                return new List<AssetInfo>();

            return _assetLibrary.assets.Where(asset => asset.isGroup).ToList();
        }

        /// <summary>
        /// 孤立した子アセット（親グループが存在しない子アセット）を修復
        /// </summary>
        public void RepairOrphanedAssets()
        {
            if (_assetLibrary?.assets == null)
                return;

            var allAssetIds = _assetLibrary.assets.Select(a => a.uid).ToHashSet();
            bool hasChanges = false;

            foreach (var asset in _assetLibrary.assets)
            {
                // 親グループが存在しない場合は親を削除
                if (asset.HasParent() && !allAssetIds.Contains(asset.parentGroupId))
                {
                    asset.RemoveFromParentGroup();
                    hasChanges = true;
                }

                // 存在しない子アセットIDを削除
                if (asset.isGroup && asset.childAssetIds.Count > 0)
                {
                    var validChildren = asset.childAssetIds.Where(id => allAssetIds.Contains(id)).ToList();
                    if (validChildren.Count != asset.childAssetIds.Count)
                    {
                        asset.childAssetIds = validChildren;
                        if (asset.childAssetIds.Count == 0)
                        {
                            asset.isGroup = false;
                        }
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                InvalidateCache();
                SaveData();
            }
        }        /// <summary>
                 /// 既存のダウンロードURLのセットを取得
                 /// </summary>
        private HashSet<string> GetExistingDownloadUrls()
        {
            var urls = new HashSet<string>();
            if (_assetLibrary?.assets != null)
            {
                foreach (var asset in _assetLibrary.assets)
                {
                    if (asset.boothItem?.boothDownloadUrl != null)
                    {
                        urls.Add(asset.boothItem.boothDownloadUrl);
                    }
                }
            }
            return urls;
        }
        #endregion
    }
}
