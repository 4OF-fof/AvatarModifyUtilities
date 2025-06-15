using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AMU.AssetManager.Data;

namespace AMU.AssetManager.Helper
{
    public class AssetThumbnailManager
    {
        private Dictionary<string, Texture2D> _thumbnailCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, DateTime> _thumbnailFileModified = new Dictionary<string, DateTime>();
        private HashSet<string> _loadingThumbnails = new HashSet<string>();
        private Queue<string> _loadQueue = new Queue<string>();
        private string _thumbnailDirectory;
        private int _maxCacheSize = 200; // キャッシュサイズを制限

        public event Action<AssetInfo> OnThumbnailSaved;
        public event Action OnThumbnailLoaded;

        public AssetThumbnailManager()
        {
            string coreDir = EditorPrefs.GetString("Setting.Core_dirPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AvatarModifyUtilities"));
            _thumbnailDirectory = Path.Combine(coreDir, "AssetManager", "Thumbnails");
            EnsureThumbnailDirectory();
        }
        public Texture2D GetThumbnail(AssetInfo asset)
        {
            if (asset == null) return null;

            // キャッシュから取得
            if (_thumbnailCache.TryGetValue(asset.uid, out var cachedTexture) && cachedTexture != null)
            {
                return cachedTexture;
            }

            // 既に読み込み中の場合はデフォルトを返す
            if (_loadingThumbnails.Contains(asset.uid))
            {
                return GetDefaultThumbnail(asset.assetType);
            }

            // 非同期で読み込みを開始
            LoadThumbnailAsync(asset);

            return GetDefaultThumbnail(asset.assetType);
        }

        /// <summary>
        /// サムネイルを非同期で読み込む
        /// </summary>
        private async void LoadThumbnailAsync(AssetInfo asset)
        {
            if (_loadingThumbnails.Contains(asset.uid)) return;

            _loadingThumbnails.Add(asset.uid);

            try
            {
                // サムネイルファイルの変更をチェック
                CheckThumbnailFileChanges(asset);

                Texture2D texture = null;

                // サムネイルパスが存在する場合
                if (!string.IsNullOrEmpty(asset.thumbnailPath) && File.Exists(asset.thumbnailPath))
                {
                    texture = await LoadTextureFromFileAsync(asset.thumbnailPath);
                }

                if (texture != null)
                {
                    // メインスレッドでキャッシュに追加
                    EditorApplication.delayCall += () =>
                    {
                        AddToCache(asset.uid, texture);
                        UpdateThumbnailModifiedTime(asset);
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
        }

        /// <summary>
        /// テクスチャファイルを非同期で読み込む
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
                        // テクスチャ作成はメインスレッドで実行
                        Texture2D texture = null;
                        await Task.Run(() =>
                        {
                            EditorApplication.delayCall += () =>
                            {
                                texture = new Texture2D(2, 2);
                                if (!texture.LoadImage(fileData))
                                {
                                    UnityEngine.Object.DestroyImmediate(texture);
                                    texture = null;
                                }
                            };
                        });

                        return texture;
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
        }

        /// <summary>
        /// キャッシュにテクスチャを追加（サイズ制限付き）
        /// </summary>
        private void AddToCache(string assetUid, Texture2D texture)
        {
            // キャッシュサイズを制限
            if (_thumbnailCache.Count >= _maxCacheSize)
            {
                var oldestEntry = _thumbnailCache.First();
                if (oldestEntry.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldestEntry.Value);
                }
                _thumbnailCache.Remove(oldestEntry.Key);
            }

            _thumbnailCache[assetUid] = texture;
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
        }

        /// <summary>
        /// 特定のアセットのサムネイルキャッシュを無効化する
        /// </summary>
        private void InvalidateThumbnailCache(string assetUid)
        {
            if (_thumbnailCache.TryGetValue(assetUid, out var texture) && texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            _thumbnailCache.Remove(assetUid);
            _thumbnailFileModified.Remove(assetUid);
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

            var texture = LoadTextureFromFileSync(imagePath);
            if (texture != null)
            {
                // Save to thumbnail directory
                string thumbnailPath = Path.Combine(_thumbnailDirectory, $"{asset.uid}.png");
                SaveTextureToFile(texture, thumbnailPath);

                // Convert path to use forward slashes for JSON storage
                asset.thumbnailPath = thumbnailPath.Replace('\\', '/');

                // 古いキャッシュを無効化してから新しいテクスチャをキャッシュ
                InvalidateThumbnailCache(asset.uid);
                AddToCache(asset.uid, texture);
                UpdateThumbnailModifiedTime(asset);

                OnThumbnailLoaded?.Invoke();
                OnThumbnailSaved?.Invoke(asset);
            }
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
            _thumbnailFileModified.Clear();
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
            string thumbnailPath = Path.Combine(_thumbnailDirectory, $"{asset.uid}.png");
            SaveTextureToFile(texture, thumbnailPath);
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
        private Texture2D GetDefaultThumbnail(string assetType)
        {
            // Return default Unity icons based on asset type
            switch (assetType)
            {
                case "Avatar":
                case "Prefab":
                    return EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                case "Material":
                    return EditorGUIUtility.IconContent("Material Icon").image as Texture2D;
                case "Texture":
                    return EditorGUIUtility.IconContent("Texture Icon").image as Texture2D;
                case "Animation":
                    return EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
                case "Script":
                    return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
                default:
                    return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
            }
        }

        private void EnsureThumbnailDirectory()
        {
            if (!Directory.Exists(_thumbnailDirectory))
            {
                Directory.CreateDirectory(_thumbnailDirectory);
            }
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
                GUI.Box(rect, "No Image");
            }
        }
    }
}
