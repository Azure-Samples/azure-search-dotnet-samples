using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;

namespace export_data
{
    /// <summary>
    /// Base class for exporting data from an index
    /// </summary>
    public abstract class Exporter
    {
        /// <summary>
        /// What fields to include in the exported data
        /// </summary>
        protected IEnumerable<string> FieldsToInclude { get; }

        /// <summary>
        /// What fields to exclude from the exported data
        /// </summary>
        protected ISet<string> FieldsToExclude { get; }

        protected SearchIndex Index { get; }

        public Exporter(SearchIndex index, IEnumerable<string> fieldsToInclude, ISet<string> fieldsToExclude)
        {
            Index = index;
            FieldsToInclude = fieldsToInclude;
            FieldsToExclude = fieldsToExclude;
        }

        // Export data from the search index
        public abstract Task ExportAsync();

        // Update the $select clause in the query to pick the fields requested
        // Learn more at https://learn.microsoft.com/azure/search/search-query-odata-select
        protected void AddSelect(SearchOptions options, params string[] requiredFields)
        {
            if (FieldsToInclude?.Any() ?? false)
            {
                foreach (string field in FieldsToInclude)
                {
                    options.Select.Add(field);
                }
            }
            else if (FieldsToExclude?.Any() ?? false)
            {
                foreach (string field in Index.Fields.Select(field => field.Name))
                {
                    if (!FieldsToExclude.Contains(field))
                    {
                        options.Select.Add(field);
                    }
                }
            }

            // If there are any required fields and we have specified either included or excluded fields,
            // ensure the required fields are present
            if (requiredFields.Length > 0 && options.Select.Any())
            {
                foreach (string requiredField in requiredFields)
                {
                    if (!options.Select.Contains(requiredField))
                    {
                        options.Select.Add(requiredField);
                    }
                }
            }
        }
    }
}
