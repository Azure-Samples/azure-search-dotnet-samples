using System;
using System.Collections.Generic;
using Microsoft.Azure.Search;
using Microsoft.Extensions.Configuration;
using Index = Microsoft.Azure.Search.Models.Index;

namespace AzSearchPerformance
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

            //Console.WriteLine("{0}", "Finding optimal batch size...\n");
            UploadDataUsingSDK.TestVariousBatchSizes(indexClient, 3);

            //Console.WriteLine("{0}", "Finding optimal number of threads...\n");
            //UploadDataUsingSDK.TestVariousThreadCounts(indexClient, 20000, 1000, 3).Wait();

            DataGenerator dg = new DataGenerator();
            List<Hotel> hotels = dg.GetHotels(100000, "large");

            Console.WriteLine("{0}", "Uploading using exponential backoff...\n");
            //UploadDataUsingSDK.UploadUsingExponentialBackoff(indexClient, hotels, 1000, 12).Wait();

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

    }
}
