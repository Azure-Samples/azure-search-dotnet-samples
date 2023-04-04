#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogInformation("C# HTTP trigger function processed a request.");

    string name = req.Query["name"];

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    name = name ?? data?.name;

    // Get the service endpoint and API key from the environment
    Uri endpoint = new Uri("https://YOUR_SERVICE.search.windows.net");
    AzureKeyCredential credential = new AzureKeyCredential(
        "YOUR_API_KEY");

    // Create a new SearchIndexClient
    SearchIndexClient indexClient = new SearchIndexClient(endpoint, credential);

    // Perform an operation
    Response<SearchServiceStatistics> stats = indexClient.GetServiceStatistics();
    Console.WriteLine($"You are using {stats.Value.Counters.IndexCounter.Usage} indexes.");

    string responseMessage = string.IsNullOrEmpty(name)
        ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
}
