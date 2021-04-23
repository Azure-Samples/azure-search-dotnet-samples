using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FunctionApp_web_search
{
    public class RequestBodyLookUp
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class RequestBodySuggest
    {
        [JsonPropertyName("q")]
        public string SearchText { get; set; }

        [JsonPropertyName("top")]
        public int Size { get; set; }

        [JsonPropertyName("suggester")]
        public string SuggesterName { get; set; }
    }

    public class RequestBodySearch
    {
        [JsonPropertyName("q")]
        public string SearchText { get; set; }

        [JsonPropertyName("skip")]
        public int Skip { get; set; }

        [JsonPropertyName("top")]
        public int Size { get; set; }

        [JsonPropertyName("filters")]
        public List<SearchFilter> Filters { get; set; }

    }


    public class SearchFilter
    {
        public string field { get; set; }
        public string value { get; set; }
    }

    public class FacetValue
    {
        public string value { get; set; }
        public long? count { get; set; }
    }
    class SearchOutput
    {
        [JsonPropertyName("count")]
        public long? Count { get; set; }
        [JsonPropertyName("results")]
        public List<SearchResult<SearchDocument>> Results { get; set; }
        [JsonPropertyName("facets")]
        public Dictionary<String, IList<FacetValue>> Facets { get; set; }
    }
    class LookupOutput
    {
        [JsonPropertyName("document")]
        public SearchDocument Document { get; set; }
    }
    public class BookModel
    {
        public string id { get; set; }
        public decimal? goodreads_book_id { get; set; }
        public decimal? best_book_id { get; set; }
        public decimal? work_id { get; set; }
        public decimal? books_count { get; set; }
        public string isbn { get; set; }
        public string isbn13 { get; set; }
        public string[] authors { get; set; }
        public decimal? original_publication_year { get; set; }
        public string original_title { get; set; }
        public string title { get; set; }
        public string language_code { get; set; }
        public double? average_rating { get; set; }
        public decimal? ratings_count { get; set; }
        public decimal? work_ratings_count { get; set; }
        public decimal? work_text_reviews_count { get; set; }
        public decimal? ratings_1 { get; set; }
        public decimal? ratings_2 { get; set; }
        public decimal? ratings_3 { get; set; }
        public decimal? ratings_4 { get; set; }
        public decimal? ratings_5 { get; set; }
        public string image_url { get; set; }
        public string small_image_url { get; set; }
    }
}
