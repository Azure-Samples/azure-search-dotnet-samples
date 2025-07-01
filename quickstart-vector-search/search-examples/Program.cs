using Azure.AI.OpenAI;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.Tracing;
using System.Text.Json.Serialization;

// Load configuration from appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

string searchEndpoint = config["AzureSearch:Endpoint"] ?? throw new Exception("AzureSearch:Endpoint missing");
string indexName = config["AzureSearch:IndexName"] ?? throw new Exception("AzureSearch:IndexName missing");
string openAIEndpoint = config["AzureOpenAI:Endpoint"] ?? "";
string openAIDeployment = config["AzureOpenAI:DeploymentName"] ?? "";
string openAIEmbeddingDeployment = config["AzureOpenAI:EmbeddingDeploymentName"] ?? "";

var credential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new AzureDeveloperCliCredential());
var searchClient = new SearchClient(new Uri(searchEndpoint), indexName, credential);

AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(openAIEndpoint), credential);
var embeddingClient = openAIClient.GetEmbeddingClient(openAIEmbeddingDeployment);

var vectorizedResult = await embeddingClient.GenerateEmbeddingAsync("quintessential lodging near running trails, eateries, retail");

//// Single search vector
//await SearchExamples.SearchSingleVector(searchClient, vectorizedResult);

//// Single search vector with Filter
//await SearchExamples.SearchSingleVectorWithFilter(searchClient, vectorizedResult);

//// Single search vector with geo filter
//await SearchExamples.SingleSearchWithGeoFilter(searchClient, vectorizedResult);

//// Hybrid search with vector and text query
//await SearchExamples.SearchHybridVectorAndText(searchClient, vectorizedResult);

// Hybrid search with vector and semantic query
await SearchExamples.SearchHybridVectoryAndSemantic(searchClient, vectorizedResult);
