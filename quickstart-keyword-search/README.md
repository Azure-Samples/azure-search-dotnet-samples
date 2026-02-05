---
page_type: sample
languages:
  - csharp
name: "Quickstart: Keyword search in Azure AI Search using C#"
description: |
  Learn how to create, load, and query an Azure AI Search index using the Azure SDK for .NET.
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart-keyword
---

# Quickstart: Keyword search in Azure AI Search using C#

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This sample demonstrates the fundamentals of creating, loading, and querying a search index for full-text search, also known as keyword search. The index is modeled on a subset of the hotels dataset, which has been reduced for readability and comprehension.

## What's in this sample

| File | Description |
|------|-------------|
| `AzureSearchQuickstart.csproj` | Project file that defines dependencies and build settings |
| `Program.cs` | Creates an index, loads documents, and runs queries |
| `Hotel.cs`, `Address.cs` | Model classes defining the index schema |
| `Hotel.Methods.cs`, `Address.Methods.cs` | ToString() overrides for console output |

## Documentation

This sample accompanies [Quickstart: Full-text search using C#](https://learn.microsoft.com/azure/search/search-get-started-text). Follow the documentation for prerequisites, setup instructions, and detailed explanations.

## Next step

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).
