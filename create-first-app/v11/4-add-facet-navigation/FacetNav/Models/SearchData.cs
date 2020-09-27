using Azure.Search.Documents.Models;

namespace FacetNav.Models
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
        public static int MaxPageRange
        {
            get
            {
                return 5;
            }
        }

        public static int PageRangeDelta
        {
            get
            {
                return 2;
            }
        }
    }
    public class SearchData
    {
        // The text to search for.
        public string searchText { get; set; }

        // The current page being displayed.
        public int currentPage { get; set; }

        // The total number of pages of results.
        public int pageCount { get; set; }

        // The left-most page number to display.
        public int leftMostPage { get; set; }

        // The number of page numbers to display - which can be less than MaxPageRange towards the end of the results.
        public int pageRange { get; set; }

        // Used when page numbers, or next or prev buttons, have been selected.
        public string paging { get; set; }

        // Property, and text of a facet (such as "Budget"). Used to communicate this text to the controller.
        public string categoryFilter { get; set; }
        public string amenityFilter { get; set; }

        // The list of results.
        public SearchResults<Hotel> resultList;
    }
}
