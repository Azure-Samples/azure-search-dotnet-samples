---
page_type: sample
languages:
  - csharp
name: "Quickstart: Agentic retrieval in Azure AI Search using C#"
description: |
  Learn how to set up an agentic retrieval pipeline in C#.
products:
  - azure
  - azure-cognitive-search
urlFragment: csharp-quickstart-agentic-retrieval
---

# Quickstart: Agentic retrieval in Azure AI Search using C#

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

This quickstart demonstrates the fundamentals of agentic retrieval using Azure AI Search. Steps include:

1. Creating and loading an `earth-at-night` search index.

1. Creating an `earth-knowledge-source` that targets your index.

1. Creating an `earth-knowledge-base` that targets your knowledge source and an LLM for query planning and answer synthesis.

1. Using the knowledge base to fetch, rank, and synthesize relevant information from the index.

## Two approaches for running this code

One approach is to create a project and load libraries using the instructions in [Quickstart: Use agentic retrieval in Azure AI Search](https://learn.microsoft.com/azure/search/search-get-started-agentic-retrieval?tabs=search-perms%2Csearch-endpoint&pivots=programming-language-csharp). The **program.cs** file in this folder provides the code. All tasks run sequentially and the output is written to the console.

Another approach is to run C# in a Jupyter notebook (**quickstart.ipynb**) in Visual Studio Code. This approach allows you to run each step atomically and review the results after each step as cell output.

## Next steps

You can learn more about Azure AI Search on the [official documentation site](https://learn.microsoft.com/azure/search).
