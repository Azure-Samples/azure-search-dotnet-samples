# C# samples for Azure AI Search fundamentals

This repository contains C# code samples used in Azure AI Search "Day One" quickstarts and tutorials. Unless noted otherwise, all samples run on the shared (free) pricing tier of an [Azure AI Search service](https://learn.microsoft.com/azure/search/search-create-service-portal).

## In this repository

| Sample | Quickstart or tutorial | Description |
|--------|------------------------|-------------|
| quickstart-agentic-retrieval | [Quickstart: Agentic retrieval](https://learn.microsoft.com/azure/search/search-get-started-agentic-retrieval?pivots=programming-language-rest) | Sets up a knowledge base in Azure AI Search to integrate LLM reasoning into query planning. We recommend the Basic tier or higher for this quickstart. |
| quickstart-keyword-search | [Quickstart: Full-text search](https://learn.microsoft.com/azure/search/search-get-started-text?tabs=keyless%2Cwindows&pivots=csharp) | Learn the fundamental tasks of working with a search index: create, load, and query for full-text search scenarios. This quickstart is a console application. The index is modeled on a subset of the Hotels dataset, widely used in Azure AI Search samples, but reduced to just four hotels for readability and comprehension. |
| quickstart-semantic-ranking | [Quickstart: Semantic ranking](https://learn.microsoft.com/azure/search/search-get-started-semantic?pivots=csharp) | Adds semantic ranking to an existing hotels-sample-index and formulates semantic queries. |
| quickstart-vector-search | [Quickstart: Vector search](https://learn.microsoft.com/azure/search/search-get-started-vector?tabs=keyless&pivots=javascript) | Creates a small hotels index that includes vectorized descriptions, and formulates vector queries. |
| tutorial-ai-enrichment | [C# Tutorial: Use skillsets to generate searchable content](https://learn.microsoft.com/azure/search/cognitive-search-tutorial-blob-dotnet) | Creates an AI enrichment pipeline consisting of an index, indexer, data source, and skillset. The skillset calls Azure AI Services image analysis and OCR, and natural language processing, extract information and structure from heterogeneous blob content, making it searchable in Azure AI Search. |

## More resources

+ See [Vector samples in Azure AI Search](https://github.com/Azure/azure-search-vector-samples/tree/main/demo-dotnet) for code samples that call the Azure SDK for .NET.

+ See [.NET samples in Azure AI Search](https://learn.microsoft.com/azure/search/samples-dotnet) for a comprehensive list of all Azure AI Search code samples that run on .NET.

+ See [Azure AI Search documentation](https://learn.microsoft.com/azure/search) for product documentation.
