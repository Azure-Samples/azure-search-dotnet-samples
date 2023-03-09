# Create your first Azure Cognitive Search application

<b>This sample is now retired and no longer being updated.</b>

## Original introduction

In this sample, start with a basic search page layout and then enhance it with paging controls, type-ahead (autocomplete), filtering and facet navigation, and results management.

This MVC sample is featured in [C# tutorial: Create your first app - Azure Cognitive Search](https://docs.microsoft.com/azure/search/tutorial-csharp-create-first-app). It's a collection of projects that demonstrate a user experience using fictitious hotels data. The first project creates a basic search page. Additional projects build on the first, adding results handling, and typeahead. To complete this tutorial, you'll need to create the sample hotels index on your search service.

## Prerequisites

+ [Azure Cognitive Search](search-create-app-portal.md)
+ [Hotel samples index](search-get-started-portal.md)
+ [Visual Studio](https://visualstudio.microsoft.com/downloads/)
+ [Azure.Search.Documents NuGet package](https://www.nuget.org/packages/Azure.Search.Documents/)

In contrast with other tutorials, this one uses a read-only hotels index on an existing demo search service maintained by Microsoft. No preliminary service or index setup is required.

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries

+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

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
     "SearchServiceQueryApiKey": "<YOUR-SEARCH-SERVICE-API-KEY>"
   }
   ```

1. Press **F5** to compile and run the project.

The solutions in this sample have template modifications. Methods in Startup.cs have been reordered, with app.UseCookiePolicy() relocated below app.UseMvc(...). This change addresses a known issue in .NET Core 2.x MVC apps where TempData is not persisted.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).