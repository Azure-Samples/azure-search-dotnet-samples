using System.Net;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WebSearch.Models;
using Azure.Core;

namespace WebSearch.Function
{
    public class Lookup
    {
        private static string searchApiKey = Environment.GetEnvironmentVariable("SearchApiKey", EnvironmentVariableTarget.Process);
        private static string searchServiceName = Environment.GetEnvironmentVariable("SearchServiceName", EnvironmentVariableTarget.Process);
        private static string searchIndexName = Environment.GetEnvironmentVariable("SearchIndexName", EnvironmentVariableTarget.Process) ?? "good-books";

        private readonly ILogger<Lookup> _logger;

        public Lookup(ILogger<Lookup> logger)
        {
            _logger = logger;
        }


        [Function("lookup")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, FunctionContext executionContext)
        {

            // Get Document Id
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string documentId = query["id"].ToString();

            // Cognitive Search 
            Uri serviceEndpoint = new Uri($"https://{searchServiceName}.search.windows.net/");

            SearchClient searchClient = new SearchClient(
                serviceEndpoint,
                searchIndexName,
                new AzureKeyCredential(searchApiKey)
            );

            var getDocumentResponse = await searchClient.GetDocumentAsync<SearchDocument>(documentId);

            var output = new LookupOutput
            {
                Document = getDocumentResponse.Value
            };

            var response = req.CreateResponse(HttpStatusCode.Found);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteAsJsonAsync(output);
            return response;
        }
    }
}
