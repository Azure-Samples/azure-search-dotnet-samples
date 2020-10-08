---
page_type: sample
languages:
  - csharp
name: Back up and restore an Azure Cognitive Search index
description: "This application backs up a 'source' index schema and its documents to a JSON files on your computer, and then uses those files to recreate a 'target' index copy in the 'target' search service that you specify. Depending on your needs, you can use all or part of this application to backup your index files and/or move an index from one search service to another. 
For example, you may use the Basic or Free pricing tier to develop your index, and then want to move it to the Standard or higher tier for production use."
products:
  - azure
  - azure-cognitive-search
urlFragment: azure-search-backup-restore-index
---

# Back up and restore an Azure Cognitive Search index

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This application copies an index from one service to another, and in the process, creates JSON files on your computer with the index schema and documents. This tool is useful when you have been using a Basic or Free pricing tier to develop your index, and then want to move it to the Standard or higher tier for production use. It is also useful if you want to back up your index to your computer and restore the index at a later time.

## IMPORTANT - PLEASE READ
Search indexes are different from other datastores because they are constantly ranking and scoring results and data may shift. If you page through search results or even use continuation tokens as this tool does, it is possible to miss some data during data extraction.

As an example, assume that you are searching for documents and a document with ID 101 is part of page 5 of the search results. Then, as you are extracting data from page to page, and move from page 4 to page 5, it is possible that now ID 101 is actually part of page 4. This means that when you look at page 5, it is no longer there and you have missed that document.

For this reason, this tool compares the number of index documents in the original index and the index copy. If the numbers don't match, the copy may be missing data. Although this safeguard does not provide a perfect solution, it does help you help prevent you from missing data.

Also, as an extra precaution, it is best if there are no changes being made to the search index when you use run this tool.

**If your index has more than 100,000 documents**, this sample, as written, will not work. This is because the REST API $skip feature, that is used for paging, has a 100K document limit. However, you can work around this limitation by adding code to iterate over, and filter on, a facet with less that 100K documents per facet value.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.
1. Extract contents if the download is a zip file. Make sure the files are read-write.

This sample is available in two versions:

+ **v10** uses the previous [Microsoft.Azure.Search](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/search/client10) client libraries

+ **v11** uses the new [Azure.Search.Documents](https://docs.microsoft.com/dotnet/api/overview/azure/search.documents-readme) client library, highly recommended for all new projects

## Run the sample

[!NOTE] In this application the term "source" identifies the search service and index and that you are backing up. The term "target" identifies the search service and index that will contain the restored (copied) index

1. Open the AzureSearchBackupRestoreIndex.sln project in Visual Studio.

1. By default, this application will copy the source index to the target search service using the target index name you provide. 
    - If you only want to back up the index and not restore it immediately, do this:
        - Comment out the code in the **Main** method after the **BackupIndexAndDocuments** method call.
        - Comment out the last two lines of the **ConfigurationSetup** method that set the _TargetSearchClient_ and _TargetIndexClient_.
    - If you want to restore a index that you previously backed up, do this:
        - Make sure that the the _BackupDirectory_ in the appsettings.json file is pointing to to the backup location.
        - Comment out the **BackupIndexAndDocuments** method call and the the line that checks the _targetCount_ in the **Main** method.
        - Comment out the lines in **ConfigurationSetup** method that set the _SourceSearchClient_ and _SourceIndexClient_.

1. Open the appsettings.json and replace the placeholder strings with all applicable values:

    - The source search service name (SourceSearchServiceName) and key (SourceAdminKey) and the name of the index that you want to restore/copy.
    - The target search service name (TargetSearchServiceName) and key (TargetAdminKey) and the name of the restored/copied index in the target service.
    - The location on your computer where you want to store the backup index schema and documents (BackupDirectory). The location must be have non-admin write permission. Include escape characters in directory paths. Example: C:\\users\<your-account-name>\indexBackup\\"

1. Compile and Run the project.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).
