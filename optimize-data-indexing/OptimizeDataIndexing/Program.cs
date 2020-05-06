using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Index = Microsoft.Azure.Search.Models.Index;

namespace OptimizeDataIndexing
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            string indexName = configuration["SearchIndexName"];
            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

            Console.WriteLine("{0}", "Deleting index...\n");
            await DeleteIndexIfExists(indexName, serviceClient);

            Console.WriteLine("{0}", "Creating index...\n");
            await CreateIndex(indexName, serviceClient);

            Console.WriteLine("{0}", "Finding optimal batch size...\n");
            await TestBatchSizes(indexClient, numTries: 3);

            //long numDocuments = 100000;
            //DataGenerator dg = new DataGenerator();
            //List<Hotel> hotels = dg.GetHotels(numDocuments);

            //Console.WriteLine("{0}", "Uploading using exponential backoff...\n");
            //ExponentialBackoff.IndexData(indexClient, hotels, 1000, 8).Wait();

            //Console.WriteLine("{0}", "Validating all data was indexed...\n");
            //ValidateIndex(serviceClient, indexName, numDocuments);

            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }


        // Create the search service client
        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
        }

        // Delete an existing index to reuse its name
        private static async Task DeleteIndexIfExists(string indexName, SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists(indexName))
            {
                await serviceClient.Indexes.DeleteAsync(indexName);
            }
        }

        private static async Task CreateIndex(string indexName, SearchServiceClient searchService)
        {
            // Create a new search index structure that matches the properties of the Hotel class.
            // The Address class is referenced from the Hotel class. The FieldBuilder
            // will enumerate these to create a complex data structure for the index.
            var definition = new Index()
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Hotel>()
            };
            await searchService.Indexes.CreateAsync(definition);
        }

        public static async Task UploadDocuments(ISearchIndexClient indexClient, List<Hotel> hotels)
        {
            var batch = IndexBatch.Upload(hotels);
            try
            {
                await indexClient.Documents.IndexAsync(batch);
            }
            catch (IndexBatchException e)
            {
                // When a service is under load, indexing might fail for some documents in the batch. 
                // Depending on your application, you can compensate by delaying and retrying. 
                // For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine("Failed to index some of the documents: {0}",
                    String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
            }
        }

        public static async Task TestBatchSizes(ISearchIndexClient indexClient, int min = 100, int max = 1000, int step = 100, int numTries = 3)
        {
            DataGenerator dg = new DataGenerator();

            Console.WriteLine("Batch Size \t Size in MB \t MB / Doc \t Time (ms) \t MB / Second");
            for (int numDocs = min; numDocs <= max; numDocs += step)
            {
                List<TimeSpan> durations = new List<TimeSpan>();
                double sizeInMb = 0.0;
                for (int x = 0; x < numTries; x++)
                {
                    List<Hotel> hotels = dg.GetHotels(numDocs, "large");

                    DateTime startTime = DateTime.Now;
                    await UploadDocuments(indexClient, hotels);
                    DateTime endTime = DateTime.Now;
                    durations.Add(endTime - startTime);

                    sizeInMb = EstimateObjectSize(hotels);
                }

                var avgDuration = durations.Average(timeSpan => timeSpan.TotalMilliseconds);
                var avgDurationInSeconds = avgDuration / 1000;
                var mbPerSecond = sizeInMb / avgDurationInSeconds;

                Console.WriteLine("{0} \t\t {1} \t\t {2} \t\t {3} \t {4}", numDocs, Math.Round(sizeInMb, 3), Math.Round(sizeInMb / numDocs, 3), Math.Round(avgDuration, 3), Math.Round(mbPerSecond, 3));

                // Pausing 2 seconds to let the search service catch its breath
                Thread.Sleep(2000);
            }

            Console.WriteLine();
        }

        // Returns size of object in MB
        public static double EstimateObjectSize(object data)
        {
            // converting object to byte[] to determine the size of the data
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            byte[] Array;

            // converting data to json for more accurate sizing
            var json = JsonConvert.SerializeObject(data);
            bf.Serialize(ms, json);
            Array = ms.ToArray();

            // converting from bytes to megabytes
            double sizeInMb = (double)Array.Length / 1000000;

            return sizeInMb;
        }

        public static void ValidateIndex(SearchServiceClient serviceClient, string indexName, long numDocsIndexed)
        {
            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

            long indexDocCount = indexClient.Documents.Count();
            while (indexDocCount != numDocsIndexed)
            {
                Console.WriteLine("Waiting for document count to update...\n");
                Thread.Sleep(2000);
                indexDocCount = indexClient.Documents.Count();
            }
            Console.WriteLine("Document Count is {0}\n", indexDocCount);


            IndexGetStatisticsResult indexStats = serviceClient.Indexes.GetStatistics(indexName);
            while (indexStats.DocumentCount != numDocsIndexed)
            {
                Console.WriteLine("Waiting for service statistics to update...\n");
                Thread.Sleep(10000);
                indexStats = serviceClient.Indexes.GetStatistics(indexName);
            }
            Console.WriteLine("Index Statistics: Document Count is {0}", indexStats.DocumentCount);
            Console.WriteLine("Index Statistics: Storage Size is {0}\n", indexStats.StorageSize);
        }

    }
}
