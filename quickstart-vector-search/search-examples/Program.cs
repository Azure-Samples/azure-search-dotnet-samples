using Azure.AI.OpenAI;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.Tracing;
using System.Text.Json.Serialization;


// Load configuration from appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

using AzureEventSourceListener listener =
    AzureEventSourceListener.CreateTraceLogger(EventLevel.Verbose);

string searchEndpoint = config["AzureSearch:Endpoint"] ?? throw new Exception("AzureSearch:Endpoint missing");
string indexName = config["AzureSearch:IndexName"] ?? throw new Exception("AzureSearch:IndexName missing");
string openAIEndpoint = config["AzureOpenAI:Endpoint"] ?? "";
string openAIDeployment = config["AzureOpenAI:DeploymentName"] ?? "";

var credential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new AzureDeveloperCliCredential());
var indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);

AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(openAIEndpoint), credential);
var embeddingClient = openAIClient.GetEmbeddingClient("text-embedding-3-small");

var vectorizedResult = embeddingClient.GenerateEmbedding("quintessential lodging near running trails, eateries, retail");

//// Single search vector
//SearchResults<Hotel> response = await searchClient.SearchAsync<Hotel>(
//    new SearchOptions
//    {
//        VectorSearch = new()
//        {
//            Queries = { new VectorizedQuery(vectorizedResult.Value.ToFloats()) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
//        },
//        Select = { "HotelId", "HotelName", "Description", "Category", "Tags" }
//    });

//Console.WriteLine($"Single Vector Search Results:");
//await foreach (SearchResult<Hotel> result in response.GetResultsAsync())
//{
//    Hotel doc = result.Document;
//    Console.WriteLine($"{doc.HotelId}: {doc.HotelName}");
//}
//Console.WriteLine();

//// Single search vector with Filter
//SearchResults<Hotel> responseWithFilter = await searchClient.SearchAsync<Hotel>(
//    new SearchOptions
//    {
//        VectorSearch = new()
//        {
//            Queries = { new VectorizedQuery(vectorizedResult.Value.ToFloats()) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
//        },
//        Filter = "Tags/any(tag: tag eq 'free wifi')",
//        Select = { "HotelId", "HotelName", "Description", "Category", "Tags" }
//    });

//Console.WriteLine($"Single Vector Search With Filter Results:");
//await foreach (SearchResult<Hotel> result in responseWithFilter.GetResultsAsync())
//{
//    Hotel doc = result.Document;
//    Console.WriteLine($"HotelId: {doc.HotelId}, HotelName: {doc.HotelName}, Tags: {string.Join(String.Empty, doc.Tags)}");
//}
//Console.WriteLine();

// Single search vector with geo filter
//SearchResults<Hotel> responseWithGeoFilter = await searchClient.SearchAsync<Hotel>(
//    new SearchOptions
//    {
//        VectorSearch = new()
//        {
//            Queries = { new VectorizedQuery(vectorizedResult.Value.ToFloats()) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
//        },
//        Filter = "geo.distance(Location, geography'POINT(-77.03241 38.90166)') le 300",
//        Select = { "HotelId", "HotelName", "Description", "Category", "Tags" },
//        Facets = { "Address/StateProvince" },

//    });

//Console.WriteLine($"Vector query with a geo filter:");
//await foreach (SearchResult<Hotel> result in responseWithGeoFilter.GetResultsAsync())
//{
//    Hotel doc = result.Document;
//    Console.WriteLine($"HotelId: {doc.HotelId}, HotelName: {doc.HotelName}, Tags: {string.Join(String.Empty, doc.Tags)}");
//}
//Console.WriteLine();

//// Hybrid search with vector and text query
//SearchResults<Hotel> responseWithFilter = await searchClient.SearchAsync<Hotel>(
//    "historic hotel walk to restaurants and shopping",
//    new SearchOptions
//    {
//        VectorSearch = new()
//        {
//            Queries = { new VectorizedQuery(vectorizedResult.Value.ToFloats()) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
//        },
//        Select = { "HotelId", "HotelName", "Description", "Category", "Tags" }
//    });

//Console.WriteLine($"Hybrid search results:");
//await foreach (SearchResult<Hotel> result in responseWithFilter.GetResultsAsync())
//{
//    Hotel doc = result.Document;
//    Console.WriteLine($"HotelId: {doc.HotelId}, HotelName: {doc.HotelName}, Tags: {string.Join(String.Empty, doc.Tags)}");
//}
//Console.WriteLine();

// Hybrid search with vector and text query
SearchResults<Hotel> responseWithFilter = await searchClient.SearchAsync<Hotel>(
    "historic hotel walk to restaurants and shopping",
    new SearchOptions
    {
        IncludeTotalCount = true,
        VectorSearch = new()
        {
            Queries = { new VectorizedQuery(vectorizedResult.Value.ToFloats()) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
        },
        Select = { "HotelId", "HotelName", "Description", "Category", "Tags" },
        SemanticSearch = new SemanticSearchOptions
        {
            SemanticConfigurationName = "semantic-config"
        },
        QueryType = SearchQueryType.Semantic
    });

Console.WriteLine($"Hybrid search results:");
await foreach (SearchResult<Hotel> result in responseWithFilter.GetResultsAsync())
{
    Hotel doc = result.Document;
    Console.WriteLine($"HotelId: {doc.HotelId}, HotelName: {doc.HotelName}, Tags: {string.Join(String.Empty, doc.Tags)}");
}
Console.WriteLine();

public class Hotel
{
    [JsonPropertyName("@search.action")]
    public string SearchAction { get; set; }
    public string HotelId { get; set; }
    public string HotelName { get; set; }
    public string Description { get; set; }
    public List<float> DescriptionVector { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; }
    public bool ParkingIncluded { get; set; }
    public DateTimeOffset? LastRenovationDate { get; set; }
    public double Rating { get; set; }
    public Address Address { get; set; }
    public Location Location { get; set; }
}

public class Location
{
    public string type { get; set; }
    public double[] coordinates { get; set; }
}


// Define C# classes matching your JSON structure
public class Address
{
    public string StreetAddress { get; set; }
    public string City { get; set; }
    public string StateProvince { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
}
