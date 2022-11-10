using System.CommandLine;
using System.Text.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace export_data
{
    public static class Program
    {
        // Supported field types for the partition bounds
        // To learn more about field types, please visit https://learn.microsoft.com/rest/api/searchservice/supported-data-types
        private static readonly SearchFieldDataType[] SupportedFieldTypes = new[]
        {
            SearchFieldDataType.DateTimeOffset
        };

        public static async Task Main(string[] args)
        {
            // Setup command line arguments
            var endpointOption = new Option<string>(
                name: "--endpoint",
                description: "Endpoint of the search service to export data from. Example: https://example.search.windows.net")
            {
                IsRequired = true,
            };
            var adminKeyOption = new Option<string>(
                name: "--admin-key",
                description: "Admin key to the search service to export data from")
            {
                IsRequired = true
            };
            var indexOption = new Option<string>(
                name: "--index-name",
                description: "Name of the index to export data from")
            {
                IsRequired = true
            };
            var fieldOption = new Option<string>(
                name: "--field-name",
                description: "Name of field used to partition the index data. This field must be filterable and sortable.")
            {
                IsRequired = true
            };
            var upperBoundOption = new Option<string>(
                name: "--upper-bound",
                description: "Largest value to use to partition the index data. Defaults to the largest value in the index.",
                getDefaultValue: () => null);
            var lowerBoundOption = new Option<string>(
                name: "--lower-bound",
                description: "Smallest value to use to partition the index data. Defaults to the smallest value in the index.",
                getDefaultValue: () => null);
            var partitionFileOption = new Option<string>(
                name: "--partition-path",
                description: "Path of the file with JSON description of partitions. Should end in .json. Default is <index name>-partitions.json",
                getDefaultValue: () => null);
            var partitionFileRequiredOption = new Option<string>(
                name: "--partition-path",
                description: "Path of the file with JSON description of partitions. Should end in .json.")
            {
                IsRequired = true
            };
            var exportDirectoryOption = new Option<string>(
                name: "--export-path",
                description: "Directory to write JSON Lines partition files to. Every line in the partition file contains a JSON object with the contents of the Search document. Format of file names is <index name>-<partition id>-documents.json",
                getDefaultValue: () => ".");
            var concurrentPartitionsOption = new Option<int>(
                name: "--concurrent-partitions",
                description: "Number of partitions to concurrently export. Default is 2",
                getDefaultValue: () => 2);
            var pageSizeOption = new Option<int>(
                name: "--page-size",
                description: "Page size to use when running export queries. Default is 1000",
                getDefaultValue: () => 1000);
            var includePartitionsOption = new Option<int[]>(
                name: "--include-partition",
                description: "List of partitions by index to include in the export. Example: --include-partition 0 --include-partition 1 only runs the export on first 2 partitions",
                getDefaultValue: () => null);
            var excludePartitionsOption = new Option<int[]>(
                name: "--exclude-partition",
                description: "List of partitions by index to exclude from the export. Example: --exclude-partition 0 --exclude-partition 1 runs the export on every partition except the first 2",
                getDefaultValue: () => null);
            var exportFieldOption = new Option<string>(
                name: "--export-field-name",
                description: "Name of the Edm.Boolean field the continuous export process will update to track which documents have been exported. Default is 'exported'",
                getDefaultValue: () => "exported");
            var exportFileOption = new Option<string>(
                name: "--export-path",
                description: "Path to write JSON Lines file to. Every line in the file contains a JSON object with the contents of the Search document. Format of file is <index name>-documents.json",
                getDefaultValue: () => null);
            var includeFieldsOption = new Option<string[]>(
                name: "--include-field",
                description: "List of fields to include in the export. Example: --include-field field1 --include-field field2.",
                getDefaultValue: () => null);
            var excludeFieldsOption = new Option<string[]>(
                name: "--exclude-field",
                description: "List of fields to exclude in the export. Example: --exclude-field field1 --exclude-field field2.",
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
                (SearchField field, SearchClient searchClient) = await InitializeFieldAndSearchClientAsync(endpoint, adminKey, indexName, fieldName);

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
                partitionFileOption
            };
            partitionCommand.SetHandler(async (string endpoint, string adminKey, string indexName, string fieldName, string inputLowerBound, string inputUpperBound, string partitionFilePath) =>
            {
                if (string.IsNullOrEmpty(partitionFilePath))
                {
                    partitionFilePath = $"{indexName}-partitions.json";
                }

                (SearchField field, SearchClient searchClient) = await InitializeFieldAndSearchClientAsync(endpoint, adminKey, indexName, fieldName);
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


                List<Partition> partitions = await new PartitionGenerator(searchClient, field, lowerBound, upperBound).GeneratePartitions();
                var output = new PartitionFile
                {
                    Endpoint = endpoint,
                    IndexName = indexName,
                    FieldName = fieldName,
                    TotalDocumentCount = partitions.Sum(partition => partition.DocumentCount),
                    Partitions = partitions
                };
                File.WriteAllText(partitionFilePath, JsonSerializer.Serialize(output, options: Util.SerializerOptions));
                Console.WriteLine($"Wrote partitions to {partitionFilePath}");
            }, endpointOption, adminKeyOption, indexOption, fieldOption, lowerBoundOption, upperBoundOption, partitionFileOption);

            var exportPartitionsCommand = new Command(name: "export-partitions", description: "Exports data from a search index using a pre-generated partition file from partition-index")
            {
                partitionFileRequiredOption,
                adminKeyOption,
                exportDirectoryOption,
                concurrentPartitionsOption,
                pageSizeOption,
                includePartitionsOption,
                excludePartitionsOption,
                includeFieldsOption,
                excludeFieldsOption
            };
            exportPartitionsCommand.SetHandler(async (invocationContext) =>
            {
                string partitionFilePath = invocationContext.ParseResult.GetValueForOption<string>(partitionFileRequiredOption);
                string adminKey = invocationContext.ParseResult.GetValueForOption<string>(adminKeyOption);
                string exportDirectory = invocationContext.ParseResult.GetValueForOption<string>(exportDirectoryOption);
                int concurrentPartitions = invocationContext.ParseResult.GetValueForOption<int>(concurrentPartitionsOption);
                int pageSize = invocationContext.ParseResult.GetValueForOption<int>(pageSizeOption);
                int[] partitionsToInclude = invocationContext.ParseResult.GetValueForOption<int[]>(includePartitionsOption);
                int[] partitionsToExclude = invocationContext.ParseResult.GetValueForOption<int[]>(excludePartitionsOption);
                string[] fieldsToInclude = invocationContext.ParseResult.GetValueForOption<string[]>(includeFieldsOption);
                string[] fieldsToExclude = invocationContext.ParseResult.GetValueForOption<string[]>(excludeFieldsOption);

                if (partitionsToExclude.Any() && partitionsToInclude.Any())
                {
                    throw new ArgumentException("Only pass either --include-partition or --exclude-partition, not both");
                }

                using FileStream input = File.OpenRead(partitionFilePath);
                var partitionFile = JsonSerializer.Deserialize<PartitionFile>(input, options: Util.SerializerOptions);
                SearchClient searchClient = InitializeSearchClient(partitionFile.Endpoint, adminKey, partitionFile.IndexName);
                SearchIndexClient searchIndexClient = InitializeSearchIndexClient(partitionFile.Endpoint, adminKey);
                SearchIndex index = await searchIndexClient.GetIndexAsync(partitionFile.IndexName);

                await new PartitionExporter(
                    partitionFile,
                    searchClient,
                    index,
                    exportDirectory,
                    concurrentPartitions,
                    pageSize,
                    partitionsToInclude,
                    partitionsToExclude.ToHashSet(),
                    fieldsToInclude,
                    fieldsToExclude.ToHashSet())
                .ExportAsync();
            });

            var exportContinuousCommand = new Command(name: "export-continuous", description: "Exports data from a search service by adding a column to track which documents have been exported and continually updating it")
            {
                endpointOption,
                adminKeyOption,
                indexOption,
                exportFieldOption,
                pageSizeOption,
                exportFileOption,
                includeFieldsOption,
                excludeFieldsOption
            };
            exportContinuousCommand.SetHandler(async (endpoint, adminKey, indexName, exportField, pageSize, exportFilePath, fieldsToInclude, fieldsToExclude) =>
            {
                if (fieldsToInclude.Any() && fieldsToExclude.Any())
                {
                    throw new ArgumentException("Only pass either --include-field or --exclude-field, not both");
                }

                if (string.IsNullOrEmpty(exportFilePath))
                {
                    exportFilePath = $"{indexName}-documents.json";
                }

                SearchClient searchClient = InitializeSearchClient(endpoint, adminKey, indexName);
                SearchIndexClient searchIndexClient = InitializeSearchIndexClient(endpoint, adminKey);
                SearchIndex index = await searchIndexClient.GetIndexAsync(indexName);

                await new ContinuousExporter(searchClient, index, searchIndexClient, exportField, pageSize, exportFilePath, fieldsToInclude, fieldsToExclude.ToHashSet()).ExportAsync();
                
            }, endpointOption, adminKeyOption, indexOption, exportFieldOption, pageSizeOption, exportFileOption, includeFieldsOption, excludeFieldsOption);

            var rootCommand = new RootCommand(description: "Export data from a search index. Requires a filterable and sortable field.")
            {
                boundsCommand,
                partitionCommand,
                exportPartitionsCommand,
                exportContinuousCommand
            };
            await rootCommand.InvokeAsync(args);
        }

        public static async Task<(SearchField field, SearchClient searchClient)> InitializeFieldAndSearchClientAsync(string endpoint, string adminKey, string indexName, string fieldName)
        {
            var endpointUri = new Uri(endpoint);
            var credential = new AzureKeyCredential(adminKey);
            var searchClient = new SearchClient(endpointUri, indexName, credential);
            var searchIndexClient = new SearchIndexClient(endpointUri, credential);
            SearchField field = await GetFieldAsync(searchIndexClient, indexName, fieldName);
            return (field, searchClient);
        }

        public static SearchClient InitializeSearchClient(string endpoint, string key, string indexName)
        {
            var endpointUri = new Uri(endpoint);
            var credential = new AzureKeyCredential(key);
            return new SearchClient(endpointUri, indexName, credential);
        }

        public static SearchIndexClient InitializeSearchIndexClient(string endpoint, string key)
        {
            var endpointUri = new Uri(endpoint);
            var credential = new AzureKeyCredential(key);
            return new SearchIndexClient(endpointUri, credential);
        }

        // Fetch the index definition and validate that the field meets the sortable and filterable requirements
        public static async Task<SearchField> GetFieldAsync(SearchIndexClient searchIndexClient, string indexName, string fieldName)
        {
            SearchIndex index = await searchIndexClient.GetIndexAsync(indexName);
            SearchField field = index.Fields.FirstOrDefault(field => field.Name == fieldName);

            if (field == null)
            {
                throw new ArgumentException($"Could not find {fieldName} in {indexName}", nameof(fieldName));
            }
            if (!(field.IsSortable ?? false) || !(field.IsFilterable ?? false))
            {
                throw new ArgumentException($"{fieldName} must be sortable and filterable", nameof(fieldName));
            }
            if (!SupportedFieldTypes.Contains(field.Type))
            {
                string supportedFieldTypesList = string.Join(", ", SupportedFieldTypes.Select(type => type.ToString()));
                throw new ArgumentException($"{fieldName} is of type {field.Type}, supported types {supportedFieldTypesList}", nameof(fieldName));
            }

            return field;
        }
    }
}