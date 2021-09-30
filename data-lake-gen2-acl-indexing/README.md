---
page_type: sample
languages:
  - csharp
name: Index Azure Data Lake Gen2 using a managed identity
description: "Index a subset of your Azure Data Lake Gen2 data by using access control lists to allow certain files and directories to be accessed by an indexer in Azure Cognitive Search."
products:
  - azure
  - azure-cognitive-search
urlFragment: data-lake-gen2-acl-indexing
---

# Index Data Lake Gen2 using Azure AD

This Azure Cognitive Search sample shows you how to configure an indexer connection to Azure Data Lake Gen2 that uses a managed identity and role assignments for selective data access. The sample loads data and sets up permissions for data access, and then runs the indexer to create and load a search index.

Normally, when setting up [managed identity with Azure Blob Storage or Data Lake Storage](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#2---add-a-role-assignment), the [Storage Blob Data Reader role](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-reader) is used. However, this role grants full access to all files in the storage account, which may be undesirable if you are using [Access Control Lists](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control) for more selective access. This sample shows you how to constrain data access to specific files and users.

## Prerequisites

+ [.NET 3](https://dotnet.microsoft.com/download/dotnet/5.0)
+ [Git](https://git-scm.com/downloads)
+ [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal) on a billable tier (free tier is not supported)
+ [Azure Storage](https://docs.microsoft.com/azure/storage/common/storage-account-create?tabs=azure-portal) with the "Enable hierarchical namespace" option
+ Client app: [Visual Studio](https://visualstudio.microsoft.com/downloads/), PowerShell, or [Visual Studio Code](https://code.visualstudio.com/download) with the [Azure Tools](https://docs.microsoft.com/dotnet/azure/configure-vs-code#install-the-azure-tools-extension-pack) extension pack

## Clone the search sample with git

At a terminal, download the sample application to your local computer.

```bash
git clone https://github.com/Azure-Samples/azure-search-dotnet-samples
```

## Set up Azure resources

1. [Sign in to the Azure portal](https://portal.azure.com). 

1. [Create a resource group if one doesn't already exist](https://docs.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups).

1. [Create a Cognitive Search service if one doesn't already exist](https://docs.microsoft.com/azure/search/search-create-service-portal), at [Basic tier](https://azure.microsoft.com/pricing/details/search/) or above.

1. Enable a managed identity for your search service using either of the following approaches:

   + [System-managed identity](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#option-1---turn-on-system-assigned-managed-identity)

   + [User-managed identity](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#option-2---assign-a-user-assigned-managed-identity-to-the-search-service-preview)

1. [Create an Azure Storage account if one doesn't already exist](https://docs.microsoft.com/azure/storage/common/storage-account-create?tabs=azure-portal). Make sure that **Enable hierarchical namespace** is checked to enable Data Lake Storage Gen 2 on the storage account.

## Grant permissions in Azure Storage

Search must be able to connect to Azure Storage, and the user who runs the app must be able to load and then secure that data. In this step, create role assignments in Azure Storage to support both tasks.

1. In your storage account page in the portal, [create a role assignment](https://docs.microsoft.com/azure/role-based-access-control/role-assignments-portal?tabs=current) that allows the search service's managed identity access to the storage account:

    + Choose [**Reader**](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#reader) (do not use **Storage Blob Data Reader**)

1. Repeat the previous step, this time [creating a role assignment](https://docs.microsoft.com/azure/role-based-access-control/role-assignments-portal?tabs=current) for the user running sample application. The role must be able to upload sample data and create role assignments in Data Lake Gen2 storage:

    + Choose a[**Storage Blob Data Contributor**](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor) or [**Storage Blob Data Owner**](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-owner)

## Edit appsettings.json

Open the **appsettings.json** file in your local copy of the sample application and change the following values.

1. "searchManagedIdentityId": "Object (principal) ID for User-assigned or System Managed Identity for Search Service":

    + For a system-assigned managed identity, go the search service's dashboard in the portal. In the left navigation pane, select Identity and then [copy the ID for the system managed identity](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#option-1---turn-on-system-assigned-managed-identity).

    + For a user-assigned managed identity, [list the user-managed identities for your subscription](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-manage-user-assigned-managed-identities?pivots=identity-mi-methods-azp#list-user-assigned-managed-identities) and then copy the object ID.

1. "searchAdminKey": "Admin key for Search Service":

    + Find the Admin API key in the [Keys tab](https://docs.microsoft.com/azure/search/search-security-api-keys#find-existing-keys) on the search service's portal page.

1. "searchEndpoint": "https://<search-service-name>.search.windows.net":

    + Find the URI in the [search service's Overview portal page](https://docs.microsoft.com/azure/search/search-manage#overview-home-page).

1. "dataLakeResourceID": "/subscriptions/<subscription-id>/resourceGroups/<resource-group-name>/providers/Microsoft.Storage/storageAccounts/<storageaccountname>":

    + Find the resource ID in the storage account's service dashboard in the portal. Go to Settings > Endpoint > Data Lake Storage, and then copy the resource ID.

1. "dataLakeEndpoint": "https://<storageaccountname>.dfs.core.windows.net":

    + Find the endpoint in the storage account's service dashboard in the portal. Go to Settings > Endpoint > Data Lake Storage, and then copy the primary endpoint.

## Run sample code and verify sample data

Use a client application that can connect to Azure and build a .NET project.

1. Using Visual Studio Code with the Azure Tools Extension:

    1. On the side bar, select the Azure Tools extension and then sign in to your Azure account. 

    1. On the side bar, open Explorer, and then open the local folder containing the sample code.

    1. Right-click the folder name and open an integrated terminal.

    1. Run the following command to execute the sample code: `dotnet run`

1. Using PowerShell on a computer that has .NET:

    1. With Administrator permissions in PowerShell, load the Az module: `Import-Module -Name Az`

    1. Connect to Azure: `Connect-AzAccount`

    1. Run the following command to execute the sample code: `dotnet run`

1. When the sample data has finished indexing, the sample will exit with a message "Completed indexing sample data"

1. Return to the Azure portal and your search service. Use [Search Explorer](https://docs.microsoft.com/azure/search/search-explorer) to view "acltestindex" to see the indexed sample data. Only data with an [Access Control List](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control) allowing the indexer's identity will appear in the index.

## Clean up resources

To clean up resources created in this tutorial, [delete the resource group](https://docs.microsoft.com/azure/azure-resource-manager/management/delete-resource-group) that contains the resources.

## Next Steps

Learn more about how Azure Data Lake Storage Gen2 works with access control lists:

+ [Access control lists (ACLs) in Azure Data Lake Storage Gen2](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control)

+ [Permissions table: Combining Azure RBAC and ACL](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control-model#permissions-table-combining-azure-rbac-and-acl)

+ [Use .NET to manage ACLs in Azure Data Lake Storage Gen2](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-acl-dotnet)