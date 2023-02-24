---
page_type: sample
languages:
  - csharp
name: Compute aggregations over a search index
description: "Compute aggregations such as mean over a search index using a query."
products:
  - azure
  - azure-cognitive-search
---

# Compute aggregations over a search index

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

In this sample, create a simple search index and upload random data to it. This sample illustrates how aggregations can be computed from the random data, and how the data can be filtered using a query.

## Prerequisites

+ [Azure Cognitive Search](search-create-app-portal.md)
+ [Visual Studio](https://visualstudio.microsoft.com/downloads/)
+ [Azure.Search.Documents NuGet package](https://www.nuget.org/packages/Azure.Search.Documents/)

In contrast with other tutorials, this one uses an index with randomly generated data. No preliminary service or index setup is required.

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

## Run the sample

1. Open a solution in Visual Studio.

1. Modify **appsettings.json** to use your search service URI and admin API key. The URI is a full URL in the format of `https://<service-name>.search.windows.net`. The admin API key is an alphanumeric string that you can obtain from the portal, PowerShell, or CLI.

   ```json
   {
     "searchServiceUrl": "<YOUR-SEARCH-SERVICE-URI>",
     "adminKey": "<YOUR-SEARCH-SERVICE-API-KEY>"
   }
   ```

1. Press **F5** to compile and run the project.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).