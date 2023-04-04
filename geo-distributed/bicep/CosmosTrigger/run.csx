#r "Microsoft.Azure.DocumentDB.Core"
#r "System.Web"

using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.Azure.Documents;

using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

public static async Task Run(IReadOnlyList<Document> input, ILogger log)
{
    if (input != null && input.Count > 0)
    {
        log.LogInformation("Documents modified " + input.Count);
        // Get the service endpoint and API key from the environment
        Uri endpoint = new Uri(Environment.GetEnvironmentVariable("SEARCH_ENDPOINT"));
        string indexName = Environment.GetEnvironmentVariable("SEARCH_INDEX_NAME");
        var credential = new DefaultAzureCredential();

        // Create a new SearchIndexClient
        SearchClient searchClient = new SearchClient(endpoint, indexName, credential);

        // Base64 encode all rid values so they can be used as keys in the search index
        foreach (Document document in input)
        {
            // https://learn.microsoft.com/azure/search/search-indexer-field-mappings#base64-encoding-options
            document.Id = HttpUtility.UrlEncode(document.Id);
        }

        // Upload all changes to the search index
        await searchClient.UploadDocumentsAsync(input);
    }
}
