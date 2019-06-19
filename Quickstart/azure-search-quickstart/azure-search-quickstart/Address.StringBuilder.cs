namespace azure_search_quickstart
{
    using System;
    using System.Text;
    using Newtonsoft.Json;

    public partial class Address
    {
        // This implementation of ToString() is used to structure results when writing to the console.
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!IsEmpty)
            {
                builder.AppendFormat("{0}\n{1}, {2} {3}\n{4}", StreetAddress, City, StateProvince, PostalCode, Country);
            }

            return builder.ToString();
        }

        [JsonIgnore]
        public bool IsEmpty => String.IsNullOrEmpty(StreetAddress) &&
                               String.IsNullOrEmpty(City) &&
                               String.IsNullOrEmpty(StateProvince) &&
                               String.IsNullOrEmpty(PostalCode) &&
                               String.IsNullOrEmpty(Country);
    }
}