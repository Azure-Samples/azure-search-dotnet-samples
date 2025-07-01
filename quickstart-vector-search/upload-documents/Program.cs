using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using VectorSearchShared;

// Load configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

string searchEndpoint = config["AzureSearch:Endpoint"] ?? throw new Exception("AzureSearch:Endpoint missing");
string indexName = config["AzureSearch:IndexName"] ?? throw new Exception("AzureSearch:IndexName missing");

// Set up Azure credentials and search client
var credential = new ChainedTokenCredential(
    new AzureCliCredential(),
    new AzureDeveloperCliCredential());
var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);

var jsonPath = Path.Combine(Directory   .GetCurrentDirectory(), "HotelData.json");
if (!File.Exists(jsonPath))
{
    Console.WriteLine($"File not found: {jsonPath}");
    return;
}

// Read and parse hotel data
var json = await File.ReadAllTextAsync(jsonPath);
List<Hotel> hotels = new List<Hotel>();
try
{
    using var doc = JsonDocument.Parse(json);
    if (doc.RootElement.ValueKind != JsonValueKind.Array)
    {
        Console.WriteLine("HotelData.json root is not a JSON array.");
        return;
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
    return;
}

if (hotels.Count == 0)
{
    Console.WriteLine("No documents found in HotelData.json.");
    return;
}
else
{
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

