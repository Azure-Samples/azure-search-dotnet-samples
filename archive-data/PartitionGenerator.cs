using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace archive_data
{
    public class PartitionGenerator
    {
        private const long MaximumDocumentCount = 100000;
        private readonly SearchClient _searchClient;
        private readonly SearchField _field;
        private readonly object _lowerBound;
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
            Partition initialPartition = await GeneratePartition(_lowerBound, _upperBound);
            return await SplitPartition(initialPartition);
        }

        private async Task<List<Partition>> SplitPartition(Partition partition)
        {
            if (partition.DocumentCount <= MaximumDocumentCount)
            {
                return new List<Partition> { partition };
            }

            object midpoint = GetMidpoint(partition.LowerBound, partition.UpperBound);
            Partition left = await GeneratePartition(partition.LowerBound, midpoint);
            Partition right = await GeneratePartition(midpoint, partition.UpperBound);

            List<Partition> partitions = await SplitPartition(left);
            partitions.AddRange(await SplitPartition(right));
            return MergePartitions(partitions);
        }

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
