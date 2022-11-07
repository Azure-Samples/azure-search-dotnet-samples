namespace archive_data
{
    public record Partition : IComparable<Partition>
    {
        public object UpperBound { get; init; }

        public object LowerBound { get; init; }

        public long DocumentCount { get; init; }

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
