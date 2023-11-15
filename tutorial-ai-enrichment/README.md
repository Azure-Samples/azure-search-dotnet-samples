---
page_type: sample
languages:
  - csharp
name: AI enrichment tutorial in C#
description: "Create an AI enrichment pipeline in Azure AI Search to extract text, structure, and information from raw content, including images and unstructured text."
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-enrichment-tutorial
---

# AI enrichment tutorial in C#

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This Azure AI Search sample demonstrates AI enrichment by building an indexing pipeline that detects and extracts text and text representations of images and scanned documents stored as blobs in Azure Blob storage. This sample leverages cognitive skills from the Azure AI Services APIs, such as entity recognition and language detection. It uses the REST APIs to make calls to Azure AI Search, including index definition, data ingestion and AI enrichment, and query execution.

This sample is a C# console application that uses .NET Core. The code is described in [C# Tutorial: AI-generated searchable content from Azure blobs using the .NET SDK](https://docs.microsoft.com/azure/search/cognitive-search-tutorial-blob-dotnet). 

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Sample data](https://github.com/Azure-Samples/azure-search-sample-data/tree/master/ai-enrichment-mixed-media)
- [Azure Storage](https://docs.microsoft.com/azure/storage/common/storage-quickstart-create-account) 
- [Azure AI Search](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Set up the sample

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.
1. Create a Blob container named `cog-search-demo` and upload the sample files of mixed content type.
1. For the Blob container, get the connection string.
1. For Azure AI Search, get the service name, admin API key, and a query API key.

This sample is available in two versions. **V10** uses the deprecated [Microsoft.Azure.Search](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.search) client libraries. We recommend **v11** and the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library for all new projects.

## Run the sample

1. In applicationsettings.json, enter the search service name, keys, and storage account connection string.
1. Press F5 to run the program.

## Next steps

You can learn more about Azure AI Search on the [official documentation site](https://docs.microsoft.com/azure/search).