---
page_type: sample
languages:
  - csharp
name: "Tutorial: Embedding model migration in Azure AI Search using C#"
description: |
  Migrate vector embeddings to a new model using a verified backup/restore baseline and a keyless .NET migration workflow.
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-embedding-model-migration
---

# Tutorial: Embedding model migration in Azure AI Search using C#

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This sample provides a keyless .NET migration workflow with three focused tools:

1. **Baseline integrity (`dotnet-fixed`)**: back up and restore an index with strict schema/document validation.
2. **Embedding migration (`dotnet-migration`)**: regenerate vectors against a new embedding model and produce a migration-ready backup.
3. **In-place additive migration (`dotnet-inplace`)**: keep the same index and add a new vector field/profile/vectorizer, then backfill vectors.
4. **Optional publish step**: restore migrated output into a new target index.

**Important naming note:** `dotnet-fixed` in this folder is a migration companion baseline tool. It does **not** replace the existing backup/restore sample elsewhere in this repository.

> [!IMPORTANT]
> This is a reference sample, not a production-ready drop-in. Validate E2E behavior in a representative test environment that mirrors your tenant auth, network, model deployments, quotas, and index schema before rollout.

## Problem this sample solves

When customers need to move from one embedding model/dimension setup to another, they often need one of two safe patterns:

- create a **new target index** and cut over when validated, or
- run an **in-place additive migration** that keeps the same index and adds a new vector field.

For new-target migrations, customers need a safe way to:

1. create a verified baseline copy,
2. regenerate vectors with the new model path, and
3. publish to a new target index with controlled cutover.

## Architecture (what connects to what)

```text
Source Search Index
  -> dotnet-fixed backup (schema + docs + manifest)
  -> dotnet-migration prepare (calls Function /document route)
  -> migrated backup artifacts
  -> dotnet-migration reembed/restore
  -> New Target Search Index (new vectorizer/query route)

Query-time path after migration:
Search vectorizer (customWebApi /query route) -> Function -> embedding model
```

## Example customer migration flow

Example: customer has `products-v1` with old vectors and wants `fast` model vectors.

1. Run `dotnet-fixed backup` against `products-v1` (source frozen).
2. Run `dotnet-fixed restore` to validate baseline reproducibility on a new test index.
3. Run `dotnet-migration prepare` with:
   - `MODEL_VARIANT=fast`
   - `VECTOR_DIMENSIONS=<measured fast dimension>`
   - Function auth/resource settings
4. Run `dotnet-migration reembed` to create `products-v2-fast` as a new index.
5. Validate retrieval quality and operational behavior, then cut traffic to `products-v2-fast`.

## What happens after migration

- The migrated index is expected to become the active index for reads.
- Ongoing ingestion must produce vectors compatible with the migrated vector field and dimensions.
- Query vectorization must keep using the migrated customWebApi query route and same model family.
- Keep the old index only for rollback window; retire it after acceptance.

## Choose your path

| If you need to... | Use |
|--------|-------------|
| Prove source backup fidelity and run identical restore | [`dotnet-fixed`](dotnet-fixed/README.md) |
| Regenerate vectors for Fast/Pro and create a migration artifact | [`dotnet-migration`](dotnet-migration/README.md) |
| Keep the same index and add/backfill a new vector field | [`dotnet-inplace`](dotnet-inplace/README.md) |
| Set all common env vars in one place | [`sample.env`](sample.env) |

## What "`fixed`" vs "`migration`" means

- **`dotnet-fixed`** means "fixed baseline backup/restore behavior."  
  Use it when you want an identical copy path (same schema/data, no model change) and strong verification.
- **`dotnet-migration`** means "model-migration behavior."  
  Use it when you intentionally change vector generation (model/dimensions/vectorizer route) and need a new migrated target index.
- **`dotnet-inplace`** means "same-index additive migration behavior."  
  Use it when you need to keep index identity and incrementally introduce a new vector field/profile/vectorizer. It requires a filterable marker field and a source-write freeze during backfill.

Typical sequence:

1. Run `dotnet-fixed` first to create a trustworthy baseline backup.
2. Run `dotnet-migration prepare` to generate migrated artifacts.
3. Run `dotnet-migration reembed` (or restore later) into a new target index.

`dotnet-fixed` output is a prerequisite input for `dotnet-migration`. In other words: for this sample workflow, baseline backup/validation comes before model migration.

## Prerequisites

- .NET 8 SDK
- Azure AI Search services (source and target)
- Microsoft Entra authentication configured for Search and Function access
- A deployed embedding Function endpoint that supports:
  - `POST /api/embed/{fast|pro}/document`
  - `POST /api/embed/{fast|pro}/query`

## Folder structure

- `dotnet-fixed/`: baseline backup/restore executable
- `dotnet-migration/`: embedding regeneration executable
- `dotnet-inplace/`: in-place additive migration executable
- `embedding-model-migration.sln`: solution containing all migration projects
- `sample.env`: consolidated environment template

## Quickstart (humans)

1. Validate both samples offline first:

```powershell
dotnet run --project .\embedding-model-migration\dotnet-fixed\AzureSearchBackupRestoreIndex.csproj -- --self-test
dotnet run --project .\embedding-model-migration\dotnet-migration\DotnetEmbeddingMigration.csproj -- --self-test
dotnet run --project .\embedding-model-migration\dotnet-inplace\DotnetInPlaceMigration.csproj -- --self-test
```

2. Run baseline backup/restore:

```powershell
dotnet run --project .\embedding-model-migration\dotnet-fixed\AzureSearchBackupRestoreIndex.csproj -- backup
dotnet run --project .\embedding-model-migration\dotnet-fixed\AzureSearchBackupRestoreIndex.csproj -- restore
```

3. Run migration:

```powershell
# Prepare migration artifact only
dotnet run --project .\embedding-model-migration\dotnet-migration\DotnetEmbeddingMigration.csproj -- prepare

# Prepare + restore into a new target index
dotnet run --project .\embedding-model-migration\dotnet-migration\DotnetEmbeddingMigration.csproj -- reembed

# Additive in-place migration in the same index
dotnet run --project .\embedding-model-migration\dotnet-inplace\DotnetInPlaceMigration.csproj -- migrate
```

## Quickstart (agents/automation)

Execution contract:

1. Run `--self-test` for both projects before cloud operations.
2. Never reuse `BACKUP_DIRECTORY` or `MIGRATION_DIRECTORY` across runs.
3. Never reuse target index names; restore targets must be new.
4. Do not inject API keys or secrets; Entra token auth only.
5. Stop on first nonzero exit; treat partial output as failed run state.

## Safety boundaries

- Use new output directories and new target index names for each run.
- The tools do not use API keys or client secrets.
- Baseline backup fails on incomplete snapshots and credential-bearing schemas.
- Migration requires a complete baseline backup (`manifest.json` present and valid).
- This sample does not back up skillsets, data sources, indexers, aliases, or external model resources.
- Review and adjust settings, retry policies, naming, and operational controls to your environment and requirements.
- After migration, ensure your ingestion/query pipeline is aligned with the migrated index configuration (same vector field, dimensions, and Function query vectorizer route).
