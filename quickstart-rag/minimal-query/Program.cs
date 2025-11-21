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

// Prompt template for grounding the LLM response in search results
string GROUNDED_PROMPT = @"You are a friendly assistant that recommends hotels based on activities and amenities.
    Answer the query using only the sources provided below in a friendly and concise bulleted manner
    Answer ONLY with the facts listed in the list of sources below.
    If there isn't enough information below, say you don't know.
    Do not generate answers that don't use the sources below.
    Query: {0}
    Sources: {1}";

// The user's query
string query = "Can you recommend a few hotels with complimentary breakfast?";

// Configure search options: top 5 results, select relevant fields
var options = new SearchOptions { Size = 5 };
options.Select.Add("Description");
options.Select.Add("HotelName");
options.Select.Add("Tags");

// Execute the search
var searchResults = await searchClient.SearchAsync<SearchDocument>(query, options);
var sources = new List<string>();

await foreach (var result in searchResults.Value.GetResultsAsync())
{
    var doc = result.Document;
    // Format each result as: HotelName:Description:Tags
    sources.Add($"{doc["HotelName"]}:{doc["Description"]}:{doc["Tags"]}");
}
string sourcesFormatted = string.Join("\n", sources);

// Format the prompt with the query and sources
string formattedPrompt = string.Format(GROUNDED_PROMPT, query, sourcesFormatted);

// Create a chat client for the specified deployment/model
ChatClient chatClient = openAIClient.GetChatClient(azureDeploymentModel);

// Send the prompt to the LLM and stream the response
var chatUpdates = chatClient.CompleteChatStreamingAsync(
    [ new UserChatMessage(formattedPrompt) ]
);

// Print the streaming response to the console
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
