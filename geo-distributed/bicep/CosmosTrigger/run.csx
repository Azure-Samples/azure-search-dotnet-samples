
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

using Microsoft.IdentityModel.Tokens;

// Customize the model with your own desired properties
public class ToDoItem
{
    public string _rid { get; set; }
    public string Description { get; set; }
}

public static async Task Run(IReadOnlyList<ToDoItem> documents, ILogger log)
{
    try
    {
        // Get the service endpoint and API key from the environment
        Uri endpoint = new Uri(Environment.GetEnvironmentVariable("SEARCH_ENDPOINT"));
        string indexName = Environment.GetEnvironmentVariable("SEARCH_INDEX_NAME");
        var credential = new DefaultAzureCredential();

        // Create a new SearchIndexClient
        SearchClient searchClient = new SearchClient(endpoint, indexName, credential);

        // Base64 encode all rid values so they can be used as keys in the search index
        SearchDocument[] searchDocuments = documents.Select(document => new SearchDocument(
            new Dictionary<string, object>
            {
                // https://learn.microsoft.com/azure/search/search-indexer-field-mappings#base64-encoding-options
                ["rid"] = Base64UrlEncoder.Encode(document._rid),
                ["description"] = document.Description
            }))
            .ToArray();

        // Upload all changes to the search index
        await searchClient.UploadDocumentsAsync(searchDocuments);

        log.LogInformation("Uploaded " + documents.Count() + " documents");
    }
    catch (Exception e)
    {
        log.LogError(e.ToString());
        throw;
    }
}