---
page_type: sample
languages:
  - csharp
name: Search over multiple search services
description: "Combine results from a single query across multiple search services"
products:
  - azure
  - azure-cognitive-search
urlFragment: multiple-search-services
---

# Search over multiple search services

This Azure Cognitive Search sample shows you how to issue a single query across multiple search services and combine the results into a single page. This sample uses [Good Books data](https://github.com/zygmuntz/goodbooks-10k).

## Prerequisites

+ [.NET 3](https://dotnet.microsoft.com/download/dotnet/5.0)
+ [Git](https://git-scm.com/downloads)
+ [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal) on a billable tier (free tier is not supported)
+ Client app: [Visual Studio](https://visualstudio.microsoft.com/downloads/), PowerShell, or [Visual Studio Code](https://code.visualstudio.com/download)

## Clone the search sample with git

At a terminal, download the sample application to your local computer.

```bash
git clone https://github.com/Azure-Samples/azure-search-dotnet-samples
```

## Set up Azure resources

1. [Sign in to the Azure portal](https://portal.azure.com).

1. [Create a resource group if one doesn't already exist](https://docs.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups).

1. [Create 2 or more Cognitive Search services if they don't already exist](https://docs.microsoft.com/azure/search/search-create-service-portal), at [Basic tier](https://azure.microsoft.com/pricing/details/search/) or above.

## Edit appsettings.json

Open the **appsettings.json** file in your local copy of the sample application and change the following values in each section for every Cognitive Search service. If there are more than 2 Cognitive Search services, add a separate section for each additional search service

1. "indexName": "Name of the index to search on the Search Service":

    + If the index does not exist, the sample application can automatically create it.

1. "adminKey": "Admin key for Search Service":

    + Find the Admin API key in the [Keys tab](https://docs.microsoft.com/azure/search/search-security-api-keys#find-existing-keys) on the search service's portal page.

1. "searchEndpoint": "https://<search-service-name>.search.windows.net":

    + Find the URI in the [search service's Overview portal page](https://docs.microsoft.com/azure/search/search-manage#overview-home-page).

1. "semanticConfigurationName": "Semantic Configuration Name on your index":

    + Required if you want to request semantic reranking. You can choose an existing configuration or [create a new one in the portal](https://learn.microsoft.com/azure/search/semantic-how-to-query-request?tabs=portal%2Cportal-query).

    **Note**: Semantic ranking comes with language requirements. See [List of supported languages](https://learn.microsoft.com/rest/api/searchservice/preview-api/search-documents#queryLanguage) for details.

## Run sample code and verify sample data

Use a client application that can build a .NET project.

1. Using Visual Studio Code

    1. On the side bar, open Explorer, and then open the local folder containing the sample code.

    1. Right-click the folder name and open an integrated terminal.

    1. Run the following command to execute the sample code and initialize the services with Good Books test data: `dotnet run --initialize`

    1. Run the following command to execute the sample code and query the services: `dotnet run --query <query text> -- facets <optional comma-separated list of facets> --searchFields <optional comma-separated list of fields to search> --displayFields <optional comma-separated list of fields to display --count <optional count of documents returned> --pageSize <optional number of documents returned per page> --queryType <optional to specify semantic> --queryLanguage <required if queryType equals semantic>`

## Clean up resources

To clean up resources created in this tutorial, [delete the resource group](https://docs.microsoft.com/azure/azure-resource-manager/management/delete-resource-group) that contains the resources.

## Next Steps

Learn more about queries in Azure Cognitive Search

+ [Access control lists (ACLs) in Azure Data Lake Storage Gen2](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control)

+ [Permissions table: Combining Azure RBAC and ACL](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control-model#permissions-table-combining-azure-rbac-and-acl)

+ [Use .NET to manage ACLs in Azure Data Lake Storage Gen2](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-acl-dotnet)