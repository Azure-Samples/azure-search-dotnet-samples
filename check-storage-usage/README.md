---
page_type: sample
languages:
  - csharp
name: Check storage usage of Azure Cognitive Search
description: "Demonstrates checking storage usage of an Azure Cognitive Search service. This example builds a C# Function App using the Azure Cognitive Search .NET SDK."
products:
  - azure
  - azure-cognitive-search
  - azure-functions
urlFragment: check-storage-usage
---

# Check Azure Cognitive Search service storage usage

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

Demonstrates checking storage usage of an Azure Cognitive Search service on a schedule. This sample may be modified to [adjust the service's capacity](https://docs.microsoft.com/azure/search/search-capacity-planning) or send an alert when the storage usage exceeds a predefined threshold.

This .NET Core application runs as an [Azure Function](https://docs.microsoft.com/azure/azure-functions/functions-overview). The program [is deployed to Azure](https://docs.microsoft.com/azure/azure-functions/functions-create-your-first-function-visual-studio?tabs=in-process) using [Visual Studio](https://visualstudio.microsoft.com/downloads/) and [runs automatically on a predefined schedule](https://docs.microsoft.com/azure/azure-functions/functions-create-scheduled-function).

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)
- [Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-overview)
- [Azure Communication Services](https://docs.microsoft.com/azure/communication-services/overview)

## Setup

1. Configure a [Communication Services](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource) resource [to send email](https://docs.microsoft.com/azure/communication-services/quickstarts/email/create-email-communication-resource).

1. Clone or download this sample repository.

1. Extract contents if the download is a zip file. Make sure the files are read-write.

## Run the sample

1. Run the function locally [using Visual Studio](https://docs.microsoft.com/azure/azure-functions/functions-develop-local)

1. Deploy the sample to Azure [using Visual Studio](https://docs.microsoft.com/azure/azure-functions/functions-create-your-first-function-visual-studio?tabs=in-process#publish-the-project-to-azure).

1. Navigate to the deployed Function App in the Azure portal.

1. [Update the application settings of the Function App](https://docs.microsoft.com/azure/azure-functions/functions-how-to-use-azure-function-app-settings?tabs=portal). In the Azure portal, navigate to **Configuration** section under **Settings**. Add the following **Application Settings**:

   + `ServiceName` is the name of your search service.
   + `ServiceAdminKey` is the [Admin API Key to access your search service](https://docs.microsoft.com/azure/search/search-security-api-keys#find-existing-keys).
   + `StorageUsedPercentThreshold` is the threshold used for determining if a search service is using too much storage. This should be a decimal number between 0 and 1 which translates to a percentage of used storage. For example, 0.8 is 80% of used storage.
   + `CommunicationServicesConnectionString` is a connection string for your [Communication Services resource](https://docs.microsoft.com/azure/communication-services/concepts/authentication#access-key).
   + `ToEmailAddress` is the email address that will be notified of low storage in the search service.
   + `FromEmailAddress` is the email address that the notification email will be sent from. It must be in the [domain associated with your Communication Services email resource](https://docs.microsoft.com/azure/communication-services/concepts/email/email-domain-and-sender-authentication)

## Verify results

[An email is sent](https://docs.microsoft.com/azure/communication-services/quickstarts/email/send-email) to the provided email address that the search service has low storage available.

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).
