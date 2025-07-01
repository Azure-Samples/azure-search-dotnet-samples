---
page_type: sample
languages:
  - csharp
name: C# semantic ranking with Azure AI Search
description: |
  This code sample provides the syntax for semantic ranking. It demonstrates an approach for adding a semantic configuration to a search index and query parameters to a query.
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart-semantic
---
# Semantic ranking quickstart in C# for Azure AI Search

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This code sample provides the syntax for setting up semantic ranking. It adds a semantic configuration to a search index and semantic parameters to a query.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure AI Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)
- [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) and the Azure SDK for .NET

## Set up the sample

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.
1. Get the service name of your service. You can find the URL in search service Overview page in the Azure portal.
1. Make sure you have permission to update and query the search index. You should **Search Service Contributor** and **Search Index Data Reader** permissions.

## Run the sample

1. Open the SemeanticSearchQuicksart.sln project in Visual Studio
1. Open **Program.cs**.
1. Replace the placeholder value for the service endpoint.
1. Compile and run the project.

## Next steps

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).
