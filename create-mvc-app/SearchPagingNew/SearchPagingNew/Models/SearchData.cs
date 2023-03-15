using Azure.Search.Documents.Models;

namespace SearchPagingNew.Models
{
    public class SearchData
    {
        public string searchText { get; set; }

        public SearchResults<Hotel> resultList;
    }
}
