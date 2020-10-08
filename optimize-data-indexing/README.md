---
page_type: sample
languages:
  - csharp
name: Optimize indexing with the push API
description: In Azure Cognitive Search, you can push data from any data source to an index. In this C# sample, learn how to index more efficiently.
products:
  - azure
  - azure-cognitive-search
urlFragment: optimize-data-indexing
---

# Optimize indexing with the push API

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

Azure Cognitive Search supports [two basic approaches](https://docs.microsoft.com/azure/search/search-what-is-data-import) for importing data into a search index: *pushing* your data into the index programmatically, or pointing an [Azure Cognitive Search indexer](https://docs.microsoft.com/azure/search/search-indexer-overview) at a supported data source to *pull* in the data.

As data volumes grow or processing needs change, you might find that simple or default indexing strategies are no longer practical. This sample demonstrates how to efficiently index data using the push model by batching requests and leveraging an exponential backoff retry strategy.

This .NET Core console app builds off of the code used in the [Quickstart](https://docs.microsoft.com/azure/search/search-get-started-dotnet) and uses the [Azure Cognitive Search .NET SDK](https://docs.microsoft.com/dotnet/api/?term=microsoft.azure.search) to create an index and efficiently load it with documents using the push model.

The app shows how to:

- Test different batch sizes to understand the most efficient batch size for indexing your data
- Efficiently index data by:
  - [Pushing](https://docs.microsoft.com/azure/search/search-what-is-data-import#pushing-data-to-an-index) data to the index in batches
  - Sending multiple batches concurrently
  - Implementing an [exponential backoff retry strategy](https://docs.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/implement-retries-exponential-backoff)

The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

> [!NOTE]
> Network transfer speeds can be a limiting factor when indexing data. You might get a better sense of indexing efficiency if your Visual Studio client is on an [Azure virtual machine](https://azure.microsoft.com/services/virtual-machines/) in the cloud in the same location as Azure Cognitive Search. The [Data Science VM](https://azure.microsoft.com/services/virtual-machines/data-science-virtual-machines/) is a good choice because Visual Studio is preinstalled.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

This sample is available in two versions.

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries

+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Run the sample

1. Open the OptimizeDataIndexing.sln project in Visual Studio.
1. Update appsetting.json to use your search service name and admin api-key. The admin key is necessary for creating objects and loading data.
1. Press F5 to build and run the project.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).

The documentation provides additional guidance on [indexing large data sets](https://docs.microsoft.com/azure/search/search-howto-large-index).
