---
page_type: sample
languages:
  - csharp
name: Index multiple data sources in Azure Cognitive Search
description: "Demonstrates indexing from multiple Azure data sources, including Cosmos DB and Blob storage. This example builds a C# console application using the Azure Cognitive Search .NET SDK."
products:
  - azure
  - azure-cognitive-search
  - azure-cosmos-db
  - azure-storage
urlFragment: multiple-data-sources
---

# Index multiple data sources using Azure Cognitive Search indexers

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

Demonstrates Azure Cognitive Search indexing from Azure Cosmos DB and Azure Blob storage, populating an index by combining data from different data sources.

This .NET Core console application is featured in [C# Tutorial: Combine data from multiple data sources in one search index](https://docs.microsoft.com/azure/search/tutorial-multiple-data-sources). When you run the program, a console window emits output messages for each step. This sample runs on an Azure Cognitive Search service, importing content from Azure Cosmos DB and Azure Blob storage, using services and connection information that you provide.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)
- [Azure Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/create-cosmosdb-resources-portal)
- [Azure Storage](https://docs.microsoft.com/azure/storage/common/storage-quickstart-create-account)

## Setup

1. Clone or download this sample repository.

1. Extract contents if the download is a zip file. Make sure the files are read-write.

1. Create and populate a Cosmos DB data source with hotels information:

   + In the [Azure portal](https://portal.azure.com), create an Azure Cosmos DB account for the **Core (SQL)** API. 
   + Create a new database named "hotel-rooms-db".
   + In Data Explorer, open the "hotel-rooms-db", create a new container named "hotels".
   + Open "hotels", select **Items**, select **Upload Item**, and then select the *src/cosmosdb/HotelsDataSubset_CosmosDB.json* file. It contains data for seven hotels, but no rooms data.
   + In the left pane, go to **Settings > Keys** and get the primary connection string. You will need this value for the *appsettings.json* file in the project.

1. Create and populate a Blob container with rooms information:

   + In the [Azure portal](https://portal.azure.com), create an Azure Storage account for blob content. 
   + Create a new blob storage container named "hotel-rooms".
   + Select this container, click **Upload**, and then upload all of the JSON files in the *src/blobs* folder, ranging from *Rooms1.json* through *Rooms15.json*. These files contain room details for each of the seven hotels.
   + In the left pane, go to **Settings > Access Keys** and get the connection string for key1. It also goes into the project's *appsettings.json* file.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries

+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Run the sample

1. Open the sample solution in Visual Studio.

1. Edit the  *appsettings.json* and fill in the appropriate account names, keys, and connection strings:

   + SearchServiceName and SearchServiceAdminKey can be found in Overview and Keys portal pages of your Azure Cognitive Search service.
   + Blob storage and Cosmos DB connection information can be found in the key pages.
   + For Blob storage, you also need the name of the storage account.

1. Press F5 to build and run the app. Status messages appear in the console window.

## Verify results

After a successful run, you should see a new index named "hotel-rooms-sample" in your search service, containing the combined hotel and room data for all seven hotels. 

   + In the [Azure portal](https://portal.azure.com), open the search service Overview page.
   + In Indexes, select the new "hotel-rooms-sample" index containing seven documents.
   + By default, the index opens in the **Search explorer** tab. Click **Search** to execute an empty search, returning all documents. Scroll down or use CTRL-F to verify that each hotel now has a "Rooms" collection with descriptions, rates, and other room-specific information.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).
