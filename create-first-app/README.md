---
page_type: sample
languages:
  - csharp
name: Create a search app using MVC
description: "Create a search page enhanced with pagination controls, filters and facets, and typeahead queries. This sample is an ASP.NET Core MVC application."
products:
  - azure
  - azure-cognitive-search
urlFragment: create-first-app
---

# Create your first Azure Cognitive Search application

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

In this sample, start with a basic search page layout and then enhance it with paging controls, type-ahead (autocomplete), filtering and facet navigation, and results management.

This ASP.NET Core Web App MVC sample is featured in [C# tutorial: Create your first app - Azure Cognitive Search](https://docs.microsoft.com/azure/search/tutorial-csharp-create-first-app). It's a collection of projects that demonstrate a user experience using fictitious hotels data hosted on your search service. The first project creates a basic search page. Additional projects build on the first, adding results handling, and typeahead. 

To complete this tutorial, you'll need to create the sample hotels index on your search service.

## Prerequisites

+ [Azure Cognitive Search](search-create-app-portal.md)
+ [Hotel samples index](search-get-started-portal.md)
+ [Visual Studio](https://visualstudio.microsoft.com/downloads/)
+ [Azure.Search.Documents NuGet package](https://www.nuget.org/packages/Azure.Search.Documents/)

Make sure the search index name is`hotels-sample-index`.

Your search service must have public network access. For the connection, the app presents a query API key to your fully-qualified search URL. Both the URL and the query API key are specified in an `appsettings.json` file.

## Setup

1. Clone or download this sample repository.

1. Extract contents if the download is a zip file. Make sure the files are read-write.

1. Use the sample code in the **v11** folder. Version 11 refers to the [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library. 

   The previous version, (**v10**) uses the [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries, which are no longer supported.

## Contents

| File/folder | Description |
|-------------|-------------|
| `1-basic-search-page`       | Initial project providing the page layout. |
| `2a-add-paging`       | Adds a vertical scroll bar and page controls |
| `2b-add-infinite-scroll`       | Demonstrates an infinite scrolling|
| `3-add-typeahead`       | Adds autocomplete query |
| `4-add-facet-navigation`       | Adds a facet navigation structure backed by filtering|
| `5-order-results`       | Adds results sorting |

## Run the sample

1. Open a solution in Visual Studio.

1. Modify **appsettings.json** to use your search service URI and API key. The URI is a full URL in the format of `https://<service-name>.search.windows.net`. The API key is an alphanumeric string that you can obtain from the portal, PowerShell, or CLI.

   ```json
   {
      "SearchServiceName": "<YOUR-SEARCH-SERVICE-URI>",
     "SearchServiceQueryApiKey": "<YOUR-SEARCH-SERVICE-QUERY-API-KEY>"
   }
   ```

1. Press **F5** to compile and run the project. The app runs on local host and opens in your default browser.

1. Select the **Search** button to return results.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).