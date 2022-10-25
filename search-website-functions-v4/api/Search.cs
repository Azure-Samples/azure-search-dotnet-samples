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
using SearchFilter = WebSearch.Models.SearchFilter;

namespace WebSearch.Function
{
    public class Search
    {
        private static string searchApiKey = Environment.GetEnvironmentVariable("SearchApiKey", EnvironmentVariableTarget.Process);
        private static string searchServiceName = Environment.GetEnvironmentVariable("SearchServiceName", EnvironmentVariableTarget.Process);
        private static string searchIndexName = Environment.GetEnvironmentVariable("SearchIndexName", EnvironmentVariableTarget.Process) ?? "good-books";

        private readonly ILogger _logger;

        public Search(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Lookup>();
        }

        [Function("search")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext executionContext)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<RequestBodySearch>(requestBody);

            // Cognitive Search 
            Uri serviceEndpoint = new Uri($"https://{searchServiceName}.search.windows.net/");

            SearchClient searchClient = new SearchClient(
                serviceEndpoint,
                searchIndexName,
                new AzureKeyCredential(searchApiKey)
            );

            SearchOptions options = new SearchOptions()
            {
                Size = data.Size,
                Skip = data.Skip,
                IncludeTotalCount = true,
                Filter = CreateFilterExpression(data.Filters)
            };
            options.Facets.Add("authors");
            options.Facets.Add("language_code");

            SearchResults<SearchDocument> searchResults = searchClient.Search<SearchDocument>(data.SearchText, options);

            var facetOutput = new Dictionary<String, IList<FacetValue>>();
            foreach (var facetResult in searchResults.Facets)
            {
                facetOutput[facetResult.Key] = facetResult.Value
                           .Select(x => new FacetValue() { value = x.Value.ToString(), count = x.Count })
                           .ToList();
            }

            var output = new SearchOutput
            {
                Count = searchResults.TotalCount,
                Results = searchResults.GetResults().ToList(),
                Facets = facetOutput
            };

            var response = req.CreateResponse(HttpStatusCode.Found);
            await response.WriteAsJsonAsync(output);
            return response;
        }
        public static string CreateFilterExpression(List<SearchFilter> filters)
        {
            if (filters == null || filters.Count <= 0)
            {
                return null;
            }

            List<string> filterExpressions = new List<string>();

            List<SearchFilter> authorFilters = filters.Where(f => f.field == "authors").ToList();
            List<SearchFilter> languageFilters = filters.Where(f => f.field == "language_code").ToList();

            List<string> authorFilterValues = authorFilters.Select(f => f.value).ToList();

            if (authorFilterValues.Count > 0)
            {
                string filterStr = string.Join(",", authorFilterValues);
                filterExpressions.Add($"{"authors"}/any(t: search.in(t, '{filterStr}', ','))");
            }

            List<string> languageFilterValues = languageFilters.Select(f => f.value).ToList();
            foreach (var value in languageFilterValues)
            {
                filterExpressions.Add($"language_code eq '{value}'");
            }

            return string.Join(" and ", filterExpressions);
        }
    }
}
