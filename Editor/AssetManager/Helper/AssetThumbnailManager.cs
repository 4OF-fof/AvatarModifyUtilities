using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;

namespace AMU.AssetManager.Helper
{
    public class AssetThumbnailManager
    {
        // シングルトンインスタンス
        private static AssetThumbnailManager _instance;
        public static AssetThumbnailManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AssetThumbnailManager();
                }
                return _instance;
            }
        }

        // LRUキャッシュの実装
        private LinkedList<string> _cacheOrder = new LinkedList<string>();
        private Dictionary<string, LinkedListNode<string>> _cacheNodes = new Dictionary<string, LinkedListNode<string>>();
        private Dictionary<string, Texture2D> _thumbnailCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, DateTime> _thumbnailFileModified = new Dictionary<string, DateTime>();
        private HashSet<string> _loadingThumbnails = new HashSet<string>();
        private Queue<string> _loadQueue = new Queue<string>();
        private string _thumbnailDirectory;
        private int _maxCacheSize = 200; // キャッシュサイズを制限
        private const float FILE_CHECK_INTERVAL = 5.0f; // ファイル変更チェックの間隔（秒）
        private Dictionary<string, float> _lastFileCheckTime = new Dictionary<string, float>(); public event Action<AssetInfo> OnThumbnailSaved;
        public event Action OnThumbnailLoaded;
        public event Action<string> OnThumbnailUpdated; // 特定のアセットのサムネイル更新通知        

        private AssetThumbnailManager()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            _thumbnailDirectory = Path.Combine(coreDir, "AssetManager", "Thumbnails");
            EnsureThumbnailDirectory();
            EnsureBoothThumbnailDirectory();
        }

        /// <summary>
        /// BoothItem専用サムネイルディレクトリを確保
        /// </summary>
        private void EnsureBoothThumbnailDirectory()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            string boothThumbnailDir = Path.Combine(coreDir, "AssetManager", "BoothItem", "Thumbnail");

            if (!Directory.Exists(boothThumbnailDir))
            {
                Directory.CreateDirectory(boothThumbnailDir);
            }
        }
        public Texture2D GetThumbnail(AssetInfo asset)
        {
            if (asset == null) return null;

            // キャッシュから取得
            if (_thumbnailCache.TryGetValue(asset.uid, out var cachedTexture) && cachedTexture != null)
            {
                // LRUキャッシュを更新
                UpdateCacheOrder(asset.uid);
                return cachedTexture;
            }

            // 既に読み込み中の場合はデフォルトを返す
            if (_loadingThumbnails.Contains(asset.uid))
            {
                return GetDefaultThumbnail(asset);
            }

            // 非同期で読み込みを開始
            LoadThumbnailAsync(asset);

            return GetDefaultThumbnail(asset);
        }/// <summary>
         /// サムネイルを非同期で読み込む
         /// </summary>
        private async void LoadThumbnailAsync(AssetInfo asset)
        {
            if (_loadingThumbnails.Contains(asset.uid)) return;

            _loadingThumbnails.Add(asset.uid);

            try
            {
                // ファイル変更チェックを間隔を空けて実行
                bool shouldCheckFile = ShouldCheckFile(asset.uid);
                if (shouldCheckFile)
                {
                    CheckThumbnailFileChanges(asset);
                }
                Texture2D texture = null;

                // サムネイルパスが存在する場合
                if (!string.IsNullOrEmpty(asset.thumbnailPath) && File.Exists(asset.thumbnailPath))
                {
                    texture = await LoadTextureFromFileAsync(asset.thumbnailPath);
                }

                // BoothItem専用ディレクトリからも検索
                if (texture == null && asset.boothItem != null)
                {
                    texture = await LoadBoothThumbnailAsync(asset);
                }

                if (texture != null)
                {
                    // メインスレッドでキャッシュに追加
                    EditorApplication.delayCall += () =>
                    {
                        AddToCache(asset.uid, texture);
                        if (shouldCheckFile)
                        {
                            UpdateThumbnailModifiedTime(asset);
                        }
                        OnThumbnailLoaded?.Invoke();
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to load thumbnail for {asset.uid}: {ex.Message}");
            }
            finally
            {
                _loadingThumbnails.Remove(asset.uid);
            }
        }        /// <summary>
                 /// テクスチャファイルを非同期で読み込む（最適化版）
                 /// </summary>
        private async Task<Texture2D> LoadTextureFromFileAsync(string filePath)
        {
            try
            {
                if (filePath.StartsWith("Assets/"))
                {
                    // Unityアセットの場合はメインスレッドで読み込み
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                }
                else
                {
                    // 外部ファイルを非同期で読み込み
                    byte[] fileData = await ReadFileAsync(filePath);

                    if (fileData != null)
                    {
                        // メインスレッドでテクスチャを作成
                        var tcs = new TaskCompletionSource<Texture2D>();

                        EditorApplication.delayCall += () =>
                        {
                            try
                            {
                                var texture = new Texture2D(2, 2);
                                if (texture.LoadImage(fileData))
                                {
                                    tcs.SetResult(texture);
                                }
                                else
                                {
                                    UnityEngine.Object.DestroyImmediate(texture);
                                    tcs.SetResult(null);
                                }
                            }
                            catch (Exception ex)
                            {
                                tcs.SetException(ex);
                            }
                        };

                        return await tcs.Task;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to load texture from {filePath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// ファイルを非同期で読み込む
        /// </summary>
        private async Task<byte[]> ReadFileAsync(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }        /// <summary>
                 /// LRUキャッシュにテクスチャを追加
                 /// </summary>
        private void AddToCache(string assetUid, Texture2D texture)
        {
            // 既存のエントリを削除
            if (_thumbnailCache.ContainsKey(assetUid))
            {
                RemoveFromCache(assetUid);
            }

            // キャッシュサイズを制限
            while (_thumbnailCache.Count >= _maxCacheSize)
            {
                // LRU: 最も古いエントリを削除
                var oldestUid = _cacheOrder.Last.Value;
                RemoveFromCache(oldestUid);
            }

            // 新しいエントリを追加
            _thumbnailCache[assetUid] = texture;
            var node = _cacheOrder.AddFirst(assetUid);
            _cacheNodes[assetUid] = node;
        }

        /// <summary>
        /// キャッシュの使用順序を更新
        /// </summary>
        private void UpdateCacheOrder(string assetUid)
        {
            if (_cacheNodes.TryGetValue(assetUid, out var node))
            {
                _cacheOrder.Remove(node);
                _cacheOrder.AddFirst(node);
            }
        }

        /// <summary>
        /// キャッシュからエントリを削除
        /// </summary>
        private void RemoveFromCache(string assetUid)
        {
            if (_thumbnailCache.TryGetValue(assetUid, out var texture) && texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            _thumbnailCache.Remove(assetUid);

            if (_cacheNodes.TryGetValue(assetUid, out var node))
            {
                _cacheOrder.Remove(node);
                _cacheNodes.Remove(assetUid);
            }
        }

        /// <summary>
        /// ファイルチェックが必要かどうかを判定
        /// </summary>
        private bool ShouldCheckFile(string assetUid)
        {
            float currentTime = Time.realtimeSinceStartup;
            if (_lastFileCheckTime.TryGetValue(assetUid, out var lastTime))
            {
                if (currentTime - lastTime < FILE_CHECK_INTERVAL)
                {
                    return false;
                }
            }
            _lastFileCheckTime[assetUid] = currentTime;
            return true;
        }

        /// <summary>
        /// サムネイルファイルが変更されているかチェックし、必要に応じてキャッシュを無効化する
        /// </summary>
        private void CheckThumbnailFileChanges(AssetInfo asset)
        {
            if (string.IsNullOrEmpty(asset.thumbnailPath) || !File.Exists(asset.thumbnailPath))
                return;

            try
            {
                var fileInfo = new FileInfo(asset.thumbnailPath);
                var currentModified = fileInfo.LastWriteTime;

                if (_thumbnailFileModified.TryGetValue(asset.uid, out var lastModified))
                {
                    if (currentModified > lastModified)
                    {
                        // ファイルが更新されているのでキャッシュを無効化
                        InvalidateThumbnailCache(asset.uid);
                        OnThumbnailUpdated?.Invoke(asset.uid);
                    }
                }
                else
                {
                    // 初回アクセスの場合
                    _thumbnailFileModified[asset.uid] = currentModified;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to check thumbnail file changes for {asset.uid}: {ex.Message}");
            }
        }        /// <summary>
                 /// 特定のアセットのサムネイルキャッシュを無効化する
                 /// </summary>
        private void InvalidateThumbnailCache(string assetUid)
        {
            RemoveFromCache(assetUid);
            _thumbnailFileModified.Remove(assetUid);
            _lastFileCheckTime.Remove(assetUid);
        }

        /// <summary>
        /// サムネイルの更新時刻を記録する
        /// </summary>
        private void UpdateThumbnailModifiedTime(AssetInfo asset)
        {
            if (!string.IsNullOrEmpty(asset.thumbnailPath) && File.Exists(asset.thumbnailPath))
            {
                try
                {
                    var fileInfo = new FileInfo(asset.thumbnailPath);
                    _thumbnailFileModified[asset.uid] = fileInfo.LastWriteTime;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AssetThumbnailManager] Failed to update thumbnail modified time for {asset.uid}: {ex.Message}");
                }
            }
        }
        public void SetCustomThumbnail(AssetInfo asset, string imagePath)
        {
            if (asset == null || string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return;

            // ファイルのハッシュ値を計算
            string fileHash = GetFileHash(imagePath);

            // 既存のファイルをハッシュベースで検索
            string existingThumbnailPath = FindExistingThumbnailByHash(fileHash);

            if (!string.IsNullOrEmpty(existingThumbnailPath))
            {
                // 既存のファイルを使用
                asset.thumbnailPath = existingThumbnailPath.Replace('\\', '/');

                // キャッシュを更新
                var texture = LoadTextureFromFileSync(existingThumbnailPath);
                if (texture != null)
                {
                    InvalidateThumbnailCache(asset.uid);
                    AddToCache(asset.uid, texture);
                    UpdateThumbnailModifiedTime(asset);
                }
            }
            else
            {
                // 新しいファイルとして保存
                var texture = LoadTextureFromFileSync(imagePath);
                if (texture != null)
                {
                    string extension = Path.GetExtension(imagePath).ToLower();
                    if (string.IsNullOrEmpty(extension))
                        extension = ".png";

                    string fileName = $"{fileHash}{extension}";
                    string thumbnailPath = Path.Combine(_thumbnailDirectory, fileName);

                    SaveTextureToFile(texture, thumbnailPath);
                    asset.thumbnailPath = thumbnailPath.Replace('\\', '/');

                    // 古いキャッシュを完全に無効化してから新しいテクスチャをキャッシュ
                    InvalidateThumbnailCache(asset.uid);
                    AddToCache(asset.uid, texture);
                    UpdateThumbnailModifiedTime(asset);
                }
            }// サムネイル更新をより確実に通知するため、EditorApplication.delayCallを使用
            EditorApplication.delayCall += () =>
            {
                OnThumbnailLoaded?.Invoke();
                OnThumbnailSaved?.Invoke(asset);
                OnThumbnailUpdated?.Invoke(asset.uid);

                    // 全てのEditorWindowを再描画
                    foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    if (window.GetType().Name == "AssetManagerWindow" ||
                        window.GetType().Name == "AssetDetailWindow")
                    {
                        window.Repaint();
                    }
                }
            };
        }

        public void ClearCache()
        {
            foreach (var texture in _thumbnailCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            _thumbnailCache.Clear();
            _cacheOrder.Clear();
            _cacheNodes.Clear();
            _thumbnailFileModified.Clear();
            _lastFileCheckTime.Clear();
        }
        private Texture2D LoadTextureFromFileSync(string filePath)
        {
            try
            {
                if (filePath.StartsWith("Assets/"))
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                }
                else
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {
                        return texture;
                    }
                    UnityEngine.Object.DestroyImmediate(texture);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to load texture from {filePath}: {ex.Message}");
                return null;
            }
        }
        private void SaveThumbnail(AssetInfo asset, Texture2D texture)
        {
            // テクスチャからハッシュ値を計算
            string textureHash = GetTextureHash(texture);
            string fileName = $"{textureHash}.png";
            string thumbnailPath = Path.Combine(_thumbnailDirectory, fileName);

            // 既存のファイルがなければ保存
            if (!File.Exists(thumbnailPath))
            {
                SaveTextureToFile(texture, thumbnailPath);
            }

            // Convert path to use forward slashes for JSON storage
            asset.thumbnailPath = thumbnailPath.Replace('\\', '/');
        }

        private void SaveTextureToFile(Texture2D texture, string filePath)
        {
            try
            {
                byte[] pngData = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to save texture to {filePath}: {ex.Message}");
            }
        }
        private Texture2D GetDefaultThumbnail(AssetInfo asset)
        {
            // グループの場合はフォルダアイコンを表示
            if (asset.isGroup)
            {
                return EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            }

            // サムネイルが設定されていない場合は、ファイル形式に関わらずPrefabアイコンを表示
            return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
        }
        private void EnsureThumbnailDirectory()
        {
            if (!Directory.Exists(_thumbnailDirectory))
            {
                Directory.CreateDirectory(_thumbnailDirectory);
            }
        }

        /// <summary>
        /// BoothItem専用ディレクトリからサムネイルを読み込む
        /// </summary>
        private async Task<Texture2D> LoadBoothThumbnailAsync(AssetInfo asset)
        {
            try
            {
                string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
                string boothThumbnailDir = Path.Combine(coreDir, "AssetManager", "BoothItem", "Thumbnail");

                if (!Directory.Exists(boothThumbnailDir))
                    return null;

                // 複数の拡張子を試行
                string[] extensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

                foreach (string ext in extensions)
                {
                    string filePath = Path.Combine(boothThumbnailDir, $"{asset.uid}{ext}");
                    if (File.Exists(filePath))
                    {
                        var texture = await LoadTextureFromFileAsync(filePath);
                        if (texture != null)
                        {
                            // thumbnailPathを更新
                            asset.thumbnailPath = filePath.Replace('\\', '/');
                            return texture;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to load BoothItem thumbnail for {asset.uid}: {ex.Message}");
            }

            return null;
        }

        public void DrawThumbnail(AssetInfo asset, float size)
        {
            var thumbnail = GetThumbnail(asset);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            if (thumbnail != null)
            {
                GUI.DrawTexture(rect, thumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                // サムネイルがない場合はデフォルトアイコンを表示
                var defaultIcon = GetDefaultThumbnail(asset);
                if (defaultIcon != null)
                {
                    GUI.DrawTexture(rect, defaultIcon, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUI.Box(rect, "No Image");
                }
            }
        }

        /// <summary>
        /// ファイルのMD5ハッシュ値を計算
        /// </summary>
        private string GetFileHash(string filePath)
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        byte[] hashBytes = md5.ComputeHash(stream);
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to calculate file hash for {filePath}: {ex.Message}");
                // フォールバックとしてファイル名とサイズを使用
                var fileInfo = new FileInfo(filePath);
                return $"{Path.GetFileNameWithoutExtension(filePath)}_{fileInfo.Length}".GetHashCode().ToString("x8");
            }
        }
        /// <summary>
        /// テクスチャのMD5ハッシュ値を計算
        /// </summary>
        private string GetTextureHash(Texture2D texture)
        {
            try
            {
                byte[] textureBytes = texture.EncodeToPNG();
                using (var md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(textureBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetThumbnailManager] Failed to calculate texture hash: {ex.Message}");
                // フォールバックとしてテクスチャサイズとフォーマットを使用
                return $"{texture.width}_{texture.height}_{texture.format}".GetHashCode().ToString("x8");
            }
        }

        /// <summary>
        /// ハッシュ値をベースに既存のサムネイルファイルを検索
        /// </summary>
        private string FindExistingThumbnailByHash(string hash)
        {
            string[] extensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

            foreach (string ext in extensions)
            {
                string filePath = Path.Combine(_thumbnailDirectory, $"{hash}{ext}");
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }
    }
}