namespace AzureSearchQuickstart
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.Azure.Search;
    using Microsoft.Azure.Search.Models;
    using Microsoft.Extensions.Configuration;

    class Program
    {
        // Demonstrates index delete, create, load, and query
        // Commented-out code is uncommented in later steps
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            string indexName = configuration["SearchIndexName"];

            Console.WriteLine("{0}", "Deleting index...\n");
            DeleteIndexIfExists(indexName, serviceClient);

            Console.WriteLine("{0}", "Creating index...\n");
            CreateIndex(indexName, serviceClient);

            // Uncomment next 3 lines in "2 - Load documents"
            // ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);
            // Console.WriteLine("{0}", "Uploading documents...\n");
            // UploadDocuments(indexClient);

            // Uncomment next 2 lines in "3 - Search an index"
            // Console.WriteLine("{0}", "Searching index...\n");
            // RunQueries(indexClient);

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

        // Upload documents as a batch
        private static void UploadDocuments(ISearchIndexClient indexClient)
        {
            var actions = new IndexAction<Hotel>[]
            {
                IndexAction.Upload(
                    new Hotel()
                    {
                        HotelId = "1",
                        HotelName = "Secret Point Motel",
                        Description = "The hotel is ideally located on the main commercial artery of the city in the heart of New York. A few minutes away is Time's Square and the historic centre of the city, as well as other places of interest that make New York one of America's most attractive and cosmopolitan cities.",
                        DescriptionFr = "L'hôtel est idéalement situé sur la principale artère commerciale de la ville en plein cœur de New York. A quelques minutes se trouve la place du temps et le centre historique de la ville, ainsi que d'autres lieux d'intérêt qui font de New York l'une des villes les plus attractives et cosmopolites de l'Amérique.",
                        Category = "Boutique",
                        Tags = new[] { "pool", "air conditioning", "concierge" },
                        ParkingIncluded = false,
                        LastRenovationDate = new DateTimeOffset(1970, 1, 18, 0, 0, 0, TimeSpan.Zero),
                        Rating = 3.6,
                        Address = new Address()
                        {
                            StreetAddress = "677 5th Ave",
                            City = "New York",
                            StateProvince = "NY",
                            PostalCode = "10022",
                            Country = "USA"
                        }
                    }
                ),
                IndexAction.Upload(
                    new Hotel()
                    {
                        HotelId = "2",
                        HotelName = "Twin Dome Motel",
                        Description = "The hotel is situated in a  nineteenth century plaza, which has been expanded and renovated to the highest architectural standards to create a modern, functional and first-class hotel in which art and unique historical elements coexist with the most modern comforts.",
                        DescriptionFr = "L'hôtel est situé dans une place du XIXe siècle, qui a été agrandie et rénovée aux plus hautes normes architecturales pour créer un hôtel moderne, fonctionnel et de première classe dans lequel l'art et les éléments historiques uniques coexistent avec le confort le plus moderne.",
                        Category = "Boutique",
                        Tags = new[] { "pool", "free wifi", "concierge" },
                        ParkingIncluded = false,
                        LastRenovationDate =  new DateTimeOffset(1979, 2, 18, 0, 0, 0, TimeSpan.Zero),
                        Rating = 3.60,
                        Address = new Address()
                        {
                            StreetAddress = "140 University Town Center Dr",
                            City = "Sarasota",
                            StateProvince = "FL",
                            PostalCode = "34243",
                            Country = "USA"
                        }
                    }
                ),
                IndexAction.Upload(
                    new Hotel()
                    {
                        HotelId = "3",
                        HotelName = "Triple Landscape Hotel",
                        Description = "The Hotel stands out for its gastronomic excellence under the management of William Dough, who advises on and oversees all of the Hotel’s restaurant services.",
                        DescriptionFr = "L'hôtel est situé dans une place du XIXe siècle, qui a été agrandie et rénovée aux plus hautes normes architecturales pour créer un hôtel moderne, fonctionnel et de première classe dans lequel l'art et les éléments historiques uniques coexistent avec le confort le plus moderne.",
                        Category = "Resort and Spa",
                        Tags = new[] { "air conditioning", "bar", "continental breakfast" },
                        ParkingIncluded = true,
                        LastRenovationDate = new DateTimeOffset(2015, 9, 20, 0, 0, 0, TimeSpan.Zero),
                        Rating = 4.80,
                        Address = new Address()
                        {
                            StreetAddress = "3393 Peachtree Rd",
                            City = "Atlanta",
                            StateProvince = "GA",
                            PostalCode = "30326",
                            Country = "USA"
                        }
                    }
                ),
                IndexAction.Upload(
                    new Hotel()
                    {
                        HotelId = "4",
                        HotelName = "Sublime Cliff Hotel",
                        Description = "Sublime Cliff Hotel is located in the heart of the historic center of Sublime in an extremely vibrant and lively area within short walking distance to the sites and landmarks of the city and is surrounded by the extraordinary beauty of churches, buildings, shops and monuments. Sublime Cliff is part of a lovingly restored 1800 palace.",
                        DescriptionFr = "Le sublime Cliff Hotel est situé au coeur du centre historique de sublime dans un quartier extrêmement animé et vivant, à courte distance de marche des sites et monuments de la ville et est entouré par l'extraordinaire beauté des églises, des bâtiments, des commerces et Monuments. Sublime Cliff fait partie d'un Palace 1800 restauré avec amour.",
                        Category = "Boutique",
                        Tags = new[] { "concierge", "view", "24-hour front desk service" },
                        ParkingIncluded = true,
                        LastRenovationDate = new DateTimeOffset(1960, 2, 06, 0, 0, 0, TimeSpan.Zero),
                        Rating = 4.6,
                        Address = new Address()
                        {
                            StreetAddress = "7400 San Pedro Ave",
                            City = "San Antonio",
                            StateProvince = "TX",
                            PostalCode = "78216",
                            Country = "USA"
                        }
                    }
                ),
             };

            var batch = IndexBatch.New(actions);

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

            // Wait 2 seconds before starting queries
            Console.WriteLine("Waiting for indexing...\n");
            Thread.Sleep(2000);
        }

        // Add query logic and handle results
        private static void RunQueries(ISearchIndexClient indexClient)
        {
            SearchParameters parameters;
            DocumentSearchResult<Hotel> results;

            // Query 1 
            Console.WriteLine("Query 1: Search for term 'Atlanta', returning the full document");
            parameters = new SearchParameters();
            results = indexClient.Documents.Search<Hotel>("Atlanta", parameters);
            WriteDocuments(results);

            // Query 2
            Console.WriteLine("Query 2: Search on the term 'Atlanta', returning selected fields");
            Console.WriteLine("Returning only these fields: HotelName, Tags, Address:\n");
            parameters =
                new SearchParameters()
                {
                    Select = new[] { "HotelName", "Tags", "Address" },
                };
            results = indexClient.Documents.Search<Hotel>("Atlanta", parameters);
            WriteDocuments(results);

            // Query 3
            Console.WriteLine("Query 3: Search for the terms 'restaurant' and 'wifi'");
            Console.WriteLine("Return only these fields: HotelName, Description, and Tags:\n");
            parameters =
                new SearchParameters()
                {
                    Select = new[] { "HotelName", "Description", "Tags" }
                };
            results = indexClient.Documents.Search<Hotel>("restaurant, wifi", parameters);
            WriteDocuments(results);

            // Query 4 -filtered query
            Console.WriteLine("Query 4: Filter on ratings greater than 4");
            Console.WriteLine("Returning only these fields: HotelName, Rating:\n");
            parameters =
                new SearchParameters()
                {
                    Filter = "Rating gt 4",
                    Select = new[] { "HotelName", "Rating" }
                };
            results = indexClient.Documents.Search<Hotel>("*", parameters);
            WriteDocuments(results);

            // Query 5 - top 2 results
            Console.WriteLine("Query 5: Search on term 'boutique'");
            Console.WriteLine("Sort by rating in descending order, taking the top two results");
            Console.WriteLine("Returning only these fields: HotelId, HotelName, Category, Rating:\n");
            parameters =
                new SearchParameters()
                {
                    OrderBy = new[] { "Rating desc" },
                    Select = new[] { "HotelId", "HotelName", "Category", "Rating" },
                    Top = 2
                };
            results = indexClient.Documents.Search<Hotel>("boutique", parameters);
            WriteDocuments(results);
        }

        // Handle search results, writing output to the console
        private static void WriteDocuments(DocumentSearchResult<Hotel> searchResults)
        {
            foreach (SearchResult<Hotel> result in searchResults.Results)
            {
                Console.WriteLine(result.Document);
            }

            Console.WriteLine();
        }
    }
}