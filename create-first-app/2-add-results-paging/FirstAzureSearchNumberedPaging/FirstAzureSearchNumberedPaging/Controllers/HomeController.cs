using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FirstAzureSearchNumberedPaging.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace FirstAzureSearchNumberedPaging.Controllers
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
                await RunQueryAsync(model, 0, 0);

                // Ensure temporary data is stored for the next call.
                TempData["page"] = 0;
                TempData["leftMostPage"] = 0;
                TempData["searchfor"] = model.searchText;
            }

            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "1" });
            }
            return View(model);
        }

        public async Task<ActionResult> Page(SearchData model)
        {
            try
            {
                int page;

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

                // Recover the search text and search for the data for the new page.
                model.searchText = TempData["searchfor"].ToString();

                await RunQueryAsync(model, page, leftMostPage);

                // Ensure Temp data is stored for next call, as TempData only stored for one call.
                TempData["page"] = (object)page;
                TempData["searchfor"] = model.searchText;
                TempData["leftMostPage"] = model.leftMostPage;
            }

            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "2" });
            }
            return View("Index", model);
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

        private async Task<ActionResult> RunQueryAsync(SearchData model, int page, int leftMostPage)
        {
            // Use static variables to set up the configuration and Azure service and index clients, for efficiency.
            _builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            _configuration = _builder.Build();

            _serviceClient = CreateSearchServiceClient(_configuration);
            _indexClient = _serviceClient.Indexes.GetClient("hotels");

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
                model.pageCount = ( (int)results.Count + GlobalVariables.ResultsPerPage - 1) / GlobalVariables.ResultsPerPage;  
                
                // This variable communicates the page number being displayed to the view.
                model.currentPage = page;

                // Calculate the range of page numbers to display.
                if (page == 0)
                {
                    leftMostPage = 0;
                }
                else
                   if (page <= leftMostPage)
                {
                    // Trigger a switch to a lower page range.
                    leftMostPage = Math.Max(page - GlobalVariables.PageRangeDelta, 0);
                }
                else
                if (page >= leftMostPage + GlobalVariables.MaxPageRange - 1)
                {
                    // Trigger a switch to a higher page range.
                    leftMostPage = Math.Min(leftMostPage + GlobalVariables.PageRangeDelta, model.pageCount - GlobalVariables.MaxPageRange);
                }
                model.leftMostPage = leftMostPage;

                // Calculate the number of page numbers to display.
                model.pageRange = Math.Min(model.pageCount - leftMostPage, GlobalVariables.MaxPageRange);
            }
            return View("Index", model);
        }
    }
}
