---
page_type: sample
languages:
  - csharp
name: "Quickstart: Agentic retrieval with image serving using C#"
description: |
  Use managed image extraction and image serving in an Azure AI Search agentic retrieval pipeline using C#.
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart-agentic-retrieval-image-serving
---

# Quickstart: Agentic retrieval with image serving using C#

![MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This console app creates an Azure Blob knowledge source that uses Content Understanding managed ingestion to create semantic chunks, preserve tables, describe document-embedded figures, and extract images. Azure AI Search embeds the enriched Markdown, stores extracted images in an asset store, generates an index with `image_path`, and serves matching images to a multimodal model during answer synthesis. The app then downloads a referenced blob separately because retrieval doesn't return image bytes.

This sample doesn't use an explicit `OcrSkill` or `normalized_images`. For the classic OCR enrichment pattern, see the repository's `tutorial-ai-enrichment` sample.

## Prerequisites

- .NET 8 SDK.
- An Azure AI Search service that supports the `2026-05-01-preview` API.
- Azure Blob Storage with a source container and an asset container. Upload a PDF with embedded images or supported image files to the source container.
- A Microsoft Foundry resource in a [region supported by Content Understanding](https://learn.microsoft.com/azure/ai-services/content-understanding/language-region-support), with Azure OpenAI embedding and multimodal chat model deployments. Use the resource endpoint in the `https://<resource-name>.services.ai.azure.com` format.
- The search service managed identity with **Storage Blob Data Contributor** on both containers and **Cognitive Services User** on the Foundry resource.
- The app identity with **Search Service Contributor**, **Search Index Data Reader**, and **Storage Blob Data Reader** on the asset container.

## Run the sample

1. Copy `sample.env` to `.env`, and replace the placeholders with your resource values. The sample uses `DefaultAzureCredential`; don't add keys or connection-string secrets.
1. Restore and run the project:

   ```powershell
   dotnet restore
   dotnet run
   ```

The app deletes the knowledge base and knowledge source in a `finally` block. To retain them for inspection, run `dotnet run -- --keep-resources`.

To validate the SDK model and request serialization without Azure resources, run `dotnet run -- --validate-local`.

A successful run verifies:

- The generated index contains a nonempty `image_path`.
- A disabled retrieval sends zero images to the model.
- An enabled retrieval reports positive `ImagesRetrieved`, `ImagesSentToModel`, and `TotalImageSizeBytes` values.
- The referenced blob contains bytes and has an `image/*` content type.

## Documentation

For architecture, permissions, and service limitations, see [Surface document-embedded images in agentic retrieval](https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-image-serving).
