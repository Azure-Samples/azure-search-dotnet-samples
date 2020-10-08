# Azure Cognitive Search .NET Samples

This repository contains C# sample code used in Azure Cognitive Search quickstarts, tutorials, and examples in product documentation. Unless otherwise noted, all samples run on the shared (free) pricing tier of an Azure Cognitive Search service.  

## Quickstart

"Day One" introduction to the fundamental tasks of working with a search index: create, load, and query. This quickstart is a .NET Core console application that outputs the status of each operation, concluding with a series of sample queries as a validation step that the index exists and contains content. The index is modeled on a subset of the Hotels dataset, widely used in Cognitive Search samples, but reduced here for readability and comprehension. 

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries
+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Create your first app

This MVC sample is a collection of projects that demonstrate a user experience using fictitious hotels data. The first project creates a basic search page. Additional projects build on the first, adding pagination, autocomplete and suggested queries, and ordered results. The index is pre-built and hosted so that you can focus on the application itself.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries
+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Multiple data sources

This .NET Core console app uses Azure Cognitive Search indexers and the .NET SDK to import data from Azure Cosmos DB and Azure Blob storage, combing data from two sources into one search index.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries
+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Backup and restore an index

This .NET Core console app backs up an index (schema and documents) to your local computer and then uses the stored backup to recreate the index in a target search service that you specify. This sample can be helpful if you want to move an index into a different pricing tier. For example, you may use the Basic or Free pricing tier to develop your index, and then move it to the Standard or higher tier for production use.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries
+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Optimize data indexing

This .NET Core console app builds off of the code used in the Quickstart and uses the Azure Cognitive Search .NET SDK to create an index, and efficiently load it with documents. The app allows users to test various batch sizes to understand the optimal batch size and then demonstrates how to efficiently upload 100,000 documents to a search index. This is done by splitting the data into batches, and spinning up several threads to upload the documents. Any failures are monitored and then retried using the exponential backoff retry strategy. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries
+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## AI Enrichment

This .NET Core console app creates an AI enrichment pipeline consisting of an index, indexer, data source, and skillset. The skillset calls Azure Cognitive Services image analysis and OCR, and natural language processing, extract information and structure from heterogeneous blob content, making it searchable in Azure Cognitive Search.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries
+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects