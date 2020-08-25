using Azure.Search.Documents.Models;
using System.Collections.Generic;

namespace FirstAzureSearchApp.Models
{
    public class SearchData
    {
        // The text to search for.
        public string searchText { get; set; }

        // The list of results.
        public List<SearchResult<Hotel>> resultList; 
    }
}
