using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace AzureCognitiveSearchIndexBackupFunction
{
    public class AzureSearchHelper
    {
        public const string ApiVersionString = "api-version=2019-05-06";

        static AzureSearchHelper()
        {
        }

        public static MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        public static HttpResponseMessage SendSearchRequest(HttpClient client, HttpMethod method, Uri uri, string json = null)
        {
            UriBuilder builder = new UriBuilder(uri);
            string separator = string.IsNullOrWhiteSpace(builder.Query) ? string.Empty : "&";
            builder.Query = builder.Query.TrimStart('?') + separator + ApiVersionString;

            var request = new HttpRequestMessage(method, builder.Uri);

            if (json != null)
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return client.SendAsync(request).Result;
        }

        public static void EnsureSuccessfulSearchResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string error = response.Content == null ? null : response.Content.ReadAsStringAsync().Result;
                throw new Exception("Search request failed: " + error);
            }
        }
        public static SearchOptions AzureSearchOptions(int Skip, int maxBatchSize)
        {
            return new SearchOptions()
            {
                SearchMode = SearchMode.All,
                Size = maxBatchSize,
                Skip = Skip
            };
        }

        public static int GetCurrentDocCount(SearchClient searchClient, ILogger _log)
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
                _log.LogError("Error: {0}", ex.ToString());
            }
            return -1;
        }

        internal static void GetServiceUri(string sourceSearchServiceName, string sourceAdminKey, out Uri ServiceUri, out HttpClient HttpClient)
        {
            ServiceUri = new Uri("https://" + sourceSearchServiceName + ".search.windows.net");
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("api-key", sourceAdminKey);
        }
	}
}
