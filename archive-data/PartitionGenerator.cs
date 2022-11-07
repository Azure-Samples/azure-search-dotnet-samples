using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static archive_data.Program;

namespace archive_data
{
    public class PartitionGenerator
    {
        private readonly SearchClient _searchClient;
        private readonly SearchField _field;
        private readonly object _lowerBound;
        private readonly object _upperBound;

        private Partition GeneratePartition(object partitionLowerBound, object partitionUpperBound)
        {
            throw new NotImplementedException();
        }


        private SearchOptions CreatePartitionSearchOptions(object partitionLowerBound, object partitionUpperBound)
        {
            var options = new SearchOptions();
            options.IncludeTotalCount = true;
            options.Size = 1;
            string lowerBoundFilter = partitionLowerBound.Equals(_lowerBound) ? "ge" : "gt";
            options.Filter = $"{_field.Name} {lowerBoundFilter} {partitionLowerBound} and {_field.Name} le {partitionUpperBound}";
            return options;
        }

        public static object GetMidpoint(object lowerBound, object upperBound)
        {
            if (lowerBound is DateTimeOffset lowerBoundDate && upperBound is DateTimeOffset upperBoundDate)
            {
                return lowerBoundDate + ((upperBoundDate - lowerBoundDate) / 2);
            }

            throw new InvalidOperationException($"Unknown lower bound type {lowerBound.GetType()}, upper bound type {upperBound.GetType()}");
        }



    }
}
