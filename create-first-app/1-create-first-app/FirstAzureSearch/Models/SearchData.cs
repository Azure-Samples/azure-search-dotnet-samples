using System.Collections;
using Microsoft.Azure.Search.Models;

namespace FirstAzureSearch.Models
{
    public static class GlobalVariables
    {
        public static int ResultsPerPage
        {
            get
            {
                return 3;
            }
        }
    }

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
