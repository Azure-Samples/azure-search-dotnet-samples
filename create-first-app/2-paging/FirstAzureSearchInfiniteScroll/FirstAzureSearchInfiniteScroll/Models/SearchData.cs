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

        public void AddHotel(string name, string desc, double rate, string bedoption, string[] tags)
        {
            // Populate a new Hotel class, but only with the data that has been provided.
            Hotel hotel = new Hotel();
            hotel.HotelName = name;
            hotel.Description = desc;
            hotel.Tags = new string[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                hotel.Tags[i] = new string(tags[i]);
            }

            // Create just a single room for the hotel, containing the sample rate and room description.
            Room room = new Room();
            room.BaseRate = rate;
            room.BedOptions = bedoption;

            hotel.Rooms = new Room[1];
            hotel.Rooms[0] = room;

            hotels.Add(hotel);
        }

        public Hotel GetHotel(int index)
        {
            Hotel h = (Hotel)hotels[index];
            return h;
        }

        public string GetFullHotelDescription(int index)
        {
            Hotel h = (Hotel)hotels[index];
            // Combine the tag data into a comma-delimited string
            string tagData = string.Join(", ", h.Tags);
            string description = h.Description;

            if (tagData.Length > 0)
            {
                description += "\nHighlights: " + tagData;
            }

            // Get the sample room data and combine into one string.
            var fullDescription = "Sample room: ";
            fullDescription += h.Rooms[0].BedOptions;
            fullDescription += " $" + h.Rooms[0].BaseRate;
            fullDescription += "\n" + description;
            return fullDescription;
        }
    }
}
