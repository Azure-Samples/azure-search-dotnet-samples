using System;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace AzureSearch.Quickstart

{
    class Program
    {
        static void Main(string[] args)
        {
            string serviceEndpoint = "<Put your search service URL here>";
            string indexName = "hotels-quickstart-csharp";

            // Create a SearchIndexClient to send create/delete index commands
            DefaultAzureCredential credential = new();
            Uri serviceUri = new Uri(serviceEndpoint);
            SearchIndexClient searchIndexClient = new SearchIndexClient(serviceUri, credential);

            // Create a SearchClient to load and query documents
            SearchClient srchclient = new SearchClient(serviceUri, indexName, credential);

            // Delete index if it exists
            Console.WriteLine("{0}", "Deleting index...\n");
            DeleteIndexIfExists(indexName, searchIndexClient);

            // Create index
            Console.WriteLine("{0}", "Creating index...\n");
            CreateIndex(indexName, searchIndexClient);

            SearchClient ingesterClient = searchIndexClient.GetSearchClient(indexName);

            // Load documents
            Console.WriteLine("{0}", "Uploading documents...\n");
            UploadDocuments(ingesterClient);

            // Wait 2 secondsfor indexing to complete before starting queries (for demo and console-app purposes only)
            Console.WriteLine("Waiting for indexing...\n");
            System.Threading.Thread.Sleep(2000);

            // Call the RunQueries method to invoke a series of queries
            Console.WriteLine("Starting queries...\n");
            RunQueries(srchclient);

            // End the program
            Console.WriteLine("{0}", "Complete. Press any key to end this program...\n");
            Console.ReadKey();
        }

        // Delete the hotels-quickstart-csharp index to reuse its name
        private static void DeleteIndexIfExists(string indexName, SearchIndexClient adminClient)
        {
            adminClient.GetIndexNames();
            {
                adminClient.DeleteIndex(indexName);
            }
        }
        // Create hotels-quickstart-csharp index
        private static void CreateIndex(string indexName, SearchIndexClient adminClient)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(Hotel));

            var definition = new SearchIndex(indexName, searchFields);

            var suggester = new SearchSuggester("sg", new[] { "HotelName", "Category", "Address/City", "Address/StateProvince" });
            definition.Suggesters.Add(suggester);

            adminClient.CreateOrUpdateIndex(definition);
        }

        // Upload documents in a single Upload request.
        private static void UploadDocuments(SearchClient searchClient)
        {
            IndexDocumentsBatch<Hotel> batch = IndexDocumentsBatch.Create(
                IndexDocumentsAction.Upload(
                    new Hotel()
                    {
                        HotelId = "1",
                        HotelName = "Stay-Kay City Hotel",
                        Description = "This classic hotel is fully-refurbished and ideally located on the main commercial artery of the city in the heart of New York. A few minutes away is Times Square and the historic centre of the city, as well as other places of interest that make New York one of America's most attractive and cosmopolitan cities.",
                        Category = "Boutique",
                        Tags = new[] { "view", "air conditioning", "concierge" },
                        ParkingIncluded = false,
                        LastRenovationDate = new DateTimeOffset(2022, 1, 18, 0, 0, 0, TimeSpan.Zero),
                        Rating = 3.6,
                        Address = new Address()
                        {
                            StreetAddress = "677 5th Ave",
                            City = "New York",
                            StateProvince = "NY",
                            PostalCode = "10022",
                            Country = "USA"
                        }
                    }),
                IndexDocumentsAction.Upload(
                    new Hotel()
                    {
                        HotelId = "2",
                        HotelName = "Old Century Hotel",
                        Description = "The hotel is situated in a nineteenth century plaza, which has been expanded and renovated to the highest architectural standards to create a modern, functional and first-class hotel in which art and unique historical elements coexist with the most modern comforts. The hotel also regularly hosts events like wine tastings, beer dinners, and live music.",
                        Category = "Boutique",
                        Tags = new[] { "pool", "free wifi", "concierge" },
                        ParkingIncluded = false,
                        LastRenovationDate = new DateTimeOffset(2019, 2, 18, 0, 0, 0, TimeSpan.Zero),
                        Rating = 3.60,
                        Address = new Address()
                        {
                            StreetAddress = "140 University Town Center Dr",
                            City = "Sarasota",
                            StateProvince = "FL",
                            PostalCode = "34243",
                            Country = "USA"
                        }
                    }),
                IndexDocumentsAction.Upload(
                    new Hotel()
                    {
                        HotelId = "3",
                        HotelName = "Gastronomic Landscape Hotel",
                        Description = "The Gastronomic Hotel stands out for its culinary excellence under the management of William Dough, who advises on and oversees all of the Hotel’s restaurant services.",
                        Category = "Suite",
                        Tags = new[] { "restaurant", "bar", "continental breakfast" },
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
                    }),
                IndexDocumentsAction.Upload(
                    new Hotel()
                    {
                        HotelId = "4",
                        HotelName = "Sublime Palace Hotel",
                        Description = "Sublime Palace Hotel is located in the heart of the historic center of Sublime in an extremely vibrant and lively area within short walking distance to the sites and landmarks of the city and is surrounded by the extraordinary beauty of churches, buildings, shops and monuments. Sublime Cliff is part of a lovingly restored 19th century resort, updated for every modern convenience.",
                        Category = "Boutique",
                        Tags = new[] { "concierge", "view", "air conditioning" },
                        ParkingIncluded = true,
                        LastRenovationDate = new DateTimeOffset(2020, 2, 06, 0, 0, 0, TimeSpan.Zero),
                        Rating = 4.60,
                        Address = new Address()
                        {
                            StreetAddress = "7400 San Pedro Ave",
                            City = "San Antonio",
                            StateProvince = "TX",
                            PostalCode = "78216",
                            Country = "USA"
                        }
                    })
                );

            try
            {
                IndexDocumentsResult result = searchClient.IndexDocuments(batch);
            }
            catch (Exception)
            {
                // If for some reason any documents are dropped during indexing, you can compensate by delaying and
                // retrying. This simple demo just logs the failed document keys and continues.
                Console.WriteLine("Failed to index some of the documents: {0}");
            }
        }

        // Run queries, use WriteDocuments to print output
        private static void RunQueries(SearchClient srchclient)
        {
            SearchOptions options;
            SearchResults<Hotel> response;

            // Query 1
            Console.WriteLine("Query #1: Search on empty term '*' to return all documents, showing a subset of fields...\n");

            options = new SearchOptions()
            {
                IncludeTotalCount = true,
                Filter = "",
                OrderBy = { "" }
            };

            options.Select.Add("HotelId");
            options.Select.Add("HotelName");
            options.Select.Add("Rating");

            response = srchclient.Search<Hotel>("*", options);
            WriteDocuments(response);

            // Query 2
            Console.WriteLine("Query #2: Search on 'hotels', filter on 'Rating gt 4', sort by Rating in descending order...\n");

            options = new SearchOptions()
            {
                Filter = "Rating gt 4",
                OrderBy = { "Rating desc" }
            };

            options.Select.Add("HotelId");
            options.Select.Add("HotelName");
            options.Select.Add("Rating");

            response = srchclient.Search<Hotel>("hotels", options);
            WriteDocuments(response);

            // Query 3
            Console.WriteLine("Query #3: Limit search to specific fields (pool in Tags field)...\n");

            options = new SearchOptions()
            {
                SearchFields = { "Tags" }
            };

            options.Select.Add("HotelId");
            options.Select.Add("HotelName");
            options.Select.Add("Tags");

            response = srchclient.Search<Hotel>("pool", options);
            WriteDocuments(response);

            // Query 4 - Use Facets to return a faceted navigation structure for a given query
            // Filters are typically used with facets to narrow results on OnClick events
            Console.WriteLine("Query #4: Facet on 'Category'...\n");

            options = new SearchOptions()
            {
                Filter = ""
            };

            options.Facets.Add("Category");

            options.Select.Add("HotelId");
            options.Select.Add("HotelName");
            options.Select.Add("Category");

            response = srchclient.Search<Hotel>("*", options);
            WriteDocuments(response);

            // Query 5
            Console.WriteLine("Query #5: Look up a specific document...\n");

            Response<Hotel> lookupResponse;
            lookupResponse = srchclient.GetDocument<Hotel>("3");

            Console.WriteLine(lookupResponse.Value.HotelId);


            // Query 6
            Console.WriteLine("Query #6: Call Autocomplete on HotelName...\n");

            var autoresponse = srchclient.Autocomplete("sa", "sg");
            WriteDocuments(autoresponse);

        }

        // Write search results to console
        private static void WriteDocuments(SearchResults<Hotel> searchResults)
        {
            foreach (SearchResult<Hotel> result in searchResults.GetResults())
            {
                Console.WriteLine(result.Document);
            }

            Console.WriteLine();
        }

        private static void WriteDocuments(AutocompleteResults autoResults)
        {
            foreach (AutocompleteItem result in autoResults.Results)
            {
                Console.WriteLine(result.Text);
            }

            Console.WriteLine();
        }
    }
}
