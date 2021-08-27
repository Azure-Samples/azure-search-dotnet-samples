---
page_type: sample
languages:
  - csharp
name: Index Data Lake Gen2 using Access Control Lists with Managed Identity
description: "Index a subset of your Data Lake Gen2 by using Access Control Lists to allow certain files and directories to be accessed by an indexer"
products:
  - azure
  - azure-cognitive-search
urlFragment: data-lake-gen2-acl-indexing
---

# Index Data Lake Gen2 Using Access Control Lists

Normally, when setting up [managed identity with Azure Blob or Data Lake Storage](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#2---add-a-role-assignment) the [Storage Blob Data Reader role](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-reader) is used. This grants full access to all files in the storage account, which may be undesirable if [Access Control Lists](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control) are being used. This tutorial demonstrates how to update Access Control Lists to grant the search service's managed identity access to certain files and directories

## Prerequisites

+ [.NET 3](https://dotnet.microsoft.com/download/dotnet/5.0)
+ [Git](https://git-scm.com/downloads)

## Clone the search sample with git

1. At a terminal, download the sample application to your local computer.

    ```bash
    git clone https://github.com/Azure-Samples/azure-search-dotnet-samples
    ```

1. Open the [Azure Portal](https://portal.azure.com). The following tasks are completed in the Portal, unless specified.

## Setup Azure resources

1. [Create a resource group if one doesn't already exist](https://docs.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups)
1. [Create a search service if one doesn't already exist](https://docs.microsoft.com/azure/search/search-create-service-portal). The [Basic service tier](https://azure.microsoft.com/pricing/details/search/) is enough to run the sample code.
1. If using System Assigned Managed Identity, [enable it on your search service](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#option-1---turn-on-system-assigned-managed-identity). If using User Assigned Managed Identity, [assign it to your search service](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#option-2---assign-a-user-assigned-managed-identity-to-the-search-service-preview)
1. [Create a storage account if one doesn't already exist](https://docs.microsoft.com/azure/storage/common/storage-account-create?tabs=azure-portal). Ensure "Enable hierarchical namespace" is checked to enable Data Lake Storage Gen 2 on the storage account.
1. [Assign the role](https://docs.microsoft.com/azure/role-based-access-control/role-assignments-portal?tabs=current) for the search service's managed identity to the storage account
    1. Use the [Reader role](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#reader) instead of Storage Blob Data Reader.
1. [Assign roles](https://docs.microsoft.com/azure/role-based-access-control/role-assignments-portal?tabs=current) to the user running sample application
    1. For the sample code to add the sample data and setup Access Control Lists correctly in the Data Lake Gen2 storage account, the user running the code should have [Storage Blob Data Contributor](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor) and [Storage Blob Data Owner](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#storage-blob-data-owner) permissions on the Data Lake Gen2 storage account

## Setup sample code

1. Open the "appsettings.json" file in the local copy of the sample application
1. Change the following values:
    1. "searchManagedIdentityId": "Object (principal) ID for User Assigned or System Managed Identity for Search Service".
        1. If using a System Managed Identity, [copy the ID out of the Identity tab](https://docs.microsoft.com/azure/search/search-howto-managed-identities-storage#option-1---turn-on-system-assigned-managed-identity) on the Search Service's portal overview page .
        1. If using a User Assigned Managed Identity, copy the [Object ID from the User Assigned Managed Identity portal overview page](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/how-manage-user-assigned-managed-identities?pivots=identity-mi-methods-azp#list-user-assigned-managed-identities)
1. "searchAdminKey": "Admin key for Search Service"
    1. Find the Admin key in the [Keys tab](https://docs.microsoft.com/azure/search/search-security-api-keys#find-existing-keys) on the Search Service's portal page
1. "searchEndpoint": "https://search-service-name.search.windows.net"
    1. Find the Url in the [Search Service's overview portal page](https://docs.microsoft.com/azure/search/search-manage#overview-home-page)
1. "dataLakeResourceID": "/subscriptions/subscription-id/resourceGroups/resource-group-name/providers/Microsoft.Storage/storageAccounts/storageaccountname"
    1. Replace "subscription-id" with the Azure subscription id of the storage account
    1. Replace "resource-group-name" with the Azure resource group name the storage account is provisioned in
    1. Replace "storageaccountname" with the name of the storage account
1. "dataLakeEndpoint": "https://storageaccountname.dfs.core.windows.net"
    1. Replace "storageaccountname" with the name of the storage account

## Run sample code and verify sample data

1. Navigate to the local copy of the sample code in a terminal
1. Run the following command to execute the sample code: `dotnet run`
1. When the sample data has finished indexing, the sample will exit with a message "Completed indexing sample data"
1. Use [Search Explorer in the portal](https://docs.microsoft.com/azure/search/search-explorer) to view "acltestindex" to see the indexed sample data. Only data with an [Access Control List](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control) allowing the indexer's identity will appear in the index

## Clean up resources

1. To clean up resources created in this tutorial, [delete the resource group](https://docs.microsoft.com/azure/azure-resource-manager/management/delete-resource-group) that contains the resources

## Next Steps

1. To learn more about how Azure Data Lake Storage Gen2 works with Access Control, please review [Access control lists (ACLs) in Azure Data Lake Storage Gen2](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control) and [Permissions table: Combining Azure RBAC and ACL](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-access-control-model#permissions-table-combining-azure-rbac-and-acl)
1. To learn more about how to use the Azure Data Lake Storage Gen2 .NET SDK to modify Access Control Lists, please review [Use .NET to manage ACLs in Azure Data Lake Storage Gen2](https://docs.microsoft.com/azure/storage/blobs/data-lake-storage-acl-dotnet)
