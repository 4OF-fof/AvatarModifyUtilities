using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// BoothのアイテムURL
    /// </summary>
    [Serializable]
    public struct BoothItemUrl : IEquatable<BoothItemUrl>
    {
        private readonly string _value;
        private static readonly Regex BoothUrlPattern = new Regex(
            @"^https?://(?:[\w\-]+\.)?booth\.pm/(?:ja/)?items/(\d+)(?:\?.*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public BoothItemUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _value = string.Empty;
            }
            else if (IsValidBoothUrl(url))
            {
                _value = NormalizeUrl(url);
            }
            else
            {
                throw new ArgumentException($"Invalid Booth URL format: {url}", nameof(url));
            }
        }

        public string Value => _value ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(_value);
        public bool IsValid => !IsEmpty && IsValidBoothUrl(_value);

        public string ItemId
        {
            get
            {
                if (IsEmpty) return string.Empty;
                var match = BoothUrlPattern.Match(_value);
                return match.Success ? match.Groups[1].Value : string.Empty;
            }
        }

        private static bool IsValidBoothUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return BoothUrlPattern.IsMatch(url);
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            // クエリパラメータを削除して正規化
            var match = BoothUrlPattern.Match(url);
            if (match.Success)
            {
                var itemId = match.Groups[1].Value;
                return $"https://booth.pm/items/{itemId}";
            }

            return url;
        }

        public bool Equals(BoothItemUrl other) => _value == other._value;
        public override bool Equals(object obj) => obj is BoothItemUrl other && Equals(other);
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(BoothItemUrl left, BoothItemUrl right) => left.Equals(right);
        public static bool operator !=(BoothItemUrl left, BoothItemUrl right) => !left.Equals(right);

        public static implicit operator string(BoothItemUrl url) => url.Value;
        public static explicit operator BoothItemUrl(string url) => new BoothItemUrl(url);

        public static BoothItemUrl Empty => new BoothItemUrl(string.Empty);
    }

    /// <summary>
    /// Boothアイテムの基本情報
    /// </summary>
    [Serializable]
    public class BoothItemSchema
    {
        [SerializeField] private BoothItemUrl _itemUrl;
        [SerializeField] private string _fileName;
        [SerializeField] private string _downloadUrl;
        [SerializeField] private string _itemTitle;
        [SerializeField] private string _authorName;
        [SerializeField] private string _itemDescription;
        [SerializeField] private string _price;
        [SerializeField] private string _imageUrl;
        [SerializeField] private DateTime _lastUpdated;

        public BoothItemUrl ItemUrl
        {
            get => _itemUrl;
            set => _itemUrl = value;
        }

        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => _fileName = value?.Trim() ?? string.Empty;
        }

        public string DownloadUrl
        {
            get => _downloadUrl ?? string.Empty;
            set => _downloadUrl = value?.Trim() ?? string.Empty;
        }

        public string ItemTitle
        {
            get => _itemTitle ?? string.Empty;
            set => _itemTitle = value?.Trim() ?? string.Empty;
        }

        public string AuthorName
        {
            get => _authorName ?? string.Empty;
            set => _authorName = value?.Trim() ?? string.Empty;
        }

        public string ItemDescription
        {
            get => _itemDescription ?? string.Empty;
            set => _itemDescription = value?.Trim() ?? string.Empty;
        }

        public string Price
        {
            get => _price ?? string.Empty;
            set => _price = value?.Trim() ?? string.Empty;
        }

        public string ImageUrl
        {
            get => _imageUrl ?? string.Empty;
            set => _imageUrl = value?.Trim() ?? string.Empty;
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            set => _lastUpdated = value;
        }

        public string ItemId => _itemUrl.ItemId;
        public bool HasData => !_itemUrl.IsEmpty || !string.IsNullOrEmpty(_fileName) || !string.IsNullOrEmpty(_downloadUrl);
        public bool IsComplete => !_itemUrl.IsEmpty && !string.IsNullOrEmpty(_fileName) && !string.IsNullOrEmpty(_downloadUrl);

        public BoothItemSchema()
        {
            _itemUrl = BoothItemUrl.Empty;
            _fileName = string.Empty;
            _downloadUrl = string.Empty;
            _itemTitle = string.Empty;
            _authorName = string.Empty;
            _itemDescription = string.Empty;
            _price = string.Empty;
            _imageUrl = string.Empty;
            _lastUpdated = DateTime.Now;
        }

        public BoothItemSchema(string itemUrl, string fileName, string downloadUrl) : this()
        {
            _itemUrl = new BoothItemUrl(itemUrl);
            _fileName = fileName?.Trim() ?? string.Empty;
            _downloadUrl = downloadUrl?.Trim() ?? string.Empty;
        }

        public BoothItemSchema Clone()
        {
            return new BoothItemSchema
            {
                _itemUrl = _itemUrl,
                _fileName = _fileName,
                _downloadUrl = _downloadUrl,
                _itemTitle = _itemTitle,
                _authorName = _authorName,
                _itemDescription = _itemDescription,
                _price = _price,
                _imageUrl = _imageUrl,
                _lastUpdated = _lastUpdated
            };
        }

        public void UpdateMetadata(string title, string author, string description, string price, string imageUrl)
        {
            _itemTitle = title?.Trim() ?? string.Empty;
            _authorName = author?.Trim() ?? string.Empty;
            _itemDescription = description?.Trim() ?? string.Empty;
            _price = price?.Trim() ?? string.Empty;
            _imageUrl = imageUrl?.Trim() ?? string.Empty;
            _lastUpdated = DateTime.Now;
        }
    }

    /// <summary>
    /// BPM（Booth Package Manager）のファイル情報
    /// </summary>
    [Serializable]
    public class BPMFileSchema
    {
        [SerializeField] private string _fileName;
        [SerializeField] private string _downloadLink;
        [SerializeField] private FileSize _fileSize;
        [SerializeField] private string _fileHash;
        [SerializeField] private DateTime _lastModified;

        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => _fileName = value?.Trim() ?? string.Empty;
        }

        public string DownloadLink
        {
            get => _downloadLink ?? string.Empty;
            set => _downloadLink = value?.Trim() ?? string.Empty;
        }

        public FileSize FileSize
        {
            get => _fileSize;
            set => _fileSize = value;
        }

        public string FileHash
        {
            get => _fileHash ?? string.Empty;
            set => _fileHash = value?.Trim() ?? string.Empty;
        }

        public DateTime LastModified
        {
            get => _lastModified == default ? DateTime.Now : _lastModified;
            set => _lastModified = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(_fileName) && !string.IsNullOrEmpty(_downloadLink);

        public BPMFileSchema()
        {
            _fileName = string.Empty;
            _downloadLink = string.Empty;
            _fileSize = new FileSize(0);
            _fileHash = string.Empty;
            _lastModified = DateTime.Now;
        }

        public BPMFileSchema(string fileName, string downloadLink) : this()
        {
            _fileName = fileName?.Trim() ?? string.Empty;
            _downloadLink = downloadLink?.Trim() ?? string.Empty;
        }
    }

    /// <summary>
    /// BPMパッケージのスキーマ
    /// </summary>
    [Serializable]
    public class BPMPackageSchema
    {
        [SerializeField] private string _packageName;
        [SerializeField] private BoothItemUrl _itemUrl;
        [SerializeField] private string _imageUrl;
        [SerializeField] private string _authorName;
        [SerializeField] private List<BPMFileSchema> _files;
        [SerializeField] private DateTime _lastUpdated;
        [SerializeField] private string _version;

        public string PackageName
        {
            get => _packageName ?? string.Empty;
            set => _packageName = value?.Trim() ?? string.Empty;
        }

        public BoothItemUrl ItemUrl
        {
            get => _itemUrl;
            set => _itemUrl = value;
        }

        public string ImageUrl
        {
            get => _imageUrl ?? string.Empty;
            set => _imageUrl = value?.Trim() ?? string.Empty;
        }

        public string AuthorName
        {
            get => _authorName ?? string.Empty;
            set => _authorName = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<BPMFileSchema> Files => _files ?? new List<BPMFileSchema>();

        public DateTime LastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            set => _lastUpdated = value;
        }

        public string Version
        {
            get => _version ?? string.Empty;
            set => _version = value?.Trim() ?? string.Empty;
        }

        public string ItemId => _itemUrl.ItemId;
        public int FileCount => _files?.Count ?? 0;
        public bool HasFiles => _files?.Count > 0;

        public BPMPackageSchema()
        {
            _packageName = string.Empty;
            _itemUrl = BoothItemUrl.Empty;
            _imageUrl = string.Empty;
            _authorName = string.Empty;
            _files = new List<BPMFileSchema>();
            _lastUpdated = DateTime.Now;
            _version = string.Empty;
        }

        public void AddFile(BPMFileSchema file)
        {
            if (file == null || !file.IsValid) return;

            _files ??= new List<BPMFileSchema>();
            _files.Add(file);
            _lastUpdated = DateTime.Now;
        }

        public void RemoveFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            _files?.RemoveAll(f => f.FileName.Equals(fileName.Trim(), StringComparison.OrdinalIgnoreCase));
            _lastUpdated = DateTime.Now;
        }

        public void ClearFiles()
        {
            _files?.Clear();
            _lastUpdated = DateTime.Now;
        }

        public BPMFileSchema GetFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            return _files?.Find(f => f.FileName.Equals(fileName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// BPMライブラリのスキーマ
    /// </summary>
    [Serializable]
    public class BPMLibrarySchema
    {
        [SerializeField] private Dictionary<string, List<BPMPackageSchema>> _authorPackages;
        [SerializeField] private DateTime _lastUpdated;
        [SerializeField] private string _version;

        public IReadOnlyDictionary<string, IReadOnlyList<BPMPackageSchema>> AuthorPackages
        {
            get
            {
                if (_authorPackages == null) return new Dictionary<string, IReadOnlyList<BPMPackageSchema>>();

                var result = new Dictionary<string, IReadOnlyList<BPMPackageSchema>>();
                foreach (var kvp in _authorPackages)
                {
                    result[kvp.Key] = kvp.Value ?? new List<BPMPackageSchema>();
                }
                return result;
            }
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated == default ? DateTime.Now : _lastUpdated;
            set => _lastUpdated = value;
        }

        public string Version
        {
            get => _version ?? string.Empty;
            set => _version = value?.Trim() ?? string.Empty;
        }

        public int AuthorCount => _authorPackages?.Count ?? 0;
        public int TotalPackageCount => _authorPackages?.Values.Sum(packages => packages?.Count ?? 0) ?? 0;

        public BPMLibrarySchema()
        {
            _authorPackages = new Dictionary<string, List<BPMPackageSchema>>();
            _lastUpdated = DateTime.Now;
            _version = "1.0";
        }

        public void AddPackage(string authorName, BPMPackageSchema package)
        {
            if (string.IsNullOrWhiteSpace(authorName) || package == null) return;

            _authorPackages ??= new Dictionary<string, List<BPMPackageSchema>>();

            var author = authorName.Trim();
            if (!_authorPackages.ContainsKey(author))
            {
                _authorPackages[author] = new List<BPMPackageSchema>();
            }

            _authorPackages[author].Add(package);
            _lastUpdated = DateTime.Now;
        }

        public void RemovePackage(string authorName, string packageName)
        {
            if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(packageName)) return;

            var author = authorName.Trim();
            if (_authorPackages?.ContainsKey(author) == true)
            {
                _authorPackages[author].RemoveAll(p => p.PackageName.Equals(packageName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (_authorPackages[author].Count == 0)
                {
                    _authorPackages.Remove(author);
                }

                _lastUpdated = DateTime.Now;
            }
        }

        public BPMPackageSchema GetPackage(string authorName, string packageName)
        {
            if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(packageName)) return null;

            var author = authorName.Trim();
            if (_authorPackages?.ContainsKey(author) == true)
            {
                return _authorPackages[author].Find(p => p.PackageName.Equals(packageName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        public IEnumerable<BPMPackageSchema> GetAllPackages()
        {
            if (_authorPackages == null) yield break;

            foreach (var packages in _authorPackages.Values)
            {
                if (packages != null)
                {
                    foreach (var package in packages)
                    {
                        yield return package;
                    }
                }
            }
        }

        public IEnumerable<string> GetAuthors()
        {
            return _authorPackages?.Keys ?? Enumerable.Empty<string>();
        }
    }
}
