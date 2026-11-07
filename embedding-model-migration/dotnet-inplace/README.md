# .NET in-place additive embedding migration sample

This sample migrates embeddings **inside the same Azure AI Search index** by:

1. Adding a new vector field and a migration-specific vector search profile/vectorizer.
2. Re-embedding every document from an existing text field.
3. Writing vectors into the new field with `merge` updates.
4. Running before/after index calls and post-migration verification.
5. Backfilling only pending records using a required marker field/value.

## When to use this sample

Use this approach when you want to keep the same index name and add a new embedding field incrementally.

> [!IMPORTANT]
> This is additive. It does **not** change the old vector field to non-searchable in place. If you need the old field removed or reconfigured, use the new-index migration flow (`dotnet-migration`) and swap indexes.

Set `SOURCE_WRITES_FROZEN=true` only after pausing ingestion/updates for the source index. This sample does not perform version-aware write arbitration.

## Prerequisites

- .NET 8 SDK
- Existing Azure AI Search index
- Existing embedding Function app from this sample set
- Keyless auth enabled for Search + Function
- Source index writes paused for the migration window

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
