using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Index = Microsoft.Azure.Search.Models.Index;

namespace OptimizeDataIndexing
{
    class Program
    {
        public static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            string indexName = configuration["SearchIndexName"];
            ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

            Console.WriteLine("{0}", "Deleting index...\n");
            DeleteIndexIfExists(indexName, serviceClient);

            Console.WriteLine("{0}", "Creating index...\n");
            CreateIndex(indexName, serviceClient);

            Console.WriteLine("{0}", "Finding optimal batch size...\n");
            TestBatchSizes(indexClient, numTries: 3);

            DataGenerator dg = new DataGenerator();
            //List<Hotel> hotels = dg.GetHotels(100000, "large");

            //Console.WriteLine(EstimateObjectSize(hotels));

            //Console.WriteLine("{0}", "Uploading using exponential backoff...\n");
            //ExponentialBackoff.IndexData(indexClient, hotels, 1000, 8).Wait();

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
        private static void DeleteIndexIfExists(string indexName, SearchServiceClient serviceClient)
        {
            if (serviceClient.Indexes.Exists(indexName))
            {
                serviceClient.Indexes.Delete(indexName);
            }
        }

        // Create an index whose fields correspond to the properties of the Hotel class.
        // The Address property of Hotel will be modeled as a complex field.
        // The properties of the Address class in turn correspond to sub-fields of the Address complex field.
        // The fields of the index are defined by calling the FieldBuilder.BuildForType() method.
        private static void CreateIndex(string indexName, SearchServiceClient serviceClient)
        {
            var definition = new Index()
            {
                Name = indexName,
                Fields = FieldBuilder.BuildForType<Hotel>()
            };

            serviceClient.Indexes.Create(definition);
        }

        public static void UploadDocuments(ISearchIndexClient indexClient, List<Hotel> hotels)
        {
            var batch = IndexBatch.Upload(hotels);
            try
            {
                indexClient.Documents.Index(batch);
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

        public static void TestBatchSizes(ISearchIndexClient indexClient, int min = 100, int max = 1000, int step = 100, int numTries = 3)
        {
            DataGenerator dg = new DataGenerator();

            Console.WriteLine("Batch Size\t Size in MB \t Avg Time (ms)\t MB/second");
            for (int numDocs = min; numDocs <= max; numDocs += step)
            {
                List<TimeSpan> durations = new List<TimeSpan>();
                double sizeInMb = 0.0;
                for (int x = 0; x < numTries; x++)
                {
                    List<Hotel> hotels = dg.GetHotels(numDocs, "large");

                    DateTime startTime = DateTime.Now;
                    UploadDocuments(indexClient, hotels);
                    DateTime endTime = DateTime.Now;
                    durations.Add(endTime - startTime);

                    sizeInMb = EstimateObjectSize(hotels);
                }

                var avgDuration = durations.Average(timeSpan => timeSpan.TotalMilliseconds);
                var avgDurationInSeconds = avgDuration / 1000;
                var mbPerSecond = sizeInMb / avgDurationInSeconds;

                Console.WriteLine("{0} \t\t {1} \t\t {2} \t {3}", numDocs, Math.Round(sizeInMb, 3), Math.Round(avgDuration, 3), Math.Round(mbPerSecond, 3));

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

    }
}
