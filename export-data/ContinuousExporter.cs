using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System.Text.Json;

namespace export_data
{
    /// <summary>
    /// Exports data continuously from an index, updating the documents when they have been exported
    /// </summary>
    public class ContinuousExporter : Exporter
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _searchIndexClient;
        private readonly string _exportFieldName;
        private readonly int _pageSize;
        private readonly string _exportPath;

        public ContinuousExporter(SearchClient searchClient, SearchIndex index, SearchIndexClient searchIndexClient, string exportFieldName, int pageSize, string exportPath, IEnumerable<string> fieldsToInclude, ISet<string> fieldsToExclude) : base(index, fieldsToInclude, fieldsToExclude)
        {
            _searchClient = searchClient;
            _searchIndexClient = searchIndexClient;
            _exportFieldName = exportFieldName;
            _pageSize = pageSize;
            _exportPath = exportPath;
        }

        public override async Task ExportAsync()
        {
            await EnsureExportColumnExists();
            SearchField keyField = Index.Fields.First(field => field.IsKey ?? false);

            var exportUpdateOptions = new IndexDocumentsOptions { ThrowOnAnyError = true };

            var options = new SearchOptions
            {
                Size = _pageSize,
                // Set SessionId to target the same replica to retrieve consistent results
                // To learn more, please visit https://learn.microsoft.com/azure/search/index-similarity-and-scoring#scoring-statistics-and-sticky-sessions
                SessionId = Guid.NewGuid().ToString()
            };
            options.OrderBy.Add($"{_exportFieldName} asc");
            AddSelect(options, _exportFieldName);

            Console.WriteLine("Starting continuous export...");
            using FileStream exportOutput = File.Open(_exportPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            bool firstDocumentExported = false;
            do
            {
                SearchResults<SearchDocument> searchResults = await _searchClient.SearchAsync<SearchDocument>(searchText: string.Empty, options: options);
                await foreach (Page<SearchResult<SearchDocument>> resultPage in searchResults.GetResultsAsync().AsPages())
                {
                    SearchResult<SearchDocument> firstResult = resultPage.Values.FirstOrDefault();
                    if (firstResult == null)
                    {
                        firstDocumentExported = true;
                        break;
                    }


                    if (firstResult.Document.TryGetValue(_exportFieldName, out object exportValue) &&
                        exportValue is bool isExported &&
                        isExported)
                    {
                        firstDocumentExported = true;
                        break;
                    }

                    var exportedUpdates = new List<SearchDocument>();
                    foreach (SearchResult<SearchDocument> searchResult in resultPage.Values)
                    {
                        searchResult.Document.Remove(_exportFieldName);
                        JsonSerializer.Serialize(exportOutput, searchResult.Document);
                        exportOutput.WriteByte((byte)'\n');

                        exportedUpdates.Add(new SearchDocument
                        {
                            [keyField.Name] = searchResult.Document[keyField.Name],
                            [_exportFieldName] = true
                        });
                    }

                    if (exportedUpdates.Any())
                    {
                        // Delays in being able to search updates may cause already exported documents to be re-exported
                        // To learn more, please see https://learn.microsoft.com/rest/api/searchservice/addupdate-or-delete-documents#response
                        await _searchClient.MergeOrUploadDocumentsAsync(exportedUpdates, exportUpdateOptions);
                        Console.WriteLine($"Exported {exportedUpdates.Count} documents");
                    }
                }
            }
            while (!firstDocumentExported);

            Console.WriteLine("Finished continuous export");
        }

        private async Task EnsureExportColumnExists()
        {
            SearchField exportField = Index.Fields.FirstOrDefault(field => field.Name == _exportFieldName);
            if (exportField == null)
            {
                exportField = new SearchField(_exportFieldName, SearchFieldDataType.Boolean)
                {
                    IsSortable = true,
                };
                Index.Fields.Add(exportField);
                await _searchIndexClient.CreateOrUpdateIndexAsync(Index);
            }
            else if (exportField.Type != SearchFieldDataType.Boolean)
            {
                throw new ArgumentException($"Export field {exportField.Name} has unexpected type {exportField.Type}, must be {SearchFieldDataType.Boolean}");
            }
        }
    }
}
