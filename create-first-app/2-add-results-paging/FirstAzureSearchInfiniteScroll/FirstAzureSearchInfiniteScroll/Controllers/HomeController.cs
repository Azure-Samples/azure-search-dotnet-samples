using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FirstAzureSearchInfiniteScroll.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Collections.Generic;
namespace FirstAzureSearchInfiniteScroll.Controllers
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
                // Use static variables to set up the configuration and Azure service and index clients, for efficiency.
                _builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                _configuration = _builder.Build();

                _serviceClient = CreateSearchServiceClient(_configuration);
                _indexClient = _serviceClient.Indexes.GetClient("hotels");

                int page;

                if (model.paging != null && model.paging == "next")
                {
                    // Increment the page.
                    page = (int)TempData["page"] + 1;

                    // Recover the search text.
                    model.searchText = TempData["searchfor"].ToString();
                }
                else
                {
                    // First call. Check for valid text input.
                    if (model.searchText == null)
                    {
                        model.searchText = "";
                    }
                    page = 0;
                }

                SearchParameters parameters;
                DocumentSearchResult<Hotel> results;

                parameters =
                   new SearchParameters()
                   {
                       // Enter Hotel property names into this list so only these values will be returned.
                       // If Select is empty, all values will be returned, which can be inefficient.
                       Select = new[] { "HotelName", "Description", "Tags", "Rooms" },
                       SearchMode = SearchMode.All,
                       Skip = page * GlobalVariables.ResultsPerPage,
                       Top = GlobalVariables.ResultsPerPage,
                       IncludeTotalResultCount = true,
                   };

                // For efficiency, the search call should ideally be asynchronous, so we use the
                // SearchAsync call rather than the Search call.
                results = await _indexClient.Documents.SearchAsync<Hotel>(model.searchText, parameters);

                if (results.Results == null)
                {
                    model.resultCount = 0;
                }
                else
                {
                    // Record the number of results.
                    // Note results.Count           is the total number of results available.
                    //      results.Results.Count   is the number of results returned, not the same as results.Count.

                    // This variable communicates the total number of results to the view.
                    model.resultCount = (int)results.Count;

                    for (int i = 0; i < results.Results.Count; i++)
                    {
                        // Check for hotels with no room data provided.
                        if (results.Results[i].Document.Rooms.Length > 0)
                        {
                            // Add a hotel with sample room data (an example of a "complex type").
                            model.AddHotel(results.Results[i].Document.HotelName,
                                 results.Results[i].Document.Description,
                                 (double)results.Results[i].Document.Rooms[0].BaseRate,
                                 results.Results[i].Document.Rooms[0].BedOptions,
                                 results.Results[i].Document.Tags);
                        }
                        else
                        {
                            // Add a hotel with no sample room data.
                            model.AddHotel(results.Results[i].Document.HotelName,
                                results.Results[i].Document.Description,
                                0d,
                                "No room data provided",
                                results.Results[i].Document.Tags);
                        }
                    }

                    // This variable communicates the total number of pages to the view.
                    model.pageCount = ((int)results.Count + GlobalVariables.ResultsPerPage - 1) / GlobalVariables.ResultsPerPage;

                    // This variable communicates the page number being displayed to the view.
                    model.currentPage = page;

                    // Ensure Temp data is stored for the next call.
                    TempData["page"] = page;
                    TempData["searchfor"] = model.searchText;
                }
            }
            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "1" });
            }
            return View("Index", model);
        }

        public async Task<ActionResult> Next(SearchData model)
        {
            model.paging = "next";
            await Index(model);

            List<string> hotels = new List<string>();
            for (int n = 0; n < model.hotels.Count; n++)
            {
                hotels.Add(model.GetHotel(n).HotelName);
                hotels.Add(model.GetFullHotelDescription(n));
            }
            return new JsonResult(hotels);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static SearchServiceClient _serviceClient;
        private static ISearchIndexClient _indexClient;
        private static IConfigurationBuilder _builder;
        private static IConfigurationRoot _configuration;

        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            // Pull these values from the appsettings.json file.
            string searchServiceName = configuration["SearchServiceName"];
            string queryApiKey = configuration["SearchServiceQueryApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(queryApiKey));
            return serviceClient;
        }

        
    }
}
