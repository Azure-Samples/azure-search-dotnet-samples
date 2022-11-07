using System.CommandLine;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;

namespace archive_data
{
    public static class Program
    {
        private static readonly SearchFieldDataType[] SupportedFieldTypes = new[]
        {
            SearchFieldDataType.DateTimeOffset,
            SearchFieldDataType.Double,
            SearchFieldDataType.Int64,
            SearchFieldDataType.Int32,
            SearchFieldDataType.String
        };

        public static async Task Main(string[] args)
        {
            // Read settings from appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var endpointOption = new Option<string>(
                name: "--endpoint",
                description: "Endpoint of the search service to export data from",
                getDefaultValue: () => configuration["searchEndpoint"]);
            var adminKeyOption = new Option<string>(
                name: "--admin-key",
                description: "Admin key to the search service to export data from",
                getDefaultValue: () => configuration["searchAdminKey"]);
            var indexOption = new Option<string>(
                name: "--index-name",
                description: "Name of the index to export data from",
                getDefaultValue: () => configuration["indexName"]);
            var fieldOption = new Option<string>(
                name: "--field-name",
                description: "Name of field used to partition the index data. This field must be filterable and sortable.",
                getDefaultValue: () => configuration["fieldName"]);
            var upperBoundOption = new Option<string>(
                name: "--upper-bound",
                description: "Largest value to use to partition the index data. Defaults to the largest value in the index.",
                getDefaultValue: () => null);
            var lowerBoundOption = new Option<string>(
                name: "--lower-bound",
                description: "Smallest value to use to partition the index data. Defaults to the smallest value in the index.",
                getDefaultValue: () => null);

            var boundsCommand = new Command("get-bounds", "Find and display the largest and lowest value for the specified field. Used to determine how to partition index data for export")
            {
                endpointOption,
                adminKeyOption,
                indexOption,
                fieldOption
            };
            boundsCommand.SetHandler(async (string endpoint, string adminKey, string indexName, string fieldName) =>
            {
                (SearchField field, SearchClient searchClient) = await InitializeAsync(endpoint, adminKey, indexName, fieldName);

                object lowerBound = await Bound.FindLowerBoundAsync(field, searchClient);
                Console.WriteLine($"Lower Bound {Bound.SerializeBound(lowerBound)}");

                object upperBound = await Bound.FindUpperBoundAsync(field, searchClient);
                Console.WriteLine($"Upper Bound {Bound.SerializeBound(upperBound)}");
            }, endpointOption, adminKeyOption, indexOption, fieldOption);

            var partitionCommand = new Command("partition-index", "Partitions the data in the index between the upper and lower bound values into partitions with at most 100,000 documents.")
            {
                endpointOption,
                adminKeyOption,
                indexOption,
                fieldOption,
                lowerBoundOption,
                upperBoundOption,
            };
            partitionCommand.SetHandler(async (string endpoint, string adminKey, string indexName, string fieldName, string inputLowerBound, string inputUpperBound) =>
            {
                (SearchField field, SearchClient searchClient) = await InitializeAsync(endpoint, adminKey, indexName, fieldName);
                object lowerBound;
                if (string.IsNullOrEmpty(inputLowerBound))
                {
                    lowerBound = await Bound.FindLowerBoundAsync(field, searchClient);
                }
                else
                {
                    lowerBound = Bound.DeserializeBound(field.Type, inputLowerBound);
                }

                object upperBound;
                if (string.IsNullOrEmpty(inputUpperBound))
                {
                    upperBound = await Bound.FindUpperBoundAsync(field, searchClient);
                }
                else
                {
                    upperBound = Bound.DeserializeBound(field.Type, inputUpperBound);
                }


            }, endpointOption, adminKeyOption, indexOption, fieldOption, lowerBoundOption, upperBoundOption);

            var rootCommand = new RootCommand(description: "Export data from a search index. Requires a filterable and sortable field.")
            {
                boundsCommand
            };
            await rootCommand.InvokeAsync(args);
        }

        public static async Task<(SearchField field, SearchClient searchClient)> InitializeAsync(string endpoint, string adminKey, string indexName, string fieldName)
        {
            var endpointUri = new Uri(endpoint);
            var credential = new AzureKeyCredential(adminKey);
            var searchClient = new SearchClient(endpointUri, indexName, credential);
            var searchIndexClient = new SearchIndexClient(endpointUri, credential);
            SearchField field = await GetFieldAsync(searchIndexClient, indexName, fieldName);
            return (field, searchClient);
        }

        public static async Task<SearchField> GetFieldAsync(SearchIndexClient searchIndexClient, string indexName, string fieldName)
        {
            SearchIndex index = await searchIndexClient.GetIndexAsync(indexName);
            SearchField field = index.Fields.FirstOrDefault(field => field.Name == fieldName);

            if (field == null)
            {
                throw new ArgumentException(nameof(fieldName), $"Could not find {fieldName} in {indexName}");
            }
            if (!(field.IsSortable ?? false) || !(field.IsFilterable ?? false))
            {
                throw new ArgumentException(nameof(fieldName), $"{fieldName} must be sortable and filterable");
            }
            if (!SupportedFieldTypes.Contains(field.Type))
            {
                string supportedFieldTypesList = string.Join(", ", SupportedFieldTypes.Select(type => type.ToString()));
                throw new ArgumentException(nameof(fieldName), $"{fieldName} is of type {field.Type}, supported types {supportedFieldTypesList}");
            }

            return field;
        }
    }
}