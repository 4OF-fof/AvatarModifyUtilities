using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMU.Editor.VrcAssetManager.Schema
{
    /// <summary>
    /// 検索フィールドの種類
    /// </summary>
    [Flags]
    public enum SearchFields
    {
        None = 0,
        Name = 1 << 0,
        Description = 1 << 1,
        Author = 1 << 2,
        Tags = 1 << 3,
        FilePath = 1 << 4,
        AssetType = 1 << 5,
        All = Name | Description | Author | Tags | FilePath | AssetType
    }

    /// <summary>
    /// ソート順序
    /// </summary>
    public enum SortOrder
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// ソート基準
    /// </summary>
    public enum SortCriteria
    {
        Name,
        CreatedDate,
        ModifiedDate,
        FileSize,
        Author,
        AssetType,
        Relevance
    }

    /// <summary>
    /// 論理演算子
    /// </summary>
    public enum LogicalOperator
    {
        And,
        Or
    }

    /// <summary>
    /// 日付範囲の指定
    /// </summary>
    [Serializable]
    public struct DateRange
    {
        [SerializeField] private DateTime _startDate;
        [SerializeField] private DateTime _endDate;
        [SerializeField] private bool _isEnabled;

        public DateTime StartDate
        {
            get => _startDate;
            set => _startDate = value;
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => _endDate = value;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public bool IsValid => _startDate <= _endDate;

        public DateRange(DateTime startDate, DateTime endDate, bool isEnabled = true)
        {
            _startDate = startDate;
            _endDate = endDate;
            _isEnabled = isEnabled && startDate <= endDate;
        }

        public bool Contains(DateTime date)
        {
            return _isEnabled && date >= _startDate && date <= _endDate;
        }

        public static DateRange Disabled => new DateRange(DateTime.MinValue, DateTime.MaxValue, false);
    }

    /// <summary>
    /// ファイルサイズ範囲の指定
    /// </summary>
    [Serializable]
    public struct FileSizeRange
    {
        [SerializeField] private FileSize _minSize;
        [SerializeField] private FileSize _maxSize;
        [SerializeField] private bool _isEnabled;

        public FileSize MinSize
        {
            get => _minSize;
            set => _minSize = value;
        }

        public FileSize MaxSize
        {
            get => _maxSize;
            set => _maxSize = value;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public bool IsValid => _minSize <= _maxSize;

        public FileSizeRange(FileSize minSize, FileSize maxSize, bool isEnabled = true)
        {
            _minSize = minSize;
            _maxSize = maxSize;
            _isEnabled = isEnabled && minSize <= maxSize;
        }

        public bool Contains(FileSize fileSize)
        {
            return _isEnabled && fileSize >= _minSize && fileSize <= _maxSize;
        }

        public static FileSizeRange Disabled => new FileSizeRange(new FileSize(0), new FileSize(long.MaxValue), false);
    }

    /// <summary>
    /// 基本的な検索条件
    /// </summary>
    [Serializable]
    public class BasicSearchCriteria
    {
        [SerializeField] private string _query;
        [SerializeField] private SearchFields _searchFields;
        [SerializeField] private bool _caseSensitive;
        [SerializeField] private bool _useRegex;

        public string Query
        {
            get => _query ?? string.Empty;
            set => _query = value?.Trim() ?? string.Empty;
        }

        public SearchFields SearchFields
        {
            get => _searchFields;
            set => _searchFields = value;
        }

        public bool CaseSensitive
        {
            get => _caseSensitive;
            set => _caseSensitive = value;
        }

        public bool UseRegex
        {
            get => _useRegex;
            set => _useRegex = value;
        }

        public bool HasQuery => !string.IsNullOrWhiteSpace(_query);

        public BasicSearchCriteria()
        {
            _query = string.Empty;
            _searchFields = SearchFields.All;
            _caseSensitive = false;
            _useRegex = false;
        }

        public BasicSearchCriteria(string query, SearchFields searchFields = SearchFields.All) : this()
        {
            _query = query?.Trim() ?? string.Empty;
            _searchFields = searchFields;
        }
    }

    /// <summary>
    /// 高度な検索条件
    /// </summary>
    [Serializable]
    public class AdvancedSearchCriteria
    {
        [SerializeField] private string _nameQuery;
        [SerializeField] private string _descriptionQuery;
        [SerializeField] private string _authorQuery;
        [SerializeField] private List<string> _selectedTags;
        [SerializeField] private List<AssetType> _selectedAssetTypes;
        [SerializeField] private LogicalOperator _tagLogicOperator;
        [SerializeField] private LogicalOperator _assetTypeLogicOperator;
        [SerializeField] private DateRange _createdDateRange;
        [SerializeField] private DateRange _modifiedDateRange;
        [SerializeField] private FileSizeRange _fileSizeRange;
        [SerializeField] private bool _isFavoriteOnly;
        [SerializeField] private bool _excludeGroups;
        [SerializeField] private bool _caseSensitive;

        public string NameQuery
        {
            get => _nameQuery ?? string.Empty;
            set => _nameQuery = value?.Trim() ?? string.Empty;
        }

        public string DescriptionQuery
        {
            get => _descriptionQuery ?? string.Empty;
            set => _descriptionQuery = value?.Trim() ?? string.Empty;
        }

        public string AuthorQuery
        {
            get => _authorQuery ?? string.Empty;
            set => _authorQuery = value?.Trim() ?? string.Empty;
        }

        public IReadOnlyList<string> SelectedTags => _selectedTags ?? new List<string>();
        public IReadOnlyList<AssetType> SelectedAssetTypes => _selectedAssetTypes ?? new List<AssetType>();

        public LogicalOperator TagLogicOperator
        {
            get => _tagLogicOperator;
            set => _tagLogicOperator = value;
        }

        public LogicalOperator AssetTypeLogicOperator
        {
            get => _assetTypeLogicOperator;
            set => _assetTypeLogicOperator = value;
        }

        public DateRange CreatedDateRange
        {
            get => _createdDateRange;
            set => _createdDateRange = value;
        }

        public DateRange ModifiedDateRange
        {
            get => _modifiedDateRange;
            set => _modifiedDateRange = value;
        }

        public FileSizeRange FileSizeRange
        {
            get => _fileSizeRange;
            set => _fileSizeRange = value;
        }

        public bool IsFavoriteOnly
        {
            get => _isFavoriteOnly;
            set => _isFavoriteOnly = value;
        }

        public bool ExcludeGroups
        {
            get => _excludeGroups;
            set => _excludeGroups = value;
        }

        public bool CaseSensitive
        {
            get => _caseSensitive;
            set => _caseSensitive = value;
        }

        public AdvancedSearchCriteria()
        {
            _nameQuery = string.Empty;
            _descriptionQuery = string.Empty;
            _authorQuery = string.Empty;
            _selectedTags = new List<string>();
            _selectedAssetTypes = new List<AssetType>();
            _tagLogicOperator = LogicalOperator.And;
            _assetTypeLogicOperator = LogicalOperator.Or;
            _createdDateRange = DateRange.Disabled;
            _modifiedDateRange = DateRange.Disabled;
            _fileSizeRange = FileSizeRange.Disabled;
            _isFavoriteOnly = false;
            _excludeGroups = true;
            _caseSensitive = false;
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            _selectedTags ??= new List<string>();
            var trimmedTag = tag.Trim();
            if (!_selectedTags.Contains(trimmedTag))
            {
                _selectedTags.Add(trimmedTag);
            }
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            _selectedTags?.Remove(tag.Trim());
        }

        public void ClearTags()
        {
            _selectedTags?.Clear();
        }

        public void AddAssetType(AssetType assetType)
        {
            _selectedAssetTypes ??= new List<AssetType>();
            if (!_selectedAssetTypes.Contains(assetType))
            {
                _selectedAssetTypes.Add(assetType);
            }
        }

        public void RemoveAssetType(AssetType assetType)
        {
            _selectedAssetTypes?.Remove(assetType);
        }

        public void ClearAssetTypes()
        {
            _selectedAssetTypes?.Clear();
        }

        public bool HasCriteria()
        {
            return !string.IsNullOrWhiteSpace(_nameQuery) ||
                   !string.IsNullOrWhiteSpace(_descriptionQuery) ||
                   !string.IsNullOrWhiteSpace(_authorQuery) ||
                   (_selectedTags?.Count > 0) ||
                   (_selectedAssetTypes?.Count > 0) ||
                   _createdDateRange.IsEnabled ||
                   _modifiedDateRange.IsEnabled ||
                   _fileSizeRange.IsEnabled ||
                   _isFavoriteOnly;
        }

        public AdvancedSearchCriteria Clone()
        {
            return new AdvancedSearchCriteria
            {
                _nameQuery = _nameQuery,
                _descriptionQuery = _descriptionQuery,
                _authorQuery = _authorQuery,
                _selectedTags = _selectedTags != null ? new List<string>(_selectedTags) : new List<string>(),
                _selectedAssetTypes = _selectedAssetTypes != null ? new List<AssetType>(_selectedAssetTypes) : new List<AssetType>(),
                _tagLogicOperator = _tagLogicOperator,
                _assetTypeLogicOperator = _assetTypeLogicOperator,
                _createdDateRange = _createdDateRange,
                _modifiedDateRange = _modifiedDateRange,
                _fileSizeRange = _fileSizeRange,
                _isFavoriteOnly = _isFavoriteOnly,
                _excludeGroups = _excludeGroups,
                _caseSensitive = _caseSensitive
            };
        }
    }

    /// <summary>
    /// ソート設定
    /// </summary>
    [Serializable]
    public class SortSettings
    {
        [SerializeField] private SortCriteria _primaryCriteria;
        [SerializeField] private SortOrder _primaryOrder;
        [SerializeField] private SortCriteria _secondaryCriteria;
        [SerializeField] private SortOrder _secondaryOrder;
        [SerializeField] private bool _useSecondarySort;

        public SortCriteria PrimaryCriteria
        {
            get => _primaryCriteria;
            set => _primaryCriteria = value;
        }

        public SortOrder PrimaryOrder
        {
            get => _primaryOrder;
            set => _primaryOrder = value;
        }

        public SortCriteria SecondaryCriteria
        {
            get => _secondaryCriteria;
            set => _secondaryCriteria = value;
        }

        public SortOrder SecondaryOrder
        {
            get => _secondaryOrder;
            set => _secondaryOrder = value;
        }

        public bool UseSecondarySort
        {
            get => _useSecondarySort;
            set => _useSecondarySort = value;
        }

        public SortSettings()
        {
            _primaryCriteria = SortCriteria.Name;
            _primaryOrder = SortOrder.Ascending;
            _secondaryCriteria = SortCriteria.CreatedDate;
            _secondaryOrder = SortOrder.Descending;
            _useSecondarySort = false;
        }

        public SortSettings(SortCriteria criteria, SortOrder order) : this()
        {
            _primaryCriteria = criteria;
            _primaryOrder = order;
        }
    }

    /// <summary>
    /// 検索結果の情報
    /// </summary>
    [Serializable]
    public class SearchResult
    {
        [SerializeField] private List<AssetId> _assetIds;
        [SerializeField] private int _totalCount;
        [SerializeField] private float _searchTime;
        [SerializeField] private DateTime _searchTimestamp;
        [SerializeField] private string _searchQuery;

        public IReadOnlyList<AssetId> AssetIds => _assetIds ?? new List<AssetId>();

        public int TotalCount
        {
            get => _totalCount;
            set => _totalCount = Math.Max(0, value);
        }

        public float SearchTime
        {
            get => _searchTime;
            set => _searchTime = Math.Max(0f, value);
        }

        public DateTime SearchTimestamp
        {
            get => _searchTimestamp;
            set => _searchTimestamp = value;
        }

        public string SearchQuery
        {
            get => _searchQuery ?? string.Empty;
            set => _searchQuery = value?.Trim() ?? string.Empty;
        }

        public int ResultCount => _assetIds?.Count ?? 0;
        public bool HasResults => ResultCount > 0;

        public SearchResult()
        {
            _assetIds = new List<AssetId>();
            _totalCount = 0;
            _searchTime = 0f;
            _searchTimestamp = DateTime.Now;
            _searchQuery = string.Empty;
        }

        public SearchResult(IEnumerable<AssetId> assetIds, string query) : this()
        {
            _assetIds = assetIds?.ToList() ?? new List<AssetId>();
            _totalCount = _assetIds.Count;
            _searchQuery = query?.Trim() ?? string.Empty;
        }

        public void SetAssets(IEnumerable<AssetId> assetIds)
        {
            _assetIds = assetIds?.ToList() ?? new List<AssetId>();
            _totalCount = _assetIds.Count;
        }

        public void AddAsset(AssetId assetId)
        {
            _assetIds ??= new List<AssetId>();
            if (!_assetIds.Contains(assetId))
            {
                _assetIds.Add(assetId);
                _totalCount = _assetIds.Count;
            }
        }

        public void RemoveAsset(AssetId assetId)
        {
            if (_assetIds?.Remove(assetId) == true)
            {
                _totalCount = _assetIds.Count;
            }
        }

        public bool ContainsAsset(AssetId assetId)
        {
            return _assetIds?.Contains(assetId) ?? false;
        }
    }

    /// <summary>
    /// 検索履歴の項目
    /// </summary>
    [Serializable]
    public class SearchHistoryItem
    {
        [SerializeField] private string _query;
        [SerializeField] private SearchFields _searchFields;
        [SerializeField] private DateTime _timestamp;
        [SerializeField] private int _resultCount;

        public string Query
        {
            get => _query ?? string.Empty;
            set => _query = value?.Trim() ?? string.Empty;
        }

        public SearchFields SearchFields
        {
            get => _searchFields;
            set => _searchFields = value;
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => _timestamp = value;
        }

        public int ResultCount
        {
            get => _resultCount;
            set => _resultCount = Math.Max(0, value);
        }

        public SearchHistoryItem()
        {
            _query = string.Empty;
            _searchFields = SearchFields.All;
            _timestamp = DateTime.Now;
            _resultCount = 0;
        }

        public SearchHistoryItem(string query, SearchFields searchFields, int resultCount) : this()
        {
            _query = query?.Trim() ?? string.Empty;
            _searchFields = searchFields;
            _resultCount = Math.Max(0, resultCount);
        }
    }
}
