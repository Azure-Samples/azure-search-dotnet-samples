---
page_type: sample
languages:
  - csharp
name: Export data from an Azure Cognitive Search index
description: "Export data from an Azure Cognitive Search service. This example builds a C# Console Application using the Azure Cognitive Search .NET SDK."
products:
  - azure
  - azure-cognitive-search
urlFragment: export-data
---

# Export Azure Cognitive Search service index data

![Flask sample MIT license badge](https://img.shields.io/badge/license-MIT-green.svg)

Export data from an Azure Cognitive Search service. This .NET application runs on the command line.

## Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- [Azure Cognitive Search service](https://docs.microsoft.com/azure/search/search-create-service-portal)

## Setup

1. Clone or download this sample repository.

1. Extract contents if the download is a zip file. Make sure the files are read-write.

## Run the sample

1. Run the app locally [using Visual Studio](https://docs.microsoft.com/azure/azure-functions/functions-develop-local) or [dotnet run](https://learn.microsoft.com/dotnet/core/tools/dotnet-run)

1. There are 4 commands in the app
    1. `get-bounds`
    1. `partition-index`
    1. `export-partitions`
    1. `export-continuous`

These commands support two different strategies for exporting data from the index

1. Partitioned export. Documents in the index are split into smaller partitions that can be concurrently exported into JSON files.
1. Continuous export. An additional field is added to your index to track export progress, and is continually updated as more documents are exported.

These strategies have different tradeoffs. You should use partitioned export when:

- You have a [sortable](https://learn.microsoft.com/azure/search/search-pagination-page-layout#ordering-with-orderby) and [filterable](https://learn.microsoft.com/azure/search/search-filters) field that can be used to partition the documents in the index.
- You are not updating any documents in the index, or you are not updating the documents in the index you want to export.
- You have a large number of documents. Partitioned export supports exporting more than 1000 documents concurrently. Export speed depends on [how your search service is provisioned](https://learn.microsoft.com/azure/search/search-capacity-planning).

You should use continuous export when:

- You do not have a [sortable](https://learn.microsoft.com/azure/search/search-pagination-page-layout#ordering-with-orderby) and [filterable](https://learn.microsoft.com/azure/search/search-filters) field. This field is required for partitioned export
- You are actively updating the documents in the index you want to export
- You have [storage space remaining on your search service](https://learn.microsoft.com/azure/search/search-limits-quotas-capacity#storage-limits), and are OK with the export process updating documents in the index. Continuous export adds an additional field to track export progress, which requires some storage be available.
- Duplicate documents may be included in the exported data. If the search service has multiple replicas, a [best-effort attempt is made to use the same replica](https://learn.microsoft.com/azure/search/index-similarity-and-scoring#scoring-statistics-and-sticky-sessions) to ensure consistent export results. There may also [be a delay in updating already exported documents](https://learn.microsoft.com/rest/api/searchservice/addupdate-or-delete-documents#response), so documents may be exported more than once.

### Partitioned export commands

#### get-bounds

The `get-bounds` command is used to find the smallest and largest values of a sortable and filterable field in the index. This is used to determine how to split up the documents in the index into smaller partitions

```
dotnet run get-bounds

Description:
  Find and display the largest and lowest value for the specified field. Used to determine how to partition index data for export

Usage:
  export-data get-bounds [options]

Options:
  --endpoint <endpoint> (REQUIRED)      Endpoint of the search service to export data from
  --admin-key <admin-key> (REQUIRED)    Admin key to the search service to export data from
  --index-name <index-name> (REQUIRED)  Name of the index to export data from
  --field-name <field-name> (REQUIRED)  Name of field used to partition the index data. This field must be filterable and sortable.
  -?, -h, --help                        Show help and usage information
```

Sample usage:

```
 dotnet run get-bounds --endpoint https://example.search.windows.net --admin-key AAAAAAA --index-name my-index --field-name date

Lower Bound 1969-12-31T16:11:38.0000000+00:00
Upper Bound 2022-11-06T12:14:21.0000000+00:00
```

In this example, `date` is a [Edm.DateTimeOffset](https://learn.microsoft.com/rest/api/searchservice/supported-data-types) with the [sortable](https://learn.microsoft.com/azure/search/search-pagination-page-layout#ordering-with-orderby) and [filterable](https://learn.microsoft.com/azure/search/search-filters) attributes applied. The lowest possible value in the index for this field is 1969/12/31 and the highest possible value in the index for this field is 2011/11/06.

#### partition-index

The `partition-index` command is used to divide the index into smaller partitions.

```
Description:
  Partitions the data in the index between the upper and lower bound values into partitions with at most 100,000 documents.

Usage:
  export-data partition-index [options]

Options:
  --endpoint <endpoint> (REQUIRED)      Endpoint of the search service to export data from. Example:
                                        https://example.search.windows.net
  --admin-key <admin-key> (REQUIRED)    Admin key to the search service to export data from
  --index-name <index-name> (REQUIRED)  Name of the index to export data from
  --field-name <field-name> (REQUIRED)  Name of field used to partition the index data. This field must be filterable and sortable.
  --lower-bound <lower-bound>           Smallest value to use to partition the index data. Defaults to the smallest value in the
                                        index. []
  --upper-bound <upper-bound>           Largest value to use to partition the index data. Defaults to the largest value in the
                                        index. []
  --partition-path <partition-path>     Path of the file with JSON description of partitions. Should end in .json. Default is <index
                                        name>-partitions.json []
  -?, -h, --help                        Show help and usage information
```

Sample usage:

```
dotnet run partition-index --endpoint https://example.search.windows.net --admin-key AAAAAAA --index-name my-index --field-name date

Wrote partitions to my-index-partitions.json
```

In this case, `my-index-partitions.json` has a JSON description of the partitions inside the index

```json
{
  "endpoint": "https://example.search.windows.net",
  "indexName": "my-index",
  "fieldName": "date",
  "totalDocumentCount": 500000,
  "partitions": [
    {
      "upperBound": "1976-08-09T12:41:58.375+00:00",
      "lowerBound": "1969-12-31T16:11:38+00:00",
      "documentCount": 62382,
      "filter": "date ge 1969-12-31T16:11:38.0000000+00:00 and date le 1976-08-09T12:41:58.3750000+00:00"
    },
    // more partitions in the same format as above
  ]
```

The JSON file contains metadata about the index and the partitions it created, such as total document count and partition field name. The `partitions` field lists all the [filters](https://learn.microsoft.com/azure/search/search-filters) used to retrieve the partitions using [pagination](https://learn.microsoft.com/azure/search/search-pagination-page-layout#paging-results).

#### export-partitions

The `export-partitions` command is used to export the partitions created by `partition-index` into JSON files.

```
Description:
  Exports data from a search index using a pre-generated partition file from partition-index

Usage:
  export-data export-partitions [options]

Options:
  --partition-path <partition-path> (REQUIRED)     Path of the file with JSON description of partitions. Should end in .json.
  --admin-key <admin-key> (REQUIRED)               Admin key to the search service to export data from
  --export-path <export-path>                      Directory to write JSON Lines partition files to. Every line in the partition
                                                   file contains a JSON object with the contents of the Search document. Format of
                                                   file names is <index name>-<partition id>-documents.json [default: .]
  --concurrent-partitions <concurrent-partitions>  Number of partitions to concurrently export. Default is 2 [default: 2]
  --page-size <page-size>                          Page size to use when running export queries. Default is 1000 [default: 1000]
  --include-partition <include-partition>          List of partitions by index to include in the export. Example:
                                                   --include-partition 0 --include-partition 1 only runs the export on first 2
                                                   partitions []
  --exclude-partition <exclude-partition>          List of partitions by index to exclude from the export. Example:
                                                   --exclude-partition 0 --exclude-partition 1 runs the export on every partition
                                                   except the first 2 []
  --include-field <include-field>                  List of fields to include in the export. Example: --include-field field1
                                                   --include-field field2. []
  --exclude-field <exclude-field>                  List of fields to exclude in the export. Example: --exclude-field field1
                                                   --exclude-field field2. []
  -?, -h, --help                                   Show help and usage information
```

Sample usage:

```cmd
dotnet run export-partitions --partition-path my-index-partitions.json --admin-key AAAAAAA --export-path C:\Users\MyAccount\output --concurrent-partitions 8
Starting partition 2
Starting partition 1
Starting partition 0
Starting partition 3
Starting partition 7
Starting partition 4
Starting partition 5
Starting partition 6
Ended partition 4
Ended partition 6
Ended partition 3
Ended partition 0
Ended partition 7
Ended partition 2
Ended partition 1
Ended partition 5
```

The `export-partitions` command was run on partitions in the `my-index-partitions.json` file, which was output by the previous `partition-index` command. `--concurrent-partitions` was set to 8, so 8 partitions in this file were loaded into JSON files concurrently. This number can be changed to customize parallelization. Higher numbers increase load on the search service but complete the export more quickly. Lower numbers use less resources, but take a longer time to complete the export.

1 JSON file per partition is output, with the file name formatted as `index-partition_index-documents.json`. The output [JSONL files](https://jsonlines.org/) have 1 JSON object per line, corresponding to a single search document. All fields marked as [retrievable](https://learn.microsoft.com/azure/search/search-query-simple-examples) are exported by default. Fields can be either explicitly included using `--include-field`, or explicitly excluded using `--exclude-field`.

Example output in `index-0-documents.json`:

```json
{"id":"document-1", "text": "first document", "date":"1969-12-31T16:11:38Z"}
{"id":"document-2","text": "second document", "date":"1969-12-31T17:05:39Z"}
...
```

### Continuous export commands

#### export-continuous

The `export-continuous` command starts finding documents that have not been exported and writes them into a JSON file

```
Description:
  Exports data from a search service by adding a column to track which documents have been exported and continually updating it

Usage:
  export-data export-continuous [options]

Options:
  --endpoint <endpoint> (REQUIRED)         Endpoint of the search service to export data from. Example:
                                           https://example.search.windows.net
  --admin-key <admin-key> (REQUIRED)       Admin key to the search service to export data from
  --index-name <index-name> (REQUIRED)     Name of the index to export data from
  --export-field-name <export-field-name>  Name of the Edm.Boolean field the continuous export process will update to track which
                                           documents have been exported. Default is 'exported' [default: exported]
  --page-size <page-size>                  Page size to use when running export queries. Default is 1000 [default: 1000]
  --export-path <export-path>              Path to write JSON Lines file to. Every line in the file contains a JSON object with the
                                           contents of the Search document. Format of file is <index name>-documents.json []
  --include-field <include-field>          List of fields to include in the export. Example: --include-field field1 --include-field
                                           field2. []
  --exclude-field <exclude-field>          List of fields to exclude in the export. Example: --exclude-field field1 --exclude-field
                                           field2. []
  -?, -h, --help                           Show help and usage information
```

Sample usage:

```
dotnet run export-continuous --endpoint https://example.search.windows.net --admin-key AAAA --index-name my-index   
```

1 JSON file is output, with the file name formatted as `my-index-documents.json`. The output [JSONL file](https://jsonlines.org/) has 1 JSON object per line, corresponding to a single search document. All fields marked as [retrievable](https://learn.microsoft.com/azure/search/search-query-simple-examples) are exported by default, except the field used to track if the document was exported or not. Fields can be either explicitly included using `--include-field`, or explicitly excluded using `--exclude-field`. If the export is cancelled, it is resumed where it left off.

Duplicate documents may be included in the exported data. If the search service has multiple replicas, a [best-effort attempt is made to use the same replica](https://learn.microsoft.com/azure/search/index-similarity-and-scoring#scoring-statistics-and-sticky-sessions) to ensure consistent export results. There may also [be a delay in updating already exported documents](https://learn.microsoft.com/rest/api/searchservice/addupdate-or-delete-documents#response), so documents may be exported more than once. Storage usage also increases as additional data is added to the index. If duplicate documents or storage limits are an issue, partitioned export is recommended.

Example output in `my-index-documents.json`:

```json
{"id":"document-1", "text": "first document"}
{"id":"document-2","text": "second document"}
```

## Next steps

You can learn more about Azure Cognitive Search on the [official documentation site](https://docs.microsoft.com/azure/search).
