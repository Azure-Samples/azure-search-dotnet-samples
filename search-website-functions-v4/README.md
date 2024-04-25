---
page_type: sample
languages:
  - csharp
name: Add Azure AI Search to a fullstack app (JS frontend + .NET backend)
description: |
  Add document search to a web app. This C# sample uses the Azure.Search.Documents library to create, load, and query the index.
products:
  - azure
  - azure-cognitive-search
  - azure-static-web-app
urlFragment: csharp-add-search-website
---

# Add search to a web app in C#

This README is an shortened version of the [full tutorial](https://docs.microsoft.com/azure/search/tutorial-csharp-overview). 

## Prerequisites

+ [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)
+ [Git](https://git-scm.com/downloads)
+ [Visual Studio Code](https://visualstudio.microsoft.com/downloads/) with the following extensions:

  + [Azure Resources](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azureresourcegroups)
  + [Azure Static Web App](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurestaticwebapps)

Optional: This tutorial doesn't run the Azure Function API locally but if you intend to run it locally, you need to install [azure-functions-core-tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=linux%2Ccsharp%2Cbash#install-the-azure-functions-core-tools).

## Fork and clone the search sample with git

Forking the sample repository is critical to be able to deploy the Static Web App. The web apps determine the build actions and deployment content based on your own GitHub fork location. Code execution in the Static Web App is remote, with Azure Static Web Apps reading from the code in your forked sample.

1. On GitHub, fork the [sample repository](https://github.com/Azure-Samples/azure-search-dotnet-samples). 

    Complete the fork process in your web browser with your GitHub account. This tutorial uses your fork as part of the deployment to an Azure Static Web App. 

1. At a bash terminal, download the sample application to your local computer. 

    Replace `YOUR-GITHUB-ALIAS` with your GitHub alias. 

    ```bash
    git clone https://github.com/YOUR-GITHUB-ALIAS/azure-search-dotnet-samples
    ```

1. In Visual Studio Code, open your local folder of the cloned repository. The remaining tasks are accomplished from Visual Studio Code, unless specified.

## Create a resource group for Azure resources

Creating a resource group gives you a logical unit to manage the resources, including deleting them when you are finished using them.

1. In Visual Studio Code, open the [Activity bar](https://code.visualstudio.com/docs/getstarted/userinterface), and select the Azure icon.

1. In the Side bar, **right-click on your Azure subscription** under the `Resource Groups` area and select **Create resource group**.

1. Enter a resource group name, such as `cognitive-search-website-tutorial`. 

1. Select a location that's supported by Azure Static Web Apps: `West US 2, East US 2, West Europe, Central US, East Asia`.

When you create the AI Search and Static Web App resources, later in the tutorial, use this resource group. 

## Create an Azure AI Search resource

Create a new search resource using PowerShell and the **Az.Search** module.

1. In Visual Studio Code, open a new terminal window.

1. Connect to Azure:

   ```powershell
   Connect-AzAccount
   ```

   > [!NOTE]
   > If you have multiple tenants and subscriptions, set parameters on Connect-AzAccount to select the ones you want to use..

1. Before creating a new search service, you can list existing search services for your subscription to see if there's one you want to use:

   ```powershell
   Get-AzResource -ResourceType Microsoft.Search/searchServices | ft
   ```

1. Load the **Az.Search** module: 

   ```powershell
   Install-Module -Name Az.Search
   ```

1. Create a new search service. Use the following cmdlet as a template, substituting valid values for the resource group, service name, tier, region, partitions, and replicas:

   ```powershell
   New-AzSearchService -ResourceGroupName "cognitive-search-website-tutorial"  -Name "my-demo-search-service" -Sku "Basic" -Location "West US2" -PartitionCount 1 -ReplicaCount 1 -HostingMode Default
   ```

    |Prompt|Enter|
    |--|--|
    |Enter a globally unique name for the new search service.|**Remember this name**. This resource name becomes part of your resource endpoint.|
    |Select a resource group for new resources|Use the resource group you created for this tutorial.|
    |Select the SKU for your search service.| You can use **Free** for this tutorial if you don't already have a free search service, otherwise choose **Basic**.|
    |Select a location for new resources.| You can select a region close to you. A search service location and a resource group that contains it can be in different regions.|

1. Create a query key that grants read access to a search service. Query keys have to be explicitly created. Copy the query key to Notepad so that you can paste it into the client code in a later step:

   ```powershell
   New-AzSearchQueryKey -ResourceGroupName "cognitive-search-website-tutorial"  -ServiceName "my-demo-search-service" -Name "my-query-key"
   ```

1. Get the search service admin API key that was automatically created for your search service. An admin API key provides write access to the search service. Copy either one of the admin keys to Notepad so that you can use it in the bulk import step that creates and loads an index:

   ```powershell
   Get-AzSearchAdminKeyPair  -ResourceGroupName "cognitive-search-website-tutorial" -ServiceName "my-demo-search-service" 
   ```

## Prepare the bulk import script for Search

The script uses the Azure SDK for AI Search:

+ [NuGet package Azure.Search.Documents](https://www.nuget.org/packages/Azure.Search.Documents/)
+ [Reference Documentation](/dotnet/api/overview/azure/search)

1. In Visual Studio Code, open the `Program.cs` file in the subdirectory,  `search-website/bulk-insert`, replace the following variables with your own values to authenticate with the Azure Search SDK:

   + YOUR-SEARCH-RESOURCE-NAME
   + YOUR-SEARCH-ADMIN-KEY

1. Open an integrated terminal in Visual Studio Code for the project directory's subdirectory, `search-website/bulk-insert`, then run the following command to install the dependencies. 

    ```bash
    dotnet restore
    ```

## Run the bulk import script for Search

1. Continue using the integrated terminal in Visual Studio for the project directory's subdirectory, `search-website/bulk-insert`, to run the following bash command to run the `Program.cs` script:

   ```bash
   dotnet run
   ```

1. As the code runs, the console displays progress. 

1. When the upload is complete, the last statement printed to the console is "Finished bulk inserting book data".

## Review the new Search Index

Once the upload completes, the Search Index is ready to use. Review your new Index.

1. In Visual Studio Code, open the Azure AI Search extension and select your Search resource.  

1. Expand Indexes, then Documents, then `good-books`, then select a doc to see all the document-specific data.

## Copy your Search resource name

Note your **Search resource name**. You will need this to connect the Azure Function app to your Search resource. 

## Create a Static Web App in Visual Studio Code

1. Open Visual Studio Code at the root folder of the **azure-search-dotnet-samples** repository.

1. Select **Azure** from the Activity Bar, then select **Static Web Apps** from the Side bar. 

1. Right-click on the subscription name then select **Create Static Web App (Advanced)**.    

1. Follow the prompts to provide the following information:

    |Prompt|Enter|
    |--|--|
    |Select a resource group for new resources. | Use the resource group you created for this tutorial.|
    |Enter the name for the new Static Web App. | Create a unique name for your resource. For example, `good-books-demo-swa`. |
    |Select a pricing option. | Choose **Free**. |
    |Choose build preset to configure default project structure. | Select **Custom**. | 
    |Enter the location of your application code. | **search-website-functions-v4/client** |
    |Enter the path of your build output...|build|

1. The resource is created, select **Open Actions in GitHub** from the Notifications. This opens a browser window pointed to your forked repo, the **Actions** page, in the **Workflows** tab. A workflow should be in execution. It's building and deploying your static web app.

1. Wait until the workflow is complete before continuing. This may take a minute or two to finish.

## Add configuration settings in Azure portal

The Azure Function app won't return Search data until the Search secrets are in settings. 

1. Select **Azure** from the Activity Bar. Open the **Static Web Apps** folder.

1. Right-click on your Static web app resource then select **Open in Portal**.

1. Under **Settings**, select **Configuration** then select **+ Add**.

1. Add each of the following settings:

    |Setting|Your Search resource value|
    |--|--|
    |SearchApiKey|Your Search query key|
    |SearchServiceName|Your Search resource name|
    |SearchIndexName|`good-books`|
    |SearchFacets|`authors*,language_code`|

    Azure AI Search requires different syntax for filtering collections than it does for strings. Add a `*` after a field name to denote that the field is of type `Collection(Edm.String)`. This allows the Azure Function to add filters correctly to queries.

1. Select **Save** to save the settings. 

1. Return to Visual Studio Code. 

1. Refresh your Static web app to see the Static web app's application settings. 

## Use search in your Static web app

1. In Visual Studio Code, open the [Activity bar](https://code.visualstudio.com/docs/getstarted/userinterface), and select the Azure icon.

1. In the Side bar, **right-click on your Azure subscription** under the `Static web apps` area and find the Static web app you created for this tutorial.

1. Right-click the Static Web App name and select **Browse site**.

1. Select **Open** in the pop-up dialog.

1. In the website search bar, enter a search query such as `code`, _slowly_ so the suggest feature suggests book titles. Select a suggestion or continue entering your own query. Press enter when you've completed your search query. 

1. Review the results then select one of the books to see more details. 

## Clean up resources

To clean up the resources created in this tutorial, delete the resource group.

1. In Visual Studio Code, open the [Activity bar](https://code.visualstudio.com/docs/getstarted/userinterface), and select the Azure icon. 

1. In the Side bar, **right-click on your Azure subscription** under the `Resource Groups` area and find the resource group you created for this tutorial.

1. Right-click the resource group name then select **Delete**.
    This deletes both the Search and Static web app resources.

1. If you no longer want the GitHub fork of the sample, remember to delete that on GitHub. Go to your fork's **Settings** then delete the fork. 