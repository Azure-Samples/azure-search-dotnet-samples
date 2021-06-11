---
page_type: sample
languages:
  - csharp
name: Quickstart in C#
description: "Learn the basic workflow in this .NET Core console app: create, load, and query a search index."
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart
---

# Quickstart sample for Cognitive Search in .NET

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This quickstart is focused on the fundamentals: creating, loading, and querying a search index. 

New and legacy versions are provided so that you can learn these operations with the client libraries used in your solution. Both versions create a search index that is modeled on a subset of the built-in Hotels dataset, reduced in this quickstart for readability and comprehension. Index definition and documents are included in the code. When you run the program, a console window emits output messages for each step: deleting and then re-creating a hotels-quickstart index, loading documents, running queries.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.
1. Get the service name and admin API key of your service. You can find this information in the Azure portal.

This sample is available in two versions.

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries

+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Run the v11 sample

1. Open the AzureSearchQuickstart-v11.sln project in Visual Studio
1. Open **Program.cs**.
1. Replace the placeholder values for service name and admin API key with valid values for your search service.
1. Compile and run the project.

## Run v10 sample

1. Open the v10\AzureSearchQuickstart.sln project in Visual Studio
1. Use **Tools > NuGet Package Manager** to check for updated packages.
1. Update the appsettings.json with the service name and admin API key of your search service
1. Compile and run the project

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).
