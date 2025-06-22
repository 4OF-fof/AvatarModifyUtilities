using System.Collections.Generic;

namespace AMU.Editor.VrcAssetManager.Schema
{
    #region FilterOptions
    public class FilterOptions
    {
        public bool filterAnd;
        public string name;
        public string authorName;
        public string description;
        public string assetType;
        public List<string> tags;
        public bool tagsAnd;
        public bool? isFavorite;
        public bool isArchived;
        public bool isChildItem;

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
            isArchived = false;
            isChildItem = false;
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
            isArchived = false;
            isChildItem = false;
        }
    }
    #endregion

    #region SortOptions
    public class SortOptions
    {
        public SortOptionsEnum sortBy;
        public bool isDescending;

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