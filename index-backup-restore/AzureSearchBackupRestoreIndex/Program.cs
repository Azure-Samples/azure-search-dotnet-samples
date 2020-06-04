﻿// This is a prototype tool that allows for extraction of data from a search index
// Since this tool is still under development, it should not be used for production usage

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSearchBackupRestore
{
    class Program
    {
        private static string SourceSearchServiceName;
        private static string SourceAdminKey;
        private static string SourceIndexName;
        private static string TargetSearchServiceName;
        private static string TargetAdminKey;
        private static string TargetIndexName;
        private static string BackupDirectory;

        private static Uri SourceSearchServiceUri;
        private static Uri TargetSearchServiceUri;

        private static SearchServiceClient SourceSearchClient;
        private static SearchClient SourceIndexClient;
        private static SearchServiceClient TargetSearchClient;
        private static SearchClient TargetIndexClient;

        private static readonly int MaxBatchSize = 500;          // JSON files will contain this many documents / file and can be up to 1000
        private static readonly int ParallelizedJobs = 10;       // Output content in parallel jobs

        static void Main(string[] args)
        {
            //Get source and target search service info and index names from appsettings.json file
            //Set up source and target search service clients
            ConfigurationSetup();

            //Backup the source index
            Console.WriteLine("\n START INDEX BACKUP");
            BackupIndexAndDocuments();

            //Recreate and import content to target index
            Console.WriteLine("\n START INDEX RESTORE");
            DeleteIndex();
            CreateTargetIndex();
            ImportFromJSON();
            Console.WriteLine("\r\n  Waiting 10 seconds for target to index content...");
            Console.WriteLine("  NOTE: For really large indexes it may take longer to index all content.\r\n");
            Thread.Sleep(10000);

            // Validate all content is in target index
            int sourceCount = GetCurrentDocCount(SourceIndexClient);
            int targetCount = GetCurrentDocCount(TargetIndexClient);
            Console.WriteLine("\nSAFEGUARD CHECK: Source and target index counts should match");
            Console.WriteLine(" Source index contains {0} docs", sourceCount);
            Console.WriteLine(" Target index contains {0} docs\r\n", targetCount);

            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }

        static void ConfigurationSetup()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            SourceSearchServiceName = configuration["SourceSearchServiceName"];
            SourceAdminKey = configuration["SourceAdminKey"];
            SourceIndexName = configuration["SourceIndexName"];
            TargetSearchServiceName = configuration["TargetSearchServiceName"];
            TargetAdminKey = configuration["TargetAdminKey"];
            TargetIndexName = configuration["TargetIndexName"];
            BackupDirectory = configuration["BackupDirectory"];

            Console.WriteLine("CONFIGURATION:");
            Console.WriteLine("\n  Source service and index {0}, {1}", SourceSearchServiceName, SourceIndexName);
            Console.WriteLine("\n  Target service and index: {0}, {1}", TargetSearchServiceName, TargetIndexName);
            Console.WriteLine("\n  Backup directory: " + BackupDirectory);

            SourceSearchServiceUri = new Uri($"https://{SourceSearchServiceName}.search.windows.net/");
            SourceSearchClient = new SearchServiceClient(SourceSearchServiceUri, new AzureKeyCredential(SourceAdminKey));
            SourceIndexClient = SourceSearchClient.GetSearchClient(SourceIndexName);

            TargetSearchServiceUri = new Uri($"https://{TargetSearchServiceName}.search.windows.net/");
            TargetSearchClient = new SearchServiceClient(TargetSearchServiceUri, new AzureKeyCredential(TargetAdminKey));
            TargetIndexClient = TargetSearchClient.GetSearchClient(TargetIndexName);
        }

        static void BackupIndexAndDocuments()
        {
            // Backup the index schema to the specified backup directory
            Console.WriteLine("\n  Backing up source index schema to {0}\r\n", BackupDirectory + SourceIndexName + ".schema");

            string GetIndexSchema()
            {
                // Extract the schema for this index
                // We use REST here because we can take the response as-is
                HttpClient HttpClient = new HttpClient();
                HttpClient.DefaultRequestHeaders.Add("api-key", SourceAdminKey);

                string schema = string.Empty;

                try
                {
                    Uri uri = new Uri(SourceSearchServiceUri, "/indexes/" + SourceIndexName);

                    HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(HttpClient, HttpMethod.Get, uri);
                    AzureSearchHelper.EnsureSuccessfulSearchResponse(response);
                    schema = response.Content.ReadAsStringAsync().Result.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message.ToString());
                }

                return schema;
            };

            string indexSchema = GetIndexSchema();

            File.WriteAllText(BackupDirectory + SourceIndexName + ".schema", indexSchema);

            // Extract the content to JSON files 
            int sourceDocCount = GetCurrentDocCount(SourceIndexClient);

            WriteIndexDocuments(sourceDocCount);     // Output content from index to json files
        }

        static void WriteIndexDocuments(int currentDocCount)
        {
            // Write document files in batches (per MaxBatchSize) in parallel
            int fileCounter = 0;

            for (int batch = 0; batch <= ((currentDocCount / MaxBatchSize) * ParallelizedJobs); batch++)
            {
                List<Task> tasks = new List<Task>();
                fileCounter++;

                if ((fileCounter - 1) * MaxBatchSize < currentDocCount)
                {
                    Console.WriteLine("Backing up source documents to {0} - (batch size = {1})", BackupDirectory + SourceIndexName + fileCounter + ".json", MaxBatchSize);

                    int skip = (fileCounter - 1) * MaxBatchSize;
                    string fileName = BackupDirectory + SourceIndexName + fileCounter + ".json";

                    tasks.Add(Task.Run(() => ExportToJSON(skip, fileName)));
                }

                Task.WaitAll(tasks.ToArray());  // Wait for all the stored procs in the group to complete
            }

            return;
        }

        static void ExportToJSON(int skip, string fileName)
        {
            // Extract all the documents from the selected index to JSON files in batches of 500 docs / file
            string json = string.Empty;

            try
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    SearchMode = SearchMode.All,
                    Size = MaxBatchSize,
                    Skip = skip
                };

                SearchResults<SearchDocument> searchResults = SourceIndexClient.Search("*", searchOptions);

                foreach (var doc in searchResults.GetResults())
                {
                    json += JsonConvert.SerializeObject(doc.Document) + ",";
                    // Geospatial is formatted such that it needs to be changed for reupload
                    // Unfortunately since it comes down in Lat, Lon format, I need to alter it to Lon, Lat for upload

                    while (json.IndexOf("CoordinateSystem") > -1)
                    {
                        // At this point the data looks like this
                        // {"Latitude":38.3399,"Longitude":-86.0887,"IsEmpty":false,"Z":null,"M":null,"CoordinateSystem":{"EpsgId":4326,"Id":"4326","Name":"WGS84"}}
                        int LatStartLocation = json.IndexOf("\"Latitude\":");
                        LatStartLocation = json.IndexOf(":", LatStartLocation) + 1;
                        int LatEndLocation = json.IndexOf(",", LatStartLocation);
                        int LonStartLocation = json.IndexOf("\"Longitude\":");
                        LonStartLocation = json.IndexOf(":", LonStartLocation) + 1;
                        int LonEndLocation = json.IndexOf(",", LonStartLocation);
                        string Lat = json.Substring(LatStartLocation, LatEndLocation - LatStartLocation);
                        string Lon = json.Substring(LonStartLocation, LonEndLocation - LonStartLocation);

                        // Now it needs to look like this
                        // { "type": "Point", "coordinates": [-122.131577, 47.678581] }
                        int GeoStartPosition = json.IndexOf("\"Latitude\":") - 1;
                        int GeoEndPosition = json.IndexOf("}}", GeoStartPosition) + 2;
                        string updatedJson = json.Substring(0, GeoStartPosition) + "{ \"type\": \"Point\", \"coordinates\": [";
                        updatedJson += Lon + ", " + Lat + "] }";
                        updatedJson += json.Substring(GeoEndPosition);
                        json = updatedJson;
                    }

                    json = json.Replace("\"Latitude\":", "\"type\": \"Point\", \"coordinates\": [");
                    json = json.Replace("\"Longitude\":", "");
                    json = json.Replace(",\"IsEmpty\":false,\"Z\":null,\"M\":null,\"CoordinateSystem\":{\"EpsgId\":4326,\"Id\":\"4326\",\"Name\":\"WGS84\"}", "]");
                    json += "\r\n";
                }

                // Output the formatted content to a file
                json = json.Substring(0, json.Length - 3); // remove trailing comma
                File.WriteAllText(fileName, "{\"value\": [");
                File.AppendAllText(fileName, json);
                File.AppendAllText(fileName, "]}");
                Console.WriteLine("  Total documents: {0}", searchResults.TotalCount);
                json = string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message.ToString());
            }

            return;
        }

        private static bool DeleteIndex()
        {
            Console.WriteLine("\n  Delete target index {0} in {1} search service, if it exists", TargetIndexName, TargetSearchServiceName);

            // Delete the index if it exists
            try
            {
                TargetSearchClient.DeleteIndex(TargetIndexName);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("  Error deleting index: {0}\r\n", ex.Message);
                Console.WriteLine("  Did you remember to set your SearchServiceName and SearchServiceApiKey?\r\n");

                return false;
            }

            return true;
        }

        static void CreateTargetIndex()
        {
            Console.WriteLine("\n  Create target index {0} in {1} search service", TargetIndexName, TargetSearchServiceName);

            // Use the schema file to create a copy of this index
            // I like using REST here since I can just take the response as-is
            string json = File.ReadAllText(BackupDirectory + SourceIndexName + ".schema");

            // Do some cleaning of this file to change index name, etc
            json = "{" + json.Substring(json.IndexOf("\"name\""));
            int indexOfIndexName = json.IndexOf("\"", json.IndexOf("name\"") + 5) + 1;
            int indexOfEndOfIndexName = json.IndexOf("\"", indexOfIndexName);
            json = json.Substring(0, indexOfIndexName) + TargetIndexName + json.Substring(indexOfEndOfIndexName);

            HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("api-key", TargetAdminKey);

            try
            {
                Uri uri = new Uri(TargetSearchServiceUri, "/indexes");
                HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(HttpClient, HttpMethod.Post, uri, json);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine("  Error: {0}", ex.Message.ToString());
            }

        }

        static int GetCurrentDocCount(SearchClient IndexClient)
        {
            // Get the current doc count of the specified index
            try
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    SearchMode = SearchMode.All,
                    IncludeTotalCount = true
                };

                SearchResults<SearchDocument> searchResults = IndexClient.Search("*", searchOptions);

                return Convert.ToInt32(searchResults.TotalCount);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message.ToString());
            }

            return -1;
        }

        static void ImportFromJSON()
        {
            Console.WriteLine("\n  Upload index documents from saved JSON files");

            // Take JSON file and import this as-is to target index
            HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("api-key", TargetAdminKey);

            try
            {
                foreach (string fileName in Directory.GetFiles(BackupDirectory, SourceIndexName + "*.json"))
                {
                    Console.WriteLine("  -Uploading documents from file {0}", fileName);
                    string json = File.ReadAllText(fileName);
                    Uri uri = new Uri(TargetSearchServiceUri, "/indexes/" + TargetIndexName + "/docs/index");
                    HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(HttpClient, HttpMethod.Post, uri, json);
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
