---
page_type: sample
languages:
  - csharp
name: Indexing multiple data sources in Azure Search
description: |
  C# console application demonstrating indexing from multiple Azure data sources, including Cosmos DB and Blob storage.
products:
  - azure
  - azure-search
  - azure-cosmos-db
  - azure-storage
urlFragment: multiple-data-sources
---

# Index multiple data sources using Azure Search indexers

Demonstrates Azure Search indexing from Azure Cosmos DB and Azure Blob storage, populating an index by combining data from different data sources.

This .NET Core console application is featured in [C# Tutorial: Combine data from multiple data sources in one Azure Search index](https://docs.microsoft.com/azure/search/tutorial-multiple-data-sources). When you run the program, a console window emits output messages for each step. This sample runs on an Azure Search service, importing content from Azure Cosmos DB and Azure Blob storage, using services and connection information that you provide.

## Contents

| File/folder | Description |
|-------------|-------------|
| `AzureSearchMultipleDataSources.sln`       | .NET Core console solution file |
| `src`       | Source files |
| `.gitignore` | Define what to ignore at commit time. |
| `CONTRIBUTING.md` | Guidelines for contributing to the sample. |
| `README.md` | This README file. |
| `LICENSE`   | The license for the sample. |

## Prerequisites

- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)
- [Azure Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)
- [Azure Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/create-cosmosdb-resources-portal)
- [Azure Storage](https://docs.microsoft.com/azure/storage/common/storage-quickstart-create-account)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

### Running multiple-data-sources

1. In the Azure portal, create an Azure Cosmos DB database named "hotel-rooms-db" and a new collection in it called "hotels". 
1. In the Cosmos Data Explorer, select the hotels collection, click Upload, and then select the file src/cosmosdb/HotelsDataSubset_CosmosDB.json. This contains data for 7 hotels, but no rooms data.
1. In your Azure Storage account, create a new blob storage container named hotel-rooms. 
1. Select this container, click Upload, and then upload all of the JSON files in the src/blobs folder, ranging from Rooms1.json through Rooms15.json. These files contain room details for each of the 7 hotels.
1. Open the sample solution in Visual Studio 2019.
1. Edit the file appsettings.json and fill in the appropriate account names, keys, and connection strings.
1. Build and run the app. 

After a successful run, you should see a new index names hotel-rooms-sample in your Azure Search Service, containing the combined hotel and room data for all 7 hotels.
