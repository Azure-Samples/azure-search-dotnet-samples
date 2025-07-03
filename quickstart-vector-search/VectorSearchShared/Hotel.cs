using System.Net;
using System.Text.Json.Serialization;

namespace VectorSearchShared
{
    public class Hotel
    {
        [JsonPropertyName("@search.action")]
        public string SearchAction { get; set; }
        public string HotelId { get; set; }
        public string HotelName { get; set; }
        public string Description { get; set; }
        public List<float> DescriptionVector { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; }
        public bool ParkingIncluded { get; set; }
        public DateTimeOffset? LastRenovationDate { get; set; }
        public double Rating { get; set; }
        public Address Address { get; set; }
        public Location Location { get; set; }
    }
}
