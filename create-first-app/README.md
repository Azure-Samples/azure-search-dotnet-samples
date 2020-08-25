---
page_type: sample
languages:
  - csharp
name: Create your first Azure Cognitive Search app
description: "Create a search page, and then enhance it with pagination controls, filters and facets, and typeahead queries. This example builds an ASP.NET Core MVC application using the Azure Cognitive Search .NET SDK."
products:
  - azure
  - azure-cognitive-search
urlFragment: create-first-app
---

# Create your first Azure Cognitive Search application

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

In this sample, start with a basic search page layout and then enhance it with paging controls, type-ahead (autocomplete), filtering and facet navigation, and results management.

This MVC sample is featured in [C# tutorial: Create your first app - Azure Cognitive Search](https://docs.microsoft.com/azure/search/tutorial-csharp-create-first-app). It's a collection of projects that demonstrate a user experience using fictitious hotels data. The first project creates a basic search page. Additional projects build on the first, adding results handling, and typeahead. The index is pre-built and hosted so that you can focus on the application itself.

## Contents

| File/folder | Description |
|-------------|-------------|
| `1-basic-search-page`       | Initial project providing the page layout. |
| `2a-add-paging`       | Adds a vertical scroll bar and page controls |
| `2b-add-infinite-scroll`       | Demonstrates an infinite scrolling|
| `3-add-typeahead`       | Adds autocomplete query |
| `4-add-facet-navigation`       | Adds a facet navigation structure backed by filtering|
| `5-order-results`       | Adds results sorting |
| `.gitignore` | Define what to ignore at commit time. |
| `CONTRIBUTING.md` | Guidelines for contributing to the sample. |
| `README.md` | This README file. |
| `LICENSE`   | The license for the sample. |

## Prerequisites

- [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)

In contrast with other tutorials, this one uses a read-only hotels index on an existing demo search service maintained by Microsoft. No preliminary service or index setup is required.

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

## Running create-first-app

1. Choose which version of the client libraries to work with:

   + **v10** uses the [Microsoft.Azure.Search](https://docs.microsoft.com/dotnet/api/overview/azure/search/client10) legacy client libraries. With the exception of security bug fixes, there will be no further development in this library. If you have existing search solutions that use this library, use the v10 samples to learn about the APIs.

   + **v11** uses the [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) library, which has been redesigned for consistency with other Azure client libraries. Moving forward, all new features will roll out in this library. If you are new to Cognitive Search, use this version in your search applications.

1. Open the first folder: **1-basic-search-page**.

1. Open the **FirstAzureSearchApp.sln** project in Visual Studio.

1. Compile and run the project.

The solutions in this sample have template modifications. Methods in Startup.cs have been reordered, with app.UseCookiePolicy() relocated below app.UseMvc(...). This change addresses a known issue in .NET Core 2.x MVC apps where TempData is not persisted.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).