# In-place additive embedding migration (.NET)
$env:AZURE_AUTH_MODE = "cli"
# $env:AZURE_CLIENT_ID = "<managed-identity-client-id>"

$env:SEARCH_ENDPOINT = "https://<search-service>.search.windows.net"
$env:INDEX_NAME = "<existing-index-name>"
$env:SEARCH_API_VERSION = "2025-09-01"

$env:TEXT_FIELD = "<existing-text-field>"
$env:SOURCE_VECTOR_FIELD = "<existing-vector-field>"
$env:NEW_VECTOR_FIELD = "<new-vector-field>"
$env:VECTOR_DIMENSIONS = "1024"
$env:MODEL_VARIANT = "fast"  # fast|pro

$env:FUNCTION_BASE_ENDPOINT = "https://<function-app>.azurewebsites.net"
$env:FUNCTION_AUTH_RESOURCE_ID = "api://<function-entra-app-client-id>"
$env:EMBEDDING_BATCH_SIZE = "4"
$env:FUNCTION_HTTP_TIMEOUT_SECONDS = "240"
$env:QUERY_PROBE_TEXT = "How do I recover my account?"

# Optional idempotent marker mode for dual-write/backfill catch-up:
# $env:MIGRATION_MARKER_FIELD = "migrationVectorVersion"
# $env:MIGRATION_MARKER_VALUE = "vector_fast_v2:fast:1024"
