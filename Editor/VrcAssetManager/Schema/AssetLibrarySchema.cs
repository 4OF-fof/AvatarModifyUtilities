using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// アセットライブラリのバージョン管理
    /// </summary>
    [Serializable]
    public struct LibraryVersion : IComparable<LibraryVersion>
    {
        [SerializeField] private int _major;
        [SerializeField] private int _minor;
        [SerializeField] private int _patch;

        public int Major => _major;
        public int Minor => _minor;
        public int Patch => _patch;

        public LibraryVersion(int major, int minor, int patch)
        {
            _major = Math.Max(0, major);
            _minor = Math.Max(0, minor);
            _patch = Math.Max(0, patch);
        }

        public static LibraryVersion Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return new LibraryVersion(1, 0, 0);

            var parts = version.Split('.');
            var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 1;
            var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
            var patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0;

            return new LibraryVersion(major, minor, patch);
        }

        public override string ToString() => $"{_major}.{_minor}.{_patch}";

        public int CompareTo(LibraryVersion other)
        {
            var majorComparison = _major.CompareTo(other._major);
            if (majorComparison != 0) return majorComparison;

            var minorComparison = _minor.CompareTo(other._minor);
            if (minorComparison != 0) return minorComparison;

            return _patch.CompareTo(other._patch);
        }

        public static bool operator >(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) > 0;
        public static bool operator <(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) < 0;
        public static bool operator >=(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) >= 0;
        public static bool operator <=(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) <= 0;
        public static bool operator ==(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) == 0;
        public static bool operator !=(LibraryVersion left, LibraryVersion right) => left.CompareTo(right) != 0;

        public override bool Equals(object obj) => obj is LibraryVersion other && CompareTo(other) == 0;
        public override int GetHashCode() => (_major << 16) | (_minor << 8) | _patch;

        public static LibraryVersion Current => new LibraryVersion(1, 0, 0);
    }

    /// <summary>
    /// アセットライブラリの完全なスキーマ
    /// </summary>
    [Serializable]
    public class AssetLibrarySchema
    {
        [SerializeField] private LibraryVersion _version;
        [SerializeField] private DateTime _lastUpdated;
        [SerializeField] private Dictionary<string, AssetSchema> _assets;
        [SerializeField] private Dictionary<string, AssetGroupSchema> _groups;

        public LibraryVersion Version
        {
            get => _version;
            set => _version = value;
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            set => _lastUpdated = value;
        }

        public IReadOnlyDictionary<AssetId, AssetSchema> Assets
        {
            get
            {
                if (_assets == null) return new Dictionary<AssetId, AssetSchema>();

                var result = new Dictionary<AssetId, AssetSchema>();
                foreach (var kvp in _assets)
                {
                    if (AssetId.TryParse(kvp.Key, out var assetId))
                    {
                        result[assetId] = kvp.Value;
                    }
                }
                return result;
            }
        }

        public IReadOnlyDictionary<string, AssetGroupSchema> Groups
        {
            get
            {
                if (_groups == null) return new Dictionary<string, AssetGroupSchema>();
                return _groups;
            }
        }

        public int AssetCount => _assets?.Count ?? 0;
        public int GroupCount => _groups?.Count ?? 0;
        public bool HasAssets => AssetCount > 0;

        public AssetLibrarySchema()
        {
            _version = LibraryVersion.Current;
            _lastUpdated = DateTime.Now;
            _assets = new Dictionary<string, AssetSchema>();
            _groups = new Dictionary<string, AssetGroupSchema>();
        }
        public bool AddAsset(AssetId assetId, AssetSchema asset)
        {
            if (string.IsNullOrEmpty(assetId.Value) || asset == null) return false;

            _assets ??= new Dictionary<string, AssetSchema>();
            _assets[assetId.Value] = asset;
            _lastUpdated = DateTime.Now;

            return true;
        }

        public bool RemoveAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return false;

            var removed = _assets?.Remove(assetId.Value) ?? false;
            if (removed)
            {
                _lastUpdated = DateTime.Now;
                _groups?.Remove(assetId.Value);
            }

            return removed;
        }

        public AssetSchema GetAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return null;
            return _assets?.GetValueOrDefault(assetId.Value);
        }

        public bool ContainsAsset(AssetId assetId)
        {
            if (string.IsNullOrEmpty(assetId.Value)) return false;
            return _assets?.ContainsKey(assetId.Value) ?? false;
        }
        public void AddGroup(string groupId, AssetGroupSchema group)
        {
            if (string.IsNullOrEmpty(groupId) || group == null) return;

            _groups ??= new Dictionary<string, AssetGroupSchema>();
            _groups[groupId] = group;
            _lastUpdated = DateTime.Now;
        }

        public void RemoveGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return;

            if (_groups?.Remove(groupId) == true)
            {
                _lastUpdated = DateTime.Now;
            }
        }

        public AssetGroupSchema GetGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;
            return _groups?.GetValueOrDefault(groupId);
        }

        public IEnumerable<AssetSchema> GetVisibleAssets()
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => !asset.State.IsArchived);
        }

        public IEnumerable<AssetSchema> GetFavoriteAssets()
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => asset.State.IsFavorite);
        }
        public IEnumerable<AssetSchema> GetAssetsByType(AssetType assetType)
        {
            if (_assets == null) return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset => asset.Metadata.AssetType == assetType);
        }

        public IEnumerable<AssetSchema> GetAssetsByAuthor(string authorName)
        {
            if (_assets == null || string.IsNullOrWhiteSpace(authorName))
                return Enumerable.Empty<AssetSchema>();

            return _assets.Values.Where(asset =>
                asset.Metadata.AuthorName.Equals(authorName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public void ClearAssets()
        {
            _assets?.Clear();
            _groups?.Clear();
            _lastUpdated = DateTime.Now;
        }

        public void Optimize()
        {
            // 無効なアセット参照をクリーンアップ
            if (_groups != null)
            {
                var validAssetIds = new HashSet<string>(_assets?.Keys ?? Enumerable.Empty<string>());
                var invalidGroups = _groups.Where(kvp => !validAssetIds.Contains(kvp.Key)).ToList();

                foreach (var invalidGroup in invalidGroups)
                {
                    _groups.Remove(invalidGroup.Key);
                }
            }

            _lastUpdated = DateTime.Now;
        }
    }
}
