using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace export_data
{
    /// <summary>
    /// Export documents partitioned by a sortable and filterable field in the index
    /// </summary>
    public class PartitionExporter : Exporter
    {
        private readonly PartitionFile _partitionFile;
        private readonly SearchClient _searchClient;
        private readonly string _exportDirectory;
        private readonly int _concurrentPartitions;
        private readonly int _pageSize;
        private readonly int[] _partitionIdsToInclude;
        private readonly ISet<int> _partitionIdsToExclude;

        public PartitionExporter(PartitionFile partitionFile, SearchClient searchClient, SearchIndex index, string exportDirectory, int concurrentPartitions, int pageSize, int[] partitionIdsToInclude, ISet<int> partitionIdsToExclude, string[] fieldsToInclude, ISet<string> fieldsToExclude) : base(index, fieldsToInclude, fieldsToExclude)
        {
            _partitionFile = partitionFile;
            _searchClient = searchClient;
            _exportDirectory = exportDirectory;
            _concurrentPartitions = concurrentPartitions;
            _pageSize = pageSize;
            _partitionIdsToInclude = partitionIdsToInclude;
            _partitionIdsToExclude = partitionIdsToExclude;
        }

        public override async Task ExportAsync()
        {
            if (!Directory.Exists(_exportDirectory))
            {
                Directory.CreateDirectory(_exportDirectory);
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var partitions = new ConcurrentQueue<PartitionToExport>();
            if (_partitionIdsToInclude != null && _partitionIdsToInclude.Length > 0)
            {
                foreach (int id in _partitionIdsToInclude)
                {
                    partitions.Enqueue(new PartitionToExport { Id = id, Partition = _partitionFile.Partitions[id] });
                }
            }
            else
            {
                for (int id = 0; id < _partitionFile.Partitions.Count; id++)
                {
                    if (_partitionIdsToExclude != null && !_partitionIdsToExclude.Contains(id))
                    {
                        partitions.Enqueue(new PartitionToExport { Id = id, Partition = _partitionFile.Partitions[id] });
                    }
                }
            }

            var exporters = new Task[_concurrentPartitions];
            for (int i = 0; i < exporters.Length; i++)
            {
                exporters[i] = Task.Run(async () =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested &&
                            partitions.TryDequeue(out PartitionToExport nextPartition))
                    {
                        Console.WriteLine($"Starting partition {nextPartition.Id}");
                        try
                        {
                            await ExportPartitionAsync(nextPartition.Id, nextPartition.Partition, cancellationTokenSource.Token);
                            Console.WriteLine($"Ended partition {nextPartition.Id}");
                        }
                        catch (Exception e)
                        {
                            Console.Error.Write(e.ToString());
                            cancellationTokenSource.Cancel();
                        }
                    }
                });
            }

            await Task.WhenAll(exporters);
        }

        private async Task ExportPartitionAsync(int partitionId, Partition partition, CancellationToken cancellationToken)
        {
            string exportPath = Path.Combine(_exportDirectory, $"{_searchClient.IndexName}-{partitionId}-documents.json");
            using FileStream exportOutput = File.Open(exportPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            var options = new SearchOptions
            {
                Filter = partition.Filter,
                Size = _pageSize,
                Skip = 0
            };
            AddSelect(options);
            options.OrderBy.Add($"{_partitionFile.FieldName} asc");

            int lastPageSize;
            do
            {
                lastPageSize = 0;
                SearchResults<SearchDocument> searchResults = await _searchClient.SearchAsync<SearchDocument>(searchText: string.Empty, options: options, cancellationToken: cancellationToken);
                await foreach (Page<SearchResult<SearchDocument>> resultPage in searchResults.GetResultsAsync().AsPages())
                {
                    lastPageSize = resultPage.Values.Count;
                    options.Skip += resultPage.Values.Count;
                    foreach (SearchResult<SearchDocument> searchResult in resultPage.Values)
                    {
                        JsonSerializer.Serialize(exportOutput, searchResult.Document);
                        exportOutput.WriteByte((byte)'\n');
                    }
                }
            }
            while (lastPageSize > 0);
        }

        private record PartitionToExport
        {
            public int Id { get; init; }

            public Partition Partition { get; init; }
        }
    }
}
