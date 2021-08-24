using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureSearchSchemaUpdate
{
    public class AzureStorageHelper
    {
        private static string ContentType = "application/json";

        // upload the file to blob storage.
        public async static Task UploadAsync(string connectionString, string containerName, string fileName, byte[] fileData)
        {
            CloudBlobContainer cloudBlobContainer = await GetContainerAsync(connectionString, containerName).ConfigureAwait(false);
            await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Off }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(fileName) && fileData != null)
            {
                var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                cloudBlockBlob.Properties.ContentType = ContentType;
                await cloudBlockBlob.UploadFromByteArrayAsync(fileData, 0, fileData.Length).ConfigureAwait(false);
            }
        }

        // download content from blob storage.
        public async static Task<string> DownloadAsync(string connectionString, string containerName, string fileName)
        {
            CloudBlobContainer cloudBlobContainer = await GetContainerAsync(connectionString, containerName).ConfigureAwait(false);
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            return await cloudBlockBlob.DownloadTextAsync().ConfigureAwait(false);
        }

        // Get blob list based condition.
        public async static Task<string[]> GetDocumentsListAsync(string connectionString, string containerName, string startsWith = "", string endsWith = ".json")
        {
            CloudBlobContainer cloudBlobContainer = await GetContainerAsync(connectionString, containerName).ConfigureAwait(false);
            BlobContinuationToken continuationToken = null;
            var blobResultSegment = await cloudBlobContainer.ListBlobsSegmentedAsync(continuationToken);
            var blobs = blobResultSegment.Results.Select(i => i.Uri.Segments.Last()).Where(e => e.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase) && e.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase)).ToArray();

            return blobs;
        }

        // Get Container object.
        private static async Task<CloudBlobContainer> GetContainerAsync(string connectionString, string containerName)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var cloudBlobClient = account.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            if (!await cloudBlobContainer.ExistsAsync().ConfigureAwait(false))
            {
                await cloudBlobContainer.CreateAsync().ConfigureAwait(false);
            }

            return cloudBlobContainer;
        }
    }
}
