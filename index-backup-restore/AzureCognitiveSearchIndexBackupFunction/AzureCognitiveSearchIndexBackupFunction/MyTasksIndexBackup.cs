using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace AzureCognitiveSearchIndexBackupFunction
{
    public class MyTasksIndexBackup
    {
        private static string SearchServiceName;
        private static string AdminKey;
        private static string IndexName;
        private static SearchIndexClient IndexClient;
        private static SearchClient SearchClient;
        private static readonly int MaxBatchSize = 500;          // JSON files will contain this many documents / file and can be up to 1000
        private static readonly int ParallelizedJobs = 10;       // Output content in parallel jobs
        private static ILogger _log;


		[FunctionName("MyTasksIndexBackup")]
		public static async Task RunMyTasksIndexBackup([TimerTrigger("00 55 23 * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
		{
			log.LogInformation($"MyTasksIndexBackup function executed at: {DateTime.Now}");
			ConfigurationSetup(log);
			await BackupIndexAndDocuments(IndexName, SearchClient);
		}



        static void ConfigurationSetup(ILogger log)
        {
            SearchServiceName = Environment.GetEnvironmentVariable("SearchServiceName");
            AdminKey = Environment.GetEnvironmentVariable("AdminKey");
            IndexName = Environment.GetEnvironmentVariable("IndexName");
            IndexClient = new SearchIndexClient(new Uri("https://" + SearchServiceName + ".search.windows.net"), new AzureKeyCredential(AdminKey));
            SearchClient = IndexClient.GetSearchClient(IndexName);
            _log = log;
        }

        private static async Task BackupIndexAndDocuments(string indexName, SearchClient searchClient)
        {
            await CreateAndUploadSchema(indexName);
            int SourceDocCount = AzureSearchHelper.GetCurrentDocCount(searchClient, _log);
            WriteIndexDocuments(SourceDocCount, indexName, searchClient);
        }

        private static async Task CreateAndUploadSchema(string indexName)
		{
			var stream = AzureSearchHelper.GenerateStreamFromString(GetIndexSchema(indexName));
			await UploadBlobToAzureStorage(indexName + ".schema", stream);
		}

        static void WriteIndexDocuments(int CurrentDocCount, string indexName, SearchClient searchClient)
        {
            // Write document files in batches (per MaxBatchSize) in parallel
            int FileCounter = 0;
            for (int batch = 0; batch <= (CurrentDocCount / MaxBatchSize); batch += ParallelizedJobs)
            {
                FileCounter = AddTasksToList(CurrentDocCount, FileCounter, indexName, searchClient);// Wait for all the stored procs in the group to complete
            }
            return;
        }

        private static async Task UploadBlobToAzureStorage(string FileName, MemoryStream stream)
		{
			BlobContainerClient blobClient = GetBlobContainerClient();
			await blobClient.CreateIfNotExistsAsync(default);
			var blob = blobClient.GetBlobClient($"{DateTime.Now.Day}/{FileName}");
            await blobClient.UploadBlobAsync(blob.Name, stream);
		}

        static string GetIndexSchema(string indexName)
		{
			// Extract the schema for this index
			// We use REST here because we can take the response as-is
			Uri ServiceUri;
			HttpClient HttpClient;
            AzureSearchHelper.GetServiceUri(SearchServiceName, AdminKey, out ServiceUri, out HttpClient);

			string Schema = string.Empty;
			try
			{
				Uri uri = new Uri(ServiceUri, "/indexes/" + indexName);
				HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(HttpClient, HttpMethod.Get, uri);
				AzureSearchHelper.EnsureSuccessfulSearchResponse(response);
				Schema = response.Content.ReadAsStringAsync().Result.ToString();
			}
			catch (Exception ex)
			{
				_log.LogError("Error: {0}", ex.ToString());
			}

			return Schema;
		}

		private static BlobContainerClient GetBlobContainerClient()
		{
			string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
			string containerName = Environment.GetEnvironmentVariable("ContainerName");
			var blobClient = new BlobContainerClient(Connection, containerName);
			return blobClient;
		}

        private static int AddTasksToList(int CurrentDocCount, int FileCounter, string IndexName, SearchClient searchClient)
        {
            List<Task> tasks = new List<Task>();
            for (int job = 0; job < ParallelizedJobs; job++)
            {
                FileCounter++;
                int fileCounter = FileCounter;
                if ((fileCounter - 1) * MaxBatchSize < CurrentDocCount)
                {
                    _log.LogInformation("  Backing up source documents to {0} - (batch size = {1})", IndexName + fileCounter + ".json", MaxBatchSize);
                    tasks.Add(Task.Factory.StartNew(async () =>
                       await ExportToJSON((fileCounter - 1) * MaxBatchSize, IndexName + fileCounter + ".json", searchClient)
                    ));
                }
            }
            Task.WaitAll(tasks.ToArray());
            return FileCounter;
        }

        static async Task ExportToJSON(int Skip, string FileName, SearchClient searchClient)
        {
            // Extract all the documents from the selected index to JSON files in batches of 500 docs / file
            string json = string.Empty;
            try
            {
                SearchOptions options = AzureSearchHelper.AzureSearchOptions(Skip, MaxBatchSize);

                SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>("*", options);
				if (response != null)
				{
                    foreach (var doc in response.GetResults())
                    {
                        json += JsonSerializer.Serialize(doc.Document) + ",";
                        json += "\r\n";
                    }
                    // Output the formatted content to a file
                    using (var stream = new MemoryStream())
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            json = json.Substring(0, json.Length - 3); // remove trailing comma
                            writer.Write("{\"value\": [");
                            writer.Write(json);
                            writer.Write("]}");
                            writer.Flush();
                            stream.Seek(0, SeekOrigin.Begin);
                            await UploadBlobToAzureStorage(FileName, stream);
                        }
                    }
                    _log.LogInformation("  Total documents: {0}", response.GetResults().Count().ToString());
                    json = string.Empty;
                }
            }
            catch (Exception ex)
            {
                _log.LogError("Error: {0}", ex.ToString());
            }
        }        
    }
}
