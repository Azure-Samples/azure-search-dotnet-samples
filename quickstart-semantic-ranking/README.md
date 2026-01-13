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
# Quickstart: Semantic ranking in Azure AI Search

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This code sample provides the syntax for setting up semantic ranking. It adds a semantic configuration to a search index and semantic parameters to a query.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure AI Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)
- [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) and the Azure SDK for .NET

## Set up the sample

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.
1. Get the service name of your service. You can find the URL in search service **Overview** page in the Azure portal.
1. Make sure you have permission to update and query the search index. You should have **Search Service Contributor** permissions to update the index, and **Search Index Data Contributor** or **Search Index Data Reader** permissions to query the index.

## Run the sample

The solution is organized into two projects. The first project updates an existing instance of the hotels-sample-index. The second project runs a series of queries.

1. Open the **quickstart-semantic-ranking.sln** solution in Visual Studio
1. Open **BuildIndex**.
1. In **Program.cs**, replace the placeholder value for the service endpoint with the name of your search service.
1. Repeat this step for **QueryIndex**.
1. Compile and run each project.

## Next steps

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).
