using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FirstAzureSearchApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

// FirstAzureSearchApp
namespace FirstAzureSearchApp.Controllers
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

                // Make the Azure Search call for the first page.
                await RunQueryAsync(model);

                // Ensure temporary data is stored for the next call.
                //TempData["page"] = 0;
                //TempData["searchfor"] = model.searchText;
            }

            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "1" });
            }
            return View(model);
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

        private async Task<ActionResult> RunQueryAsync(SearchData model)
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
                   Select = new[] { "HotelName", "Description" }
               };

            // For efficiency, the search call should be asynchronous, so we use the
            // SearchAsync call rather than the Search call.
            results = await _indexClient.Documents.SearchAsync<Hotel>(model.searchText, parameters);

            if (results.Results == null)
            {
                throw new Exception("Null results");
            }
            else
            {
                model.resultList = results;
            }

            // Display the results.
            return View("Index", model);
        }
    }
}
