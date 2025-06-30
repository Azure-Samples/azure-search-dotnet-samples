using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;


// Load configuration from appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

string searchEndpoint = config["AzureSearch:Endpoint"] ?? throw new Exception("AzureSearch:Endpoint missing");
string indexName = config["AzureSearch:IndexName"] ?? throw new Exception("AzureSearch:IndexName missing");

// Read documents from HotelData.json
var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "HotelData.json");
if (!File.Exists(jsonPath))
{
    Console.WriteLine($"File not found: {jsonPath}");
    return;
}
var json = await File.ReadAllTextAsync(jsonPath);

// Parse JSON array into a List<string> where each string is a JSON object
var hotelJsonStrings = new List<string>();
using (JsonDocument doc = JsonDocument.Parse(json))
{
    if (doc.RootElement.ValueKind == JsonValueKind.Array)
    {
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            hotelJsonStrings.Add(element.GetRawText());
        }
    }
    else
    {
        Console.WriteLine("HotelData.json root is not a JSON array.");
        return;
    }
}

// hotelJsonStrings now contains each hotel as a JSON string
// Example: print the first hotel JSON string
if (hotelJsonStrings.Count > 0)
{
    Console.WriteLine("First hotel JSON string:");
    Console.WriteLine(hotelJsonStrings[0]);
}

// Deserialize into a strongly-typed list
var hotels = new List<Hotel>();
foreach (var hotelJson in hotelJsonStrings)
{
    var hotel = JsonSerializer.Deserialize<Hotel>(hotelJson);
    if (hotel != null)
    {
        hotels.Add(hotel);
    }
}

if (hotels == null || hotels.Count == 0)
{
    Console.WriteLine("No documents found in HotelData.json.");
    return;
}

// Create SearchClient
var credential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new AzureDeveloperCliCredential());
var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);

// Upload documents
try
{
    var result = await searchClient.UploadDocumentsAsync(hotels);
    foreach (var r in result.Value.Results)
    {
        Console.WriteLine($"Key: {r.Key}, Succeeded: {r.Succeeded}, ErrorMessage: {r.ErrorMessage}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Failed to upload documents: " + ex);
}

public class Hotels
{
    public List<Hotel> HotelsList { get; set; } = new List<Hotel>();
}

// Define C# classes matching your JSON structure
public class Address
{
    [AllowNull]
    public string StreetAddress { get; set; }

    [AllowNull]
    public string City { get; set; }

    [AllowNull]
    public string StateProvince { get; set; }

    [AllowNull]
    public string PostalCode { get; set; }

    [AllowNull]
    public string Country { get; set; }
}

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
    public string ParkingIncluded { get; set; }
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