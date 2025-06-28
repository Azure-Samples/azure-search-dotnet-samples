# C# samples for Azure AI Search fundamentals

This repository contains C# code samples used in Azure AI Search "Day One" quickstarts and tutorials. Unless noted otherwise, all samples run on the shared (free) pricing tier of an [Azure AI Search service](https://learn.microsoft.com/azure/search/search-create-service-portal).

## In this repository

| Sample | Description |
|--------|-------------|
| create-mvc-app | This ASP.NET Core MVC sample demonstrates server-side search behaviors, such as filters and sorting. |
| quickstart | Learn the fundamental tasks of working with a search index: create, load, and query for full-text search scenarios. This quickstart is a console application. The index is modeled on a subset of the Hotels dataset, widely used in Azure AI Search samples, but reduced to just four hotels for readability and comprehension. |
| quickstart-semantic-search | Adds semantic search to the previous quickstart. |
| quickstart-agentic-retrieval | Sets up a knowledge agent in Azure AI Search to integrate LLM reasoning into query planning. We recommend the Basic tier or higher for this quickstart. |
| tutorial-ai-enrichment | This console app creates an AI enrichment pipeline consisting of an index, indexer, data source, and skillset. The skillset calls Azure AI Services image analysis and OCR, and natural language processing, extract information and structure from heterogeneous blob content, making it searchable in Azure AI Search. |

## More resources

+ See [Vector samples in Azure AI Search](https://github.com/Azure/azure-search-vector-samples/tree/main/demo-dotnet) for code samples that call the Azure SDK for .NET.

+ See [.NET samples in Azure AI Search](https://learn.microsoft.com/azure/search/samples-dotnet) for a comprehensive list of all Azure AI Search code samples that run on .NET.

+ See [Azure AI Search documentation](https://learn.microsoft.com/azure/search) for product documentation.
