namespace export_data
{
    /// <summary>
    /// Record of all partitions to be exported from a search index
    /// </summary>
    public record PartitionFile
    {
        // Endpoint used to connect to the search service
        public string Endpoint { get; init; }

        // Name of the search index to export
        public string IndexName { get; init; }

        // Name of the sortable and filterable field to use to partition the index documents
        public string FieldName { get; init; }

        // Sum of all the partition approximate document counts
        public long TotalDocumentCount { get; init; }

        // List of all partitions. Sorted by lower bound.
        public List<Partition> Partitions { get; init; }
    }
}
