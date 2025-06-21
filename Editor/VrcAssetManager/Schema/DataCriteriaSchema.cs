using System.Collections.Generic;

namespace AMU.Editor.VrcAssetManager.Schema
{
    #region FilterOptions
    public class FilterOptions
    {
        public bool filterAnd { get; set; }
        public string name { get; set; }
        public string authorName { get; set; }
        public string description { get; set; }
        public string assetType { get; set; }
        public List<string> tags { get; set; }
        public bool tagsAnd { get; set; }
        public bool? isFavorite { get; set; }
        public bool? isArchived { get; set; }
        public bool isUnCategorized { get; set; }

        public FilterOptions()
        {
            filterAnd = false;
            name = string.Empty;
            authorName = string.Empty;
            description = string.Empty;
            assetType = string.Empty;
            tags = new List<string>();
            tagsAnd = false;
            isFavorite = null;
            isArchived = null;
            isUnCategorized = false;
        }

        public void ClearFilter()
        {
            filterAnd = false;
            name = string.Empty;
            authorName = string.Empty;
            description = string.Empty;
            assetType = string.Empty;
            tags = new List<string>();
            tagsAnd = false;
            isFavorite = null;
            isArchived = null;
            isUnCategorized = false;
        }
    }
    #endregion

    #region SortOptions
    public class SortOptions
    {
        public SortOptionsEnum sortBy { get; set; }
        public bool isDescending { get; set; }

        public SortOptions()
        {
            sortBy = SortOptionsEnum.Name;
            isDescending = true;
        }
    }

    public enum SortOptionsEnum
    {
        Name,
        Date
    }
    #endregion
}