param cosmosDbAccountName string = 'test-cosmosdb-account-provisioning'

param cosmosDbDatabaseName string = 'test-cosmosdb-account-database'

param cosmosDbContainerName string = 'test-cosmosdb-account-container'

@description('Service name must only contain lowercase letters, digits or dashes, cannot use dash as the first two or last one characters, cannot contain consecutive dashes, and is limited between 2 and 60 characters in length.')
@minLength(2)
@maxLength(50)
param searchServiceNamePrefix string = 'test-provisioning-prefix'

param primaryLocation string = 'eastus'

param secondaryLocation string = 'westus'

param location string = resourceGroup().location

@allowed([
  'free'
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

param dataSourceName string = 'cosmosdb-datasource'

param dataSourceQuery string = ''

param indexName string = 'cosmosdb-index'

param indexerName string = 'cosmosdb-indexer'

@description('This is the built-in Cosmos DB Account Reader role. See https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#cosmos-db-account-reader-role')
resource cosmosDbAccountReaderRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: 'fbdf93bf-df7d-467e-a4d2-9458aa1360c8'
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
          '/partitionKey'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource primarySearchService 'Microsoft.Search/searchServices@2022-09-01' = {
  name: '${searchServiceNamePrefix}-${primaryLocation}'
  location: primaryLocation
  sku: {
    name: searchServiceSku
  }
  identity: {
    type: 'SystemAssigned'
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
  identity: {
    type: 'SystemAssigned'
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

resource primaryCosmosDbAccountReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: cosmosDbAccount
  name: guid(cosmosDbAccount.id, primarySearchService.id, cosmosDbAccountReaderRoleDefinition.id)
  properties: {
    roleDefinitionId: cosmosDbAccountReaderRoleDefinition.id
    principalId: primarySearchService.identity.principalId
  }
}

resource secondaryCosmosDbAccountReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: cosmosDbAccount
  name: guid(cosmosDbAccount.id, secondarySearchService.id, cosmosDbAccountReaderRoleDefinition.id)
  properties: {
    roleDefinitionId: cosmosDbAccountReaderRoleDefinition.id
    principalId: secondarySearchService.identity.principalId
  }
}

module setupPrimaryCosmosDbIndexer 'search-indexer.bicep' = {
  name: 'setupPrimaryCosmosDbIndexer'
  params: {
    dataSourceDefinition: '{"name": "${dataSourceName}", "type": "cosmosdb", "container": { "name": "${cosmosDbContainerName}", "query": "${dataSourceQuery}" }, "credentials": { "connectionString": "ResourceId=${cosmosDbAccount.id};DatabaseName=${cosmosDbDatabaseName}" } }'
    indexDefinition: '{"name": "${indexName}, "fields": [{ "name": "rid", "type": "Edm.String", "key": true }, { "name": "description", "type": "Edm.String", "retrievable": true, "searchable": true }] }'
    indexerDefinition: '{"name": "${indexerName}, "dataSourceName": "${dataSourceName}", "targetIndexName": "${indexName}" }'
    location: location
    searchServiceName: primarySearchService.name
  }
}

module setupSecondaryCosmosDbIndexer 'search-indexer.bicep' = {
  name: 'setupSecondaryCosmosDbIndexer'
  params: {
    dataSourceDefinition: '{"name": "${dataSourceName}", "type": "cosmosdb", "container": { "name": "${cosmosDbContainerName}", "query": "${dataSourceQuery}" }, "credentials": { "connectionString": "ResourceId=${cosmosDbAccount.id};DatabaseName=${cosmosDbDatabaseName}" } }'
    indexDefinition: '{"name": "${indexName}, "fields": [{ "name": "rid", "type": "Edm.String", "key": true }, { "name": "description", "type": "Edm.String", "retrievable": true, "searchable": true }] }'
    indexerDefinition: '{"name": "${indexerName}, "dataSourceName": "${dataSourceName}", "targetIndexName": "${indexName}" }'
    location: location
    searchServiceName: primarySearchService.name
  }
}
