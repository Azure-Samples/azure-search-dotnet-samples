using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;

namespace export_data
{
    /// <summary>
    /// Potential value for the sortable and filterable field, used as a bound between different potential partitions
    /// </summary>
    public static class Bound
    {
        public static async Task<object> FindUpperBoundAsync(SearchField field, SearchClient searchClient) =>
            DeserializeBound(field, await FindUpperBoundDocumentAsync(field, searchClient));

        /// <summary>
        /// Find the largest value of the sortable and filterable field in the index
        /// </summary>
        public static async Task<SearchDocument> FindUpperBoundDocumentAsync(SearchField field, SearchClient searchClient)
        {
            var upperBoundOptions = new SearchOptions();
            upperBoundOptions.Select.Add(field.Name);
            upperBoundOptions.Size = 1;
            upperBoundOptions.OrderBy.Add($"{field.Name} desc");
            SearchResults<SearchDocument> upperBoundResults = await searchClient.SearchAsync<SearchDocument>(
                searchText: string.Empty,
                options: upperBoundOptions);
            SearchDocument upperBoundDocument = await GetFirstResultAsync(upperBoundResults);
            if (upperBoundDocument == null)
            {
                throw new ArgumentException($"Could not find largest value for field {field.Name}");
            }
            return upperBoundDocument;
        }

        public static async Task<object> FindLowerBoundAsync(SearchField field, SearchClient searchClient) =>
            DeserializeBound(field, await FindLowerBoundDocumentAsync(field, searchClient));

        /// <summary>
        /// Find the smallest value of the sortable and filterable field in the index
        /// </summary>
        public static async Task<SearchDocument> FindLowerBoundDocumentAsync(SearchField field, SearchClient searchClient)
        {
            var lowerBoundOptions = new SearchOptions();
            lowerBoundOptions.Select.Add(field.Name);
            lowerBoundOptions.Size = 1;
            lowerBoundOptions.OrderBy.Add($"{field.Name} asc");
            SearchResults<SearchDocument> lowerBoundResults = await searchClient.SearchAsync<SearchDocument>(
                searchText: string.Empty,
                options: lowerBoundOptions);
            SearchDocument lowerBoundDocument = await GetFirstResultAsync(lowerBoundResults);
            if (lowerBoundDocument == null)
            {
                throw new ArgumentException($"Could not find smallest value for field {field.Name}");
            }

            return lowerBoundDocument;
        }

        public static async Task<SearchDocument> GetFirstResultAsync(SearchResults<SearchDocument> results)
        {
            await foreach (SearchResult<SearchDocument> result in results.GetResultsAsync())
            {
                return result.Document;
            }

            return null;
        }

        public static object DeserializeBound(SearchField field, SearchDocument document)
        {
            if (field.Type == SearchFieldDataType.DateTimeOffset)
            {
                return document.GetDateTimeOffset(field.Name);
            }

            throw new InvalidOperationException($"Unexpected field type {field.Type}");
        }

        public static object DeserializeBound(SearchFieldDataType fieldType, string bound)
        {
            if (fieldType == SearchFieldDataType.DateTimeOffset)
            {
                return DateTimeOffset.Parse(bound);
            }

            throw new InvalidOperationException($"Unexpected field type {fieldType}");
        }

        public static string SerializeBound(object bound)
        {
            if (bound is DateTimeOffset boundDate)
            {
                return boundDate.ToString("o");
            }

            return Convert.ToString(bound);
        }

        // Strategy: If the partition's lower bound is the lowest possible value of the field, include it in the partition
        // Otherwise, exclude it from the partition
        // Always include the highest value in the partition
        // To learn more about filter syntax, please visit https://learn.microsoft.com/azure/search/search-filters
        public static string GenerateBoundFilter(string field, object lowestBound, object partitionLowerBound, object partitionUpperBound)
        {
            string lowerBoundFilter = partitionLowerBound.Equals(lowestBound) ? "ge" : "gt";
            return  $"{field} {lowerBoundFilter} {SerializeBound(partitionLowerBound)} and {field} le {SerializeBound(partitionUpperBound)}";
        }
    }
}
