# Provide variables
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential, get_bearer_token_provider
import os

load_dotenv(override=True) # Take environment variables from .env.

# The following variables from your .env file are used in this notebook
search_endpoint = os.environ["AZURE_SEARCH_ENDPOINT"]
credential = DefaultAzureCredential()
token_provider = get_bearer_token_provider(credential, "https://search.azure.com/.default")
index_name = os.getenv("AZURE_SEARCH_INDEX", "hotels-sample-index")

from azure.search.documents.indexes import SearchIndexClient
from azure.search.documents import SearchClient
from azure.search.documents.indexes.models import (
    ComplexField,
    SimpleField,
    SearchFieldDataType,
    SearchableField,
    SearchIndex,
    SemanticConfiguration,
    SemanticField,
    SemanticPrioritizedFields,
    SemanticSearch
)

# Update search schema
index_client = SearchIndexClient(
    endpoint=search_endpoint, credential=credential)
fields = [
        SimpleField(name="HotelId", type=SearchFieldDataType.String, key=True, facetable=True, filterable=True, sortable=False),
        SearchableField(name="HotelName", type=SearchFieldDataType.String, facetable=False, filterable=False, sortable=False, retrievable=True, analyzer_name="en.microsoft"),
        SearchableField(name="Description", type=SearchFieldDataType.String, analyzer_name="en.microsoft"),
        SearchableField(name="Description_fr", type=SearchFieldDataType.String, analyzer_name="fr.microsoft"),
        SearchableField(name="Category", type=SearchFieldDataType.String, facetable=True, filterable=True, sortable=False, analyzer_name="en.microsoft"),

        SearchableField(name="Tags", collection=True, type=SearchFieldDataType.String, facetable=True, filterable=True, sortable=False, analyzer_name="en.microsoft"),

        SimpleField(name="ParkingIncluded", type=SearchFieldDataType.Boolean, facetable=True, filterable=True, sortable=False),
        SimpleField(name="LastRenovationDate", type=SearchFieldDataType.DateTimeOffset, facetable=False, filterable=False, sortable=True),
        SimpleField(name="Rating", type=SearchFieldDataType.Double, facetable=True, filterable=True, sortable=True),

        ComplexField(name="Address", fields=[
            SearchableField(name="StreetAddress", type=SearchFieldDataType.String, facetable=False, filterable=False, sortable=False, analyzer_name="en.microsoft"),
            SearchableField(name="City", type=SearchFieldDataType.String, facetable=True, filterable=True, sortable=False, analyzer_name="en.microsoft"),
            SearchableField(name="StateProvince", type=SearchFieldDataType.String, facetable=True, filterable=True, sortable=False, analyzer_name="en.microsoft"),
            SearchableField(name="PostalCode", type=SearchFieldDataType.String, facetable=True, filterable=True, sortable=False, analyzer_name="en.microsoft"),
            SearchableField(name="Country", type=SearchFieldDataType.String, facetable=True, filterable=True, sortable=False, analyzer_name="en.microsoft"),
        ])        ,
        SimpleField(name="Location", type=SearchFieldDataType.GeographyPoint, facetable=False, filterable=True, sortable=True),
        ComplexField(name="Rooms",collection=True,fields=[
                SearchableField(name="Description", type=SearchFieldDataType.String, analyzer_name="en.microsoft"),
                SearchableField(name="Description_fr", type=SearchFieldDataType.String, analyzer_name="fr.microsoft"),
                SearchableField(name="Type", type=SearchFieldDataType.String, analyzer_name="en.microsoft", facetable=True, filterable=True),
                SimpleField(name="BaseRate", type=SearchFieldDataType.Double, facetable=True, filterable=True),
                SearchableField(name="BedOptions", type=SearchFieldDataType.String, analyzer_name="en.microsoft", facetable=True, filterable=True),
                SimpleField(name="SleepsCount", type=SearchFieldDataType.Int64, facetable=True, filterable=True),
                SimpleField(name="SmokingAllowed", type=SearchFieldDataType.Boolean, facetable=True, filterable=True),
                SearchableField(name="Tags", collection=True, type=SearchFieldDataType.String, analyzer_name="en.microsoft", facetable=True, filterable=True)
            ]
        ),
        SimpleField(name="id", type=SearchFieldDataType.String, searchable=False, retrievable=False, facetable=False, filterable=False, sortable=False), 
        SimpleField(name="rid", type=SearchFieldDataType.String, searchable=False, retrievable=False, facetable=False, filterable=False, sortable=False)
        ]

semantic_config = SemanticConfiguration(
    name="semantic-config",
    prioritized_fields=SemanticPrioritizedFields(
        title_field=SemanticField(field_name="HotelName"),
        keywords_fields=[SemanticField(field_name="Category")],
        content_fields=[SemanticField(field_name="Description")]
    )
)

# Specify the semantic settings with the configuration
semantic_search = SemanticSearch(configurations=[semantic_config])

semantic_settings = SemanticSearch(configurations=[semantic_config])
scoring_profiles = []
suggester = [{'name': 'sg', 'source_fields': ['Rooms/Tags', 'Rooms/Type', 'Address/City', 'Address/Country']}]

# Update the search index with the semantic settings
index = SearchIndex(name=index_name, fields=fields, suggesters=suggester, scoring_profiles=scoring_profiles, semantic_search=semantic_search)
result = index_client.create_or_update_index(index)
print(f' {result.name} updated')

# Run an empty query (returns selected fields, all documents, no ranking, search score is uniform 1.0)
search_client = SearchClient(endpoint=search_endpoint,
                      index_name=index_name,
                      credential=credential)

results =  search_client.search(query_type='simple',
    search_text="*" ,
    select='HotelName,Description',
    include_total_count=True)

print ('Total Documents Matching Query:', results.get_count())
for result in results:
    print(result["@search.score"])
    print(result["HotelName"])
    print(f"Description: {result['Description']}")

# Run a text query (returns a BM25-scored result set)
results =  search_client.search(query_type='simple',
    search_text="walk to restaurants and shopping" ,
    select='HotelName,HotelId,Description',
    include_total_count=True)
    
for result in results:
    print(result["@search.score"])
    print(result["HotelName"])
    print(f"Description: {result['Description']}")

# Runs a semantic query (runs a BM25-ranked query and promotes the most relevant matches to the top)
results =  search_client.search(query_type='semantic', semantic_configuration_name='semantic-config',
    search_text="walk to restaurants and shopping", 
    select='HotelName,Description,Category', query_caption='extractive')

for result in results:
    print(result["@search.reranker_score"])
    print(result["HotelName"])
    print(f"Description: {result['Description']}")

    captions = result["@search.captions"]
    if captions:
        caption = captions[0]
        if caption.highlights:
            print(f"Caption: {caption.highlights}\n")
        else:
            print(f"Caption: {caption.text}\n")

# Run a semantic query that returns semantic answers  
results =  search_client.search(query_type='semantic', semantic_configuration_name='semantic-config',
 search_text="what's a good hotel for people who like to read",
 select='HotelName,Description,Category', query_caption='extractive', query_answer="extractive",)

semantic_answers = results.get_answers()
for answer in semantic_answers:
    if answer.highlights:
        print(f"Semantic Answer: {answer.highlights}")
    else:
        print(f"Semantic Answer: {answer.text}")
    print(f"Semantic Answer Score: {answer.score}\n")

for result in results:
    print(result["@search.reranker_score"])
    print(result["HotelName"])
    print(f"Description: {result['Description']}")

    captions = result["@search.captions"]
    if captions:
        caption = captions[0]
        if caption.highlights:
            print(f"Caption: {caption.highlights}\n")
        else:
            print(f"Caption: {caption.text}\n")