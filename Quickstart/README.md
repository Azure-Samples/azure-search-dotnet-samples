---
page_type: sample
languages:
  - csharp
name: Quickstart in C#
description: "Learn basic steps in C# for creating, loading, and querying an Azure Search index in a .NET Core console application."
products:
  - azure
  - azure-search
urlFragment: csharp-quickstart
---

# Quickstart sample for Azure Search in .NET

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

Demonstrates using the Azure Search .NET SDK to create an index, load it with documents, and execute a few queries. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

This .NET Core console application is featured in [Quickstart: Create your first app - Azure Search](https://docs.microsoft.com/azure/search/tutorial-csharp-create-first-app). When you run the program, a console window emits output messages for each step: deleting and then re-creating a hotels-quickstart index, loading documents, running queries. This sample uses the [.NET SDK](https://docs.microsoft.com/dotnet/api/?term=microsoft.azure.search) and runs on an Azure Search service using connection information that you provide.

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
- [Azure Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

### Running quickstart
1. Open the AzureSearchQuickstart.sln project in Visual Studio
1. Update the appsettings.json with the service and api details of your Azure Search service
1. Compile and Run the project

## Next steps

You can learn more about Azure Search on the [official documentation site](https://docs.microsoft.com/azure/search).