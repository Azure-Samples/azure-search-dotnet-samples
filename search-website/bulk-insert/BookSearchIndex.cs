using Azure.Search.Documents.Indexes.Models;
using System.Collections.Generic;

namespace AzureSearch.BulkInsert
{
    public class BookSearchIndex : SearchIndex
    {
        readonly List<SearchField> _searchFields = new()
        {
            new("id", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsKey = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new("goodreads_book_id", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("best_book_id", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("work_id", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("books_count", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("isbn", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new("isbn13", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("authors", SearchFieldDataType.Collection(SearchFieldDataType.String))
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new("original_publication_year", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("original_title", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new("title", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = true,
                AnalyzerName = "standard.lucene"
            },
            new("language_code", SearchFieldDataType.String)
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = false,
                IsSortable = false
            },
            new("average_rating", SearchFieldDataType.Double)
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = true
            },
            new("ratings_count", SearchFieldDataType.Int64)
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = true
            },
            new("work_ratings_count", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("work_text_reviews_count", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("ratings_1", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("ratings_2", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("ratings_3", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("ratings_4", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("ratings_5", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new("image_url", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new("small_image_url", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            }
        };

        public BookSearchIndex(string name) : base(name)
        {
            _searchFields.ForEach(Fields.Add);

            Suggesters.Add(new SearchSuggester("sg", "authors", "original_title"));
            CorsOptions = new CorsOptions(new[] { "*" }) { MaxAgeInSeconds = 300 };
        }
    }
}
