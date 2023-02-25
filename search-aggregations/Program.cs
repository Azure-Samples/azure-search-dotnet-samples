// See https://aka.ms/new-console-template for more information

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Configuration;
using System.Globalization;

// Load app settings
IConfigurationRoot appSettings = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
// Search service endpoint
var endpoint = new Uri(appSettings["searchServiceUrl"]);
// Admin key to search service
var credential = new AzureKeyCredential(appSettings["adminKey"]);
// Number of samples to upload to the index
var sampleValueCount = long.Parse(appSettings["sampleValueCount"]);
// Maximum value to generate as a sample
var sampleValueMax = double.Parse(appSettings["sampleValueMax"]);
// Minimum value to generate as a sample
var sampleValueMin = double.Parse(appSettings["sampleValueMin"]);
// Name of index to store samples
string sampleIndexName = appSettings["sampleIndexName"];

// Create sample index schema
var sampleIndex = new SearchIndex(sampleIndexName)
{
    Fields =
    {
        new SearchField("id", SearchFieldDataType.String) { IsKey = true },
        new SearchField("value", SearchFieldDataType.Double) { IsFilterable = true }
    }
};
Console.WriteLine("Dropping and recreating sample index...");
var searchIndexClient = new SearchIndexClient(endpoint, credential);
await searchIndexClient.DeleteIndexAsync(sampleIndexName);
await searchIndexClient.CreateIndexAsync(sampleIndex);

// Create sample values
var sampleValues = new double[sampleValueCount];
var random = new Random();
for (int i = 0; i < sampleValueCount; i++)
{
    sampleValues[i] = (random.NextDouble() * (sampleValueMax - sampleValueMin)) + sampleValueMin;
}

// Helper function: Create Search Documents from samples
IEnumerable<SearchDocument> GetDocuments()
{
    for (int i = 0; i < sampleValues.Length; i++)
    {
        double sampleValue = sampleValues[i];
        yield return new SearchDocument
        {
            ["id"] = i.ToString(CultureInfo.InvariantCulture),
            ["value"] = sampleValue
        };
    }
}

Console.WriteLine("Uploading samples to sample index...");
var searchClient = new SearchClient(endpoint, sampleIndexName, credential);
IndexDocumentsResult response = await searchClient.UploadDocumentsAsync(GetDocuments());
if (response.Results.Any(result => !result.Succeeded))
{
    throw new RequestFailedException($"Failed to upload documents, error {response.Results.First(result => !result.Succeeded).ErrorMessage}");
}

// Wait a few seconds before querying documents that were just uploaded
// Learn more at https://learn.microsoft.com/rest/api/searchservice/addupdate-or-delete-documents#response
TimeSpan delay = TimeSpan.FromSeconds(5);
Console.WriteLine("Waiting {0} seconds before computing statistics...", delay.TotalSeconds);
await Task.Delay(TimeSpan.FromSeconds(5));

Console.WriteLine("Computing statistics for all samples...");
await GetAggregateStatisticsUsingPaging(
    sampleValues,
    await searchClient.SearchAsync<SearchDocument>("*"));

// Use filters to restrict which values are queried
double halfPoint = (sampleValueMax + sampleValueMin) / 2.0;
Console.WriteLine("Computing statistics for all samples less than {0:.##}...", halfPoint);
await GetAggregateStatisticsUsingPaging(
    sampleValues.Where(sample => sample < halfPoint),
    await searchClient.SearchAsync<SearchDocument>(
        "*",
        options: new SearchOptions
        {
            Filter = SearchFilter.Create($"value lt {halfPoint}")
        }));

// Page through a query and compute statistics from specific values in the result
// Note that you cannot page through more than 100,000 values at a time.
// To learn more see https://learn.microsoft.com/dotnet/api/azure.search.documents.searchoptions.skip
async Task GetAggregateStatisticsUsingPaging(IEnumerable<double> sampleValues, SearchResults<SearchDocument> searchResults)
{
    var runningStatistics = new RunningStatistics();
    double sum = 0;
    AsyncPageable<SearchResult<SearchDocument>> resultPages = searchResults.GetResultsAsync();
    await foreach (Page<SearchResult<SearchDocument>> results in resultPages.AsPages())
    {
        double[] pageValues = results.Values.Select(result => result.Document["value"]).Cast<double>().ToArray();
        runningStatistics.PushRange(pageValues);
        sum += pageValues.Sum();
    }

    Console.WriteLine("Expected Count: {0}, Aggregated Count: {1}", sampleValues.Count(), runningStatistics.Count);
    Console.WriteLine("Expected Average: {0:.##}, Aggregated Average: {1:.##}", sampleValues.Average(), runningStatistics.Mean);
    Console.WriteLine("Expected Min: {0:.##}, Aggregated Min: {1:.##}", sampleValues.Min(), runningStatistics.Minimum);
    Console.WriteLine("Expected Max: {0:.##}, Aggregated Max: {1:.##}", sampleValues.Max(), runningStatistics.Maximum);
    Console.WriteLine("Expected Sum: {0:.##}, Aggregated Sum: {1:.##}", sampleValues.Sum(), sum);
}
