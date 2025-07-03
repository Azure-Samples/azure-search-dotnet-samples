using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using VectorSearchShared;

public static class SearchExamples
{
    // <SnippetSearchSingleVector>
    public static async Task SearchSingleVector(SearchClient searchClient, ReadOnlyMemory<float> precalculatedVector)
    {
        SearchResults<Hotel> response = await searchClient.SearchAsync<Hotel>(
            new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(precalculatedVector) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
                },
                Select = { "HotelId", "HotelName", "Description", "Category", "Tags" },
            });

        Console.WriteLine($"Single Vector Search Results:");
        await foreach (SearchResult<Hotel> result in response.GetResultsAsync())
        {
            Hotel doc = result.Document;
            Console.WriteLine($"Score: {result.Score}, HotelId: {doc.HotelId}, HotelName: {doc.HotelName}");
        }
        Console.WriteLine();
    }
    // </SnippetSearchSingleVector>

    // <SnippetSearchSingleVectorWithFilter>
    public static async Task SearchSingleVectorWithFilter(SearchClient searchClient, ReadOnlyMemory<float> precalculatedVector)
    {
        SearchResults<Hotel> responseWithFilter = await searchClient.SearchAsync<Hotel>(
            new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(precalculatedVector) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
                },
                Filter = "Tags/any(tag: tag eq 'free wifi')",
                Select = { "HotelId", "HotelName", "Description", "Category", "Tags" }
            });

        Console.WriteLine($"Single Vector Search With Filter Results:");
        await foreach (SearchResult<Hotel> result in responseWithFilter.GetResultsAsync())
        {
            Hotel doc = result.Document;
            Console.WriteLine($"Score: {result.Score}, HotelId: {doc.HotelId}, HotelName: {doc.HotelName}, Tags: {string.Join(String.Empty, doc.Tags)}");
        }
        Console.WriteLine();
    }
    // </SnippetSearchSingleVectorWithFilter>

    // <SnippetSingleSearchWithGeoFilter>
    public static async Task SingleSearchWithGeoFilter(SearchClient searchClient, ReadOnlyMemory<float> precalculatedVector)
    {
        SearchResults<Hotel> responseWithGeoFilter = await searchClient.SearchAsync<Hotel>(
            new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(precalculatedVector) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
                },
                Filter = "geo.distance(Location, geography'POINT(-77.03241 38.90166)') le 300",
                Select = { "HotelId", "HotelName", "Description", "Address", "Category", "Tags" },
                Facets = { "Address/StateProvince" },
            });

        Console.WriteLine($"Vector query with a geo filter:");
        await foreach (SearchResult<Hotel> result in responseWithGeoFilter.GetResultsAsync())
        {
            Hotel doc = result.Document;
            Console.WriteLine($"HotelId: {doc.HotelId}");
            Console.WriteLine($"HotelName: {doc.HotelName}");
            Console.WriteLine($"Score: {result.Score}");
            Console.WriteLine($"City/State: {doc.Address.City}/{doc.Address.StateProvince}");
            Console.WriteLine($"Description: {doc.Description}");
            Console.WriteLine();
        }
        Console.WriteLine();
    }
    // </SnippetSingleSearchWithGeoFilter>

    // <SnippetSearchHybridVectorAndText>
    public static async Task<SearchResults<Hotel>> SearchHybridVectorAndText(SearchClient searchClient, ReadOnlyMemory<float> precalculatedVector)
    {
        SearchResults<Hotel> responseWithFilter = await searchClient.SearchAsync<Hotel>(
            "historic hotel walk to restaurants and shopping",
            new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(precalculatedVector) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
                },
                Select = { "HotelId", "HotelName", "Description", "Category", "Tags" },
                Size = 5,
            });

        Console.WriteLine($"Hybrid search results:");
        await foreach (SearchResult<Hotel> result in responseWithFilter.GetResultsAsync())
        {
            Hotel doc = result.Document;
            Console.WriteLine($"Score: {result.Score}");
            Console.WriteLine($"HotelId: {doc.HotelId}");
            Console.WriteLine($"HotelName: {doc.HotelName}");
            Console.WriteLine($"Description: {doc.Description}");
            Console.WriteLine($"Category: {doc.Category}");
            Console.WriteLine($"Tags: {string.Join(String.Empty, doc.Tags)}");
            Console.WriteLine();
        }
        Console.WriteLine();
        return responseWithFilter;
    }
    // </SnippetSearchHybridVectorAndText>

    // <SnippetSearchHybridVectorAndSemantic>
    public static async Task SearchHybridVectorAndSemantic(SearchClient searchClient, ReadOnlyMemory<float> precalculatedVector)
    {
        SearchResults<Hotel> responseWithFilter = await searchClient.SearchAsync<Hotel>(
            "historic hotel walk to restaurants and shopping",
            new SearchOptions
            {
                IncludeTotalCount = true,
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(precalculatedVector) { KNearestNeighborsCount = 5, Fields = { "DescriptionVector" } } }
                },
                Select = { "HotelId", "HotelName", "Description", "Category", "Tags" },
                SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "semantic-config"
                },
                QueryType = SearchQueryType.Semantic,
                Size = 5
            });

        Console.WriteLine($"Hybrid search results:");
        await foreach (SearchResult<Hotel> result in responseWithFilter.GetResultsAsync())
        {
            Hotel doc = result.Document;
            Console.WriteLine($"Score: {result.Score}");
            Console.WriteLine($"HotelId: {doc.HotelId}");
            Console.WriteLine($"HotelName: {doc.HotelName}");
            Console.WriteLine($"Description: {doc.Description}");
            Console.WriteLine($"Category: {doc.Category}");
            Console.WriteLine();
        }
        Console.WriteLine();
    }
    // </SnippetSearchHybridVectoryAndSeman
}
