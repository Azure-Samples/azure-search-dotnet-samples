param searchServiceName string

param location string

param dataSourceDefinition string

param indexDefinition string

param indexerDefinition string

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

resource setupIndexer 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'setupIndexer'
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
    arguments: ' -dataSourceDefinition ${dataSourceDefinition} -indexDefinition ${indexDefinition} -indexerDefinition ${indexerDefinition} -searchServiceName ${searchServiceName}'
    scriptContent: '''
      param(
        [string] [Parameter(Mandatory=$true)] $searchServiceName
        [string] [Parameter(Mandatory=$true)] $dataSourceDefinition,
        [string] [Parameter(Mandatory=$true)] $indexDefinition,
        [string] [Parameter(Mandatory=$true)] $indexerDefinition
      )

      $ErrorActionPreference = 'Stop'
      $DeploymentScriptOutputs = @{}

      $token = Get-AzAccessToken -ResourceUrl https://search.azure.com | select -expand Token

      Invoke-WebRequest
        -Method 'PUT'
        -Headers @{ 'Authorization' = "Bearer $token"; 'Content-Type' = 'application/json'; }
        -Body $indexDefinition
        -Uri "https://$searchServiceName.search.windows.net/indexes?api-version=2021-04-30-Preview"
      }
    '''
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'P1D'
  }
}

