---
page_type: sample
languages:
  - csharp
name: Quickstart in C# (new)
description: "Use the Azure.Search.Documents client library to create, load, and query an Azure Cognitive Search index in a .NET Core console application."
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart
---

# Quickstart sample for Azure.Search.Documents v11

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

Uses the new Azure.Search.Documents version 11 library to create an index, load it with documents, and execute a few queries. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

This .NET Core console application is featured in [Quickstart: Create your first app - Azure Cognitive Search](https://docs.microsoft.com/azure/search/tutorial-csharp-create-first-app), now updated to use version 11 of the .NET SDK for Azure Cognitive Search. When you run the program, a console window emits output messages for each step: deleting and then re-creating a hotels-quickstart-v11 index, loading documents, running queries. This sample uses the [.NET SDK v11](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme?view=azure-dotnet) and runs on an Azure Cognitive Search service using connection information that you provide.

## Contents

| File/folder | Description |
|-------------|-------------|
| `AzureSearchQuickstart-v11.sln`       | .NET Core console solution file |
| `AzureSearchQuickstart-v11`       | Source files |
| `.gitignore` | Define what to ignore at commit time. |
| `CONTRIBUTING.md` | Guidelines for contributing to the sample. |
| `README.md` | This README file. |
| `LICENSE`   | The license for the sample. |

## Prerequisites

- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.
1. Get the service name and admin API key of your service. You can find this information in the Azure portal.

### Run the program

1. Open the AzureSearchQuickstart-v11.sln project in Visual Studio
1. Open **Program.cs**.
1. Replace the placeholder values for service name and admin API key with valid values for your search service.
1. Compile and run the project.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).