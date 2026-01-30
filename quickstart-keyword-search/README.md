---
page_type: sample
languages:
  - csharp
name: "Quickstart: Keyword search in Azure AI Search using C#"
description: |
  Learn how to create, load, and query an Azure AI Search index using the Azure SDK for .NET
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart-keyword
---

# Quickstart: Keyword search in Azure AI Search using C#

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This quickstart is focused on the fundamentals: creating, loading, and querying a search index. It creates a search index that is modeled on a subset of the built-in Hotels dataset, reduced in this quickstart for readability and comprehension. Index definition and documents are included in the code. When you run the program, a console window emits output messages for each step: deleting and then re-creating a hotels-quickstart-csharp index, loading documents, running queries.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure AI Search service](https://learn.microsoft.com/azure/search/search-create-service-portal)
- [Azure.Search.Documents](https://learn.microsoft.com/dotnet/api/overview/azure/search.documents-readme)

## Set up the sample

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.
1. Get the name of your search service. You can find the URL on the search service **Overview** page in the Azure portal.
1. Make sure you have permissions to create, load, and query an index: **Search Service Contributor**, **Search Index Data Contributor**, and **Search Index Data Reader**.

## Run the sample

1. Open the **AzureSearchQuickstart.sln** project in Visual Studio.
1. Open **Program.cs**.
1. Replace the placeholder value for the service name with a valid value for your search service.
1. Compile and run the project.

## Next steps

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).
