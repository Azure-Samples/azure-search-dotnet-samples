# dotnet-migration (embedding regeneration)

`dotnet-migration` takes a completed `dotnet-fixed` backup, regenerates vectors with your embedding Function, and writes a new migration backup. It can also restore the migrated output into a new target index.

Use this tool when your goal is **intentional embedding change** (new model/dimensions), not identical copy.

Prerequisite: run `dotnet-fixed` first and use its completed backup (`manifest.json` present and valid) as `BACKUP_DIRECTORY`.

## What this sample does

1. Reads a complete baseline backup (`manifest.json`, schema, document pages).
2. Validates model/query route compatibility before document regeneration.
3. Rewrites only the configured vector field using `fast` or `pro`.
4. Writes a new migration backup to a fresh output directory.
5. Optionally restores migrated output to a new index (`reembed` mode).

## Commands

```powershell
# Offline validation (no cloud calls)
dotnet run --project .\embedding-model-migration\dotnet-migration\DotnetEmbeddingMigration.csproj -- --self-test

# Prepare migration backup only
dotnet run --project .\embedding-model-migration\dotnet-migration\DotnetEmbeddingMigration.csproj -- prepare

# Prepare + restore migrated output to target index
dotnet run --project .\embedding-model-migration\dotnet-migration\DotnetEmbeddingMigration.csproj -- reembed
```

## Required environment variables

Use `settings.example.ps1` as the source of truth.

| Variable | Purpose |
|---|---|
| `BACKUP_DIRECTORY` | Completed baseline backup input |
| `MIGRATION_DIRECTORY` | New empty output folder for migrated artifacts |
| `SEARCH_API_VERSION` | Must match baseline backup API version |
| `TEXT_FIELD` | Existing source text field |
| `VECTOR_FIELD` | Existing source vector field |
| `MODEL_VARIANT` | `fast` or `pro` |
| `VECTOR_DIMENSIONS` | Output dimension returned by selected model/deployment |
| `FUNCTION_BASE_ENDPOINT` | Function app root URL |
| `FUNCTION_AUTH_RESOURCE_ID` | Entra audience (for token acquisition) |
| `TARGET_SEARCH_ENDPOINT` | Required for `reembed` |
| `TARGET_INDEX_NAME` | Required for `reembed`, must be new |

Optional:

- `EMBEDDING_BATCH_SIZE` (default `4`, range `1-16`)
- `FUNCTION_HTTP_TIMEOUT_SECONDS` (default `240`, range `240-1800`)
- `SEARCH_FUNCTION_IDENTITY_RESOURCE_ID` (if using user-assigned MI for query vectorizer)

## Human and agent guidance

- `MIGRATION_DIRECTORY` must be different from `BACKUP_DIRECTORY`.
- Run with `prepare` first when validating a new model/deployment.
- Use `reembed` only after `prepare` behavior is validated.
- Never reuse target index names between retries.
- Treat nonzero exit codes as hard failures and inspect artifacts before retry.

## After migration: expected runtime behavior

- Query vectorization is expected to run through the migrated index's `customWebApi` vectorizer route (`/api/embed/{model}/query`).
- Ingestion going forward must generate vectors compatible with the migrated index (`VECTOR_FIELD`, `VECTOR_DIMENSIONS`, model variant).
- If you keep indexers/skills outside this sample, update them to use the same model and dimensions used here.
