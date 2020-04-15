using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzSearchPerformance
{
    class UploadDataUsingSDK
    {

        public static void UploadDocuments(ISearchIndexClient indexClient, List<Hotel> hotels)
        {
            var batch = IndexBatch.Upload(hotels);
            try
            {
                indexClient.Documents.Index(batch);
            }
            catch (IndexBatchException e)
            {
                // When a service is under load, indexing might fail for some documents in the batch. 
                // Depending on your application, you can compensate by delaying and retrying. 
                // For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine("Failed to index some of the documents: {0}",
                    String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));

                // 207 HTTP Response Code
                    // Most important error code to track

            }
        }

        public static async Task<DocumentIndexResult> UploadDocumentsAsync(ISearchIndexClient indexClient, List<Hotel> hotels)
        {
            var batch = IndexBatch.Upload(hotels);

            try
            {
                return await indexClient.Documents.IndexAsync(batch);
            }
            catch (IndexBatchException e)
            {
                // When a service is under load, indexing might fail for some documents in the batch. 
                // Depending on your application, you can compensate by delaying and retrying. 
                // For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine("Failed to index some of the documents: {0}",
                    String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));

                return null;
            }

        }

        public static void TestVariousBatchSizes(ISearchIndexClient indexClient, int numTries)
        {
            int min = 100;
            int max = 2000;
            int step = 100;

            DataGenerator dg = new DataGenerator();

            Console.WriteLine("Batch Size \t Avg Time (ms)\t Avg Time/Doc (ms)");
            for (int numDocs = min; numDocs <= max; numDocs += step)
            {
                List<TimeSpan> durations = new List<TimeSpan>();
                for (int x = 0; x < numTries; x++)
                {
                    List<Hotel> hotels = dg.GetHotels(numDocs);

                    DateTime startTime = DateTime.Now;
                    UploadDocuments(indexClient, hotels);
                    DateTime endTime = DateTime.Now;
                    durations.Add(endTime - startTime);
                }

                //Console.WriteLine("Ended at: {0}", endTime);
                var avgDuration = durations.Average(timeSpan => timeSpan.TotalMilliseconds);
                Console.WriteLine("{0} \t\t {1} \t {2}", numDocs, Math.Round(avgDuration, 4), Math.Round((avgDuration / numDocs), 4));

                // Pausing 2 seconds to let the search service catch its breath
                Thread.Sleep(2000);
            }
        }


        public static async Task<DocumentIndexResult> ExponentialBackoffAsync(ISearchIndexClient indexClient, List<Hotel> hotels, int id, bool verbose=true)
        {
            // Create batch of documents for indexing
            var batch = IndexBatch.Upload(hotels);

            // Define parameters for exponential backoff
            int attempts = 0;
            TimeSpan delay = delay = TimeSpan.FromSeconds(2);
            int maxRetryAttempts = 5;

            // Implement exponential backoff
            do
            {
                try
                {
                    attempts++;
                    var response = await indexClient.Documents.IndexAsync(batch);
                    return response;
                }
                catch (IndexBatchException ex)
                {
                    if (verbose)
                    {
                        Console.WriteLine("BATCH STARTING AT DOC {0}:", id);
                        Console.WriteLine("[Attempt: {0} of {1} Failed] [{2}]", attempts, maxRetryAttempts, ex.Response.StatusCode);
                        Console.WriteLine("Error: {0}", ex.Message);

                    }
                    if (attempts == maxRetryAttempts)
                    {
                        // Should add more admin notifications on errors like this where max retry failure happened
                        if (verbose)
                               Console.WriteLine("Max Retries hit. Giving up on this batch");
                        break;
                    }

                    // Print information regarding status codes
                    var statusCodes = ex.IndexingResults.Select(x => x.StatusCode).Distinct();
                    foreach(int s in statusCodes)
                    {
                        int count = ex.IndexingResults.Count(x => x.StatusCode == s);
                        if (verbose)
                            Console.WriteLine("[Status Code {0}] - {1} documents", s, count);
                    }

                    if (verbose)
                        Console.WriteLine("Retrying failed documents using exponential backoff...\n");

                    // Get all failed results and update the batch to just those
                    var failedItems = ex.IndexingResults.Where(x => x.Succeeded == false).Select(y => y.Key);
                    var newBatch = hotels.Where(x => failedItems.Any(x2 => x2 == x.HotelId));
                    batch = IndexBatch.Upload(newBatch);

                    Task.Delay(delay).Wait();
                    delay = delay * 2;
                }
                catch (Exception ex)
                {
                    if (verbose)
                    {
                        Console.WriteLine("BATCH STARTING AT DOC {0}:", id);
                        Console.WriteLine("[Attempt: {0} of {1} Failed] - Error: {2} \n", attempts, maxRetryAttempts, ex.Message);
                    }
                    if (attempts == maxRetryAttempts)
                        // Should add more adimn notifications on errors like this where max retry failure happened
                        break;

                    Task.Delay(delay).Wait();
                    delay = delay * 2;
                }
            } while (true);

            return null;
        }

        public static async Task UploadUsingExponentialBackoff(ISearchIndexClient indexClient, List<Hotel> hotels, int batchSize, int numThreads)
        {
            int numDocs = hotels.Count;
            Console.WriteLine("Uploading {0} documents...\n", numDocs.ToString());

            DateTime startTime = DateTime.Now;
            Console.WriteLine("Started at: {0} \n", startTime);

            Console.WriteLine("Creating {0} threads...\n", numThreads);
            List<Task<DocumentIndexResult>> uploadTasks = new List<Task<DocumentIndexResult>>();
            for (int i = 0; i < numDocs; i += batchSize)
            {
                List<Hotel> hotelBatch = hotels.GetRange(i, batchSize);
                var task = ExponentialBackoffAsync(indexClient, hotelBatch, i);
                uploadTasks.Add(task);
                Console.WriteLine("Sending a batch of {0} docs starting with doc {1}...\n", batchSize, i);

                if (uploadTasks.Count >= numThreads)
                {
                    Task<DocumentIndexResult> firstTaskFinished = await Task.WhenAny(uploadTasks);
                    Console.WriteLine("Finished a thread, kicking off another...");
                    uploadTasks.Remove(firstTaskFinished);
                }
            }

            // waiting for remaining results to finish
            await Task.WhenAll(uploadTasks);

            DateTime endTime = DateTime.Now;

            TimeSpan runningTime = endTime - startTime;
            Console.WriteLine("\nEnded at: {0} \n", endTime);
            Console.WriteLine("Upload time total: {0}", runningTime);
            double timePerBatch = Math.Round(runningTime.TotalMilliseconds / (numDocs / batchSize), 4);
            Console.WriteLine("Upload time per batch: {0} ms", timePerBatch);
            double timePerDoc = Math.Round(runningTime.TotalMilliseconds / numDocs, 4);
            Console.WriteLine("Upload time per document: {0} ms \n", timePerDoc);
        }

        public static async Task TestVariousThreadCounts(ISearchIndexClient indexClient, int numDocs, int batchSize, int numTries)
        {
            int min = 1;
            int max = 15;
            int step = 2;

            DataGenerator dg = new DataGenerator();

            Console.WriteLine("Thread Count \t Avg Time (ms)\t Avg Time/Doc (ms)");
            for (int numThreads = min; numThreads <= max; numThreads += step)
            {
                List<TimeSpan> durations = new List<TimeSpan>();
                for (int x = 0; x < numTries; x++)
                {
                    List<Hotel> hotels = dg.GetHotels(numDocs);

                    DateTime startTime = DateTime.Now;

                    List<Task<DocumentIndexResult>> uploadTasks = new List<Task<DocumentIndexResult>>();
                    for (int i = 0; i < numDocs; i += batchSize)
                    {
                        List<Hotel> hotelBatch = hotels.GetRange(i, batchSize);
                        var task = ExponentialBackoffAsync(indexClient, hotelBatch, i, false);
                        uploadTasks.Add(task);

                        if (uploadTasks.Count >= numThreads)
                        {
                            Task<DocumentIndexResult> firstTaskFinished = await Task.WhenAny(uploadTasks);
                            uploadTasks.Remove(firstTaskFinished);
                        }
                    }

                    // waiting for remaining results to finish
                    await Task.WhenAll(uploadTasks);

                    DateTime endTime = DateTime.Now;
                    durations.Add(endTime - startTime);

                    // Pausing 10 seconds to let the search service catch its breath
                    Thread.Sleep(10000);
                }

                //Console.WriteLine("Ended at: {0}", endTime);
                var avgDuration = durations.Average(timeSpan => timeSpan.TotalMilliseconds);
                Console.WriteLine("{0} \t\t {1} \t {2}", numThreads, Math.Round(avgDuration, 4), Math.Round((avgDuration / numDocs), 4));

                // Pausing 10 seconds to let the search service catch its breath
                Thread.Sleep(20000);
            }
        }

        
    }
}
