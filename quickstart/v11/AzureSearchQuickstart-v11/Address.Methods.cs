using System;
using System.Text;
using System.Text.Json.Serialization;

namespace AzureSearch.Quickstart
{
    public partial class Address
    {
        // This implementation of ToString() is only for the purposes of the sample console application.
        // You can override ToString() in your own model class if you want, but you don't need to in order
        // to use the Azure Search .NET SDK.
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