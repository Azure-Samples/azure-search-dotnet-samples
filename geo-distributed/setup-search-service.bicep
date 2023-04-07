param searchServiceName string

param location string

param dataSourceType string

param dataSourceConnectionString string = ''

param dataSourceContainerName string = ''

resource searchService 'Microsoft.Search/searchServices@2022-09-01' existing = {
  name: searchServiceName
}

resource deploymentIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${searchService.name}-deployment-identity'
  location: location
}

@description('This is the built-in Search Service Contributor role. See https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#search-service-contributor')
resource searchServiceContributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
}

resource indexContributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: searchService
  name: guid(searchService.id, deploymentIdentity.id, searchServiceContributorRoleDefinition.id)
  properties: {
    roleDefinitionId: searchServiceContributorRoleDefinition.id
    principalId: deploymentIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource setupSearchService 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: '${searchServiceName}-setup'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${deploymentIdentity.id}': {}
    }
  }
  kind: 'AzurePowerShell'
  properties: {
    azPowerShellVersion: '8.3'
    timeout: 'PT30M'
    arguments: '-dataSourceContainerName \\"${dataSourceContainerName}\\" -dataSourceConnectionString \\"${dataSourceConnectionString}\\" -dataSourceType \\"${dataSourceType}\\" -searchServiceName \\"${searchServiceName}\\"'
    scriptContent: loadTextContent('SetupSearchService.ps1')
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'P1D'
  }
}

output indexName string = setupSearchService.properties.outputs.indexName
