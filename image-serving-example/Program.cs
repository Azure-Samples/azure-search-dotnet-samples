using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.KnowledgeBases;
using Azure.Search.Documents.KnowledgeBases.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using dotenv.net;
using System.ClientModel.Primitives;

namespace AzureSearch.ImageServingExample;

internal static class Program
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(30);

    public static async Task Main(string[] args)
    {
        if (args.Contains("--validate-local", StringComparer.OrdinalIgnoreCase))
        {
            ValidateLocalModels();
            return;
        }

        DotEnv.Load();
        Settings settings = Settings.Load();
        var credential = new DefaultAzureCredential();
        var indexClient = new SearchIndexClient(settings.SearchEndpoint, credential);
        bool keepResources = args.Contains("--keep-resources", StringComparer.OrdinalIgnoreCase);

        try
        {
            AzureBlobKnowledgeSource source = CreateKnowledgeSource(settings);
            AzureBlobKnowledgeSource created = (AzureBlobKnowledgeSource)(await indexClient
                .CreateOrUpdateKnowledgeSourceAsync(source)).Value;
            string indexName = created.AzureBlobParameters.CreatedResources
                .AdditionalProperties["index"];
            Console.WriteLine($"Knowledge source: {created.Name}");
            Console.WriteLine($"Generated index: {indexName}");

            await WaitForIngestionAsync(indexClient, settings.KnowledgeSourceName);
            string imagePath = await FindIndexedImagePathAsync(
                settings.SearchEndpoint,
                indexName,
                credential);
            Console.WriteLine($"Image path: {imagePath}");

            KnowledgeBase knowledgeBase = CreateKnowledgeBase(settings);
            await indexClient.CreateOrUpdateKnowledgeBaseAsync(knowledgeBase);

            var retrievalClient = new KnowledgeBaseRetrievalClient(
                settings.SearchEndpoint,
                settings.KnowledgeBaseName,
                credential);
            KnowledgeBaseRetrievalResponse disabled = await RetrieveAsync(
                retrievalClient,
                settings,
                enableImageServing: false,
                settings.Query);
            ImageTotals disabledTotals = GetImageTotals(disabled);
            string disabledAnswer = GetAnswer(disabled);
            Console.WriteLine($"Disabled answer: {disabledAnswer}");
            ImageTotals enabledTotals = new(0, 0, 0);
            string enabledAnswer = string.Empty;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                KnowledgeBaseRetrievalResponse enabled = await RetrieveAsync(
                    retrievalClient,
                    settings,
                    enableImageServing: true,
                    settings.Query);
                enabledTotals = GetImageTotals(enabled);
                enabledAnswer = GetAnswer(enabled);
                if (enabledTotals.ImagesSentToModel > 0)
                {
                    break;
                }
                Console.WriteLine(
                    $"Image serving isn't ready (attempt {attempt} of 5). Retrying.");
                await Task.Delay(PollInterval);
            }
            Console.WriteLine($"Enabled answer:  {enabledAnswer}");
            Console.WriteLine($"Disabled: {disabledTotals}");
            Console.WriteLine($"Enabled:  {enabledTotals}");

            if (disabledTotals.ImagesSentToModel != 0)
            {
                throw new InvalidOperationException(
                    "The disabled request unexpectedly sent images to the model.");
            }
            if (enabledTotals.ImagesRetrieved <= 0 ||
                enabledTotals.ImagesSentToModel <= 0 ||
                enabledTotals.TotalImageSizeBytes <= 0)
            {
                throw new InvalidOperationException(
                    "The enabled request didn't report nonzero image-serving statistics.");
            }

            await DownloadImageAsync(settings, credential, imagePath);
            Console.WriteLine("Image-serving validation passed.");
        }
        finally
        {
            if (!keepResources)
            {
                await DeleteIfExistsAsync(
                    () => indexClient.DeleteKnowledgeBaseAsync(settings.KnowledgeBaseName),
                    "knowledge base",
                    settings.KnowledgeBaseName);
                await DeleteIfExistsAsync(
                    () => indexClient.DeleteKnowledgeSourceAsync(settings.KnowledgeSourceName),
                    "knowledge source",
                    settings.KnowledgeSourceName);
            }
        }
    }

    private static void ValidateLocalModels()
    {
        var settings = new Settings(
            new Uri("https://example.search.windows.net"),
            "image-serving-ks",
            "image-serving-kb",
            "What is shown in the image?",
            "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/store",
            new Uri("https://store.blob.core.windows.net"),
            "source-documents",
            "image-assets",
            new Uri("https://example.services.ai.azure.com"),
            "embedding-deployment",
            "embedding-model",
            "chat-deployment",
            "chat-model");

        string sourceJson = ModelReaderWriter.Write(
            CreateKnowledgeSource(settings),
            ModelReaderWriterOptions.Json).ToString();
        const string foundryEndpoint = "https://example.services.ai.azure.com";
        if (!sourceJson.Contains("ResourceId=", StringComparison.Ordinal) ||
            !sourceJson.Contains("\"assetStore\"", StringComparison.Ordinal) ||
            sourceJson.Split(foundryEndpoint, StringSplitOptions.None).Length - 1 != 3 ||
            sourceJson.Contains("OcrSkill", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Knowledge source serialization didn't match managed ingestion.");
        }

        string knowledgeBaseJson = ModelReaderWriter.Write(
            CreateKnowledgeBase(settings),
            ModelReaderWriterOptions.Json).ToString();
        if (knowledgeBaseJson.Split(foundryEndpoint, StringSplitOptions.None).Length - 1 != 1)
        {
            throw new InvalidOperationException(
                "Knowledge base serialization didn't use the Foundry endpoint.");
        }

        const string projectedImagePath = "11.7:encoded/path.jpg";
        if (GetBlobName(projectedImagePath, "image-assets") != "encoded/path.jpg")
        {
            throw new InvalidOperationException(
            "Image path normalization didn't remove the projection prefix.");
        }

        const string absoluteImageReferences =
            "https://store.blob.core.windows.net/image-assets/encoded%2Ffirst.jpg;" +
            "https://store.blob.core.windows.net/image-assets/second.jpg";
        if (GetBlobName(absoluteImageReferences, "image-assets") != "encoded/first.jpg")
        {
            throw new InvalidOperationException(
                "Image path normalization didn't select the first absolute reference.");
        }

        foreach (bool enabled in new[] { false, true })
        {
            var request = new KnowledgeBaseRetrievalRequest();
            request.KnowledgeSourceParams.Add(
                new AzureBlobKnowledgeSourceParams(settings.KnowledgeSourceName)
                {
                    EnableImageServing = enabled
                });
            string requestJson = ModelReaderWriter.Write(
                request,
                ModelReaderWriterOptions.Json).ToString();
            string expected = $"\"enableImageServing\":{enabled.ToString().ToLowerInvariant()}";
            if (!requestJson.Contains(expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Request serialization is missing {expected}.");
            }
        }
        Console.WriteLine(
            "Local validation passed: managed ingestion and image-serving requests serialize correctly.");
    }

    private static AzureBlobKnowledgeSource CreateKnowledgeSource(Settings settings)
    {
        string connection = $"ResourceId={settings.StorageResourceId}";
        var embeddingParameters = new AzureOpenAIVectorizerParameters
        {
            ResourceUri = settings.FoundryEndpoint,
            DeploymentName = settings.EmbeddingDeployment,
            ModelName = settings.EmbeddingModel
        };
        var chatParameters = new AzureOpenAIVectorizerParameters
        {
            ResourceUri = settings.FoundryEndpoint,
            DeploymentName = settings.ChatDeployment,
            ModelName = settings.ChatModel
        };
        var ingestion = new KnowledgeSourceIngestionParameters
        {
            ContentExtractionMode = KnowledgeSourceContentExtractionMode.Standard,
            EmbeddingModel = new KnowledgeSourceAzureOpenAIVectorizer
            {
                AzureOpenAIParameters = embeddingParameters
            },
            ChatCompletionModel = new KnowledgeBaseAzureOpenAIModel(chatParameters),
            DisableImageVerbalization = false,
            AiServices = new AIServices(settings.FoundryEndpoint),
            AssetStore = new AssetStore(connection, settings.AssetContainer)
        };
        var blobParameters = new AzureBlobKnowledgeSourceParameters(
            connection,
            settings.SourceContainer)
        {
            IngestionParameters = ingestion
        };
        return new AzureBlobKnowledgeSource(
            settings.KnowledgeSourceName,
            blobParameters);
    }

    private static KnowledgeBase CreateKnowledgeBase(Settings settings)
    {
        var chatParameters = new AzureOpenAIVectorizerParameters
        {
            ResourceUri = settings.FoundryEndpoint,
            DeploymentName = settings.ChatDeployment,
            ModelName = settings.ChatModel
        };
        return new KnowledgeBase(
            settings.KnowledgeBaseName,
            new[]
            {
                new KnowledgeSourceReference(settings.KnowledgeSourceName)
                {
                    EnableImageServing = true
                }
            })
        {
            OutputMode = KnowledgeRetrievalOutputMode.AnswerSynthesis,
            RetrievalReasoningEffort = new KnowledgeRetrievalMediumReasoningEffort(),
            Models = { new KnowledgeBaseAzureOpenAIModel(chatParameters) }
        };
    }

    private static async Task WaitForIngestionAsync(
        SearchIndexClient client,
        string knowledgeSourceName)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + PollTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            KnowledgeSourceStatus status = (await client
                .GetKnowledgeSourceStatusAsync(knowledgeSourceName)).Value;
            SynchronizationState? current = status.CurrentSynchronizationState;
            if (current is not null)
            {
                Console.WriteLine(
                    $"Managed ingestion {status.SynchronizationStatus}: " +
                    $"{current.ItemsUpdatesProcessed} processed, " +
                    $"{current.ItemsUpdatesFailed} failed, " +
                    $"{current.ItemsSkipped} skipped.");
                if (current.ItemsUpdatesFailed > 0)
                {
                    string errors = string.Join(
                        Environment.NewLine,
                        current.Errors.Select(error => error.ErrorMessage));
                    throw new InvalidOperationException(
                        $"Managed ingestion has failed items.{Environment.NewLine}{errors}");
                }
            }
            if (current is null && status.LastSynchronizationState is not null)
            {
                if (status.LastSynchronizationState.ItemsUpdatesFailed > 0)
                {
                    throw new InvalidOperationException(
                        $"Managed ingestion failed for " +
                        $"{status.LastSynchronizationState.ItemsUpdatesFailed} item(s).");
                }
                Console.WriteLine(
                    $"Managed ingestion processed " +
                    $"{status.LastSynchronizationState.ItemsUpdatesProcessed} item(s).");
                return;
            }
            await Task.Delay(PollInterval);
        }
        throw new TimeoutException("Timed out waiting for managed ingestion.");
    }

    private static async Task<string> FindIndexedImagePathAsync(
        Uri endpoint,
        string indexName,
        DefaultAzureCredential credential)
    {
        var searchClient = new SearchClient(endpoint, indexName, credential);
        var options = new SearchOptions { Size = 100 };
        options.Select.Add("image_path");
        SearchResults<SearchDocument> results = await searchClient.SearchAsync<SearchDocument>(
            "*",
            options);
        await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync())
        {
            if (!result.Document.TryGetValue("image_path", out object? value))
            {
                continue;
            }
            string? imagePath = value switch
            {
                string text => text,
                IEnumerable<string> paths => paths.FirstOrDefault(),
                _ => value?.ToString()
            };
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                return imagePath;
            }
        }
        throw new InvalidOperationException(
            "The generated index doesn't contain a nonempty image_path.");
    }

    private static async Task<KnowledgeBaseRetrievalResponse> RetrieveAsync(
        KnowledgeBaseRetrievalClient client,
        Settings settings,
        bool enableImageServing,
        string query)
    {
        var request = new KnowledgeBaseRetrievalRequest
        {
            IncludeActivity = true,
            OutputMode = KnowledgeRetrievalOutputMode.AnswerSynthesis
        };
        request.Messages.Add(new KnowledgeBaseMessage(
            new[] { new KnowledgeBaseMessageTextContent(query) })
        {
            Role = "user"
        });
        request.KnowledgeSourceParams.Add(
            new AzureBlobKnowledgeSourceParams(settings.KnowledgeSourceName)
            {
                EnableImageServing = enableImageServing
            });
        return (await client.RetrieveAsync(request)).Value;
    }

    private static string GetAnswer(KnowledgeBaseRetrievalResponse response) =>
        string.Join(
            Environment.NewLine,
            response.Response
                .SelectMany(message => message.Content)
                .OfType<KnowledgeBaseMessageTextContent>()
                .Select(content => content.Text));

    private static ImageTotals GetImageTotals(KnowledgeBaseRetrievalResponse response)
    {
        IEnumerable<ImageServingStatistics> statistics = response.Activity
            .OfType<KnowledgeBaseAzureBlobActivityRecord>()
            .Where(record => record.ImageServing is not null)
            .Select(record => record.ImageServing);
        return new ImageTotals(
            statistics.Sum(item => item.ImagesRetrieved ?? 0),
            statistics.Sum(item => item.ImagesSentToModel ?? 0),
            statistics.Sum(item => item.TotalImageSizeBytes ?? 0));
    }

    private static async Task DownloadImageAsync(
        Settings settings,
        DefaultAzureCredential credential,
        string imagePath)
    {
        string blobName = GetBlobName(imagePath, settings.AssetContainer);
        var serviceClient = new BlobServiceClient(settings.StorageAccountUrl, credential);
        BlobClient blobClient = serviceClient
            .GetBlobContainerClient(settings.AssetContainer)
            .GetBlobClient(blobName);
        Azure.Storage.Blobs.Models.BlobProperties properties =
            (await blobClient.GetPropertiesAsync()).Value;
        BinaryData bytes = (await blobClient.DownloadContentAsync()).Value.Content;
        string contentType = properties.ContentType ?? string.Empty;
        if (bytes.ToMemory().Length == 0 ||
            !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Downloaded blob is invalid: {bytes.ToMemory().Length} bytes, " +
                $"content type '{contentType}'.");
        }
        Console.WriteLine(
            $"Downloaded {bytes.ToMemory().Length} bytes with content type {contentType}.");
    }

    private static string GetBlobName(string imagePath, string assetContainer)
    {
        string blobName = imagePath.Split(';', 2)[0];
        if (Uri.TryCreate(blobName, UriKind.Absolute, out Uri? imageUri))
        {
            string path = Uri.UnescapeDataString(imageUri.AbsolutePath).TrimStart('/');
            string prefix = assetContainer + "/";
            blobName = path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? path[prefix.Length..]
                : path;
        }
        else
        {
            int projectionPrefix = blobName.IndexOf(':');
            if (projectionPrefix >= 0)
            {
                blobName = blobName[(projectionPrefix + 1)..];
            }
        }
        return blobName;
    }

    private static async Task DeleteIfExistsAsync(
        Func<Task<Response>> delete,
        string resourceType,
        string resourceName)
    {
        try
        {
            await delete();
            Console.WriteLine($"Deleted {resourceType} '{resourceName}'.");
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            Console.WriteLine($"{resourceType} '{resourceName}' doesn't exist.");
        }
    }

    private sealed record ImageTotals(
        int ImagesRetrieved,
        int ImagesSentToModel,
        long TotalImageSizeBytes);

    private sealed record Settings(
        Uri SearchEndpoint,
        string KnowledgeSourceName,
        string KnowledgeBaseName,
        string Query,
        string StorageResourceId,
        Uri StorageAccountUrl,
        string SourceContainer,
        string AssetContainer,
        Uri FoundryEndpoint,
        string EmbeddingDeployment,
        string EmbeddingModel,
        string ChatDeployment,
        string ChatModel)
    {
        public static Settings Load() => new(
            GetUri("AZURE_SEARCH_ENDPOINT"),
            Get("AZURE_SEARCH_KNOWLEDGE_SOURCE_NAME"),
            Get("AZURE_SEARCH_KNOWLEDGE_BASE_NAME"),
            Get("AZURE_SEARCH_QUERY"),
            Get("AZURE_STORAGE_RESOURCE_ID"),
            GetUri("AZURE_STORAGE_ACCOUNT_URL"),
            Get("AZURE_BLOB_SOURCE_CONTAINER"),
            Get("AZURE_BLOB_ASSET_CONTAINER"),
            GetUri("AZURE_FOUNDRY_ENDPOINT"),
            Get("AZURE_FOUNDRY_EMBEDDING_DEPLOYMENT"),
            Get("AZURE_FOUNDRY_EMBEDDING_MODEL"),
            Get("AZURE_FOUNDRY_CHAT_DEPLOYMENT"),
            Get("AZURE_FOUNDRY_CHAT_MODEL"));

        private static string Get(string name) =>
            Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException($"{name} isn't set.");

        private static Uri GetUri(string name) => new(Get(name));
    }
}
