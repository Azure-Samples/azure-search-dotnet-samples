# Non-secret placeholders. No API keys, function keys or client secrets are used.
$env:AZURE_AUTH_MODE = "cli" # default: DefaultAzureCredential, including managed identity
# Optional hosted user-assigned identity client ID:
# $env:AZURE_CLIENT_ID = "<managed-identity-client-id>"
$env:SOURCE_SEARCH_ENDPOINT = "https://<source-search-service>.search.windows.net"
$env:SOURCE_INDEX_NAME = "<source-index-name>"
$env:TARGET_SEARCH_ENDPOINT = "https://<target-search-service>.search.windows.net"
$env:TARGET_INDEX_NAME = "<new-nonexistent-index-name>"
$env:SEARCH_API_VERSION = "<api-version-covering-all-source-index-features>"
$env:BACKUP_DIRECTORY = "<absolute-path-to-new-empty-backup-directory>"
