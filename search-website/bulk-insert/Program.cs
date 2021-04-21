using System;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using AzureSearch.BulkInsert;
using System.Net.Http;
using System.Threading.Tasks;
using ServiceStack;
using System.Collections.Generic;

namespace ConsoleApp1
{
    class Program
    {
        const string BOOKS_URL = "https://raw.githubusercontent.com/zygmuntz/goodbooks-10k/master/books.csv";
        const string SEARCH_ENDPOINT = "https://YOUR-SEARCH-RESOURCE-NAME.search.windows.net";
        const string SEARCH_KEY = "YOUR-SEARCH-ADMIN-KEY";
        const string SEARCH_INDEX_NAME = "good-books";

        static void Main(string[] args)
        {
            Uri searchEndpointUri = new Uri(SEARCH_ENDPOINT);

            SearchClient client = new SearchClient(
                searchEndpointUri,
                SEARCH_INDEX_NAME,
                new AzureKeyCredential(SEARCH_KEY));

            SearchIndexClient clientIndex = new SearchIndexClient(
                searchEndpointUri,
                new AzureKeyCredential(SEARCH_KEY));

            CreateIndexAsync(clientIndex).Wait();
            BulkInsertAsync(client).Wait();
        }
        static async Task CreateIndexAsync(SearchIndexClient clientIndex)
        {
            Console.WriteLine("Creating (or updating) search index");
            SearchIndex index = new BookSearchIndex(SEARCH_INDEX_NAME);
            var result = await clientIndex.CreateOrUpdateIndexAsync(index);

            Console.WriteLine(result);
        }

        static async Task BulkInsertAsync(SearchClient client)
        {
            Console.WriteLine("Download data file");
            using HttpClient httpClient = new HttpClient();
            var csv = await httpClient.GetStringAsync(BOOKS_URL);

            Console.WriteLine("Reading and parsing raw CSV data");
            var books =
                csv.ReplaceFirst("book_id", "id").FromCsv<List<BookModel>>();

            Console.WriteLine("Uploading bulk book data");
            _ = await client.UploadDocumentsAsync(books);

            Console.WriteLine("Finished bulk inserting book data");
        }
    }
}
