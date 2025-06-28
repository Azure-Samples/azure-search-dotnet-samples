---
page_type: sample
languages:
  - csharp
name: Create an Azure AI Search app using MVC with .NET
description: "Explore server-side search features like filters, sorting, and relevance tuning in this ASP.NET Core (MVC) app for Azure AI Search."
products:
  - azure
  - azure-cognitive-search
urlFragment: create-mvc-app
---

# Create a search app in ASP.NET Core

In this sample, start with a basic ASP.NET Core (Model-View-Controller) app that connects to the hotels-sample-index on your search service for server-side operations. In Azure AI Search, you can return sorted, filtered, and boosted results. The sample includes methods for sorting and filtering. For boosting, you can add a scoring profile to the hotels-sample-index to boost results based on criteria.

This sample code is featured in [C# tutorial: Add search to ASP.NET Core - Azure AI Search](https://docs.microsoft.com/azure/search/tutorial-csharp-create-mvc-app). 

## Prerequisites

+ [Azure AI Search](search-create-app-portal.md)
+ [Hotel samples index](search-get-started-portal.md)
+ [Visual Studio](https://visualstudio.microsoft.com/downloads/)
+ [Azure.Search.Documents NuGet package](https://www.nuget.org/packages/Azure.Search.Documents/)
+ [Microsoft.Spatial NuGet package](https://www.nuget.org/packages/Microsoft.Spatial/)

To complete this tutorial, you'll need to create the hotels-sample-index on your search service. Make sure the search index name is`hotels-sample-index`, or change the index name in the `HomeController.cs` file.

Your search service must have public network access. For the connection, the app presents a query API key to your fully-qualified search URL. Both the URL and the query API key are specified in an `appsettings.json` file.

## Set up the project

1. Clone or download this sample repository.

1. Extract contents if the download is a zip file. Make sure the files are read-write.

1. Open the solution in Visual Studio.

1. If necessary, update the NuGet packages to get the most recent stable versions.

1. Modify `appsettings.json` to specify your search service and query API key.

   ```json
   {
      "SearchServiceName": "https://<YOUR-SEARCH-SERVICE-NAME>.search.windows.net",
      "SearchServiceQueryApiKey": "<YOUR-SEARCH-SERVICE-QUERY-API-KEY>"
   }
   ```

## Run the sample

1. Press **F5** to compile and run the project. The app runs on local host and opens in your default browser.

1. Select the **Search** button to return all results.

1. This demo uses the default search configuration, supporting the [simple syntax](https://learn.microsoft.com/azure/search/query-simple-syntax) and `searchMode=Any`. You can enter keywords with boolean operators.

1. To change server-side behaviors, modify the **RunQueryAsync** method in the HomeController. There are several use-cases that commented out:

   + Use case 1 is basic search results presented in a table.
   + Use case 2 adds a filter over the Category field.
   + Use case 3 adds a sort order over the Rating field.

   Recall that the index field attributes determine which fields are searchable, filterable, sortable, facetable, and retrievable. If you want different filters and sort fields, delete and recreate the hotels index with the attribution necessary for your scenario.

1. Relevance tuning is a server-side operation. If you want to boost the relevance of a document based on whether the match was found in a certain field, such as "Tags", or by location, [add a scoring profile](https://learn.microsoft.com/azure/search/index-add-scoring-profiles) to the hotel-search-index, and then rerun your queries.

If you want to explore client-side operations that respond to user actions, consider adding a React template to your solution. Client-side operations include faceting, autocomplete, and suggestions. The following demo app and tutorial demonstrate client-side behaviors:

+ [C# Tutorial: Add search to a website with .NET](https://learn.microsoft.com/azure/search/tutorial-csharp-overview)
+ [Demo app](https://victorious-beach-0ab88b51e.azurestaticapps.net/)

## Next steps

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).