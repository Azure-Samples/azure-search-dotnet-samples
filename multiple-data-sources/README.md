# azure-search-multiple-data-sources
A sample C#/.NET application that creates an Azure search index and populates it by combining data from different data sources.

Shorthand instructions for running and testing this code:

* In the Azure Portal, create a Cosmos DB database named "hotel-rooms-db" and a new collection in it called "hotels". In the Cosmos Data Explorer, select the hotels collection, click Upload, and then select the file src/cosmosdb/HotelsDataSubset_CosmosDB.json. This contains data for 7 hotels, but no rooms data.
* In your Azure Storage account, create a new blob storage container named hotel-rooms. Select this container, click Upload, and then upload all of the JSON files in the src/blobs folder, ranging from Rooms1.json through Rooms15.json. These files contain room details for each of the 7 hotels.
* Open the sample solution in Visual Studio.
* Edit the file appsettings.json and fill in the appropriate account names, keys, and connection strings.
* Build and run the app. After a successful run, you should see a new index names hotel-rooms-sample in your Azure Search Service, containing the combined hotel and room data for all 7 hotels.
