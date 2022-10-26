using System.Net;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WebSearch.Models;
using Azure.Core;
using System.Text.Json;
using System.Reflection.Metadata;

namespace WebSearch.Function
{
    public class Suggest
    {
        private static string searchApiKey = Environment.GetEnvironmentVariable("SearchApiKey", EnvironmentVariableTarget.Process);
        private static string searchServiceName = Environment.GetEnvironmentVariable("SearchServiceName", EnvironmentVariableTarget.Process);
        private static string searchIndexName = Environment.GetEnvironmentVariable("SearchIndexName", EnvironmentVariableTarget.Process) ?? "good-books";

        private readonly ILogger<Lookup> _logger;

        public Suggest(ILogger<Lookup> logger)
        {
            _logger = logger;
        }

        [Function("suggest")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, 
            FunctionContext executionContext)
        {

            // Get Document Id
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<RequestBodySuggest>(requestBody);

            // Cognitive Search 
            Uri serviceEndpoint = new Uri($"https://{searchServiceName}.search.windows.net/");

            SearchClient searchClient = new SearchClient(
                serviceEndpoint,
                searchIndexName,
                new AzureKeyCredential(searchApiKey)
            );

            SuggestOptions options = new SuggestOptions()
            {
                Size = data.Size
            };

            var suggesterResponse = await searchClient.SuggestAsync<BookModel>(data.SearchText, data.SuggesterName, options);
            var searchSuggestions = new Dictionary<string, List<SearchSuggestion<BookModel>>>();
            searchSuggestions["suggestions"] = suggesterResponse.Value.Results.ToList();

            var response = req.CreateResponse(HttpStatusCode.Found);
            await response.WriteAsJsonAsync(searchSuggestions);
            return response;
        }
    }
}
