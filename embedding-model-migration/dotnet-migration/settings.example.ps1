# Non-secret placeholders only. Supply real resource identifiers in your session.
$env:AZURE_AUTH_MODE = "cli" # default: DefaultAzureCredential, including managed identity
# Optional hosted user-assigned managed identity client ID:
# $env:AZURE_CLIENT_ID = "<managed-identity-client-id>"
$env:BACKUP_DIRECTORY = "<absolute-path-to-completed-dotnet-baseline-backup>"
$env:MIGRATION_DIRECTORY = "<absolute-path-to-new-empty-fast-or-pro-output>"
$env:SEARCH_API_VERSION = "<same-api-version-as-backup-supporting-authResourceId>"
$env:TEXT_FIELD = "<existing-top-level-text-field>"
$env:VECTOR_FIELD = "<existing-top-level-vector-field>"
$env:MODEL_VARIANT = "fast" # fast or pro
$env:VECTOR_DIMENSIONS = "<actual-output-dimensions-of-this-deployment>"
$env:EMBEDDING_BATCH_SIZE = "4" # 1-16; use 1 to minimize whole-batch replay cost
$env:FUNCTION_HTTP_TIMEOUT_SECONDS = "240" # 240-1800; default 240
$env:FUNCTION_BASE_ENDPOINT = "https://<function-app>.azurewebsites.net"
$env:FUNCTION_AUTH_RESOURCE_ID = "api://<function-entra-application-client-id>"
# Optional vectorizer caller identity; otherwise Azure Search uses its system identity:
# $env:SEARCH_FUNCTION_IDENTITY_RESOURCE_ID = "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.ManagedIdentity/userAssignedIdentities/<identity-name>"
$env:TARGET_SEARCH_ENDPOINT = "https://<target-search-service>.search.windows.net"
$env:TARGET_INDEX_NAME = "<new-nonexistent-index-name>"
