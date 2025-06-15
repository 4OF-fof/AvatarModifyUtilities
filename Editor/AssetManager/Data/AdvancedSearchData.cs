using System;
using System.Collections.Generic;
using UnityEngine;

namespace AMU.AssetManager.Data
{
    [Serializable]
    public class AdvancedSearchCriteria
    {
        public string nameQuery = "";
        public string descriptionQuery = "";
        public string authorQuery = "";
        public List<string> selectedTags = new List<string>();
        public bool searchInName = true;
        public bool searchInDescription = true;
        public bool searchInAuthor = true;
        public bool searchInTags = true;
        public bool useAndLogicForTags = true; // true: AND, false: OR

        public AdvancedSearchCriteria()
        {
        }

        public AdvancedSearchCriteria Clone()
        {
            return new AdvancedSearchCriteria
            {
                nameQuery = this.nameQuery,
                descriptionQuery = this.descriptionQuery,
                authorQuery = this.authorQuery,
                selectedTags = new List<string>(this.selectedTags),
                searchInName = this.searchInName,
                searchInDescription = this.searchInDescription,
                searchInAuthor = this.searchInAuthor,
                searchInTags = this.searchInTags,
                useAndLogicForTags = this.useAndLogicForTags
            };
        }

        public bool HasCriteria()
        {
            return !string.IsNullOrEmpty(nameQuery) ||
                   !string.IsNullOrEmpty(descriptionQuery) ||
                   !string.IsNullOrEmpty(authorQuery) ||
                   (selectedTags != null && selectedTags.Count > 0);
        }
    }
}
