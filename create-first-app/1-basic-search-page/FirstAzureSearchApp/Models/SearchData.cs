using Microsoft.Azure.Search.Models;

namespace FirstAzureSearchApp.Models
{
    public class SearchData
    {
        // The text to search for.
        public string searchText { get; set; }

        // The list of results.
        public DocumentSearchResult<Hotel> resultList;
    }
}
