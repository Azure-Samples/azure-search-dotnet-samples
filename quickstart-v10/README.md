---
page_type: sample
languages:
  - csharp
name: Quickstart in C# (old)
description: "Uses the legacy .NET SDK client library, Microsoft.Azure.Search, to create a .NET Core console application."
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart
---

# Quickstart sample for Microsoft.Azure.Search (version 10) in .NET

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

Demonstrates version 10 of the Azure Cognitive Search .NET SDK to create, load, and query an index. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

Version 10 is now considered a legacy API, replaced by version 11 and the Azure.Search.Documents library. If you are new to Azure Cognitive Search, please use Azure.Search.Documents (version 11) instead.

When you run the program, a console window emits output messages for each step: deleting and then re-creating a hotels-quickstart index, loading documents, running queries. This sample uses the [Microsoft.Azure.Search libraries](https://docs.microsoft.com/dotnet/api/?term=microsoft.azure.search) and runs on an Azure Cognitive Search service using connection information that you provide.

## Contents

| File/folder | Description |
|-------------|-------------|
| `AzureSearchQuickstart.sln`       | .NET Core console solution file |
| `AzureSearchQuickstart`       | Source files |
| `.gitignore` | Define what to ignore at commit time. |
| `CONTRIBUTING.md` | Guidelines for contributing to the sample. |
| `README.md` | This README file. |
| `LICENSE`   | The license for the sample. |

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

### Running quickstart
1. Open the AzureSearchQuickstart.sln project in Visual Studio
1. Update the appsettings.json with the service and api details of your search service
1. Compile and Run the project

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).