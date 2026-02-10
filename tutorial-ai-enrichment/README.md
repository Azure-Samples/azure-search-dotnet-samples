---
page_type: sample
languages:
  - csharp
name: "Tutorial: AI enrichment in Azure AI Search using C#"
description: Learn how to create an AI enrichment pipeline in Azure AI Search to extract text, structure, and information from raw content, including images and unstructured text.
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-enrichment-tutorial
---

# Tutorial: AI enrichment in Azure AI Search using C#

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This sample demonstrates AI enrichment by building an indexing pipeline that detects and extracts text from images and scanned documents stored in Azure Blob Storage. You use built-in Azure AI Search skills to extract text and structure from the documents, and then you create a search index to enable searching over the enriched content.

## What's in this sample

| File | Description |
|------|-------------|
| `Program.cs` | Main program that creates the enrichment pipeline |
| `DemoIndex.cs` | Index definition with enriched fields |
| `appsettings.json` | Configuration file for service endpoints and keys |

## Documentation

This sample accompanies [Tutorial: Skillsets in Azure AI Search using C#](https://learn.microsoft.com/azure/search/cognitive-search-tutorial-blob-dotnet). Follow the documentation for prerequisites, setup instructions, and detailed explanations.

## Next step

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).