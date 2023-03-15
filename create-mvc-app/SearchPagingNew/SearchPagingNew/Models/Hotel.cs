using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace SearchPagingNew.Models
{
    public class Hotel
    {
        [SimpleField(IsFilterable = true, IsKey = true)]
        public string HotelId { get; set; }

        [SearchableField(IsSortable = true)]
        public string HotelName { get; set; }

        [SearchableField(AnalyzerName =LexicalAnalyzerName.Values.EnLucene)]
        public string HotelDescription { get; set; }
    }
}
