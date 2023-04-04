param cosmosDbAccountName string = 'test-cosmosdb-account-provisioning'

param cosmosDbDatabaseName string = 'test-cosmosdb-account-database'

param cosmosDbContainerName string = 'test-cosmosdb-account-container'

@description('Service name must only contain lowercase letters, digits or dashes, cannot use dash as the first two or last one characters, cannot contain consecutive dashes, and is limited between 2 and 60 characters in length.')
@minLength(2)
@maxLength(50)
param searchServiceNamePrefix string = 'test-provisioning-prefix-2'

param primaryLocation string = 'eastus'

param secondaryLocation string = 'westus'

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
param searchServiceSku string = 'basic'

@description('Replicas distribute search workloads across the service. You need at least two replicas to support high availability of query workloads (not applicable to the free tier).')
@minValue(1)
@maxValue(12)
param searchServiceReplicaCount int = 1

@description('Partitions allow for scaling of document count as well as faster indexing by sharding your index over multiple search units.')
@allowed([
  1
  2
  3
  4
  6
  12
])
param searchServicePartitionCount int = 1

@description('Applicable only for SKUs set to standard3. You can set this property to enable a single, high density partition that allows up to 1000 indexes, which is much higher than the maximum indexes allowed for any other SKU.')
@allowed([
  'default'
  'highDensity'
])
param searchServiceHostingMode string = 'default'

@description('Storage Account type')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageAccountType string = 'Standard_LRS'

param funcPrefix string = '${uniqueString(resourceGroup().id)}func'


@description('This is the built-in Cosmos DB Account Reader role. See https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#cosmos-db-account-reader-role')
resource cosmosDbAccountReaderRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: 'fbdf93bf-df7d-467e-a4d2-9458aa1360c8'
}

@description('This is the built-in Search Service Contributor role. See https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#search-service-contributor')
resource searchServiceContributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
}

var locations = [
  {
    locationName: primaryLocation
    failoverPriority: 0
  }
  {
    locationName: secondaryLocation
    failoverPriority: 1
  }
]

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2022-05-15' = {
  name: toLower(cosmosDbAccountName)
  kind: 'GlobalDocumentDB'
  location: location
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: locations
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: true
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  parent: cosmosDbAccount
  name: cosmosDbDatabaseName
  properties: {
    resource: {
      id: cosmosDbDatabaseName
    }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  parent: database
  name: cosmosDbContainerName
  properties: {
    resource: {
      id: cosmosDbContainerName
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource leaseContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  parent: database
  name: 'leases'
  properties: {
    resource: {
      id: 'leases'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
    }
  }
}

var primaryStorageAccountName = '${funcPrefix}1'
var secondaryStorageAccountName = '${funcPrefix}2'

var primaryHostingPlanName = '${funcPrefix}primaryplan'
var secondaryHostingPlanName = '${funcPrefix}secondaryplan'

var primaryFunctionAppName = '${funcPrefix}primaryfunc'
var secondaryFunctionAppName = '${funcPrefix}secondaryfunc'

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
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource secondaryHostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: secondaryHostingPlanName
  location: secondaryLocation
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
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
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: listConnectionStrings(cosmosDbAccount.id, '2019-12-12').connectionStrings[0].connectionString
        }
      ]
    }
    httpsOnly: true
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
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
      ]
    }
    httpsOnly: true
  }
}

resource primaryDocuments 'Microsoft.Web/sites/functions@2021-02-01' = {
  parent: primaryFunctionApp
  name: 'Documents'
  kind: 'function'
  properties: {
    config: {
      bindings: [
        {
          type: 'cosmosDBTrigger'
          name: 'input'
          direction: 'in'
          connectionStringSetting: 'COSMOSDB_CONNECTION_STRING'
          databaseName: database.name
          collectionName: container.name
          leaseCollectionName: 'leases'
          createLeaseCollectionIfNotExists: false
        }
      ]
    }
    files: {
      'run.csx': loadTextContent('CosmosTrigger/run.csx')
      'function.proj': loadTextContent('CosmosTrigger/function.proj')
    }
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
