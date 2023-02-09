using System;
using Azure;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using AzureSearchQuickstart_v11;

namespace AzureSearch.Quickstart

{
    class Program
    {
        static void Main(string[] args)
        {
            string serviceName = "<your-service-here>";
            string apiKey = "<your-key-here>";
            string indexName = "hotels-sample-index";

            // Create a SearchIndexClient to send create/delete index commands
            Uri serviceEndpoint = new Uri($"https://{serviceName}.search.windows.net/");
            AzureKeyCredential credential = new AzureKeyCredential(apiKey);
            SearchClientOptions indexClientOptions = new SearchClientOptions();
            indexClientOptions.AddPolicy(new DefaultSemanticConfigHttpPipelinePolicy(), HttpPipelinePosition.PerCall);

            SearchIndexClient adminClient = new SearchIndexClient(serviceEndpoint, credential, indexClientOptions);
            
            // Create a SearchClient that demonstrates the Debug feature
            SearchClientOptions searchClientOptions = new SearchClientOptions();
            searchClientOptions.AddPolicy(new DebugHttpPipelinePolicy(), HttpPipelinePosition.PerCall);
            SearchClient srchclient = new SearchClient(serviceEndpoint, indexName, credential, searchClientOptions);

            // Create a second SearchClient to demonstrate the Fallback feature
            SearchClientOptions searchClientOptions2 = new SearchClientOptions();
            searchClientOptions2.AddPolicy(new FallbackHttpPipelinePolicy(), HttpPipelinePosition.PerCall);
            SearchClient srchclient2 = new SearchClient(serviceEndpoint, indexName, credential, searchClientOptions);


            // Delete index if it exists
            Console.WriteLine("{0}", "Deleting index...\n");
            DeleteIndexIfExists(indexName, adminClient);

            // Create index
            Console.WriteLine("{0}", "Creating index...\n");
            CreateIndex(indexName, adminClient);

            SearchClient ingesterClient = adminClient.GetSearchClient(indexName);

            // Load documents
            Console.WriteLine("{0}", "Uploading documents...\n");
            UploadDocuments(ingesterClient);

            // Wait 2 seconds for indexing to complete before starting queries (for demo and console-app purposes only)
            Console.WriteLine("Waiting for indexing...\n");
            System.Threading.Thread.Sleep(2000);

            // Call the RunQueries method to invoke a series of queries
            Console.WriteLine("Starting queries...\n");
            RunSemanticQueries(srchclient);

            Console.WriteLine("Starting second batch of queries...\n");
            RunSemanticQueries(srchclient2);

            // End the program
            Console.WriteLine("{0}", "Complete. Press any key to end this program...\n");
            Console.ReadKey();
        }

        // Delete the hotels-quickstart index to reuse its name
        private static void DeleteIndexIfExists(string indexName, SearchIndexClient adminClient)
        {
            adminClient.GetIndexNames();
            {
                adminClient.DeleteIndex(indexName);
            }
        }
        // Create hotels-quickstart index
        private static void CreateIndex(string indexName, SearchIndexClient adminClient)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(Hotel));

            var definition = new SearchIndex(indexName, searchFields);

            var suggester = new SearchSuggester("sg", new[] { "HotelName", "Category", "Address/City", "Address/StateProvince" });
            definition.Suggesters.Add(suggester);

            SemanticSettings semanticSettings = new SemanticSettings();
            semanticSettings.Configurations.Add(new SemanticConfiguration
                (
                    "semconfig",
                    new PrioritizedFields()
                    {
                        TitleField = new SemanticField { FieldName = "HotelName" },
                        ContentFields = {
                            new SemanticField { FieldName = "Category" }
                        },
                        KeywordFields = {
                            new SemanticField { FieldName = "Address/City" }
                        }
                    }
                )
            );
            definition.SemanticSettings = semanticSettings;

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
                    }),
                IndexDocumentsAction.Upload(
                    new Hotel()
                    {
                        HotelId = "2",
                        HotelName = "Twin Dome Motel",
                        Description = "The hotel is situated in a  nineteenth century plaza, which has been expanded and renovated to the highest architectural standards to create a modern, functional and first-class hotel in which art and unique historical elements coexist with the most modern comforts.",
                        DescriptionFr = "L'hôtel est situé dans une place du XIXe siècle, qui a été agrandie et rénovée aux plus hautes normes architecturales pour créer un hôtel moderne, fonctionnel et de première classe dans lequel l'art et les éléments historiques uniques coexistent avec le confort le plus moderne.",
                        Category = "Boutique",
                        Tags = new[] { "pool", "free wifi", "concierge" },
                        ParkingIncluded = false,
                        LastRenovationDate = new DateTimeOffset(1979, 2, 18, 0, 0, 0, TimeSpan.Zero),
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
                    }),
                IndexDocumentsAction.Upload(
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

        // Run semantic queries and print output.
        private static void RunSemanticQueries(SearchClient srchclient)
        {
            SearchOptions options;
            SearchResults<Hotel> response;

            // Query 1
            Console.WriteLine("Query #1: Search on empty term '*' to return all documents, showing a subset of fields...\n");

            options = new SearchOptions()
            {
                IncludeTotalCount = true,
                Filter = "",
                OrderBy = { "" },
                QueryType = SearchQueryType.Semantic,
                QueryLanguage = QueryLanguage.EnUs,
                SemanticConfigurationName = "semconfig"
            };

            options.Select.Add("HotelId");
            options.Select.Add("HotelName");
            options.Select.Add("Rating");

            response = srchclient.Search<Hotel>("*", options);
            WriteDocuments(response);

            // Query 2
            // Omit semantic config name, fallback to default semantic config.
            Console.WriteLine("Query #2: Search on 'hotels', filter on 'Rating gt 4'\n");

            options = new SearchOptions()
            {
                Filter = "Rating gt 4",
                QueryType = SearchQueryType.Semantic,
                QueryLanguage = QueryLanguage.EnUs,
            };

            options.Select.Add("HotelId");
            options.Select.Add("HotelName");
            options.Select.Add("Rating");

            response = srchclient.Search<Hotel>("hotels", options);
            WriteDocuments(response);

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
                OrderBy = { "" },
                QueryType = SearchQueryType.Semantic,
                QueryLanguage = QueryLanguage.EnUs,
                SemanticConfigurationName = "semconfig"
            };

            options.Select.Add("HotelId");
            options.Select.Add("HotelName");
            options.Select.Add("Rating");

            response = srchclient.Search<Hotel>("*", options);
            WriteDocuments(response);

            // Query 2
            Console.WriteLine("Query #2: Search on 'hotels', filter on 'Rating gt 4'\n");

            options = new SearchOptions()
            {
                Filter = "Rating gt 4",
                QueryType = SearchQueryType.Semantic,
                QueryLanguage = QueryLanguage.EnUs,
                SemanticConfigurationName = "semconfig"
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
                SearchFields = { "Tags" },
                QueryType = SearchQueryType.Semantic,
                QueryLanguage = QueryLanguage.EnUs,
                SemanticConfigurationName = "semconfig"
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
                Filter = "",
                QueryType = SearchQueryType.Semantic,
                QueryLanguage = QueryLanguage.EnUs,
                SemanticConfigurationName = "semconfig"
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
            //Console.WriteLine("Query #6: Call Autocomplete on HotelName...\n");

            //var autoresponse = srchclient.Autocomplete("sa", "sg");
            //WriteDocuments(autoresponse);

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
