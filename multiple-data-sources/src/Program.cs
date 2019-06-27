using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure;

namespace AzureSearch.SDKHowTo
{
    public sealed class Program
    {
        // This sample shows how to create an Azure Search index, and how to index and merge data
        // from two different data sources.

        static IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        static IConfigurationRoot configuration = builder.Build();

        static async Task Main(string[] args)
        {
            // First, connect to a search service
            SearchServiceClient searchService = CreateSearchServiceClient(configuration);

            string indexName = "hotel-rooms-sample";

            // Next, create the Azure Search index
            Console.WriteLine("Deleting index...\n");
            await DeleteIndexIfExists(indexName, searchService);

            Console.WriteLine("Creating index...\n");
            await CreateIndex(indexName, searchService);

            // Set up a CosmosDB data source and indexer, and run the indexer to import hotel data
            Console.WriteLine("Indexing Cosmos DB hotel data...\n");
            await CreateAndRunCosmosDbIndexer(indexName, searchService);

            // Set up a Blob Storage data source and indexer, and run the indexer to merge hotel room data
            Console.WriteLine("Indexing and merging hotel room data from blob storage...\n");
            await CreateAndRunBlobIndexer(indexName, searchService);

            Console.WriteLine("Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchServiceClient searchService = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return searchService;
        }

        private static async Task DeleteIndexIfExists(string indexName, SearchServiceClient searchService)
        {
            if (searchService.Indexes.Exists(indexName))
            {
                await searchService.Indexes.DeleteAsync(indexName);
            }
        }

        private static async Task CreateIndex(string indexName, SearchServiceClient searchService)
        {
            // Create a new search index structure that matches the properties of the Hotel class.
            // The Address and Room classes are referenced from the Hotel class. The FieldBuilder
            // will enumerate these to create a complex data structure for the index.
            var definition = new Index()
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Hotel>()
            };
            await searchService.Indexes.CreateAsync(definition);
        }

        private static async Task CreateAndRunCosmosDbIndexer(string indexName, SearchServiceClient searchService)
        {
            // Append the database name to the connection string
            string cosmosConnectString = 
                configuration["CosmosDBConnectionString"]
                + ";Database=" 
                + configuration["CosmosDBDatabaseName"];

            DataSource cosmosDbDataSource = DataSource.CosmosDb(
                name: configuration["CosmosDBDatabaseName"], 
                cosmosDbConnectionString: cosmosConnectString,
                collectionName: "hotels",
                useChangeDetection: true);

            // The Cosmos DB data source does not need to be deleted if it already exists,
            // but the connection string might need to be updated if it has changed.
            await searchService.DataSources.CreateOrUpdateAsync(cosmosDbDataSource);

            Console.WriteLine("Creating Cosmos DB indexer...\n");

            Indexer cosmosDbIndexer = new Indexer(
                name: "hotel-rooms-cosmos-indexer",
                dataSourceName: cosmosDbDataSource.Name,
                targetIndexName: indexName,
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)));

            // Indexers keep metadata about how much they have already indexed.
            // If we already ran this sample, the indexer will remember that it already
            // indexed the sample data and not run again.
            // To avoid this, reset the indexer if it exists.
            bool exists = await searchService.Indexers.ExistsAsync(cosmosDbIndexer.Name);
            if (exists)
            {
                await searchService.Indexers.ResetAsync(cosmosDbIndexer.Name);
            }
            await searchService.Indexers.CreateOrUpdateAsync(cosmosDbIndexer);

            Console.WriteLine("Running Cosmos DB indexer...\n");

            try
            {
                await searchService.Indexers.RunAsync(cosmosDbIndexer.Name);
            }
            catch (CloudException e) when (e.Response.StatusCode == (HttpStatusCode)429)
            {
                Console.WriteLine("Failed to run indexer: {0}", e.Response.Content);
            }
        }

        private static async Task CreateAndRunBlobIndexer(string indexName, SearchServiceClient searchService)
        {
            DataSource blobDataSource = DataSource.AzureBlobStorage(
                name: configuration["BlobStorageAccountName"],
                storageConnectionString: configuration["BlobStorageConnectionString"],
                containerName: "hotel-rooms");

            // The blob data source does not need to be deleted if it already exists,
            // but the connection string might need to be updated if it has changed.
            await searchService.DataSources.CreateOrUpdateAsync(blobDataSource);

            Console.WriteLine("Creating Blob Storage indexer...\n");

            // Add a field mapping to match the Id field in the documents to 
            // the HotelId key field in the index
            List<FieldMapping> map = new List<FieldMapping> {
                new FieldMapping("Id", "HotelId")
            };

            Indexer blobIndexer = new Indexer(
                name: "hotel-rooms-blob-indexer",
                dataSourceName: blobDataSource.Name,
                targetIndexName: indexName,
                fieldMappings: map,
                parameters: new IndexingParameters().ParseJson(),
                schedule: new IndexingSchedule(TimeSpan.FromDays(1)));

            // Reset the indexer if it already exists
            bool exists = await searchService.Indexers.ExistsAsync(blobIndexer.Name);
            if (exists)
            {
                await searchService.Indexers.ResetAsync(blobIndexer.Name);
            }
            await searchService.Indexers.CreateOrUpdateAsync(blobIndexer);

            Console.WriteLine("Running Blob Storage indexer...\n");

            try
            {
                await searchService.Indexers.RunAsync(blobIndexer.Name);
            }
            catch (CloudException e) when (e.Response.StatusCode == (HttpStatusCode)429)
            {
                Console.WriteLine("Failed to run indexer: {0}", e.Response.Content);
            }
        }
    }
}
