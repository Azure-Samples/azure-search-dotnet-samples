namespace archive_data
{
    public record PartitionFile
    {
        public string Endpoint { get; init; }

        public string IndexName { get; init; }

        public string FieldName { get; init; }

        public long TotalDocumentCount { get; init; }

        public List<Partition> Partitions { get; init; }
    }
}
