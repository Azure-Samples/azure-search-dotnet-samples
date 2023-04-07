using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

// Customize the model with your own desired properties
public class ToDoItem
{
    public string rid { get; set; }
    public string Description { get; set; }
}

public class Result
{
    public double? score { get; set; }

    public ToDoItem item {get; set;}
}

public static async Task<HttpResponseMessage> Run(HttpRequest req, ILogger log)
{
    try
    {
        // Get the service endpoint and API key from the environment
        Uri endpoint = new Uri(Environment.GetEnvironmentVariable("SEARCH_ENDPOINT"));
        string indexName = Environment.GetEnvironmentVariable("SEARCH_INDEX_NAME");
        var credential = new DefaultAzureCredential();

        // Create a new SearchClient
        SearchClient searchClient = new SearchClient(endpoint, indexName, credential);

        // Parse query parameters
        if (!req.Query.TryGetValue("search", out StringValues searchText))
        {
            searchText = "*";
        }
        if (!req.Query.TryGetValue("size", out StringValues size))
        {
            size = "";
        }

        if (!int.TryParse(size, out int parsedSize))
        {
            parsedSize = 0;
        }

        // Forward search results
        var results = new List<Result>();
        SearchResults<ToDoItem> searchResults = await searchClient.SearchAsync<ToDoItem>(searchText, new SearchOptions { Size = parsedSize });
        foreach (SearchResult<ToDoItem> result in searchResults.GetResults())
        {
            results.Add(new Result { score = result.Score, item = result.Document });
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(results), Encoding.UTF8, "application/json") };
    }
    catch (RequestFailedException e)
    {
        // Return failed status code
        log.LogError(e.ToString());
        return new HttpResponseMessage((HttpStatusCode)e.Status);
    }
    catch (Exception e)
    {
        log.LogError(e.ToString());
        throw;
    }
}
