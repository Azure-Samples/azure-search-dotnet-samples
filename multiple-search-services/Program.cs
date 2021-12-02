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
                    description: "Comma-separated list of fields to search")
            };
            rootCommand.Description = "Setup and query multiple indexes across search services";
            rootCommand.Handler = CommandHandler.Create<
                bool,
                string,
                int,
                int,
                string>(RunCommand);
            await rootCommand.InvokeAsync(args);

        }

        static async Task RunCommand(bool initialize, string query, int page, int pageSize, string searchFields)
        {
            if (pageSize < 1 || pageSize > 100)
            {
                throw new Exception("Invalid page size. Page size must be between 1 and 100");
            }

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

            if (initialize)
            {
                await CreateIndexesAsync(services);
                await BulkInsertAsync(services);
            }

            if (!String.IsNullOrEmpty(query))
            {
                (int actualPageNumber, List<(Service, SearchResult<BookModel>)> results) = await RunQueryAsync(query, page, pageSize, searchFields, services);
                if (actualPageNumber != page)
                {
                    Console.WriteLine("Page {0}: No results", page);
                    return;
                }

                Console.WriteLine("Page {0} size: {1}", actualPageNumber, results.Count);
                foreach ((Service service, SearchResult<BookModel> result) in results)
                {
                    Console.WriteLine("Service {0}, Score {1}, Title {2}, Id {3}", service.Name, result.Score, result.Document.title, result.Document.goodreads_book_id);
                }
            }
        }

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

        static async Task BulkInsertAsync(List<Service> services)
        {
            Console.WriteLine("Download data file");
            using HttpClient httpClient = new HttpClient();
            var csv = await httpClient.GetStringAsync(BOOKS_URL);

            Console.WriteLine("Reading and parsing raw CSV data");
            var books =
                csv.ReplaceFirst("book_id", "id").FromCsv<List<BookModel>>();

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

        static async Task<(int, List<(Service, SearchResult<BookModel>)>)> RunQueryAsync(string query, int pageNumber, int pageSize, string searchFields, List<Service> services)
        {
            var searchResults = new List<(Service, IAsyncEnumerator<Page<SearchResult<BookModel>>>)>();
            foreach (Service service in services)
            {
                IAsyncEnumerable<Page<SearchResult<BookModel>>> response = SearchAsync(service, query, pageSize, searchFields);
                searchResults.Add((service, response.GetAsyncEnumerator()));
            }

            int currentPageNumber = 0;
            var currentPage = new List<(Service, SearchResult<BookModel>)>();
            do
            {
                var resultPages = new List<(Service, Page<SearchResult<BookModel>>)>();
                var nextSearchResults = new List<(Service, IAsyncEnumerator<Page<SearchResult<BookModel>>>)>();
                foreach ((Service service, IAsyncEnumerator<Page<SearchResult<BookModel>>> pageEnumerator) in searchResults)
                {
                    if (await pageEnumerator.MoveNextAsync())
                    {
                        resultPages.Add((service, pageEnumerator.Current));
                        nextSearchResults.Add((service, pageEnumerator));
                    }
                }

                searchResults = nextSearchResults;
                var mergedSearchResults = new List<(Service, SearchResult<BookModel>)>();
                foreach ((Service service, Page<SearchResult<BookModel>> resultPage) in resultPages)
                {
                    foreach (SearchResult<BookModel> result in resultPage.Values)
                    {
                        mergedSearchResults.Add((service, result));
                    }
                }

                mergedSearchResults.Sort((a, b) =>
                {
                    (_, SearchResult<BookModel> resultA) = a;
                    (_, SearchResult<BookModel> resultB) = b;
                    if (resultA.Score.HasValue && resultB.Score.HasValue)
                    {
                        return resultB.Score.Value.CompareTo(resultA.Score.Value);
                    }

                    if (resultA.Score.HasValue && !resultB.Score.HasValue)
                    {
                        return -1;
                    }

                    if (!resultA.Score.HasValue && resultB.Score.HasValue)
                    {
                        return 1;
                    }

                    return 0;
                });

                foreach ((Service service, SearchResult<BookModel> mergedSearchResult) in mergedSearchResults)
                {
                    currentPage.Add((service, mergedSearchResult));
                    if (currentPage.Count == pageSize)
                    {
                        if (currentPageNumber == pageNumber)
                        {
                            return (currentPageNumber, currentPage);
                        }

                        currentPage.Clear();
                        currentPageNumber++;
                    }
                }
            }
            while (searchResults.Any());

            return (currentPageNumber, currentPage);
        }

        static async IAsyncEnumerable<Page<SearchResult<BookModel>>> SearchAsync(Service service, string query, int pageSize, string searchFields)
        {
            int skip = 0;
            int currentResultCount;
            do
            {
                currentResultCount = 0;
                var options = new SearchOptions { Size = pageSize, Skip = skip };
                if (!String.IsNullOrEmpty(searchFields))
                {
                    foreach (string searchField in searchFields.Split(','))
                    {
                        options.SearchFields.Add(searchField);
                    }
                }

                Response<SearchResults<BookModel>> results = await service.SearchClient.SearchAsync<BookModel>(query, options);
                await foreach (Page<SearchResult<BookModel>> page in results.Value.GetResultsAsync().AsPages())
                {
                    currentResultCount += page.Values.Count;
                    skip += page.Values.Count;
                    if (page.Values.Count > 0)
                    {
                        yield return page;
                    }
                }
            }
            while (currentResultCount > 0);
        }

        class Service
        {
            public string Name { get; set; }
            public string AdminKey { get; set; }
            public string SearchEndpoint { get; set; }
            public string IndexName { get; set; }

            public Uri SearchEndpointUri => new Uri(SearchEndpoint);
            public AzureKeyCredential SearchKeyCredential => new AzureKeyCredential(AdminKey);
            public SearchClient SearchClient => new SearchClient(SearchEndpointUri, IndexName, SearchKeyCredential);
            public SearchIndexClient SearchIndexClient => new SearchIndexClient(SearchEndpointUri, SearchKeyCredential);
        }
    }
}
