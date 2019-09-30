---
page_type: sample
languages:
  - csharp
name: Build your first Azure Search app
description: "Learn how to create a search page, and then enhance it with pagination controls, filters and facets, and typeahead query support. This example builds an ASP.NET Core MVC application using the Azure Search .NET SDK."
products:
  - azure
  - azure-search
urlFragment: create-first-app
---

# Create your first Azure Search application

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

In this sample, start with a basic search page layout and then enhance it with paging controls, type-ahead (autocomplete), filtering and facet navigation, and results management.

This MVC sample is featured in [C# tutorial: Create your first app - Azure Search](https://docs.microsoft.com/azure/search/tutorial-csharp-create-first-app). It's a collection of projects that demonstrate a user experience using fictitious hotels data. The first project creates a basic search page. Additional projects build on the first, adding results handling, and typeahead. The index is pre-built and hosted so that you can focus on the application itself.

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

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

### Running create-first-app
1. Open the create-first-app.sln project in Visual Studio
1. Update the appsettings.json with the service and api details of your Azure Search service
1. Compile and Run the project

The solutions in this sample have template modifications. Methods in Startup.cs have been reordered, with app.UseCookiePolicy() relocated below app.UseMvc(...). This change addresses a known issue in .NET Core 2.x MVC apps where TempData is not persisted.

## Next steps

You can learn more about Azure Search on the [official documentation site](https://docs.microsoft.com/azure/search).