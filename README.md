---
topic: sample
services: azure-search
platforms: dotnet
languages:
  - csharp
name: Azure Search Sample Data
description: |
  Find C# samples for Azure Search in this repository.
products:
  - azure-search
---

# Azure Search .NET Samples

This repository contains C# sample code used in Azure Search quickstarts, tutorials, and examples. All samples run on the shared (free) Azure Search service.  

We are in the process of consolidating Azure Search .NET samples into this repo. Until that work is completed, we recommend visiting this repository for additional examples: [https://github.com/Azure-Samples/search-dotnet-getting-started](https://github.com/Azure-Samples/search-dotnet-getting-started)

## Quickstart

This solution uses the Azure Search .NET SDK to create an index, load it with documents, and execute a few queries. The index is modeled on a subset of the Hotels dataset, reduced for readability and comprehension. Index definition and documents are included in the code.

### Running quickstart
+ Open the azure-search-quickstart.sln project in Visual Studio
+ Update the appsettings.json with the service and api details of your Azure Search service
+ Compile and Run the project

## Create your first app

This MVC sample is a collection of projects that demonstrate a user experience using fictitious hotels data. The first project creates a basic search page. Additional projects build on the first, adding results handling, and typeahead. The index is pre-built and hosted so that you can focus on the application itself.

### Running create-first-app
+ Open the create-first-app.sln project in Visual Studio
+ Update the appsettings.json with the service and api details of your Azure Search service
+ Compile and Run the project