@description('Service name must only contain lowercase letters, digits or dashes, cannot use dash as the first two or last one characters, cannot contain consecutive dashes, and is limited between 2 and 60 characters in length.')
@minLength(2)
@maxLength(50)
param searchServiceNamePrefix string = '${uniqueString(resourceGroup().id)}service'

param primaryLocation string

param secondaryLocation string

param location string = resourceGroup().location

@allowed([
  'basic'
  'standard'
  'standard2'
  'standard3'
  'storage_optimized_l1'
  'storage_optimized_l2'
])
@description('The pricing tier of the search service you want to create (for example, basic or standard).')
param searchServiceSku string

@description('Replicas distribute search workloads across the service. You need at least two replicas to support high availability of query workloads (not applicable to the free tier).')
@minValue(1)
@maxValue(12)
param searchServiceReplicaCount int

@description('Partitions allow for scaling of document count as well as faster indexing by sharding your index over multiple search units.')
@allowed([
  1
  2
  3
  4
  6
  12
])
param searchServicePartitionCount int

@description('Applicable only for SKUs set to standard3. You can set this property to enable a single, high density partition that allows up to 1000 indexes, which is much higher than the maximum indexes allowed for any other SKU.')
@allowed([
  'default'
  'highDensity'
])
param searchServiceHostingMode string

@description('Storage Account type')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageAccountType string

param funcPrefix string = '${uniqueString(resourceGroup().id)}func'

param trafficManagerName string = '${uniqueString(resourceGroup().id)}trafficManager'

@description('Relative DNS name for the traffic manager profile, must be globally unique.')
param trafficManagerDnsName string = '${uniqueString(resourceGroup().id)}tm'

@description('This is the built-in Search Service Index Data Reader role. See https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#search-index-data-reader')
resource searchServiceIndexDataReaderRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
}

var primaryStorageAccountName = '${funcPrefix}1'
var secondaryStorageAccountName = '${funcPrefix}2'

var primaryHostingPlanName = '${funcPrefix}primarytodoplan'
var secondaryHostingPlanName = '${funcPrefix}secondarytodoplan'

var primaryFunctionAppName = '${funcPrefix}primarytodofunc'
var secondaryFunctionAppName = '${funcPrefix}secondarytodofunc'

var primaryApplicationInsightsName = primaryFunctionAppName
var secondaryApplicationInsightsName = secondaryFunctionAppName

resource primaryStorageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: primaryStorageAccountName
  location: primaryLocation
  sku: {
    name: storageAccountType
  }
  kind: 'Storage'
}

resource secondaryStorageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: secondaryStorageAccountName
  location: secondaryLocation
  sku: {
    name: storageAccountType
  }
  kind: 'Storage'
}

resource primaryHostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: primaryHostingPlanName
  location: primaryLocation
  sku: {
    name: 'B1'
    tier: 'Standard'
  }
  properties: {}
}

resource secondaryHostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: secondaryHostingPlanName
  location: secondaryLocation
  sku: {
    name: 'B1'
    tier: 'Standard'
  }
  properties: {}
}

resource primaryApplicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: primaryApplicationInsightsName
  location: primaryLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource secondaryApplicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: secondaryApplicationInsightsName
  location: primaryLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource primarySearchService 'Microsoft.Search/searchServices@2022-09-01' = {
  name: '${searchServiceNamePrefix}-${primaryLocation}'
  location: primaryLocation
  sku: {
    name: searchServiceSku
  }
  properties: {
    replicaCount: searchServiceReplicaCount
    partitionCount: searchServicePartitionCount
    hostingMode: searchServiceHostingMode
    authOptions: {
      aadOrApiKey: {
          aadAuthFailureMode: 'http403'
      }
    }
  }
}

resource secondarySearchService 'Microsoft.Search/searchServices@2022-09-01' = {
  name: '${searchServiceNamePrefix}-${secondaryLocation}'
  location: secondaryLocation
  sku: {
    name: searchServiceSku
  }
  properties: {
    replicaCount: searchServiceReplicaCount
    partitionCount: searchServicePartitionCount
    hostingMode: searchServiceHostingMode
    authOptions: {
      aadOrApiKey: {
          aadAuthFailureMode: 'http403'
      }
    }
  }
}

module setupPrimaryService 'setup-search-service.bicep' = {
  name: 'setup-primary-search-service'
  params: {
    dataSourceType: 'cosmosdb'
    location: location
    searchServiceName: primarySearchService.name
  }
}

module setupSecondaryService 'setup-search-service.bicep' = {
  name: 'setup-secondary-search-service'
  params: {
    dataSourceType: 'cosmosdb'
    location: location
    searchServiceName: secondarySearchService.name
  }
}

resource primaryFunctionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: primaryFunctionAppName
  location: primaryLocation
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: primaryHostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${primaryStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${primaryStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${primaryStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${primaryStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(primaryFunctionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'AzureFunctionsJobHost__extensionBundle__version'
          value: '[4.0.0, 5.0.0)'
        }
        {
          name: 'AzureFunctionsJobHost__logging__logLevel__Hosts.Triggers.CosmosDB'
          value: 'Warning'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: primaryApplicationInsights.properties.ConnectionString
        }
        {
          name: 'SEARCH_INDEX_NAME'
          value: setupPrimaryService.outputs.indexName
        }
        {
          name: 'SEARCH_ENDPOINT'
          value: 'https://${primarySearchService.name}.search.windows.net'
        }
      ]
    }
    httpsOnly: true
  }
}

resource primaryIndexDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: primarySearchService
  name: guid(primarySearchService.id, primaryFunctionApp.id, searchServiceIndexDataReaderRoleDefinition.id)
  properties: {
    roleDefinitionId: searchServiceIndexDataReaderRoleDefinition.id
    principalId: primaryFunctionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource secondaryFunctionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: secondaryFunctionAppName
  location: secondaryLocation
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: secondaryHostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${secondaryStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${secondaryStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${secondaryStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${secondaryStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(secondaryFunctionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'AzureFunctionsJobHost__extensionBundle__version'
          value: '[4.0.0, 5.0.0)'
        }
        {
          name: 'AzureFunctionsJobHost__logging__logLevel__Hosts.Triggers.CosmosDB'
          value: 'Warning'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: secondaryApplicationInsights.properties.ConnectionString
        }
        {
          name: 'SEARCH_INDEX_NAME'
          value: setupSecondaryService.outputs.indexName
        }
        {
          name: 'SEARCH_ENDPOINT'
          value: 'https://${secondarySearchService.name}.search.windows.net'
        }
      ]
    }
    httpsOnly: true
  }
}

resource secondaryIndexDataContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: secondarySearchService
  name: guid(secondarySearchService.id, secondaryFunctionApp.id, searchServiceIndexDataReaderRoleDefinition.id)
  properties: {
    roleDefinitionId: searchServiceIndexDataReaderRoleDefinition.id
    principalId: secondaryFunctionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

var functionName = 'SearchToDo'

resource primaryToDo 'Microsoft.Web/sites/functions@2021-02-01' = {
  parent: primaryFunctionApp
  name: functionName
  kind: 'function'
  properties: {
    config: {
      bindings: [
        {
          authLevel: 'anonymous'
          name: 'req'
          type: 'httpTrigger'
          direction: 'in'
          methods: [ 'get' ]
        }
        {
          name: '$return'
          type: 'http'
          direction: 'out'
        }
      ]
    }
    files: {
      'run.csx': loadTextContent('SearchToDo/run.csx')
      'function.proj': loadTextContent('SearchToDo/function.proj')
    }
  }
}

resource secondaryToDo 'Microsoft.Web/sites/functions@2021-02-01' = {
  parent: secondaryFunctionApp
  name: functionName
  kind: 'function'
  properties: {
    config: {
      bindings: [
        {
          authLevel: 'anonymous'
          name: 'req'
          type: 'httpTrigger'
          direction: 'in'
          methods: [ 'get' ]
        }
        {
          name: '$return'
          type: 'http'
          direction: 'out'
        }
      ]
    }
    files: {
      'run.csx': loadTextContent('SearchToDo/run.csx')
      'function.proj': loadTextContent('SearchToDo/function.proj')
    }
  }
}

resource trafficManager 'Microsoft.Network/trafficManagerProfiles@2018-08-01' = {
    name: trafficManagerName
    location: 'global'
    properties: {
      profileStatus: 'Enabled'
      trafficRoutingMethod: 'Performance'
      dnsConfig: {
        relativeName: trafficManagerDnsName
        ttl: 30
      }
      monitorConfig: {
        protocol: 'HTTPS'
        port: 443
        path: '/api/${functionName}'
        expectedStatusCodeRanges: [
          {
            min: 200
            max: 202
          }
        ]
      }
    }
  }

  resource primaryTrafficManagerEndpoint 'Microsoft.Network/trafficManagerProfiles/externalEndpoints@2018-08-01' = {
    parent: trafficManager
    name: 'primaryEndpoint'
    properties: {
      target: primaryFunctionApp.properties.defaultHostName
      endpointLocation: primaryLocation
    }
  }

  resource secondaryTrafficManagerEndpoint 'Microsoft.Network/trafficManagerProfiles/externalEndpoints@2018-08-01' = {
    parent: trafficManager
    name: 'secondaryEndpoint'
    properties: {
      target: secondaryFunctionApp.properties.defaultHostName
      endpointLocation: secondaryLocation
    }
  }

