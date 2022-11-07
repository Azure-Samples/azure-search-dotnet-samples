namespace archive_data
{
    public record Partition
    {
        public object UpperBound { get; init; }

        public object LowerBound { get; init; }

        public int ApproximateDocumentCount { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
