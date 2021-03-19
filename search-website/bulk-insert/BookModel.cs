namespace AzureSearch.BulkInsert
{
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