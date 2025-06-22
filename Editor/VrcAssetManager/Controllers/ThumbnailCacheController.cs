using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    public class ThumbnailCacheController
    {
        private readonly Dictionary<string, Texture2D> _cache = new();

        public Texture2D Load(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_cache.TryGetValue(path, out var tex) && tex != null)
                return tex;
            if (!File.Exists(path)) return null;
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                _cache[path] = texture;
                return texture;
            }
            Object.DestroyImmediate(texture);
            return null;
        }

        public void Clear() => _cache.Clear();
    }
}
