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
    public static class Suggest
    {

        private static string searchApiKey = Environment.GetEnvironmentVariable("SearchApiKey", EnvironmentVariableTarget.Process);
        private static string searchServiceName = Environment.GetEnvironmentVariable("SearchServiceName", EnvironmentVariableTarget.Process);
        private static string searchIndexName = Environment.GetEnvironmentVariable("SearchIndexName", EnvironmentVariableTarget.Process) ?? "good-books";


        [FunctionName("suggest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,"post", Route = null)] HttpRequest req,
            ILogger log)
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
            var response = new Dictionary<string, List<SearchSuggestion<BookModel>>>();
            response["suggestions"] = suggesterResponse.Value.Results.ToList();

            return new OkObjectResult(response);
        }
    }
}


