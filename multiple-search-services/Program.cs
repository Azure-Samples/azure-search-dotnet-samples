using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MultipleSearchServices
{
    class Program
    {
        // URL of Good Books data to populate test indexes
        const string BOOKS_URL = "https://raw.githubusercontent.com/zygmuntz/goodbooks-10k/master/books.csv";

        async static Task Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new System.CommandLine.Option<bool>(
                    new[] { "--initialize" },
                    getDefaultValue: () => false,
                    description: "Set this option to create indexes and upload test data"),
                new System.CommandLine.Option<string>(
                    new[] { "--query" },
                    getDefaultValue: () => null,
                    description: "Query to run against the test indexes"),
                new System.CommandLine.Option<int>(
                    new[] { "--page" },
                    getDefaultValue: () => 0,
                    description: "What page of query results to return"),
                new System.CommandLine.Option<int>(
                    new[] { "--pageSize" },
                    getDefaultValue: () => 50,
                    description: "Amount of results to return per query page from each search service. Default is 50, maximum is 100, minimum is 1"),
                new System.CommandLine.Option<string>(
                    new[] { "--searchFields" },
                    getDefaultValue: () => null,
                    description: "Comma-separated list of fields to search"),
                new System.CommandLine.Option<string>(
                    new[] { "--displayFields" },
                    getDefaultValue: () => null,
                    description: "Comma-separated list of fields to display. Defaults to all fields that are retrievable from the index"),
                new System.CommandLine.Option<string>(
                    new[] { "--facets" },
                    getDefaultValue: () => null,
                    description: "Comma-separated list of facets"),
                new System.CommandLine.Option<bool>(
                    new[] { "--count" },
                    getDefaultValue: () => false,
                    description: "Include count of total results in output when running a query"),
                new System.CommandLine.Option<string>(
                    new[] { "--queryType" },
                    getDefaultValue: () => null,
                    description: "Set type of query"),
                new System.CommandLine.Option<string>(
                    new[] { "--queryLanguage" },
                    getDefaultValue: () => null,
                    description: "Set language of query")
            };
            rootCommand.Description = "Setup and query multiple indexes across search services";
            rootCommand.Handler = CommandHandler.Create<
                bool,
                string,
                int,
                int,
                string,
                string,
                string,
                bool,
                string,
                string>(RunCommand);
            await rootCommand.InvokeAsync(args);
        }

        static async Task RunCommand(bool initialize, string query, int page, int pageSize, string searchFields, string displayFields, string facets, bool count, string queryType, string queryLanguage)
        {
            if (pageSize < 1 || pageSize > 100)
            {
                throw new Exception("Invalid page size. Page size must be between 1 and 100");
            }

            SearchQueryType searchQueryType = ParseQueryType(queryType);

            // Read settings from appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            var services = new List<Service>();
            foreach (IConfigurationSection section in configuration.GetChildren())
            {
                var service = new Service { Name = section.Key };
                try
                {
                    section.Bind(service);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Problem parsing appsettings.json");
                    throw e;
                }

                services.Add(service);
            }

            // Setup test data if requested
            if (initialize)
            {
                await CreateIndexesAsync(services);
                await BulkInsertAsync(services);
            }

            // Run query if requested
            if (!String.IsNullOrEmpty(query))
            {
                (int actualPageNumber, List<MultiSearchResult> multiResults, MultiSearchFacets multiFacets, long totalCount) = await RunQueryAsync(query, page, pageSize, searchFields, displayFields, facets, count, searchQueryType, queryLanguage, services);

                if (count == true)
                {
                    Console.WriteLine("Total count: {0}", totalCount);
                }

                if (multiFacets.Facets.Any())
                {
                    Console.WriteLine("Faceted field count: {0}", multiFacets.Facets.Count);
                    foreach (KeyValuePair<string, List<MultiSearchFacet>> multiFacetList in multiFacets.Facets)
                    {
                        Console.WriteLine(multiFacetList.Key);
                        Console.WriteLine("Number of facets: {0}", multiFacetList.Value.Count);
                        // Sort facets in desecending order by count before usage
                        multiFacetList.Value.Sort();
                        foreach (MultiSearchFacet multiFacet in multiFacetList.Value)
                        {
                            if (multiFacet.FacetType == FacetType.Value)
                            {
                                Console.WriteLine("Value {0}, Count {1}", multiFacet.Value, multiFacet.Count);
                            }
                            else if (multiFacet.FacetType == FacetType.Range)
                            {
                                Console.WriteLine("From {0}, To {1}, Count {2}", multiFacet.From, multiFacet.To, multiFacet.Count);
                            }
                        }
                    }
                }

                if (actualPageNumber != page)
                {
                    Console.WriteLine("Page {0}: No results", page);
                    return;
                }

                Console.WriteLine("Page {0} size: {1}", actualPageNumber, multiResults.Count);
                foreach (MultiSearchResult multiResult in multiResults)
                {
                    string fields = String.Join(", ", multiResult.Result.Document.OrderBy(field => field.Key).Select(field => $"{field.Key} {field.Value}"));
                    if (searchQueryType != SearchQueryType.Semantic)
                    {
                        Console.WriteLine("Service {0}, Score {1}, {2}", multiResult.Service.Name, multiResult.Result.Score, fields);
                    }
                    else
                    {
                        Console.WriteLine("Service {0}, reranker score {1}, score {2}, {3}", multiResult.Service.Name, multiResult.Result.RerankerScore, multiResult.Result.Score, fields);
                    }
                }
            }
        }

        // Create test indexes for good books data
        static async Task CreateIndexesAsync(List<Service> services)
        {
            Console.WriteLine("Creating (or updating) search index");
            foreach (Service service in services)
            {
                SearchIndex index = new BookSearchIndex(service.IndexName);
                var result = await service.SearchIndexClient.CreateOrUpdateIndexAsync(index);

                Console.WriteLine(result);
            }
        }

        // Add good books data to test indexes
        // Data is subdivided equally among all provided services
        static async Task BulkInsertAsync(List<Service> services)
        {
            Console.WriteLine("Download data file");
            using HttpClient httpClient = new HttpClient();
            var csv = await httpClient.GetStringAsync(BOOKS_URL);

            Console.WriteLine("Reading and parsing raw CSV data");
            var books =
                csv.ReplaceFirst("book_id", "id").FromCsv<List<BookModel>>();
            foreach (BookModel book in books)
            {
                // Faceting is sensitive to whitespace. Normalize by removing whitespace at the beginning and end
                // of the author names
                book.authors = book.authors.Select(author => author.Trim()).ToArray();
            }

            // Try to evenly divide all data across each service
            // If there are any books left over, put them in the last service
            int booksPerService = books.Count / services.Count;
            int remainingBooks = books.Count % services.Count;
            IEnumerable<BookModel> booksLeft = books;
            Console.WriteLine("Uploading bulk book data");
            for (int i = 0; i < services.Count; i++)
            {
                IEnumerable<BookModel> booksToUpload;
                if (i < services.Count - 1)
                {
                    booksToUpload = booksLeft.Take(booksPerService);
                    booksLeft = booksLeft.Skip(booksPerService);
                }
                else
                {
                    booksToUpload = booksLeft.Take(booksPerService + remainingBooks);
                }

                _ = await services[i].SearchClient.UploadDocumentsAsync(booksToUpload);
            }
            
            Console.WriteLine("Finished bulk inserting book data");
        }

        // Run the query and combine results across multiple services
        static async Task<(int, List<MultiSearchResult>, MultiSearchFacets, long)> RunQueryAsync(string query, int pageNumber, int pageSize, string searchFields, string displayFields, string facets, bool count, SearchQueryType queryType, string queryLanguage, List<Service> services)
        {
            // Page results from all services
            var searchResults = new List<IAsyncEnumerator<MultiSearchResultsPage>>();
            foreach (Service service in services)
            {
                IAsyncEnumerable<MultiSearchResultsPage> response = SearchAsync(service, query, pageSize, searchFields, displayFields, facets, count, queryType, queryLanguage);
                searchResults.Add(response.GetAsyncEnumerator());
            }

            // Merge each individual page from every service
            // Sort the combined page by result score
            int currentPageNumber = 0;
            var currentPage = new List<MultiSearchResult>();
            var mergedFacets = new MultiSearchFacets();
            var documentCounts = new Dictionary<string, long>();
            do
            {
                // Combine the current page of results from each service
                // If the service has no more results, it is discarded
                var resultPages = new List<MultiSearchResultsPage>();
                var nextSearchResults = new List<IAsyncEnumerator<MultiSearchResultsPage>>();
                foreach (IAsyncEnumerator<MultiSearchResultsPage> pageEnumerator in searchResults)
                {
                    if (await pageEnumerator.MoveNextAsync())
                    {
                        resultPages.Add(pageEnumerator.Current);
                        nextSearchResults.Add(pageEnumerator);
                    }
                }

                searchResults = nextSearchResults;
                var mergedSearchResults = new List<MultiSearchResult>();
                foreach (MultiSearchResultsPage resultPage in resultPages)
                {
                    foreach (SearchResult<SearchDocument> result in resultPage.Page)
                    {
                        mergedSearchResults.Add(new MultiSearchResult { Service = resultPage.Service, Result = result });
                    }

                    if (resultPage.Facets != null)
                    {
                        MergeFacets(resultPage.Facets, mergedFacets);
                    }

                    // Total count on each page is the total count of results for the query in that service
                    if (!documentCounts.ContainsKey(resultPage.Service.Name + resultPage.Service.IndexName))
                    {
                        documentCounts[resultPage.Service.Name + resultPage.Service.IndexName] = resultPage.TotalCount;
                    }
                }

                // Sort the combined pages by score descending
                mergedSearchResults.Sort((a, b) =>
                {
                    MultiSearchResult resultA = a;
                    MultiSearchResult resultB = b;
                    // Use the reranker score in semantic queries, otherwise use the normal score
                    double? resultAScore = queryType == SearchQueryType.Semantic ? resultA.Result.RerankerScore : resultA.Result.Score;
                    double? resultBScore = queryType == SearchQueryType.Semantic ? resultB.Result.RerankerScore : resultB.Result.Score;

                    if (resultAScore.HasValue && resultBScore.HasValue)
                    {
                        return resultBScore.Value.CompareTo(resultAScore.Value);
                    }

                    if (resultAScore.HasValue && !resultBScore.HasValue)
                    {
                        return -1;
                    }

                    if (!resultAScore.HasValue && resultBScore.HasValue)
                    {
                        return 1;
                    }

                    return 0;
                });

                // Return sub-pages of results from the combined page
                foreach (MultiSearchResult mergedSearchResult in mergedSearchResults)
                {
                    currentPage.Add(mergedSearchResult);
                    if (currentPage.Count == pageSize)
                    {
                        if (currentPageNumber == pageNumber)
                        {
                            return (currentPageNumber, currentPage, mergedFacets, documentCounts.Values.Sum());
                        }

                        currentPage.Clear();
                        currentPageNumber++;
                    }
                }
            }
            while (searchResults.Any());

            // Return any leftover results as the last page
            return (currentPageNumber, currentPage, mergedFacets, documentCounts.Values.Sum());
        }

        // Return all results from a service for a given query using a specific page size
        static async IAsyncEnumerable<MultiSearchResultsPage> SearchAsync(Service service, string query, int pageSize, string searchFields, string displayFields, string facets, bool count, SearchQueryType queryType, string queryLanguage)
        {
            // Client-side page through all the results from the service
            int skip = 0;
            long totalCount = 0;
            var searchResults = new List<SearchResult<SearchDocument>>();
            bool returnedFacets = false;
            do
            {
                searchResults.Clear();
                // Specify specific fields to search if given
                var options = new SearchOptions { Size = pageSize, Skip = skip, IncludeTotalCount = count, QueryType = queryType, QueryLanguage = queryLanguage, SemanticConfigurationName = service.SemanticConfigurationName };

                if (!String.IsNullOrEmpty(searchFields))
                {
                    foreach (string searchField in searchFields.Split(','))
                    {
                        options.SearchFields.Add(searchField);
                    }
                }
                // Specify specific fields to retrieve if given
                if (!String.IsNullOrEmpty(displayFields))
                {
                    foreach (string displayField in displayFields.Split())
                    {
                        options.Select.Add(displayField);
                    }
                }
                // Specify facets if given
                if (!String.IsNullOrEmpty(facets))
                {
                    foreach (string facet in facets.Split(','))
                    {
                        options.Facets.Add(facet);
                    }
                }

                // Page through a single query. A continuation token may be returned for partial results from a single query
                SearchResults<SearchDocument> results = await service.SearchClient.SearchAsync<SearchDocument>(query, options);
                totalCount = results.TotalCount.GetValueOrDefault();

                await foreach (Page<SearchResult<SearchDocument>> page in results.GetResultsAsync().AsPages())
                {
                    // Skip ahead however many results we've seen when running the next query for client-side paging
                    // For more information, please see https://docs.microsoft.com/azure/search/search-pagination-page-layout
                    skip += page.Values.Count;
                    searchResults.AddRange(page.Values);
                }

                // Facets only need to be returned on the first page of results since the same query is run repeatedly
                // with different skip values, which doesn't change the returned facet values
                if (searchResults.Any())
                {
                    yield return new MultiSearchResultsPage { Service = service, Page = searchResults, Facets = !returnedFacets ? results.Facets : null, TotalCount = totalCount };
                    returnedFacets = true;
                }
            }
            while (searchResults.Any());
        }

        // Merge facets across multiple search services
        static void MergeFacets(IDictionary<string, IList<FacetResult>> singleServiceFacets, MultiSearchFacets mergedFacets)
        {
            foreach (KeyValuePair<string, IList<FacetResult>> resultFacets in singleServiceFacets)
            {
                string fieldName = resultFacets.Key;
                IList<FacetResult> serviceFacets = resultFacets.Value;
                if (mergedFacets.Facets.TryGetValue(fieldName, out List<MultiSearchFacet> multiServiceFacets))
                {
                    // If a facet can be merged into an existing multi-service facet, combine the counts
                    // Otherwise, add a new multi-service facet matching the existing facet
                    foreach (FacetResult facet in serviceFacets)
                    {
                        bool foundFacet = false;
                        foreach (MultiSearchFacet multiServiceFacet in multiServiceFacets)
                        {
                            if (multiServiceFacet.CanMergeFacet(facet))
                            {
                                foundFacet = true;
                                multiServiceFacet.Count += facet.Count.Value;
                                break;
                            }
                        }

                        if (!foundFacet)
                        {
                            multiServiceFacets.Add(new MultiSearchFacet(facet));
                        }
                    }
                }
                else
                {
                    // Initialize the multi-service facet list with the facets from this service for this field
                    mergedFacets.Facets.Add(fieldName, serviceFacets.Select(f => new MultiSearchFacet(f)).ToList());
                }
            }
        }

        class MultiSearchResultsPage
        {
            public Service Service { get; set; }
            public IEnumerable<SearchResult<SearchDocument>> Page { get; set; }
            public IDictionary<string, IList<FacetResult>> Facets { get; set; }
            public long TotalCount { get; set; }
        }

        class MultiSearchResult
        {
            public Service Service { get; set; }
            public SearchResult<SearchDocument> Result { get; set; }
        }

        class MultiSearchFacets
        {
            public MultiSearchFacets()
            {
                Facets = new Dictionary<string, List<MultiSearchFacet>>();
            }

            public Dictionary<string, List<MultiSearchFacet>> Facets { get; }
        }

        class MultiSearchFacet : IComparable<MultiSearchFacet>
        {
            public MultiSearchFacet(FacetResult facetResult)
            {
                Value = facetResult.Value;
                From = facetResult.From;
                To = facetResult.To;
                FacetType = facetResult.FacetType;
                if (facetResult.Count.HasValue)
                {
                    Count = facetResult.Count.Value;
                }
            }

            public FacetType FacetType { get; set; }
            public object Value { get; set; }
            public object From { get; set; }
            public object To { get; set; }
            public long Count { get; set; }
            
            public bool CanMergeFacet(FacetResult result)
            {
                return CompareFacetField(result.Value, Value) &&
                    CompareFacetField(result.To, To) &&
                    CompareFacetField(result.From, From) &&
                    CompareFacetField(result.FacetType, FacetType) &&
                    result.Count.HasValue;
            }

            // Compare parts of different facets
            private static bool CompareFacetField(object a, object b)
            {
                if (a == null && b == null)
                {
                    return true;
                }

                if (a != null && b == null)
                {
                    return false;
                }

                if (a == null && b != null)
                {
                    return false;
                }

                return a.Equals(b);
            }

            // Compare a multi search facet to another one. Order by count descending
            public int CompareTo(MultiSearchFacet other)
            {
                return other.Count.CompareTo(Count);
            }
        }

        private static SearchQueryType ParseQueryType(string queryType)
        {
            if (String.IsNullOrEmpty(queryType) || queryType.EqualsIgnoreCase("simple"))
            {
                return SearchQueryType.Simple;
            }
            else if (queryType.EqualsIgnoreCase("full"))
            {
                return SearchQueryType.Full;
            }
            else if (queryType.EqualsIgnoreCase("semantic"))
            {
                return SearchQueryType.Semantic;
            }
            else
            {
                throw new Exception("Invalid query type " + queryType);
            }
        }

        class Service
        {
            public string Name { get; set; }
            public string AdminKey { get; set; }
            public string SearchEndpoint { get; set; }
            public string IndexName { get; set; }
            public string SemanticConfigurationName { get; set; }

            public Uri SearchEndpointUri => new Uri(SearchEndpoint);
            public AzureKeyCredential SearchKeyCredential => new AzureKeyCredential(AdminKey);
            public SearchClient SearchClient => new SearchClient(SearchEndpointUri, IndexName, SearchKeyCredential);
            public SearchIndexClient SearchIndexClient => new SearchIndexClient(SearchEndpointUri, SearchKeyCredential);
        }
    }
}
