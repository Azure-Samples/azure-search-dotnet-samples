param(
    [string] [Parameter(Mandatory=$true)] $searchServiceName,
    [string] [Parameter(Mandatory=$true)] [AllowEmptyString()] $dataSourceContainerName,
    [string] [Parameter(Mandatory=$true)] [AllowEmptyString()] $dataSourceConnectionString,
    [string] [Parameter(Mandatory=$true)] $dataSourceType
)

$ErrorActionPreference = 'Stop'

$apiversion = '2020-06-30'
$token = Get-AzAccessToken -ResourceUrl https://search.azure.com | select -expand Token
$headers = @{ 'Authorization' = "Bearer $token"; 'Content-Type' = 'application/json'; }
$uri = "https://$searchServiceName.search.windows.net"
$indexDefinition = $null
$dataSourceDefinition = $null
$indexerDefinition = $null
$DeploymentScriptOutputs = @{}

# Create data source, index, and indexer definitions
switch ($dataSourceType)
{
    "cosmosdb" {
        $indexDefinition = @{
            'name' = 'cosmosdb-index';
            'fields' = @(
                @{ 'name' = 'rid'; 'type' = 'Edm.String'; 'key' = $true },
                @{ 'name' = 'description'; 'type' = 'Edm.String'; 'retrievable' = $true; 'searchable' = $true }
            );
        }
        $dataSourceDefinition = @{
            'name' = 'cosmosdb-datasource';
            'type' = 'cosmosdb';
            'container' = @{
                'name' = $dataSourceContainerName;
            };
            'credentials' = @{
                'connectionString' = $dataSourceConnectionString
            };
        }
        $indexerDefinition = @{
            'name' = 'cosmosdb-indexer';
            'targetIndexName' = 'cosmosdb-index';
            'dataSourceName' = 'cosmosdb-datasource';
            'schedule' = @{ 'interval' = 'PT5M' };
        }
        $DeploymentScriptOutputs['indexName'] = 'cosmosdb-index'
    }
    default {
        throw "Unsupported data source type $dataSourceType"
    }
}

try {
    # https://learn.microsoft.com/rest/api/searchservice/create-index
    Invoke-WebRequest `
        -Method 'PUT' `
        -Uri "$uri/indexes/$($indexDefinition['name'])?api-version=$apiversion" `
        -Headers  $headers `
        -Body (ConvertTo-Json $indexDefinition)

    if ($dataSourceContainerName.Length -gt 0 -and $dataSourceConnectionString.Length -gt 0)
    {
        # https://learn.microsoft.com/rest/api/searchservice/create-data-source
        Invoke-WebRequest `
            -Method 'PUT' `
            -Uri "$uri/datasources/$($dataSourceDefinition['name'])?api-version=$apiversion" `
            -Headers $headers `
            -Body (ConvertTo-Json $dataSourceDefinition)

        # https://learn.microsoft.com/rest/api/searchservice/create-indexer
        Invoke-WebRequest `
            -Method 'PUT' `
            -Uri "$uri/indexers/$($indexerDefinition['name'])?api-version=$apiversion" `
            -Headers $headers `
            -Body (ConvertTo-Json $indexerDefinition)
    }
} catch {
    Write-Error $_.ErrorDetails.Message
    throw
}