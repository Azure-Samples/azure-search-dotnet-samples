# .NET in-place additive embedding migration sample

This sample migrates embeddings **inside the same Azure AI Search index** by:

1. Adding a new vector field and a migration-specific vector search profile/vectorizer.
2. Re-embedding every document from an existing text field.
3. Writing vectors into the new field with `mergeOrUpload`.
4. Running before/after index calls and post-migration verification.
5. Backfilling only pending records (`new-vector-field eq null`) or stale records (optional marker mode).

## When to use this sample

Use this approach when you want to keep the same index name and add a new embedding field incrementally.

> [!IMPORTANT]
> This is additive. It does **not** change the old vector field to non-searchable in place. If you need the old field removed or reconfigured, use the new-index migration flow (`dotnet-migration`) and swap indexes.

To support dual-write without reprocessing the full corpus every run, set `MIGRATION_MARKER_FIELD` (and optionally `MIGRATION_MARKER_VALUE`). The sample then backfills only documents whose marker differs from the current marker value.

## Prerequisites

- .NET 8 SDK
- Existing Azure AI Search index
- Existing embedding Function app from this sample set
- Keyless auth enabled for Search + Function

## Configuration

Copy `settings.example.ps1`, fill values, and run:

```powershell
. .\settings.example.ps1
```

## Run

```powershell
dotnet run --project .\DotnetInPlaceMigration.csproj -- migrate
```

Expected output includes:
- `Before: ... count=...`
- progress lines `Backfilled X/Y documents`
- `After: ... count=...`
- `After query probe: returned=...`

## Offline self-test

```powershell
dotnet run --project .\DotnetInPlaceMigration.csproj -- --self-test
```
