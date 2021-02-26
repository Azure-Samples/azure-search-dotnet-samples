using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using FacetNav.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FacetNav.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(SearchData model)
        {
            try
            {
                // Ensure the search string is valid.
                if (model.searchText == null)
                {
                    model.searchText = "";
                }

                // Make the search call for the first page.
                await RunQueryAsync(model, 0, 0, "", "").ConfigureAwait(false);
            }
            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "1" });
            }

            return View(model);
        }

        public async Task<ActionResult> FacetAsync(SearchData model)
        {
            try
            {
                // Filters set by the model override those stored in temporary data.
                string catFilter;
                string ameFilter;
                if (model.categoryFilter != null)
                {
                    catFilter = model.categoryFilter;
                } else
                {
                    catFilter = TempData["categoryFilter"].ToString();
                }

                if (model.amenityFilter != null)
                {
                    ameFilter = model.amenityFilter;
                } else
                {
                    ameFilter = TempData["amenityFilter"].ToString();
                }

                // Recover the search text.
                model.searchText = TempData["searchfor"].ToString();

                // Initiate a new search.
                await RunQueryAsync(model, 0, 0, catFilter, ameFilter).ConfigureAwait(false);
            }
            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "2" });
            }

            return View("Index", model);
        }

        public async Task<ActionResult> PageAsync(SearchData model)
        {
            try
            {
                int page;

                // Calculate the page that should be displayed.
                switch (model.paging)
                {
                    case "prev":
                        page = (int)TempData["page"] - 1;
                        break;

                    case "next":
                        page = (int)TempData["page"] + 1;
                        break;

                    default:
                        page = int.Parse(model.paging);
                        break;
                }

                // Recover the leftMostPage.
                int leftMostPage = (int)TempData["leftMostPage"];

                // Recover the filters.
                string catFilter = TempData["categoryFilter"].ToString();
                string ameFilter = TempData["amenityFilter"].ToString();

                // Recover the search text.
                model.searchText = TempData["searchfor"].ToString();

                // Search for the new page.
                await RunQueryAsync(model, page, leftMostPage, catFilter, ameFilter).ConfigureAwait(false);
            }
            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "2" });
            }

            return View("Index", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static SearchIndexClient _indexClient;
        private static SearchClient _searchClient;
        private static IConfigurationBuilder _builder;
        private static IConfigurationRoot _configuration;

        private void InitSearch()
        {
            // Create a configuration using the appsettings file.
            _builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            _configuration = _builder.Build();

            // Pull the values from the appsettings.json file.
            string searchServiceUri = _configuration["SearchServiceUri"];
            string queryApiKey = _configuration["SearchServiceQueryApiKey"];

            // Create a service and index client.
            _indexClient = new SearchIndexClient(new Uri(searchServiceUri), new AzureKeyCredential(queryApiKey));
            _searchClient = _indexClient.GetSearchClient("hotels-sample-index");
        }

        private async Task<ActionResult> RunQueryAsync(SearchData model, int page, int leftMostPage, string catFilter, string ameFilter)
        {
            InitSearch();

            string facetFilter = "";

            if (catFilter.Length > 0 && ameFilter.Length > 0)
            {
                // Both facets apply.
                facetFilter = $"{catFilter} and {ameFilter}"; 
            } else
            {
                // One, or zero, facets apply.
                facetFilter = $"{catFilter}{ameFilter}";
            }

            var options = new SearchOptions
            {
                Filter = facetFilter,

                SearchMode = SearchMode.All,

                // Skip past results that have already been returned.
                Skip = page * GlobalVariables.ResultsPerPage,

                // Take only the next page worth of results.
                Size = GlobalVariables.ResultsPerPage,

                // Include the total number of results.
                IncludeTotalCount = true,
            };

            // Return information on the text, and number, of facets in the data.
            options.Facets.Add("Category,count:20");
            options.Facets.Add("Tags,count:20");

            // Enter Hotel property names into this list, so only these values will be returned.
            options.Select.Add("HotelName");
            options.Select.Add("Description");
            options.Select.Add("Category");
            options.Select.Add("Tags");

            // For efficiency, the search call should be asynchronous, so use SearchAsync rather than Search.
            model.resultList = await _searchClient.SearchAsync<Hotel>(model.searchText, options).ConfigureAwait(false);           

            // This variable communicates the total number of pages to the view.
            model.pageCount = ((int)model.resultList.TotalCount + GlobalVariables.ResultsPerPage - 1) / GlobalVariables.ResultsPerPage;

            // This variable communicates the page number being displayed to the view.
            model.currentPage = page;

            // Calculate the range of page numbers to display.
            if (page == 0)
            {
                leftMostPage = 0;
            }
            else if (page <= leftMostPage)
            {
                // Trigger a switch to a lower page range.
                leftMostPage = Math.Max(page - GlobalVariables.PageRangeDelta, 0);
            }
            else if (page >= leftMostPage + GlobalVariables.MaxPageRange - 1)
            {
                // Trigger a switch to a higher page range.
                leftMostPage = Math.Min(page - GlobalVariables.PageRangeDelta, model.pageCount - GlobalVariables.MaxPageRange);
            }
            model.leftMostPage = leftMostPage;

            // Calculate the number of page numbers to display.
            model.pageRange = Math.Min(model.pageCount - leftMostPage, GlobalVariables.MaxPageRange);

            // Ensure Temp data is stored for the next call.
            TempData["page"] = page;
            TempData["leftMostPage"] = model.leftMostPage;
            TempData["searchfor"] = model.searchText;
            TempData["categoryFilter"] = catFilter;
            TempData["amenityFilter"] = ameFilter;
           
            // Return the new view.
            return View("Index", model);
        }
    }
}
