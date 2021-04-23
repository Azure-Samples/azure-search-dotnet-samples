using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using Azure;
using System.Collections.Generic;
using System.Linq;



namespace FunctionApp_web_search
{
    public static class Search
    {
        private static string searchApiKey = Environment.GetEnvironmentVariable("SearchApiKey", EnvironmentVariableTarget.Process);
        private static string searchServiceName = Environment.GetEnvironmentVariable("SearchServiceName", EnvironmentVariableTarget.Process);
        private static string searchIndexName = Environment.GetEnvironmentVariable("SearchIndexName", EnvironmentVariableTarget.Process) ?? "good-books";


        [FunctionName("search")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
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
                Filter= CreateFilterExpression(data.Filters)
            };
            options.Facets.Add("authors");
            options.Facets.Add("language_code");

            SearchResults<SearchDocument> response = searchClient.Search<SearchDocument>(data.SearchText, options);

            var facetOutput = new Dictionary<String, IList<FacetValue>>();
            foreach(var facetResult in response.Facets) {
                facetOutput[facetResult.Key] = facetResult.Value
                           .Select(x => new FacetValue() { value = x.Value.ToString(), count = x.Count })
                           .ToList();     
            }

            var output = new SearchOutput
            {
                Count = response.TotalCount,
                Results = response.GetResults().ToList(),
                Facets = facetOutput
            };

            return new OkObjectResult(output);
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


