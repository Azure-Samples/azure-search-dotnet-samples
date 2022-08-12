using System;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace check_storage_usage
{
    public class CheckStorageUsage
    {
        private readonly IConfiguration _configuration;

        public CheckStorageUsage(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Run on a timer
        // https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=in-process&pivots=programming-language-csharp#ncrontab-expressions
        [FunctionName("CheckStorageUsage")]
        public async Task Run([TimerTrigger("0 30 * * * *")]TimerInfo timer, ILogger log)
        {
            string serviceName = Environment.GetEnvironmentVariable("ServiceName");
            log.LogInformation($"Checking search storage usage for {serviceName}: {DateTime.Now}");

            string serviceAdminApiKey = Environment.GetEnvironmentVariable("ServiceAdminApiKey");
            // Storage used percentage threshold is a number between 0 and 1 representing how much storage should be
            // used before alerting
            // Example: 0.8 = 80%
            float storageUsedPercentThreshold = float.Parse(Environment.GetEnvironmentVariable("StorageUsedPercentThreshold"));

            var searchIndexClient = new SearchIndexClient(new Uri($"https://{serviceName}.search.windows.net"), new AzureKeyCredential(serviceAdminApiKey));
            SearchServiceStatistics statistics = await searchIndexClient.GetServiceStatisticsAsync();
            float storagedUsedPercent = (float)statistics.Counters.StorageSizeCounter.Usage / (float)statistics.Counters.StorageSizeCounter.Quota;

            if (storagedUsedPercent > storageUsedPercentThreshold)
            {
                log.LogInformation("Search service {0} is using {1:P2} of its storage which exceeds the alerting threshold of {2:P2}", serviceName, storagedUsedPercent, storageUsedPercentThreshold);
            }
        }
    }
}
