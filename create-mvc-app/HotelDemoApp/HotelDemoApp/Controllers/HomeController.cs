using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using HotelDemoApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HotelDemoApp.Controllers
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
                // Check for a search string
                if (model.searchText == null)
                {
                    model.searchText = "";
                }

                // Send the query to Search.
                await RunQueryAsync(model);
            }

            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "1" });
            }
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static SearchClient _searchClient;
        private static SearchIndexClient _indexClient;
        private static IConfigurationBuilder _builder;
        private static IConfigurationRoot _configuration;

        private void InitSearch()
        {
            // Create a configuration using appsettings.json
            _builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            _configuration = _builder.Build();

            // Read the values from appsettings.json
            string searchServiceUri = _configuration["SearchServiceUri"];
            string queryApiKey = _configuration["SearchServiceQueryApiKey"];

            // Create a service and index client.
            _indexClient = new SearchIndexClient(new Uri(searchServiceUri), new AzureKeyCredential(queryApiKey));
            _searchClient = _indexClient.GetSearchClient("hotels-sample-index");
        }

        // Use case 1: simple search, searchMode=Any
        private async Task<ActionResult> RunQueryAsync(SearchData model)
        {
            InitSearch();

            var options = new SearchOptions()
            {
                IncludeTotalCount = true
            };

            // Enter Hotel property names into this list so only these values will be returned.
            // If Select is empty, all values will be returned, which can be inefficient.
            options.Select.Add("HotelName");
            options.Select.Add("Category");
            options.Select.Add("Rating");
            options.Select.Add("Tags");
            options.Select.Add("Address/City");
            options.Select.Add("Address/StateProvince");
            options.Select.Add("Description");

            // For efficiency, the search call should be asynchronous, so use SearchAsync rather than Search.
            model.resultList = await _searchClient.SearchAsync<Hotel>(model.searchText, options).ConfigureAwait(false);

            // Display the results.
            return View("Index", model);
        }

        //// Use case 2: Filter on Category 
        //// (other "filterable" fields include Address/City and Address/StateProvince)
        //private async Task<ActionResult> RunQueryAsync(SearchData model)
        //{
        //    InitSearch();

        //    var options = new SearchOptions()
        //    {
        //        IncludeTotalCount = true,
        //        Filter = "search.in(Category,'Budget,Suite')"
        //    };

        //    // Enter Hotel property names into this list so only these values will be returned.
        //    // If Select is empty, all values will be returned, which can be inefficient.
        //    options.Select.Add("HotelName");
        //    options.Select.Add("Category");
        //    options.Select.Add("Rating");
        //    options.Select.Add("Tags");
        //    options.Select.Add("Address/City");
        //    options.Select.Add("Address/StateProvince");
        //    options.Select.Add("Description");

        //    // For efficiency, the search call should be asynchronous, so use SearchAsync rather than Search.
        //    model.resultList = await _searchClient.SearchAsync<Hotel>(model.searchText, options).ConfigureAwait(false);

        //    // Display the results.
        //    return View("Index", model);
        //}

        //// Use case 3: Sort on Rating (there are no other "sortable" fields in the Hotels sample index)
        //private async Task<ActionResult> RunQueryAsync(SearchData model)
        //{
        //    InitSearch();

        //    var options = new SearchOptions()
        //    {
        //        IncludeTotalCount = true,
        //    };

        //    options.OrderBy.Add("Rating desc");

        //    // Enter Hotel property names into this list so only these values will be returned.
        //    // If Select is empty, all values will be returned, which can be inefficient.
        //    options.Select.Add("HotelName");
        //    options.Select.Add("Category");
        //    options.Select.Add("Rating");
        //    options.Select.Add("Tags");
        //    options.Select.Add("Address/City");
        //    options.Select.Add("Address/StateProvince");
        //    options.Select.Add("Description");

        //    // For efficiency, the search call should be asynchronous, so use SearchAsync rather than Search.
        //    model.resultList = await _searchClient.SearchAsync<Hotel>(model.searchText, options).ConfigureAwait(false);

        //    // Display the results.
        //    return View("Index", model);
        //}
        public IActionResult Privacy()
        {
            return View();
        }
    }
}