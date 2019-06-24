namespace AzureSearchQuickstart
{
    using System;
    using System.Text;
    using Newtonsoft.Json;

    public partial class Address
    {
        // This implementation of ToString() is only for the purposes of the sample console application.
        // You can override ToString() in your own model class if you want, but you don't need to in order
        // to use the Azure Search .NET SDK.

        public override string ToString() =>
            IsEmpty ?
                string.Empty :
                $"{StreetAddress}\n{City}, {StateProvince} {PostalCode}\n{Country}";

        [JsonIgnore]
        public bool IsEmpty => String.IsNullOrEmpty(StreetAddress) &&
                               String.IsNullOrEmpty(City) &&
                               String.IsNullOrEmpty(StateProvince) &&
                               String.IsNullOrEmpty(PostalCode) &&
                               String.IsNullOrEmpty(Country);
    }
}