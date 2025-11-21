using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using Microsoft.Extensions.Logging;

// Azure resource endpoints and deployment info
string azureSearchServiceEndpoint = "PUT-YOUR-SEARCH-SERVICE-ENDPOINT-HERE";
string azureOpenAIEndpoint = "PUT-YOUR-AZURE-OPENAI-ENDPOINT-HERE";
string azureDeploymentModel = "PUT-YOUR-CHAT-MODEL-DEPLOYMENT-NAME-HERE";
string indexName = "hotels-sample-index-dotnet";

// Set up Azure credentials and clients
var credential = new DefaultAzureCredential();
var searchClient = new SearchClient(new Uri(azureSearchServiceEndpoint), indexName, credential);
var openAIClient = new AzureOpenAIClient(new Uri(azureOpenAIEndpoint), credential);

// Prompt template for the OpenAI model
string groundedPrompt =
    @"You are a friendly assistant that recommends hotels based on activities and amenities.
    Answer the query using only the sources provided below in a friendly and concise bulleted manner.
    Answer ONLY with the facts listed in the list of sources below.
    If there isn't enough information below, say you don't know.
    Do not generate answers that don't use the sources below.
    Query: {0}
    Sources: {1}";

// The user query and fields to select from search
var query = "Can you recommend a few hotels that offer complimentary breakfast? Tell me their description, address, tags, and the rate for one room that sleeps 4 people.";
var selectedFields = new[] { "HotelName", "Description", "Address", "Rooms", "Tags" };

// Configure search options
var options = new SearchOptions { Size = 5 };
foreach (var field in selectedFields)
{
    options.Select.Add(field);
}

// Run Azure Cognitive Search
var searchResults = await searchClient.SearchAsync<SearchDocument>(query, options);

// Filter and format search results
var sourcesFiltered = new List<Dictionary<string, object>>();
await foreach (var result in searchResults.Value.GetResultsAsync())
{
    sourcesFiltered.Add(
        selectedFields
            .Where(f => result.Document.TryGetValue(f, out _))
            .ToDictionary(f => f, f => result.Document[f])
    );
}
var sourcesFormatted = string.Join("\n", sourcesFiltered.ConvertAll(source => JsonSerializer.Serialize(source)));

// Format the prompt for OpenAI
string formattedPrompt = string.Format(groundedPrompt, query, sourcesFormatted);

// Get a chat client for the OpenAI deployment
ChatClient chatClient = openAIClient.GetChatClient(azureDeploymentModel);

// Send the prompt to Azure OpenAI and stream the response
var chatUpdates = chatClient.CompleteChatStreamingAsync(
    new[] { new UserChatMessage(formattedPrompt) }
);

// Output the streamed chat response
await foreach (var chatUpdate in chatUpdates)
{
    if (chatUpdate.Role.HasValue)
    {
        Console.Write($"{chatUpdate.Role} : ");
    }
    foreach (var contentPart in chatUpdate.ContentUpdate)
    {
        Console.Write(contentPart.Text);
    }
}
