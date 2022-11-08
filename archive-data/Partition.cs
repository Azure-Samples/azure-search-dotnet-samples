namespace export_data
{
    /// <summary>
    /// Represents a sub-partition of a search index
    /// </summary>
    public record Partition : IComparable<Partition>
    {
        // Highest value included in this partition
        public object UpperBound { get; init; }

        // Lowest value, might be included in this partition
        public object LowerBound { get; init; }

        // Approximate document count included in this partition
        public long DocumentCount { get; init; }

        // Filter query string used to retrieve this partition
        // To learn more, please visit https://learn.microsoft.com/azure/search/search-filters
        public string Filter { get; init; }

        public int CompareTo(Partition other)
        {
            if (LowerBound is DateTimeOffset lowerBoundDate &&
                other.LowerBound is DateTimeOffset otherLowerBoundDate)
            {
                return lowerBoundDate.CompareTo(otherLowerBoundDate);
            }

            throw new InvalidOperationException($"Unexpected lower bound type {LowerBound.GetType()}, other lower bound type {other.LowerBound.GetType()}");
        }

        public Partition Merge(Partition other, string field, object lowestBound) =>
            new Partition
            {
                LowerBound = LowerBound,
                UpperBound = other.LowerBound,
                DocumentCount = DocumentCount + other.DocumentCount,
                Filter = Bound.GenerateBoundFilter(field, lowestBound, partitionLowerBound: LowerBound, partitionUpperBound: other.LowerBound)
            };
    }
}
