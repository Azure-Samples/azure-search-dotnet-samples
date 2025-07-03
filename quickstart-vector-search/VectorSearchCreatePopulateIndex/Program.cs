using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using VectorSearchShared;

// Load configuration from appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

string searchEndpoint = config["AzureSearch:Endpoint"] ?? throw new Exception("AzureSearch:Endpoint missing");
string indexName = config["AzureSearch:IndexName"] ?? throw new Exception("AzureSearch:IndexName missing");

var credential = new AzureDeveloperCliCredential();
var indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);

// Define the index schema (fields, suggesters, semantic config)
await CreateSearchIndex(indexName, indexClient);

// Upload documents to the index
await UploadDocs(searchClient);

// Run a test query
await RunTestSearch(searchClient);

// <SnippetCreateSearchindex>
static async Task CreateSearchIndex(string indexName, SearchIndexClient indexClient)
{
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
    new VectorSearchField("DescriptionVector", 1536, "my-vector-profile"),
    new SearchableField("Category") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true },
    new SearchableField("Tags", collection: true) { AnalyzerName = LexicalAnalyzerName.EnMicrosoft, IsFacetable = true, IsFilterable = true },
    new SimpleField("ParkingIncluded", SearchFieldDataType.Boolean) { IsFacetable = true, IsFilterable = true },
    new SimpleField("LastRenovationDate", SearchFieldDataType.DateTimeOffset) { IsSortable = true },
    new SimpleField("Rating", SearchFieldDataType.Double) { IsFacetable = true, IsFilterable = true, IsSortable = true },
    addressField,
    new SimpleField("Location", SearchFieldDataType.GeographyPoint) { IsFilterable = true, IsSortable = true },
};

    // Create the suggester configuration
    var suggester = new SearchSuggester("sg", new[] { "Address/City", "Address/Country" });

    // Create the semantic search
    var semanticSearch = new SemanticSearch()
    {
        Configurations =
        {
            new SemanticConfiguration(
            name: "semantic-config",
            prioritizedFields: new SemanticPrioritizedFields
            {
                TitleField = new SemanticField("HotelName"),
                KeywordsFields = { new SemanticField("Category") },
                ContentFields = { new SemanticField("Description") }
            })
    }
    };

    // Add vector search configuration
    var vectorSearch = new VectorSearch();
    vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(name: "my-hnsw-vector-config-1"));
    vectorSearch.Profiles.Add(new VectorSearchProfile(name: "my-vector-profile", algorithmConfigurationName: "my-hnsw-vector-config-1"));

    var definition = new SearchIndex(indexName)
    {
        Fields = allFields,
        Suggesters = { suggester },
        VectorSearch = vectorSearch,
        SemanticSearch = semanticSearch
    };

    // Create or update the index
    Console.WriteLine($"Creating or updating index '{indexName}'...");
    var result = await indexClient.CreateOrUpdateIndexAsync(definition);
    Console.WriteLine($"Index '{result.Value.Name}' updated.");
    Console.WriteLine();
}
// </SnippetCreateSearchindex>


static async Task RunTestSearch(SearchClient searchClient)
{
    Console.WriteLine("Simple query: all documents, selected fields");
    var simpleResults = await searchClient.SearchAsync<SearchDocument>(
        "*",
        new SearchOptions
        {
            QueryType = SearchQueryType.Simple,
            Select = { "HotelName", "Description" },
            IncludeTotalCount = true
        });
    Console.WriteLine($"Total Documents Matching Query: {simpleResults.Value.TotalCount}");

    await foreach (var doc in simpleResults.Value.GetResultsAsync())
    {
        Console.WriteLine($"Hotel Name: {doc.Document["HotelName"]}, Hotel Description: {doc.Document["Description"]}");
    }
}

// <SnippetUploadDocs>
static async Task UploadDocs(SearchClient searchClient)
{
    var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "HotelData.json");

    // Read and parse hotel data
    var json = await File.ReadAllTextAsync(jsonPath);
    List<Hotel> hotels = new List<Hotel>();
    try
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("HotelData.json root is not a JSON array.");
        }
        // Deserialize all hotel objects
        hotels = doc.RootElement.EnumerateArray()
            .Select(e => JsonSerializer.Deserialize<Hotel>(e.GetRawText()))
            .Where(h => h != null)
            .ToList();
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Failed to parse HotelData.json: {ex.Message}");
    }

    try
    {
        // Upload hotel documents to Azure Search
        var result = await searchClient.UploadDocumentsAsync(hotels);
        foreach (var r in result.Value.Results)
        {
            Console.WriteLine($"Key: {r.Key}, Succeeded: {r.Succeeded}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to upload documents: " + ex);
    }
}
// </SnippetUploadDocs>