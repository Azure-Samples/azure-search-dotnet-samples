using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace archive_data
{
    public class PartitionExporter
    {
        private readonly PartitionFile _partitionFile;
        private readonly SearchClient _searchClient;
        private readonly string _exportDirectory;
        private readonly int _concurrentPartitions;
        private readonly int _pageSize;
        private readonly List<int> _partitionIdsToInclude;
        private readonly HashSet<int> _partitionIdsToExclude;

        public PartitionExporter(PartitionFile partitionFile, SearchClient searchClient, string exportDirectory, int concurrentPartitions, int pageSize, List<int> partitionIdsToInclude, HashSet<int> partitionIdsToExclude)
        {
            _partitionFile = partitionFile;
            _searchClient = searchClient;
            _exportDirectory = exportDirectory;
            _concurrentPartitions = concurrentPartitions;
            _pageSize = pageSize;
            _partitionIdsToInclude = partitionIdsToInclude;
            _partitionIdsToExclude = partitionIdsToExclude;
        }

        public async Task ExportAsync()
        {
            if (!Directory.Exists(_exportDirectory))
            {
                Directory.CreateDirectory(_exportDirectory);
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var partitions = new ConcurrentQueue<PartitionToExport>();
            if (_partitionIdsToInclude != null && _partitionIdsToInclude.Count > 0)
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
            string exportPath = Path.Combine(_exportDirectory, $"{_searchClient.IndexName}-{partitionId}-documents.jsonl");
            using FileStream exportOutput = File.OpenWrite(exportPath);
            var options = new SearchOptions
            {
                Filter = partition.Filter,
                Size = _pageSize,
                Skip = 0
            };
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
