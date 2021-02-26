using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrderResults.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents;
using Azure;
using Azure.Search.Documents.Models;

namespace OrderResults.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            InitSearch();

            // Set up the facets call in the search parameters.
            SearchOptions options = new SearchOptions();
            // Search for up to 20 amenities.
            options.Facets.Add("Tags,count:20");

            SearchResults<Hotel> searchResult = await _searchClient.SearchAsync<Hotel>("*", options);

            // Convert the results to a list that can be displayed in the client.
            List<string> facets = searchResult.Facets["Tags"].Select(x => x.Value.ToString()).ToList();

            // Initiate a model with a list of facets for the first view.
            SearchData model = new SearchData(facets);

            // Save the facet text for the next view.
            SaveFacets(model, false);

            // Render the view including the facets.
            return View(model);
        }

        // Save the facet text to temporary storage, optionally saving the state of the check boxes.
        private void SaveFacets(SearchData model, bool saveChecks = false)
        {
            for (int i = 0; i < model.facetText.Length; i++)
            {
                TempData["facet" + i.ToString()] = model.facetText[i];
                if (saveChecks)
                {
                    TempData["faceton" + i.ToString()] = model.facetOn[i];
                }
            }
            TempData["facetcount"] = model.facetText.Length;
        }

        // Recover the facet text to a model, optionally recoving the state of the check boxes.
        private void RecoverFacets(SearchData model, bool recoverChecks = false)
        {
            // Create arrays of the appropriate length.
            model.facetText = new string[(int)TempData["facetcount"]];
            if (recoverChecks)
            {
                model.facetOn = new bool[(int)TempData["facetcount"]];
            }

            for (int i = 0; i < (int)TempData["facetcount"]; i++)
            {
                model.facetText[i] = TempData["facet" + i.ToString()].ToString();
                if (recoverChecks)
                {
                    model.facetOn[i] = (bool)TempData["faceton" + i.ToString()];
                }
            }
        }

        [HttpPost]
        public async Task<ActionResult> Index(SearchData model)
        {
            try
            {
                InitSearch();

                int page;

                if (model.paging != null && model.paging == "next")
                {
                    // Recover the facet text, and the facet check box settings.
                    RecoverFacets(model, true);

                    // Increment the page.
                    page = (int)TempData["page"] + 1;

                    // Recover the search text.
                    model.searchText = TempData["searchfor"].ToString();
                }
                else
                {
                    // First search with text. 
                    // Recover the facet text, but ignore the check box settings, and use the current model settings.
                    RecoverFacets(model, false);

                    // First call. Check for valid text input, and valid scoring profile.
                    if (model.searchText == null)
                    {
                        model.searchText = "";
                    }
                    if (model.scoring == null)
                    {
                        model.scoring = "Default";
                    }
                    page = 0;
                }

                // Setup the search parameters.
                var options = new SearchOptions
                {
                    SearchMode = SearchMode.All,

                    // Skip past results that have already been returned.
                    Skip = page * GlobalVariables.ResultsPerPage,

                    // Take only the next page worth of results.
                    Size = GlobalVariables.ResultsPerPage,

                    // Include the total number of results.
                    IncludeTotalCount = true,
                };
                // Select the data properties to be returned.
                options.Select.Add("HotelName");
                options.Select.Add("Description");
                options.Select.Add("Tags");
                options.Select.Add("Rooms");
                options.Select.Add("Rating");
                options.Select.Add("LastRenovationDate");

                List<string> parameters = new List<string>();
                // Set the ordering based on the user's radio button selection.
                switch (model.scoring)
                {
                    case "RatingRenovation":
                        // Set the ordering/scoring parameters.
                        options.OrderBy.Add("Rating desc");
                        options.OrderBy.Add("LastRenovationDate desc");
                        break;

                    case "boostAmenities":
                        {
                            options.ScoringProfile = model.scoring;

                            // Create a string list of amenities that have been clicked.
                            for (int a = 0; a < model.facetOn.Length; a++)
                            {
                                if (model.facetOn[a])
                                {
                                    parameters.Add(model.facetText[a]);
                                }
                            }

                            if (parameters.Count > 0)
                            {
                                options.ScoringParameters.Add($"amenities-{ string.Join(',', parameters)}");
                            }
                            else
                            {
                                // No amenities selected, so set profile back to default.
                                options.ScoringProfile = "";
                            }
                        }
                        break;

                    case "renovatedAndHighlyRated":
                        options.ScoringProfile = model.scoring;
                        break;

                    default:
                        break;
                }

                // For efficiency, the search call should be asynchronous, so use SearchAsync rather than Search.
                model.resultList = await _searchClient.SearchAsync<Hotel>(model.searchText, options);

                // Ensure TempData is stored for the next call.
                TempData["page"] = page;
                TempData["searchfor"] = model.searchText;
                TempData["scoring"] = model.scoring;
                SaveFacets(model, true);

                // Calculate the room rate ranges.
                await foreach (var result in model.resultList.GetResultsAsync())
                {
                    var cheapest = 0d;
                    var expensive = 0d;

                    foreach (var room in result.Document.Rooms)
                    {
                        var rate = room.BaseRate;
                        if (rate < cheapest || cheapest == 0)
                        {
                            cheapest = (double)rate;
                        }
                        if (rate > expensive)
                        {
                            expensive = (double)rate;
                        }
                    }

                    result.Document.cheapest = cheapest;
                    result.Document.expensive = expensive;
                }
            }
            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "1" });
            }

            return View("Index", model);
        }

        public async Task<ActionResult> NextAsync(SearchData model)
        {
            // Set the next page setting, and call the Index(model) action.
            model.paging = "next";
            model.scoring = TempData["scoring"].ToString();

            await Index(model);

            // Create an empty list.
            var nextHotels = new List<string>();

            // Add a hotel details to the list.
            await foreach (var result in model.resultList.GetResultsAsync())
            {
                var ratingText = $"Rating: {result.Document.Rating}";
                var rateText = $"Rates from ${result.Document.cheapest} to ${result.Document.expensive}";
                var lastRenovatedText = $"Last renovated: {result.Document.LastRenovationDate.Value.Year}";

                string amenities = string.Join(", ", result.Document.Tags);
                string fullDescription = result.Document.Description;
                fullDescription += $"\nAmenities: {amenities}";

                // Add strings to the list.
                nextHotels.Add(result.Document.HotelName);
                nextHotels.Add(ratingText);
                nextHotels.Add(rateText);
                nextHotels.Add(lastRenovatedText);
                nextHotels.Add(fullDescription);
            }

            // Rather than return a view, return the list of data.
            return new JsonResult(nextHotels);
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
    }
}
