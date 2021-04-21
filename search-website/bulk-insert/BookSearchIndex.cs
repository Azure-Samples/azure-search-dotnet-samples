using Azure.Search.Documents.Indexes.Models;
using System.Collections.Generic;

namespace AzureSearch.BulkInsert
{
    public class BookSearchIndex : SearchIndex
    {

        SearchField id = new SearchField("id", SearchFieldDataType.String)
        {
            IsFacetable = false,
            IsFilterable = false,
            IsKey = true,
            IsHidden = false, // Sets IsRetrievable to true, when false
            IsSearchable = true,
            IsSortable = false,
            AnalyzerName = "standard.lucene"
        };
        readonly List<SearchField> _searchFields = new List<SearchField>()
        {
            new SearchField("id", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsKey = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new SearchField("goodreads_book_id", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("best_book_id", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("work_id", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("books_count", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("isbn", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new SearchField("isbn13", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("authors", SearchFieldDataType.Collection(SearchFieldDataType.String))
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new SearchField("original_publication_year", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("original_title", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new SearchField("title", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = true,
                AnalyzerName = "standard.lucene"
            },
            new SearchField("language_code", SearchFieldDataType.String)
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = false,
                IsSortable = false
            },
            new SearchField("average_rating", SearchFieldDataType.Double)
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = true
            },
            new SearchField("ratings_count", SearchFieldDataType.Int64)
            {
                IsFacetable = true,
                IsFilterable = true,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = true
            },
            new SearchField("work_ratings_count", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("work_text_reviews_count", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("ratings_1", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("ratings_2", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("ratings_3", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("ratings_4", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("ratings_5", SearchFieldDataType.Int64)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSortable = false
            },
            new SearchField("image_url", SearchFieldDataType.String)
            {
                IsFacetable = false,
                IsFilterable = false,
                IsHidden = false, // Sets IsRetrievable to true, when false
                IsSearchable = true,
                IsSortable = false,
                AnalyzerName = "standard.lucene"
            },
            new SearchField("small_image_url", SearchFieldDataType.String)
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
