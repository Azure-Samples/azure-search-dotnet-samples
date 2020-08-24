---
page_type: sample
languages:
  - csharp
name: Quickstart in C#
description: "Run version 11 (Azure.Search.Documents) or version 10 (Microsoft.Azure.Search) of the client library to create a .NET Core console app that creates, loads, and queries a search index."
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart
---

# Quickstart sample for Cognitive Search in .NET

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

New and legacy versions are provided so that you can learn fundamental operations with the client libraries used in your solution. Both versions create a search index that is modeled on a subset of the built-in Hotels dataset, reduced in this quickstart for readability and comprehension. Index definition and documents are included in the code. When you run the program, a console window emits output messages for each step: deleting and then re-creating a hotels-quickstart index, loading documents, running queries. 

Version 10 uses the [Microsoft.Azure.Search](https://docs.microsoft.com/dotnet/api/?term=microsoft.azure.search) libraries. Version 10 is now considered a legacy API, replaced by version 11. If you are new to Azure Cognitive Search, please use Azure.Search.Documents (version 11) instead.

Version 11 uses the [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme?view=azure-dotnet) library. Version 11 is a fully redesigned library that is more consistent with other client libraries in the Azure SDK. Moving forward, all 

Run either version against an Azure Cognitive Search service using connection information that you provide.

## Contents

| File/folder | Description |
|-------------|-------------|
| `v<number>\AzureSearchQuickstart.sln`       | .NET Core console solution file |
| `v<number>\AzureSearchQuickstart`       | Source files |
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

## Run version 11

1. Open the v11\AzureSearchQuickstart-v11.sln project in Visual Studio
1. Open **Program.cs**.
1. Replace the placeholder values for service name and admin API key with valid values for your search service.
1. Compile and run the project.

## Run version 10

1. Open the v10\AzureSearchQuickstart.sln project in Visual Studio
1. Use **Tools > NuGet Package Manager** to check for updated packages.
1. Update the appsettings.json with the service name and admin API key of your search service
1. Compile and run the project

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).