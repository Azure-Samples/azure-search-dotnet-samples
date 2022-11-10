using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System.Collections.Concurrent;

namespace export_data
{
    /// <summary>
    /// Splits up a search index into smaller partitions
    /// </summary>
    /// <remarks>
    /// Requires a sortable and filterable field. Max partition size is 100,000, to learn more please visit
    /// https://learn.microsoft.com/azure/search/search-pagination-page-layout#paging-results
    /// </remarks>
    public class PartitionGenerator
    {
        // Max page size is 100,000
        private const long MaximumDocumentCount = 100000;
        // Search client for paging through results
        private readonly SearchClient _searchClient;
        // Sortable filterable field to partition documents
        private readonly SearchField _field;
        // Lowest value for the field. Documents with a field value less than this will not be partitioned
        private readonly object _lowerBound;
        // Highest value for the field. Documents with a field value greater than this will not be partitioned
        private readonly object _upperBound;

        public PartitionGenerator(SearchClient searchClient, SearchField field, object lowerBound, object upperBound)
        {
            _searchClient = searchClient;
            _field = field;
            _lowerBound = lowerBound;
            _upperBound = upperBound;
        }

        public async Task<List<Partition>> GeneratePartitions()
        {
            var partitions = new List<Partition>();
            var dataToPartition = new Stack<Partition>();
            dataToPartition.Push(await GeneratePartition(_lowerBound, _upperBound));

            // Keep splitting the initial partition in half until all partitions are <= 100,000 documents
            while (dataToPartition.TryPop(out Partition nextPartition))
            {
                if (nextPartition.DocumentCount <= MaximumDocumentCount)
                {
                    partitions.Add(nextPartition);
                    continue;
                }

                object midpoint = GetMidpoint(nextPartition.LowerBound, nextPartition.UpperBound);
                dataToPartition.Push(await GeneratePartition(nextPartition.LowerBound, midpoint));
                dataToPartition.Push(await GeneratePartition(midpoint, nextPartition.UpperBound));
            }

            // Then merge all the partitions back together to create larger ones
            return MergePartitions(partitions);
        }

        // Merges smaller partitions into the largest ones possible
        private List<Partition> MergePartitions(List<Partition> partitions)
        {
            partitions.Sort();
            IEnumerator<Partition> partitionEnumerator = partitions.GetEnumerator();
            if (!partitionEnumerator.MoveNext())
            {
                return partitions;
            }

            var mergedPartitions = new List<Partition>();
            Partition nextPartition = partitionEnumerator.Current;
            while (partitionEnumerator.MoveNext())
            {
                Partition mergedPartition = nextPartition.Merge(partitionEnumerator.Current, _field.Name, _lowerBound);
                if (mergedPartition.DocumentCount > MaximumDocumentCount)
                {
                    mergedPartitions.Add(nextPartition);
                    nextPartition = partitionEnumerator.Current;
                }
                else
                {
                    nextPartition = mergedPartition;
                }
            }
            mergedPartitions.Add(nextPartition);
            return mergedPartitions;
        }

        // Execute a filtered search against the index to generate a candidate partition
        private async Task<Partition> GeneratePartition(object partitionLowerBound, object partitionUpperBound)
        {
            SearchOptions options = CreatePartitionSearchOptions(partitionLowerBound, partitionUpperBound);
            SearchResults<SearchDocument> partitionResults = await _searchClient.SearchAsync<SearchDocument>(searchText: string.Empty, options: options);
            if (!partitionResults.TotalCount.HasValue)
            {
                throw new InvalidOperationException("Expected results to have total count");
            }

            return new Partition
            {
                LowerBound = partitionLowerBound,
                UpperBound = partitionUpperBound,
                DocumentCount = partitionResults.TotalCount.Value,
                Filter = options.Filter
            };
        }

        private SearchOptions CreatePartitionSearchOptions(object partitionLowerBound, object partitionUpperBound) =>
            new()
            {
                IncludeTotalCount = true,
                Size = 1,
                Filter = Bound.GenerateBoundFilter(_field.Name, _lowerBound, partitionLowerBound, partitionUpperBound)
            };

        // Get a value for the sortable and filterable field between the lower and upper bound.
        public static object GetMidpoint(object lowerBound, object upperBound)
        {
            if (lowerBound is DateTimeOffset lowerBoundDate && upperBound is DateTimeOffset upperBoundDate)
            {
                return lowerBoundDate + ((upperBoundDate - lowerBoundDate) / 2);
            }

            throw new InvalidOperationException($"Unknown lower bound type {lowerBound.GetType()}, upper bound type {upperBound.GetType()}");
        }
    }
}
