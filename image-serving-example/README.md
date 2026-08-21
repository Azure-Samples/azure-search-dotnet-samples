---
page_type: sample
languages:
  - csharp
name: "Image serving for agentic retrieval (preview) using C#"
description: |
  Run an end-to-end Azure AI Search agentic retrieval image-serving workflow (preview) using C#.
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-image-serving-example
---

# Example: Image serving for agentic retrieval (preview) using C#

![MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This end-to-end console app creates a blob knowledge source that uses managed ingestion with Azure Content Understanding in Foundry Tools to create semantic chunks, preserve tables, describe document-embedded figures, and extract images. Azure AI Search stores extracted images in an asset store, stores `image_path` references in the generated index, and supplies image content associated with matching results to the multimodal model during answer synthesis.

The retrieve response reports aggregate image-serving statistics, but it doesn't guarantee an extracted-image `image_path` or image bytes. Separately from retrieval, the app runs an ordinary wildcard search against the generated index to select an indexed `image_path`, and then downloads that asset to validate application access. The selected path isn't demonstrably associated with a chunk that contributed to the retrieve response.

This sample doesn't use an explicit `OcrSkill` or `normalized_images`. For the classic OCR enrichment pattern, see the [tutorial-ai-enrichment](https://github.com/Azure-Samples/azure-search-dotnet-samples/tree/main/tutorial-ai-enrichment) sample.

## Prerequisites

- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

- For required resources and permissions, see [Surface document-embedded images in agentic retrieval (preview)](https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-image-serving#prerequisites).

## Run the sample

1. Copy `sample.env` to `.env` and replace the placeholders with your resource values. The sample uses `DefaultAzureCredential`. Don't add keys or connection-string secrets.

1. Restore and run the project:

   ```powershell
   dotnet restore
   dotnet run
   ```

The app deletes the knowledge base and knowledge source in a `finally` block. To retain them for inspection, run `dotnet run -- --keep-resources`.

To validate the SDK model and request serialization without Azure resources, run `dotnet run -- --validate-local`.

A successful run verifies the following independent paths:

- An ordinary wildcard query against the generated index finds a nonempty `image_path`, and the app downloads that asset by using its own identity.

- A disabled retrieval sends zero images to the model.

- An enabled retrieval reports positive `ImagesRetrieved`, `ImagesSentToModel`, and `TotalImageSizeBytes` values.

- The separately selected blob contains bytes and has an `image/*` content type.

## Documentation

For architecture, permissions, and service limitations, see [Surface document-embedded images in agentic retrieval (preview)](https://learn.microsoft.com/azure/search/agentic-retrieval-how-to-image-serving).

## Next step

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).
