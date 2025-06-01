using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AMU.BoothPackageManager.Helper
{
    public class BPMImageManager
    {
        private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, string> imagePathCache = new Dictionary<string, string>();
        private bool thumbnailDirectoryChecked = false;

        public event Action OnImageLoaded;

        public Texture2D GetCachedImage(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            return imageCache.TryGetValue(url, out var cached) ? cached : null;
        }

        public void UpdateImagePathCache()
        {
            imagePathCache.Clear();

            string thumbnailDir = BPMPathManager.GetThumbnailDirectory();
            if (!Directory.Exists(thumbnailDir)) return;

            try
            {
                var allFiles = Directory.GetFiles(thumbnailDir, "*", SearchOption.TopDirectoryOnly);
                string[] extensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

                foreach (var filePath in allFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string extension = Path.GetExtension(filePath);

                    if (extensions.Contains(extension.ToLower()))
                    {
                        imagePathCache[fileName] = filePath;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"画像パスキャッシュの更新に失敗: {ex.Message}");
            }
        }

        private string GetCachedImagePath(string imageHash)
        {
            return imagePathCache.TryGetValue(imageHash, out string path) ? path : null;
        }

        private void EnsureThumbnailDirectory()
        {
            if (thumbnailDirectoryChecked) return;

            string thumbnailDir = BPMPathManager.GetThumbnailDirectory();
            if (!Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }
            thumbnailDirectoryChecked = true;
        }

        public async void LoadImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url) || imageCache.ContainsKey(url)) return;

            imageCache[url] = null;

            try
            {
                EnsureThumbnailDirectory();

                string imageHash = BPMPathManager.GetImageHash(url);
                string localImagePath = GetCachedImagePath(imageHash);

                Texture2D tex = null;

                if (!string.IsNullOrEmpty(localImagePath))
                {
                    byte[] fileBytes = await Task.Run(() => File.ReadAllBytes(localImagePath));
                    tex = new Texture2D(2, 2);
                    if (!tex.LoadImage(fileBytes))
                    {
                        UnityEngine.Object.DestroyImmediate(tex);
                        tex = null;
                    }
                }
                else
                {
                    using (var httpClient = new HttpClient())
                    {
                        byte[] bytes = await httpClient.GetByteArrayAsync(url);

                        tex = new Texture2D(2, 2);
                        if (tex.LoadImage(bytes))
                        {
                            string extension = ".png";
                            string urlLower = url.ToLower();
                            if (urlLower.Contains(".jpg") || urlLower.Contains(".jpeg"))
                                extension = ".jpg";
                            else if (urlLower.Contains(".gif"))
                                extension = ".gif";
                            else if (urlLower.Contains(".bmp"))
                                extension = ".bmp";

                            string thumbnailDir = BPMPathManager.GetThumbnailDirectory();
                            string saveImagePath = Path.Combine(thumbnailDir, imageHash + extension);
                            await File.WriteAllBytesAsync(saveImagePath, bytes);

                            // 画像パスキャッシュを更新
                            imagePathCache[imageHash] = saveImagePath;
                        }
                        else
                        {
                            UnityEngine.Object.DestroyImmediate(tex);
                            tex = null;
                        }
                    }
                }

                EditorApplication.delayCall += () =>
                {
                    imageCache[url] = tex;
                    OnImageLoaded?.Invoke();
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

        public void ClearCaches()
        {
            foreach (var kvp in imageCache)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(kvp.Value);
                }
            }
            imageCache.Clear();
            imagePathCache.Clear();
            thumbnailDirectoryChecked = false;
        }
    }
}
