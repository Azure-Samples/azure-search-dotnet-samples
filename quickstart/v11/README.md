---
page_type: sample
languages:
  - csharp
name: Quickstart in C#
description: |
  Learn how to create, load, and query an Azure Cognitive Search index using the Azure SDK for .NET
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart
---

# C# quickstart for Azure Cognitive Search

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This quickstart is focused on the fundamentals: creating, loading, and querying a search index. 

New and legacy versions are provided so that you can learn these operations with the client libraries used in your solution. Both versions create a search index that is modeled on a subset of the built-in Hotels dataset, reduced in this quickstart for readability and comprehension. Index definition and documents are included in the code. When you run the program, a console window emits output messages for each step: deleting and then re-creating a hotels-quickstart index, loading documents, running queries.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)
- [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) and the Azure SDK for .NEt

## Set up the sample

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.
1. Get the service name and admin API key of your service. You can find this information in the Azure portal.

## Run the sample

1. Open the AzureSearchQuickstart-v11.sln project in Visual Studio
1. Open **Program.cs**.
1. Replace the placeholder values for service name and admin API key with valid values for your search service.
1. Compile and run the project.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).
