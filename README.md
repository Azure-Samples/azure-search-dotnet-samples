---
topic: sample
services: azure-search
platforms: dotnet
languages:
  - csharp
name: Azure Search C# samples
description: |
  Find C# samples for Azure Search in this repository.
products:
  - azure
  - azure-search
urlFragment: dotnet-csharp-samples
---

# Azure Search .NET Samples

This repository contains C# sample code used in Azure Search quickstarts, tutorials, and examples. All samples run on the shared (free) Azure Search service.  

## Quickstart

This .NET Core console app uses the Azure Search .NET SDK to create an index, load it with documents, and execute a few queries. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

## Create your first app

This MVC sample is a collection of projects that demonstrate a user experience using fictitious hotels data. The first project creates a basic search page. Additional projects build on the first, adding results handling, and typeahead. The index is pre-built and hosted so that you can focus on the application itself.

## Multiple data sources

This .NET Core console app uses Azure Search indexers and the .NET SDK to import data from Azure Cosmos DB and Azure Blob storage, combing data from two sources into one Azure Search index.