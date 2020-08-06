using System;
using System.Text.Json.Serialization;

namespace AzureSearch.SDK.Quickstart.v11
{
    public class Hotel
    {
        [JsonPropertyName("hotelId")]
        public string Id { get; set; }

        [JsonPropertyName("hotelName")]
        public string Name { get; set; }

        [JsonPropertyName("hotelCategory")]
        public string Category { get; set; }

        [JsonPropertyName("baseRate")]
        public Int32 Rate { get; set; }

        [JsonPropertyName("lastRenovationDate")]
        public DateTime Updated { get; set; }
    }
}