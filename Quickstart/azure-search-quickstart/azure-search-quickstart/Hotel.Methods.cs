namespace azure_search_quickstart
{
    using System;
    using System.Text;

    public partial class Hotel
    {
        // This implementation of ToString() is used to structure results when writing to the console.
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!String.IsNullOrEmpty(HotelId))
            {
                builder.AppendFormat("HotelId: {0}\n", HotelId);
            }

            if (!String.IsNullOrEmpty(HotelName))
            {
                builder.AppendFormat("Name: {0}\n", HotelName);
            }

            if (!String.IsNullOrEmpty(Description))
            {
                builder.AppendFormat("Description: {0}\n", Description);
            }

            if (!String.IsNullOrEmpty(DescriptionFr))
            {
                builder.AppendFormat("Description (French): {0}\n", DescriptionFr);
            }

            if (!String.IsNullOrEmpty(Category))
            {
                builder.AppendFormat("Category: {0}\n", Category);
            }

            if (Tags != null && Tags.Length > 0)
            {
                builder.AppendFormat("Tags: [ {0} ]\n", String.Join(", ", Tags));
            }

            if (ParkingIncluded.HasValue)
            {
                builder.AppendFormat("Parking included: {0}\n", ParkingIncluded.Value ? "yes" : "no");
            }

            if (LastRenovationDate.HasValue)
            {
                builder.AppendFormat("Last renovated on: {0}\n", LastRenovationDate);
            }

            if (Rating.HasValue)
            {
                builder.AppendFormat("Rating: {0}\n", Rating);
            }

            if (Address != null && !Address.IsEmpty)
            {
                builder.AppendFormat("Address: \n{0}\n", Address.ToString());
            }

            return builder.ToString();
        }
    }
}
