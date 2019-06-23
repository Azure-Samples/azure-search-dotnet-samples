using Microsoft.Azure.Search.Models;
using System.Collections;

namespace OrderResults.Models
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
        // Calculate the cheapest and most expensive room, for each hotel in the results list.
        public void CalcRateRange()
        {
            for (int n = 0; n < resultList.Results.Count; n++)
            {
                // Calculate room rates.
                var cheapest = 10000d;
                var expensive = 0d;

                for (var r = 0; r < resultList.Results[n].Document.Rooms.Length; r++)
                {
                    var rate = resultList.Results[n].Document.Rooms[r].BaseRate;
                    if (rate < cheapest)
                    {
                        cheapest = (double)rate;
                    }
                    if (rate > expensive)
                    {
                        expensive = (double)rate;
                    }
                }
                resultList.Results[n].Document.cheapest = cheapest;
                resultList.Results[n].Document.expensive = expensive;
            }
        }

        // The text to search for.
        public string searchText { get; set; }

        // Record if the next page is requested.
        public string paging { get; set; }

        // The list of results.
        public DocumentSearchResult<Hotel> resultList;
    }
}
