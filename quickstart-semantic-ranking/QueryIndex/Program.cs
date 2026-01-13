using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class SemanticQuery
{
    static async Task Main(string[] args)
    {
        string searchServiceName = "PUT-YOUR-SEARCH-SERVICE-NAME-HERE";
        string indexName = "hotels-sample-index";
        string endpoint = $"https://{searchServiceName}.search.windows.net";
        var credential = new Azure.Identity.DefaultAzureCredential();

        var client = new SearchClient(new Uri(endpoint), indexName, credential);

        // Query 1: Simple query
        string searchText = "walking distance to live music";
        Console.WriteLine("\nQuery 1: Simple query using the search string 'walking distance to live music'.");
        await RunQuery(client, searchText, new SearchOptions
        {
            Size = 5,
            QueryType = SearchQueryType.Simple,
            IncludeTotalCount = true,
            Select = { "HotelId", "HotelName", "Description" }
        });
        Console.WriteLine("Press Enter to continue to the next query...");
        Console.ReadLine();

        // Query 2: Semantic query (no captions, no answers)
        Console.WriteLine("\nQuery 2: Semantic query (no captions, no answers) for 'walking distance to live music'.");
        var semanticOptions = new SearchOptions
        {
            Size = 5,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "semantic-config"
            },
            IncludeTotalCount = true,
            Select = { "HotelId", "HotelName", "Description" }
        };
        await RunQuery(client, searchText, semanticOptions);
        Console.WriteLine("Press Enter to continue to the next query...");
        Console.ReadLine();

        // Query 3: Semantic query with captions
        Console.WriteLine("\nQuery 3: Semantic query with captions.");
        var captionsOptions = new SearchOptions
        {
            Size = 5,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "semantic-config",
                QueryCaption = new QueryCaption(QueryCaptionType.Extractive)
                {
                    HighlightEnabled = true
                }
            },
            IncludeTotalCount = true,
            Select = { "HotelId", "HotelName", "Description" }
        };
        // Add the field(s) you want captions for to the QueryCaption.Fields collection
        captionsOptions.HighlightFields.Add("Description");
        await RunQuery(client, searchText, captionsOptions, showCaptions: true);
        Console.WriteLine("Press Enter to continue to the next query...");
        Console.ReadLine();

        // Query 4: Semantic query with answers
        // This query uses different search text designed for an answers scenario
        string searchText2 = "what's a good hotel for people who like to read";
        searchText = searchText2; // Update searchText for the next query
        Console.WriteLine("\nQuery 4: Semantic query with a verbatim answer from the Description field for 'what's a good hotel for people who like to read'.");
        var answersOptions = new SearchOptions
        {
            Size = 5,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "semantic-config",
                QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive)
            },
            IncludeTotalCount = true,
            Select = { "HotelId", "HotelName", "Description" }
        };
        await RunQuery(client, searchText2, answersOptions, showAnswers: true);

        static async Task RunQuery(
        SearchClient client,
        string searchText,
        SearchOptions options,
        bool showCaptions = false,
        bool showAnswers = false)
        {
            try
            {
                var response = await client.SearchAsync<SearchDocument>(searchText, options);

                if (showAnswers && response.Value.SemanticSearch?.Answers != null)
                {
                    Console.WriteLine("Extractive Answers:");
                    foreach (var answer in response.Value.SemanticSearch.Answers)
                    {
                        Console.WriteLine($"  {answer.Highlights}");
                    }
                    Console.WriteLine(new string('-', 40));
                }

                await foreach (var result in response.Value.GetResultsAsync())
                {
                    var doc = result.Document;
                    // Print captions first if available
                    if (showCaptions && result.SemanticSearch?.Captions != null)
                    {
                        foreach (var caption in result.SemanticSearch.Captions)
                        {
                            Console.WriteLine($"Caption: {caption.Highlights}");
                        }
                    }
                    Console.WriteLine($"HotelId: {doc.GetString("HotelId")}");
                    Console.WriteLine($"HotelName: {doc.GetString("HotelName")}");
                    Console.WriteLine($"Description: {doc.GetString("Description")}");
                    Console.WriteLine($"@search.score: {result.Score}");

                    // Print @search.rerankerScore if available
                    if (result.SemanticSearch != null && result.SemanticSearch.RerankerScore.HasValue)
                    {
                        Console.WriteLine($"@search.rerankerScore: {result.SemanticSearch.RerankerScore.Value}");
                    }
                    Console.WriteLine(new string('-', 40));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying index: {ex.Message}");
            }
        }
    }
}