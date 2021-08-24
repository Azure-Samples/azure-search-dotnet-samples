// This is a prototype tool that allows for extraction of data from a search index
// Since this tool is still under development, it should not be used for production usage

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSearchSchemaUpdate
{
    class Program
    {
        private static string NewSchemaFile = "NewSchema.schema";
        private static string SearchServiceName;
        private static string AdminKey;
        private static string IndexName;
        private static string BackupContainer;
        private static string StorageAccountConnectionString;

        private static SearchIndexClient IndexClient;
        private static SearchClient SearchClient;

        private static int MaxBatchSize = 500;          // JSON files will contain this many documents / file and can be up to 1000
        private static int ParallelizedJobs = 10;       // Output content in parallel jobs

        static async Task Main(string[] args)
        {

            //Get search service info and index name from appsettings.json file
            //Set up search service clients
            ConfigurationSetup();

            //Backup the old index
            Console.WriteLine("\nSTART INDEX BACKUP");
            int oldDocsCount = await BackupIndexAndDocumentsAsync().ConfigureAwait(false);

            //Recreate and import content to updated index
            Console.WriteLine("\nSTART INDEX RESTORE");
            await DeleteIndexAsync().ConfigureAwait(false);
            await CreateIndexAsync().ConfigureAwait(false);

            // import docs from back up storage.
            await ImportFromJSONAsync().ConfigureAwait(false);
            Console.WriteLine("\r\n  Waiting 10 seconds for updated index content...");
            Console.WriteLine("  NOTE: For really large indexes it may take longer to index all content.\r\n");
            Thread.Sleep(10000);

            // Validate all content is in updated index
            int updatedCount = GetCurrentDocCount(SearchClient);
            Console.WriteLine("\nSAFEGUARD CHECK: Old and updated index counts should match");
            Console.WriteLine(" Old index contains {0} docs", oldDocsCount);
            Console.WriteLine(" Updated index contains {0} docs\r\n", updatedCount);

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }

        static void ConfigurationSetup()
        {

            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SearchServiceName = configuration["SearchServiceName"];
            AdminKey = configuration["AdminKey"];
            IndexName = configuration["IndexName"];
            BackupContainer = configuration["BackupContainerName"];
            StorageAccountConnectionString = configuration["StorageConnectionString"];

            Console.WriteLine("CONFIGURATION:");
            Console.WriteLine("\n  Search service and index {0}, {1}", SearchServiceName, IndexName);
            Console.WriteLine("\n  Backup container: " + BackupContainer);

            IndexClient = new SearchIndexClient(new Uri("https://" + SearchServiceName + ".search.windows.net"), new AzureKeyCredential(AdminKey));
            SearchClient = IndexClient.GetSearchClient(IndexName);
        }

        static async Task<int> BackupIndexAndDocumentsAsync()
        {
            // Backup the index schema to the specified backup container
            Console.WriteLine("\n  Backing up index schema to container: {0}\r\n", BackupContainer + "\\" + IndexName + ".schema");

            await AzureStorageHelper.UploadAsync(StorageAccountConnectionString, BackupContainer, IndexName + ".schema", await GetIndexSchemaAsync().ConfigureAwait(false)).ConfigureAwait(false);

            // Extract the content to JSON files 
            int oldDocCount = GetCurrentDocCount(SearchClient);
            WriteIndexDocuments(oldDocCount);     // Output content from index to json files
            return oldDocCount;
        }

        static void WriteIndexDocuments(int CurrentDocCount)
        {
            // Write document files in batches (per MaxBatchSize) in parallel
            string IDFieldName = GetIDFieldName();
            int FileCounter = 0;
            for (int batch = 0; batch <= (CurrentDocCount / MaxBatchSize); batch += ParallelizedJobs)
            {

                List<Task> tasks = new List<Task>();
                for (int job = 0; job < ParallelizedJobs; job++)
                {
                    FileCounter++;
                    int fileCounter = FileCounter;
                    if ((fileCounter - 1) * MaxBatchSize < CurrentDocCount)
                    {
                        Console.WriteLine("  Backing up documents to {0} - (batch size = {1})", IndexName + fileCounter + ".json", MaxBatchSize);

                        tasks.Add(Task.Factory.StartNew(() =>
                            ExportToJSONAsync((fileCounter - 1) * MaxBatchSize, IDFieldName, IndexName + fileCounter + ".json")
                        ));
                    }

                }
                Task.WaitAll(tasks.ToArray());  // Wait for all the stored procs in the group to complete
            }

            return;
        }

        static async Task ExportToJSONAsync(int Skip, string IDFieldName, string FileName)
        {
            // Extract all the documents from the selected index to JSON files in batches of 500 docs / file
            string json = string.Empty;
            try
            {
                SearchOptions options = new SearchOptions()
                {
                    SearchMode = SearchMode.All,
                    Size = MaxBatchSize,
                    Skip = Skip
                };

                SearchResults<SearchDocument> response = SearchClient.Search<SearchDocument>("*", options);

                foreach (var doc in response.GetResults())
                {
                    json += JsonSerializer.Serialize(doc.Document) + ",";
                    json = json.Replace("\"Latitude\":", "\"type\": \"Point\", \"coordinates\": [");
                    json = json.Replace("\"Longitude\":", "");
                    json = json.Replace(",\"IsEmpty\":false,\"Z\":null,\"M\":null,\"CoordinateSystem\":{\"EpsgId\":4326,\"Id\":\"4326\",\"Name\":\"WGS84\"}", "]");
                    json += "\r\n";
                }

                // Output the formatted content to a file
                json = json.Substring(0, json.Length - 3); // remove trailing comma

                json = "{\"value\": [" + json + "]}";
                await AzureStorageHelper.UploadAsync(StorageAccountConnectionString, BackupContainer, FileName, System.Text.Encoding.ASCII.GetBytes(json)).ConfigureAwait(false);
                Console.WriteLine("  Total documents: {0}", response.GetResults().Count().ToString());
                json = string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message.ToString());
            }
        }

        static string GetIDFieldName()
        {
            // Find the id field of this index
            string IDFieldName = string.Empty;
            try
            {
                var schema = IndexClient.GetIndex(IndexName);
                foreach (var field in schema.Value.Fields)
                {
                    if (field.IsKey == true)
                    {
                        IDFieldName = Convert.ToString(field.Name);
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message.ToString());
            }

            return IDFieldName;
        }

        static async Task<byte[]> GetIndexSchemaAsync()
        {

            // Extract the schema for this index
            // We use REST here because we can take the response as-is

            Uri ServiceUri = new Uri("https://" + SearchServiceName + ".search.windows.net");
            HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("api-key", AdminKey);

            byte[] Schema = null;
            try
            {
                Uri uri = new Uri(ServiceUri, "/indexes/" + IndexName);
                HttpResponseMessage response = await AzureSearchHelper.SendSearchRequestAsync(HttpClient, HttpMethod.Get, uri);
                await AzureSearchHelper.EnsureSuccessfulSearchResponseAsync(response);
                Schema = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message.ToString());
            }

            return Schema;
        }

        private static async Task<bool> DeleteIndexAsync()
        {
            Console.WriteLine("\n  Delete the index {0} in {1} search service, if it exists", IndexName, SearchServiceName);
            // Delete the index if it exists
            try
            {
                await IndexClient.DeleteIndexAsync(IndexName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Error deleting index: {0}\r\n", ex.Message);
                Console.WriteLine("  Did you remember to set your SearchServiceName and SearchServiceApiKey?\r\n");
                return false;
            }

            return true;
        }

        static async Task CreateIndexAsync()
        {
            Console.WriteLine("\n  Create index {0} in {1} search service", IndexName, SearchServiceName);
            // Use the schema file to create an updated index.
            // I like using REST here since I can just take the response as-is

            if (File.Exists(NewSchemaFile))
            {
                // create using the new schema file.
                await AzureStorageHelper.UploadAsync(StorageAccountConnectionString, BackupContainer, NewSchemaFile, await File.ReadAllBytesAsync(NewSchemaFile).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                // if new schema file is not present, as failover, we will create the index with old schema as is from blob storage.
                NewSchemaFile = IndexName + ".schema";
            }

            string json = await AzureStorageHelper.DownloadAsync(StorageAccountConnectionString, BackupContainer, NewSchemaFile).ConfigureAwait(false);

            // Do some cleaning of this file to change index name, etc
            json = "{" + json.Substring(json.IndexOf("\"name\""));
            int indexOfIndexName = json.IndexOf("\"", json.IndexOf("name\"") + 5) + 1;
            int indexOfEndOfIndexName = json.IndexOf("\"", indexOfIndexName);
            json = json.Substring(0, indexOfIndexName) + IndexName + json.Substring(indexOfEndOfIndexName);

            Uri ServiceUri = new Uri("https://" + SearchServiceName + ".search.windows.net");
            HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("api-key", AdminKey);

            try
            {
                Uri uri = new Uri(ServiceUri, "/indexes");
                HttpResponseMessage response = await AzureSearchHelper.SendSearchRequestAsync(HttpClient, HttpMethod.Post, uri, json).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Error: {0}", ex.Message.ToString());
            }
        }

        static int GetCurrentDocCount(SearchClient searchClient)
        {
            // Get the current doc count of the specified index
            try
            {
                SearchOptions options = new SearchOptions()
                {
                    SearchMode = SearchMode.All,
                    IncludeTotalCount = true
                };

                SearchResults<Dictionary<string, object>> response = searchClient.Search<Dictionary<string, object>>("*", options);
                return Convert.ToInt32(response.TotalCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Error: {0}", ex.Message.ToString());
            }

            return -1;
        }

        static async Task ImportFromJSONAsync()
        {
            Console.WriteLine("\n  Upload index documents from saved JSON files");
            // Take JSON file and import this as-is to updated index
            Uri ServiceUri = new Uri("https://" + SearchServiceName + ".search.windows.net");
            HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("api-key", AdminKey);

            try
            {
                foreach (string fileName in await AzureStorageHelper.GetDocumentsListAsync(StorageAccountConnectionString, BackupContainer, IndexName, ".json").ConfigureAwait(false))
                {
                    Console.WriteLine("  -Uploading documents from file {0}", fileName);
                    string json = await AzureStorageHelper.DownloadAsync(StorageAccountConnectionString, BackupContainer, fileName).ConfigureAwait(false);
                    Uri uri = new Uri(ServiceUri, "/indexes/" + IndexName + "/docs/index");
                    HttpResponseMessage response = await AzureSearchHelper.SendSearchRequestAsync(HttpClient, HttpMethod.Post, uri, json).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Error: {0}", ex.Message.ToString());
            }
        }
    }
}
