using dotenv.net;
using System.Text.Json;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.KnowledgeBases;
using Azure.Search.Documents.KnowledgeBases.Models;

namespace AzureSearch.Quickstart
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load environment variables from the .env file
            // Ensure your .env file is in the same directory with the required variables
            DotEnv.Load();

            string searchEndpoint = Environment.GetEnvironmentVariable("SEARCH_ENDPOINT")
                ?? throw new InvalidOperationException("SEARCH_ENDPOINT isn't set.");
            string aoaiEndpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT")
                ?? throw new InvalidOperationException("AOAI_ENDPOINT isn't set.");

            string aoaiEmbeddingModel = "text-embedding-3-large";
            string aoaiEmbeddingDeployment = "text-embedding-3-large";
            string aoaiGptModel = "gpt-5-mini";
            string aoaiGptDeployment = "gpt-5-mini";

            string indexName = "earth-at-night";
            string knowledgeSourceName = "earth-knowledge-source";
            string knowledgeBaseName = "earth-knowledge-base";

            var credential = new DefaultAzureCredential();

            // Define fields for the index
            var fields = new List<SearchField>
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchField("page_chunk", SearchFieldDataType.String) { IsFilterable = false, IsSortable = false, IsFacetable = false },
                new SearchField("page_embedding_text_3_large", SearchFieldDataType.Collection(SearchFieldDataType.Single)) { VectorSearchDimensions = 3072, VectorSearchProfileName = "hnsw_text_3_large" },
                new SimpleField("page_number", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true, IsFacetable = true }
            };

            // Define a vectorizer
            var vectorizer = new AzureOpenAIVectorizer(vectorizerName: "azure_openai_text_3_large")
            {
                Parameters = new AzureOpenAIVectorizerParameters
                {
                    ResourceUri = new Uri(aoaiEndpoint),
                    DeploymentName = aoaiEmbeddingDeployment,
                    ModelName = aoaiEmbeddingModel
                }
            };

            // Define a vector search profile and algorithm
            var vectorSearch = new VectorSearch()
            {
                Profiles =
                {
                    new VectorSearchProfile(
                        name: "hnsw_text_3_large",
                        algorithmConfigurationName: "alg"
                    )
                    {
                        VectorizerName = "azure_openai_text_3_large"
                    }
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(name: "alg")
                },
                Vectorizers =
                {
                    vectorizer
                }
            };

            // Define a semantic configuration
            var semanticConfig = new SemanticConfiguration(
                name: "semantic_config",
                prioritizedFields: new SemanticPrioritizedFields
                {
                    ContentFields = { new SemanticField("page_chunk") }
                }
            );

            var semanticSearch = new SemanticSearch()
            {
                DefaultConfigurationName = "semantic_config",
                Configurations = { semanticConfig }
            };

            // Create the index
            var index = new SearchIndex(indexName)
            {
                Fields = fields,
                VectorSearch = vectorSearch,
                SemanticSearch = semanticSearch
            };

            // Create the index client, deleting and recreating the index if it exists
            var indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
            await indexClient.CreateOrUpdateIndexAsync(index);
            Console.WriteLine($"Index '{indexName}' created or updated successfully.");

            // Upload sample documents from the GitHub URL
            string url = "https://raw.githubusercontent.com/Azure-Samples/azure-search-sample-data/refs/heads/main/nasa-e-book/earth-at-night-json/documents.json";
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var documents = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
            var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);
            var searchIndexingBufferedSender = new SearchIndexingBufferedSender<Dictionary<string, object>>(
                searchClient,
                new SearchIndexingBufferedSenderOptions<Dictionary<string, object>>
                {
                    KeyFieldAccessor = doc => doc["id"].ToString(),
                }
            );

            await searchIndexingBufferedSender.UploadDocumentsAsync(documents);
            await searchIndexingBufferedSender.FlushAsync();
            Console.WriteLine($"Documents uploaded to index '{indexName}' successfully.");

            // Create a knowledge source
            var indexKnowledgeSource = new SearchIndexKnowledgeSource(
                name: knowledgeSourceName,
                searchIndexParameters: new SearchIndexKnowledgeSourceParameters(searchIndexName: indexName)
                {
                    SourceDataFields = { new SearchIndexFieldReference(name: "id"), new SearchIndexFieldReference(name: "page_chunk"), new SearchIndexFieldReference(name: "page_number") }
                }
            );

            await indexClient.CreateOrUpdateKnowledgeSourceAsync(indexKnowledgeSource);
            Console.WriteLine($"Knowledge source '{knowledgeSourceName}' created or updated successfully.");

            // Create a knowledge base
            var openAiParameters = new AzureOpenAIVectorizerParameters
            {
                ResourceUri = new Uri(aoaiEndpoint),
                DeploymentName = aoaiGptDeployment,
                ModelName = aoaiGptModel
            };

            var model = new KnowledgeBaseAzureOpenAIModel(azureOpenAIParameters: openAiParameters);

            var knowledgeBase = new KnowledgeBase(
                name: knowledgeBaseName,
                knowledgeSources: new KnowledgeSourceReference[] { new KnowledgeSourceReference(knowledgeSourceName) }
            )
            {
                RetrievalReasoningEffort = new KnowledgeRetrievalLowReasoningEffort(),
                AnswerInstructions = "Provide a two sentence concise and informative answer based on the retrieved documents.",
                Models = { model }
            };

            await indexClient.CreateOrUpdateKnowledgeBaseAsync(knowledgeBase);
            Console.WriteLine($"Knowledge base '{knowledgeBaseName}' created or updated successfully.");

            // Set up messages
            string instructions = @"A Q&A agent that can answer questions about the Earth at night.
            If you don't have the answer, respond with ""I don't know"".";

            var messages = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", instructions }
                }
            };

            // Run agentic retrieval
            var baseClient = new KnowledgeBaseRetrievalClient(
                endpoint: new Uri(searchEndpoint),
                knowledgeBaseName: knowledgeBaseName,
                tokenCredential: new DefaultAzureCredential()
            );

            string query = @"Why do suburban belts display larger December brightening than urban cores even though absolute light levels are higher downtown? Why is the Phoenix nighttime street grid is so sharply visible from space, whereas large stretches of the interstate between midwestern cities remain comparatively dim?";

            messages.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", query }
            });

            Console.WriteLine($"Running the query...{query}");
            var retrievalRequest = new KnowledgeBaseRetrievalRequest();
            foreach (Dictionary<string, string> message in messages) {
                if (message["role"] != "system") {
                    retrievalRequest.Messages.Add(new KnowledgeBaseMessage(content: new[] { new KnowledgeBaseMessageTextContent(message["content"]) }) { Role = message["role"] });
                }
            }
            retrievalRequest.RetrievalReasoningEffort = new KnowledgeRetrievalLowReasoningEffort();
            var retrievalResult = await baseClient.RetrieveAsync(retrievalRequest);

            messages.Add(new Dictionary<string, string>
            {
                { "role", "assistant" },
                { "content", (retrievalResult.Value.Response[0].Content[0] as KnowledgeBaseMessageTextContent)!.Text }
            });

            // Print the response, activity, and references
            Console.WriteLine("Response:");
            Console.WriteLine((retrievalResult.Value.Response[0].Content[0] as KnowledgeBaseMessageTextContent)!.Text);

            Console.WriteLine("Activity:");
            foreach (var activity in retrievalResult.Value.Activity)
            {
                Console.WriteLine($"Activity Type: {activity.GetType().Name}");
                string activityJson = JsonSerializer.Serialize(
                    activity,
                    activity.GetType(),
                    new JsonSerializerOptions { WriteIndented = true }
                );
                Console.WriteLine(activityJson);
            }

            Console.WriteLine("References:");
            foreach (var reference in retrievalResult.Value.References)
            {
                Console.WriteLine($"Reference Type: {reference.GetType().Name}");
                string referenceJson = JsonSerializer.Serialize(
                    reference,
                    reference.GetType(),
                    new JsonSerializerOptions { WriteIndented = true }
                );
                Console.WriteLine(referenceJson);
            }

            // Continue the conversation
            string nextQuery = "How do I find lava at night?";
            Console.WriteLine($"Continue the conversation with this query: {nextQuery}");
            messages.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", nextQuery }
            });

            retrievalRequest = new KnowledgeBaseRetrievalRequest();
            foreach (Dictionary<string, string> message in messages) {
                if (message["role"] != "system") {
                    retrievalRequest.Messages.Add(new KnowledgeBaseMessage(content: new[] { new KnowledgeBaseMessageTextContent(message["content"]) }) { Role = message["role"] });
                }
            }
            retrievalRequest.RetrievalReasoningEffort = new KnowledgeRetrievalLowReasoningEffort();
            retrievalResult = await baseClient.RetrieveAsync(retrievalRequest);

            messages.Add(new Dictionary<string, string>
            {
                { "role", "assistant" },
                { "content", (retrievalResult.Value.Response[0].Content[0] as KnowledgeBaseMessageTextContent)!.Text }
            });

            // Print the new response, activity, and references
            Console.WriteLine("Response:");
            Console.WriteLine((retrievalResult.Value.Response[0].Content[0] as KnowledgeBaseMessageTextContent)!.Text);

            Console.WriteLine("Activity:");
            foreach (var activity in retrievalResult.Value.Activity)
            {
                Console.WriteLine($"Activity Type: {activity.GetType().Name}");
                string activityJson = JsonSerializer.Serialize(
                    activity,
                    activity.GetType(),
                    new JsonSerializerOptions { WriteIndented = true }
                );
                Console.WriteLine(activityJson);
            }

            Console.WriteLine("References:");
            foreach (var reference in retrievalResult.Value.References)
            {
                Console.WriteLine($"Reference Type: {reference.GetType().Name}");
                string referenceJson = JsonSerializer.Serialize(
                    reference,
                    reference.GetType(),
                    new JsonSerializerOptions { WriteIndented = true }
                );
                Console.WriteLine(referenceJson);
            }

            // Clean up resources
            await indexClient.DeleteKnowledgeBaseAsync(knowledgeBaseName);
            Console.WriteLine($"Knowledge base '{knowledgeBaseName}' deleted successfully.");

            await indexClient.DeleteKnowledgeSourceAsync(knowledgeSourceName);
            Console.WriteLine($"Knowledge source '{knowledgeSourceName}' deleted successfully.");

            await indexClient.DeleteIndexAsync(indexName);
            Console.WriteLine($"Index '{indexName}' deleted successfully.");
        }
    }
}