using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Search.Documents.Indexes;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Azure.Storage.Blobs.Models;

namespace AzureCognitiveSearchIndexBackupFunction
{
    public class RestoreIndex
    {
        private static string SearchServiceName;
        private static string AdminKey;
        private static SearchIndexClient SourceIndexClient;
        private static ILogger _log;


        [FunctionName("RestoreIndex")]
        public static async Task<IActionResult> CreateTargetIndexFunction(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            ConfigurationSetup(log);
            var restoreOptions = new RestoreOptions();
            log.LogInformation("Restore Index Started");
			try
			{
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                restoreOptions = Newtonsoft.Json.JsonConvert.DeserializeObject<RestoreOptions>(requestBody);
            }
			catch (Exception ex)
			{

                _log.LogError("Error: {0}", ex.ToString());
            }
            
			await DeleteIndex(restoreOptions.IndexName);
            await CreateTargetIndex(restoreOptions.IndexName, restoreOptions.Date);
            await ImportFromJSON(restoreOptions.IndexName, restoreOptions.Date);
            return new OkObjectResult("");
        }

        static void ConfigurationSetup(ILogger log)
        {
            SearchServiceName = Environment.GetEnvironmentVariable("SearchServiceName");
            AdminKey = Environment.GetEnvironmentVariable("AdminKey");
            SourceIndexClient = new SearchIndexClient(new Uri("https://" + SearchServiceName + ".search.windows.net"), new AzureKeyCredential(AdminKey));
            _log = log;
        }

		private static BlobContainerClient GetBlobContainerClient()
		{
			string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
			string containerName = Environment.GetEnvironmentVariable("ContainerName");
			var blobClient = new BlobContainerClient(Connection, containerName);
			return blobClient;
		}

        static async Task CreateTargetIndex(string IndexName, string Date)
        {
            _log.LogInformation("\n  Create target index {0} in {1} search service", IndexName, SearchServiceName);
            // Use the schema file to create a copy of this index
            // I like using REST here since I can just take the response as-is
            BlobContainerClient blobClient = GetBlobContainerClient();
            await blobClient.CreateIfNotExistsAsync(default);
            var blob = blobClient.GetBlobClient($"{Date}/{IndexName}" + ".schema");
            
            string json = string.Empty;
            if (await blobClient.ExistsAsync())
            {
                var response = await blob.DownloadAsync();
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadToEndAsync();
                        json = line;
                    }
                }
            }
            
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
                HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(HttpClient, HttpMethod.Post, uri, json);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _log.LogError("  Error: {0}", ex.ToString());
            }
        }

        public static async Task<bool> DeleteIndex(string IndexName)
        {
            _log.LogInformation("\n  Delete target index {0} in {1} search service, if it exists", IndexName, SearchServiceName);
            // Delete the index if it exists
            try
            {
               await SourceIndexClient.DeleteIndexAsync(IndexName);
            }
            catch (Exception ex)
            {
                _log.LogError("  Error deleting index: {0}\r\n", ex.ToString());
                _log.LogInformation("  Did you remember to set your SearchServiceName and SearchServiceApiKey?\r\n");
                return false;
            }

            return true;
        }

        static async Task ImportFromJSON(string IndexName,string Date)
		{
			_log.LogInformation("\n  Upload index documents from saved JSON files");
			// Take JSON file and import this as-is to target index
			Uri ServiceUri;
			HttpClient HttpClient;
            AzureSearchHelper.GetServiceUri(SearchServiceName, AdminKey, out ServiceUri, out HttpClient);
            BlobContainerClient blobClient = GetBlobContainerClient();
			IAsyncEnumerable<BlobItem> segment = blobClient.GetBlobsAsync(prefix: $"{Date}/{IndexName}");
			try
			{
				await foreach (BlobItem blobItem in segment)
				{
					var blob = blobClient.GetBlobClient(blobItem.Name);
					if (blob != null && blob.Name.Contains(".json"))
					{
						_log.LogInformation("  -Uploading documents from file {0}", blobItem.Name);
						string json = string.Empty;
						if (await blobClient.ExistsAsync())
						{
							var responsse = await blob.DownloadAsync();
							using (var streamReader = new StreamReader(responsse.Value.Content))
							{
								while (!streamReader.EndOfStream)
								{
									var readTheFile = await streamReader.ReadToEndAsync();
									json = readTheFile;
								}
							}
							Uri uri = new Uri(ServiceUri, "/indexes/" + IndexName + "/docs/index");
							HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(HttpClient, HttpMethod.Post, uri, json);
							response.EnsureSuccessStatusCode();
						}
					}
				}
			}
			catch (Exception ex)
			{
				_log.LogError("  Error: {0}", ex.ToString());
			}
		}
	}
}
