# dotnet-fixed (baseline backup/restore)

`dotnet-fixed` creates a strict backup of an Azure AI Search index and restores it to a **new** target index with full verification.

Use this tool when your goal is **baseline integrity** (copy + verify), not model change.

## What this sample does

1. Exports source schema + documents into a versioned backup folder.
2. Restores into a new target index using conditional create semantics.
3. Verifies schema compatibility and document fidelity (including vectors).

## Commands

```powershell
# Offline validation (no cloud calls)
dotnet run --project .\embedding-model-migration\dotnet-fixed\AzureSearchBackupRestoreIndex.csproj -- --self-test

# Backup source index
dotnet run --project .\embedding-model-migration\dotnet-fixed\AzureSearchBackupRestoreIndex.csproj -- backup

# Restore backup into a new target index
dotnet run --project .\embedding-model-migration\dotnet-fixed\AzureSearchBackupRestoreIndex.csproj -- restore
```

## Required environment variables

Use `settings.example.ps1` as the source of truth.

| Variable | Purpose |
|---|---|
| `AZURE_AUTH_MODE` | `cli` or `default` Entra auth mode |
| `SOURCE_SEARCH_ENDPOINT` | Source service URL for `backup` |
| `SOURCE_INDEX_NAME` | Source index for `backup` |
| `TARGET_SEARCH_ENDPOINT` | Target service URL for `restore` |
| `TARGET_INDEX_NAME` | New target index for `restore` |
| `SEARCH_API_VERSION` | Same API version for backup and restore |
| `BACKUP_DIRECTORY` | New empty folder for backup artifacts |

## Output artifacts

`BACKUP_DIRECTORY` contains:

- `schema.json`
- `documents-XXXXXXXX.json` pages
- `manifest.json` (written last; indicates complete backup)

## Human and agent guidance

- Never point restore to an existing index name.
- Treat missing `manifest.json` as incomplete backup.
- Keep source writes frozen during backup and target writes frozen during restore verification.
- Stop on nonzero exit codes; do not continue with partial outputs.
- If you plan to migrate embeddings, run this first and keep its output as the source for `dotnet-migration`.
