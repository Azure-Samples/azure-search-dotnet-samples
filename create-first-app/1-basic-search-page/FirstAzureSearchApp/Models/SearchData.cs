using Microsoft.Azure.Search.Models;

namespace FirstAzureSearchApp.Models
{
    public class SearchData
    {
        public SearchData()
        {
        }

        [System.ComponentModel.DataAnnotations.Key]

        // The text to search for in the hotels data.
        public string searchText { get; set; }

        public DocumentSearchResult<Hotel> resultList;
    }
}
