# Azure Cognitive Search .NET Samples

This repository contains C# sample code used in Azure Cognitive Search quickstarts, tutorials, and examples. All samples run on the shared (free) Azure Cognitive Search service.  

## Quickstart v11

This version of the quickstart is updated to use the new Azure.Search.Documents client library (version 11) of the Azure SDK for .NET. This is a NET Core console app that uses the new library to create, load, and query an index. 

## Quickstart v10

This .NET Core console app uses the Azure Cognitive Search .NET SDK (version 10) to create an index, load it with documents, and execute a few queries. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

## Create your first app

This MVC sample is a collection of projects that demonstrate a user experience using fictitious hotels data. The first project creates a basic search page. Additional projects build on the first, adding results handling, and typeahead. The index is pre-built and hosted so that you can focus on the application itself.

## Multiple data sources

This .NET Core console app uses Azure Cognitive Search indexers and the .NET SDK to import data from Azure Cosmos DB and Azure Blob storage, combing data from two sources into one search index.

## Backup and restore an index

This .NET Core console app uses the .NET SDK and Azure Cognitive Search REST API to backup an index (schema and documents) to your computer and then uses the stored back up to recreate the index in a target search service that you specify. This tool is useful if you want to move an index into a different pricing tier. For example, you may use the Basic or Free pricing tier to develop your index, and then want to move it to the Standard or higher tier for production use. You can also use it to backup the index to your computer, so you can restore it at a later time, if needed.

**IMPORTANT** Search indexes are different from other datastores because they are constantly ranking and scoring results and data may shift. It is possible to miss some data during data extraction. This sample code also only works for indexes with less than 100,000 documents. However, it can be amended for larger indexes. See the README in the index-backup-restore folder for more details.

## Optimize data indexing

This .NET Core console app builds off of the code used in the Quickstart and uses the Azure Cognitive Search .NET SDK to create an index, and efficiently load it with documents. The app allows users to test various batch sizes to understand the optimal batch size and then demonstrates how to efficiently upload 100,000 documents to a search index. This is done by splitting the data into batches, and spinning up several threads to upload the documents. Any failures are monitored and then retried using the exponential backoff retry strategy. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

## AI Enrichment

This .NET Core console app creates an AI enrichment pipeline consisting of an index, indexer, data source, and skillset. The skillset calls Azure Cognitive Services image analysis, natural language processing, and OCR to extract information and structure from heterogeneous blob content, making it searchable in Azure Cognitive Search.
