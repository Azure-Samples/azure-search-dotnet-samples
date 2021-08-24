---
page_type: sample
languages:
  - csharp
name: Update (Rebuild) an Azure Cognitive Search index schema using Back up and restore strategy using blob storage.
description: "This application is written to update (rebuild) the schema of an Index as In all conditions we can not update the schema, it has to be rebuilt again to update in certain conditions. For more information - refer to https://docs.microsoft.com/en-us/azure/search/search-howto-reindex, It uses below strategy, first we need to attach the new(updated) schema of the index to this solution in NewSchema.schema file. First this application backs up a old (existing) index schema and its documents to Azure storage account, and then it deletes the old (exisitng) index, then this application uses attached NewSchema (if available in solution else uses the existing index schema from blob storage as a failover case, not to imapct the client for longer time) to recreate the index with updated schema. Depending on your needs, you can use all or part of this application to backup your index files. 
For example, you may use the Basic or Free pricing tier to develop your index, and then want to move it to the Standard or higher tier for production use."
products:
  - azure
  - azure-cognitive-search
  - azure-storage
urlFragment: azure-search-index-schema-update
---

# Update (Rebuild scenario) an Azure Cognitive Search index schema using Back up and restore strategy using blob storage

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This application rebuilds the index with the updated schema provided in NewSchema.schema, and in the process, It creates JSON files and uploads them(the index schema and documents) to blob storage. This tool is useful when you want to update your index schema and it requires to be rebuilt (as per the documentation https://docs.microsoft.com/en-us/azure/search/search-howto-reindex).

## IMPORTANT - PLEASE READ
Search indexes are different from other datastores because they are constantly ranking and scoring results and data may shift. If you page through search results or even use continuation tokens as this tool does, it is possible to miss some data during data extraction.

As an example, assume that you are searching for documents and a document with ID 101 is part of page 5 of the search results. Then, as you are extracting data from page to page, and move from page 4 to page 5, it is possible that now ID 101 is actually part of page 4. This means that when you look at page 5, it is no longer there and you have missed that document.

For this reason, this tool compares the number of index documents in the original index and the updated index. If the numbers don't match, the updated index may be missing data. Although this safeguard does not provide a perfect solution, it does help you help prevent you from missing data.

Also, as an extra precaution, it is best if there are no changes being made to the search index when you use run this tool.

**If your index has more than 100,000 documents**, this sample, as written, will not work. This is because the REST API $skip feature, that is used for paging, has a 100K document limit. However, you can work around this limitation by adding code to iterate over, and filter on, a facet with less that 100K documents per facet value.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)
- [Azure storage account](https://azure.microsoft.com/en-in/services/storage/blobs/)

## This solution is leveraged from the exisitng sample: index-backup-restore (which uses local file storage) and this solution uses azure blob storage.

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

This sample is available in below version:

+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Run the sample

[!NOTE] In this application there is a file "NewSchema.schema" which is the updated schema. Please update it with your latest schema which is the end result.

1. Open the AzureSearchSchemaUpdate.sln project in Visual Studio.

1. By default, this application will back up the exisitng schema and documents, then it will delete the exisitng Index and then use the NewSchem.schema to create the index. 

1. Open the appsettings.json and replace the placeholder strings with all applicable values:

    - The search service name (SearchServiceName) and key (AdminKey) and the name of the index (IndexName) that you want to update the schema.
    - The connection string of stoage account (StorageConnectionString) and container name (BackupContainerName) where the schema and documents are backed-up/restored/copied from existing index.

1. Compile and Run the project.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).
