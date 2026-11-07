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

This sample provides a keyless .NET migration workflow with two focused tools:

1. **Baseline integrity (`dotnet-fixed`)**: back up and restore an index with strict schema/document validation.
2. **Embedding migration (`dotnet-migration`)**: regenerate vectors against a new embedding model and produce a migration-ready backup.
3. **Optional publish step**: restore migrated output into a new target index.

## Choose your path

| If you need to... | Use |
|--------|-------------|
| Prove source backup fidelity and run identical restore | [`dotnet-fixed`](dotnet-fixed/README.md) |
| Regenerate vectors for Fast/Pro and create a migration artifact | [`dotnet-migration`](dotnet-migration/README.md) |
| Set all common env vars in one place | [`sample.env`](sample.env) |

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
- `embedding-model-migration.sln`: solution containing both projects
- `sample.env`: consolidated environment template

## Quickstart (humans)

1. Validate both samples offline first:

```powershell
dotnet run --project .\embedding-model-migration\dotnet-fixed\AzureSearchBackupRestoreIndex.csproj -- --self-test
dotnet run --project .\embedding-model-migration\dotnet-migration\DotnetEmbeddingMigration.csproj -- --self-test
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
