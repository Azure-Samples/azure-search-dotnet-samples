using System.Collections;

namespace FirstAzureSearchInfiniteScroll.Models
{
    public static class GlobalVariables
    {
        public static int ResultsPerPage
        {
            get
            {
                return 3;
            }
        }
    }

    public class SearchData
    {
        public SearchData()
        {
            hotels = new ArrayList();
        }

        [System.ComponentModel.DataAnnotations.Key]

        // The text to search for in the hotels data.
        public string searchText { get; set; }

        // The total number of results found for the search text.
        public int resultCount { get; set; }

        // The list of hotels to display in the current page.
        public ArrayList hotels;

        // The current page being displayed.
        public int currentPage { get; set; }

        // The total number of pages of results.
        public int pageCount { get; set; }

        public string paging { get; set; }

        public void AddHotel(string name, string desc, double rate, string bedOption, string[] tags)
        {
            // Populate a new Hotel class, but only with the data that has been provided.
            Hotel hotel = new Hotel();
            hotel.HotelName = name;
            hotel.Description = desc;
            hotel.Tags = tags;

            // Create a single room for the hotel, containing the sample rate and room description.
            Room room = new Room
            {
                BaseRate = rate,
                BedOptions = bedOption
            };

            hotel.Rooms = new Room[1];
            hotel.Rooms[0] = room;

            hotels.Add(hotel);
        }

        public Hotel GetHotel(int index)
        {
            return (Hotel)hotels[index];
        }

        public string GetFullHotelDescription(int index)
        {
            Hotel h = (Hotel)hotels[index];

            // Combine the tag data into a comma-delimited string.
            string tagData = string.Join(", ", h.Tags);
            string description = h.Description;

            // Add highlights only if there are any.
            if (tagData.Length > 0)
            {
                description += $"\nHighlights: {tagData}";
            }

            return $"Sample room: {h.Rooms[0].BedOptions} ${h.Rooms[0].BaseRate}\n{description}";
        }
    }
}
