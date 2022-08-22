using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Azure.Communication.Email.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace check_storage_usage
{
    public class CheckStorageUsage
    {
        // Run on a timer every 30 minutes
        // https://docs.microsoft.com/azure/azure-functions/functions-bindings-timer
        [FunctionName("CheckStorageUsage")]
        public async Task Run([TimerTrigger("0 */30 * * * *")]TimerInfo timer, ILogger log)
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
                string connectionString = Environment.GetEnvironmentVariable("CommunicationServicesConnectionString");
                var emailClient = new EmailClient(connectionString);

                string subject = string.Format("Low storage space on search service {0}", serviceName);
                string body = string.Format("Search service {0} is using {1:P2} of its storage which exceeds the alerting threshold of {2:P2}", serviceName, storagedUsedPercent, storageUsedPercentThreshold);
                EmailContent emailContent = new EmailContent(subject);
                emailContent.PlainText = body;
                string toEmailAddress = Environment.GetEnvironmentVariable("ToEmailAddress");
                string fromEmailAddress = Environment.GetEnvironmentVariable("FromEmailAddress");
                List<EmailAddress> emailAddresses = new List<EmailAddress> { new EmailAddress(toEmailAddress) };
                EmailRecipients emailRecipients = new EmailRecipients(emailAddresses);
                EmailMessage emailMessage = new EmailMessage(fromEmailAddress, emailContent, emailRecipients);
                Response<SendEmailResult> response = emailClient.Send(emailMessage);
                log.LogInformation("Sent email about low storage, status code {0}", response.GetRawResponse().Status);
            }
        }
    }
}
