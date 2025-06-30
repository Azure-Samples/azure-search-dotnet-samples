using Azure;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.Tracing;
using System.Text.Json;

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

// Define the index schema (fields, suggesters, semantic config)
// Address complex field
var addressField = new ComplexField("Address");
addressField.Fields.Add(new SearchableField("StreetAddress") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft });
addressField.Fields.Add(new SearchableField("City") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true });
addressField.Fields.Add(new SearchableField("StateProvince") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true });
addressField.Fields.Add(new SearchableField("PostalCode") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true });
addressField.Fields.Add(new SearchableField("Country") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true });

var allFields = new List<SearchField>()
{
    new SimpleField("HotelId", SearchFieldDataType.String) { IsKey = true, IsFacetable = true, IsFilterable = true },
    new SearchableField("HotelName") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
    new SearchableField("Description") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
    new SimpleField("DescriptionVector", SearchFieldDataType.Single),
    new SearchableField("Category") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true },
    new SearchableField("Tags", collection: true) { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true },
    new SimpleField("ParkingIncluded", SearchFieldDataType.Boolean) { IsFacetable = true, IsFilterable = true },
    new SimpleField("LastRenovationDate", SearchFieldDataType.DateTimeOffset) { IsSortable = true },
    new SimpleField("Rating", SearchFieldDataType.Double) { IsFacetable = true, IsFilterable = true, IsSortable = true },
    addressField,
    new SimpleField("Location", SearchFieldDataType.GeographyPoint) { IsFilterable = true, IsSortable = true },
};

// Create the semantic configuration
var suggester = new SearchSuggester("sg", new[] { "Address/City", "Address/Country" });

var semanticConfig = new SemanticConfiguration(
    name: "semantic-config",
    prioritizedFields: new SemanticPrioritizedFields
    {
        TitleField = new SemanticField("HotelName"),
        KeywordsFields = { new SemanticField("Category") },
        ContentFields = { new SemanticField("Description") }
    });

// --- Add vector search configuration ---
var vectorSearch = new VectorSearch();
vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(name: "my-hnsw-vector-config-1"));
vectorSearch.Profiles.Add(new VectorSearchProfile(name: "my-vector-profile", algorithmConfigurationName: "my-hnsw-vector-config-1"));

var definition = new SearchIndex(indexName)
{
    Fields = allFields,
    Suggesters = { suggester },
    VectorSearch = vectorSearch // <-- Add this line
};

// 2. Create or update the index
Console.WriteLine($"Creating or updating index '{indexName}'...");
var result = await indexClient.CreateOrUpdateIndexAsync(definition);
Console.WriteLine($"Index '{result.Value.Name}' updated.");

// 3. Run an empty query (returns selected fields, all documents)
Console.WriteLine("\nSimple query: all documents, selected fields");
var simpleResults = await searchClient.SearchAsync<SearchDocument>(
    "*",
    new SearchOptions { QueryType = SearchQueryType.Simple, Select = { "HotelName", "Description" }, IncludeTotalCount = true });
Console.WriteLine($"Total Documents Matching Query: {simpleResults.Value.TotalCount}");
await foreach (var doc in simpleResults.Value.GetResultsAsync())
{
    Console.WriteLine(doc.Document["@search.score"]);
    Console.WriteLine(doc.Document["HotelName"]);
    Console.WriteLine($"Description: {doc.Document["Description"]}");
}
