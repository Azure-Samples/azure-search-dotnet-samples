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

namespace FunctionApp_web_search
{
    public static class Lookup
    {

        private static string searchApiKey = Environment.GetEnvironmentVariable("SearchApiKey", EnvironmentVariableTarget.Process);
        private static string searchServiceName = Environment.GetEnvironmentVariable("SearchServiceName", EnvironmentVariableTarget.Process);
        private static string searchIndexName = Environment.GetEnvironmentVariable("SearchIndexName", EnvironmentVariableTarget.Process) ?? "good-books";


        [FunctionName("lookup")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            // Get Document Id
            string documentId = req.Query["id"]; ;

            // Cognitive Search 
            Uri serviceEndpoint = new Uri($"https://{searchServiceName}.search.windows.net/");

            SearchClient searchClient = new SearchClient(
                serviceEndpoint,
                searchIndexName,
                new AzureKeyCredential(searchApiKey)
            );

            var response = await searchClient.GetDocumentAsync<SearchDocument>(documentId);

            var output = new LookupOutput
            {
                Document = response.Value
            };

            return new OkObjectResult(output);
        }
    }
}


