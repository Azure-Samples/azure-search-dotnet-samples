using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

class BuildIndex
{
    static async Task Main(string[] args)
    {
        string searchServiceName = "PUT-YOUR-SEARCH-SERVICE-NAME-HERE";
        string indexName = "hotels-sample-index";
        string endpoint = $"https://{searchServiceName}.search.windows.net";
        var credential = new Azure.Identity.DefaultAzureCredential();

        var client = new SearchClient(new Uri(endpoint), indexName, credential);

        await ListIndexesAsync(endpoint, credential);
        await UpdateIndexAsync(endpoint, credential, indexName);
    }

    // Print a list of all indexes on the search service
    static async Task ListIndexesAsync(string endpoint, Azure.Core.TokenCredential credential)
    {
        try
        {
            var indexClient = new Azure.Search.Documents.Indexes.SearchIndexClient(
                new Uri(endpoint),
                credential
            );

            var indexes = indexClient.GetIndexesAsync();

            Console.WriteLine("Here's a list of all indexes on the search service. You should see hotels-sample-index:");
            await foreach (var index in indexes)
            {
                Console.WriteLine(index.Name);
            }
            Console.WriteLine(); // Add an empty line for readability
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing indexes: {ex.Message}");
        }
    }

    static async Task UpdateIndexAsync(string endpoint, Azure.Core.TokenCredential credential, string indexName)
    {
        try
        {
            var indexClient = new Azure.Search.Documents.Indexes.SearchIndexClient(
                new Uri(endpoint),
                credential
            );

            // Get the existing definition of hotels-sample-index
            var indexResponse = await indexClient.GetIndexAsync(indexName);
            var index = indexResponse.Value;

            // Add a semantic configuration
            const string semanticConfigName = "semantic-config";
            AddSemanticConfiguration(index, semanticConfigName);

            // Update the index with the new information
            var updatedIndex = await indexClient.CreateOrUpdateIndexAsync(index);
            Console.WriteLine("Index updated successfully.");

            // Print the updated index definition as JSON
            var refreshedIndexResponse = await indexClient.GetIndexAsync(indexName);
            var refreshedIndex = refreshedIndexResponse.Value;
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string indexJson = JsonSerializer.Serialize(refreshedIndex, jsonOptions);
            Console.WriteLine($"Here is the revised index definition:\n{indexJson}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating index: {ex.Message}");
        }
    }

    static void AddSemanticConfiguration(SearchIndex index, string semanticConfigName)
    {
        if (index.SemanticSearch == null)
        {
            index.SemanticSearch = new SemanticSearch();
        }
        var configs = index.SemanticSearch.Configurations;
        if (configs == null)
        {
            throw new InvalidOperationException("SemanticSearch.Configurations is null and cannot be assigned. Your service must be Basic tier or higher.");
        }
        if (!configs.Any(c => c.Name == semanticConfigName))
        {
            var prioritizedFields = new SemanticPrioritizedFields
            {
                TitleField = new SemanticField("HotelName"),
                ContentFields = { new SemanticField("Description") },
                KeywordsFields = { new SemanticField("Tags"), new SemanticField("Category") }
            };

            configs.Add(
                new SemanticConfiguration(
                    semanticConfigName,
                    prioritizedFields
                )
            );
            Console.WriteLine($"Added new semantic configuration '{semanticConfigName}' to the index definition.");
        }
        else
        {
            Console.WriteLine($"Semantic configuration '{semanticConfigName}' already exists in the index definition.");
        }
        index.SemanticSearch.DefaultConfigurationName = semanticConfigName;
    }
}