using Azure.Search.Documents.Models;

namespace FirstAzureSearchApp.Models
{
    public class SearchData
    {
        // The text to search for.
        public string searchText { get; set; }

        // The list of results.
        public SearchResults<Hotel> resultList;
    }
}
