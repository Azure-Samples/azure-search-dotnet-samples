using System;
using System.Collections.Generic;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using Index = Microsoft.Azure.Search.Models.Index;

namespace EnrichwithAI

{
    class Program
    {
        public static void Main(string[] args)
        {
            // Create service client
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();
            SearchServiceClient serviceClient = CreateSearchServiceClient(configuration);

            // Create or Update the data source
            Console.WriteLine("Creating or updating the data source...");
            DataSource dataSource = CreateOrUpdateDataSource(serviceClient, configuration);

            // Create the skills
            Console.WriteLine("Creating the skills...");
            OcrSkill ocrSkill = CreateOcrSkill();
            MergeSkill mergeSkill = CreateMergeSkill();
            EntityRecognitionSkill entityRecognitionSkill = CreateEntityRecognitionSkill();
            LanguageDetectionSkill languageDetectionSkill = CreateLanguageDetectionSkill();
            SplitSkill splitSkill = CreateSplitSkill();
            KeyPhraseExtractionSkill keyPhraseExtractionSkill = CreateKeyPhraseExtractionSkill();

            // Create the skillset
            Console.WriteLine("Creating or updating the skillset...");
            List<Skill> skills = new List<Skill>();
            skills.Add(ocrSkill);
            skills.Add(mergeSkill);
            skills.Add(languageDetectionSkill);
            skills.Add(splitSkill);
            skills.Add(entityRecognitionSkill);
            skills.Add(keyPhraseExtractionSkill);

            Skillset skillset = CreateOrUpdateDemoSkillSet(serviceClient, skills);

            // Create the index
            Console.WriteLine("Creating the index...");
            Index demoIndex = CreateDemoIndex(serviceClient);

            // Create the indexer, map fields, and execute transformations
            Console.WriteLine("Creating the indexer and executing the pipeline...");
            Indexer demoIndexer = CreateDemoIndexer(serviceClient, dataSource, skillset, demoIndex);

            // Check indexer overall status
            Console.WriteLine("Check the indexer overall status...");
            CheckIndexerOverallStatus(serviceClient, demoIndexer);
        }
        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
        }
        private static DataSource CreateOrUpdateDataSource(SearchServiceClient serviceClient, IConfigurationRoot configuration)
        {
            DataSource dataSource = DataSource.AzureBlobStorage(
                name: "demodata",
                storageConnectionString: configuration["AzureBlobConnectionString"],
                containerName: "cog-search-demo",
                description: "Demo files to demonstrate cognitive search capabilities.");

            // The data source does not need to be deleted if it was already created
            // since we are using the CreateOrUpdate method
            try
            {
                serviceClient.DataSources.CreateOrUpdate(dataSource);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create or update the data source\n Exception message: {0}\n", e.Message);
                ExitProgram("Cannot continue without a data source");
            }

            return dataSource;
        }
        private static OcrSkill CreateOcrSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry(
                name: "image",
                source: "/document/normalized_images/*"));

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry(
                name: "text",
                targetName: "text"));

            OcrSkill ocrSkill = new OcrSkill(
                description: "Extract text (plain and structured) from image",
                context: "/document/normalized_images/*",
                inputs: inputMappings,
                outputs: outputMappings,
                defaultLanguageCode: OcrSkillLanguage.En,
                shouldDetectOrientation: true);

            return ocrSkill;
        }
        private static MergeSkill CreateMergeSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry(
                name: "text",
                source: "/document/content"));
            inputMappings.Add(new InputFieldMappingEntry(
                name: "itemsToInsert",
                source: "/document/normalized_images/*/text"));
            inputMappings.Add(new InputFieldMappingEntry(
                name: "offsets",
                source: "/document/normalized_images/*/contentOffset"));

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry(
                name: "mergedText",
                targetName: "merged_text"));

            MergeSkill mergeSkill = new MergeSkill(
                description: "Create merged_text which includes all the textual representation of each image inserted at the right location in the content field.",
                context: "/document",
                inputs: inputMappings,
                outputs: outputMappings,
                insertPreTag: " ",
                insertPostTag: " ");

            return mergeSkill;
        }
        private static LanguageDetectionSkill CreateLanguageDetectionSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry(
                name: "text",
                source: "/document/merged_text"));

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry(
                name: "languageCode",
                targetName: "languageCode"));

            LanguageDetectionSkill languageDetectionSkill = new LanguageDetectionSkill(
                description: "Detect the language used in the document",
                context: "/document",
                inputs: inputMappings,
                outputs: outputMappings);

            return languageDetectionSkill;
        }
        private static SplitSkill CreateSplitSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();

            inputMappings.Add(new InputFieldMappingEntry(
                name: "text",
                source: "/document/merged_text"));
            inputMappings.Add(new InputFieldMappingEntry(
                name: "languageCode",
                source: "/document/languageCode"));

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry(
                name: "textItems",
                targetName: "pages"));

            SplitSkill splitSkill = new SplitSkill(
                description: "Split content into pages",
                context: "/document",
                inputs: inputMappings,
                outputs: outputMappings,
                textSplitMode: TextSplitMode.Pages,
                maximumPageLength: 4000);

            return splitSkill;
        }
        private static EntityRecognitionSkill CreateEntityRecognitionSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry(
                name: "text",
                source: "/document/pages/*"));

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry(
                name: "organizations",
                targetName: "organizations"));

            List<EntityCategory> entityCategory = new List<EntityCategory>();
            entityCategory.Add(EntityCategory.Organization);

            EntityRecognitionSkill entityRecognitionSkill = new EntityRecognitionSkill(
                description: "Recognize organizations",
                context: "/document/pages/*",
                inputs: inputMappings,
                outputs: outputMappings,
                categories: entityCategory,
                defaultLanguageCode: EntityRecognitionSkillLanguage.En);

            return entityRecognitionSkill;
        }
        private static KeyPhraseExtractionSkill CreateKeyPhraseExtractionSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry(
                name: "text",
                source: "/document/pages/*"));
            inputMappings.Add(new InputFieldMappingEntry(
                name: "languageCode",
                source: "/document/languageCode"));

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry(
                name: "keyPhrases",
                targetName: "keyPhrases"));

            KeyPhraseExtractionSkill keyPhraseExtractionSkill = new KeyPhraseExtractionSkill(
                description: "Extract the key phrases",
                context: "/document/pages/*",
                inputs: inputMappings,
                outputs: outputMappings);

            return keyPhraseExtractionSkill;
        }
        private static Skillset CreateOrUpdateDemoSkillSet(SearchServiceClient serviceClient, IList<Skill> skills)
        {
            Skillset skillset = new Skillset(
                name: "demoskillset",
                description: "Demo skillset",
                skills: skills);

            // Create the skillset in your search service.
            // The skillset does not need to be deleted if it was already created
            // since we are using the CreateOrUpdate method
            try
            {
                serviceClient.Skillsets.CreateOrUpdate(skillset);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create the skillset\n Exception message: {0}\n", e.Message);
                ExitProgram("Cannot continue without a skillset");
            }

            return skillset;
        }
        private static Index CreateDemoIndex(SearchServiceClient serviceClient)
        {
            var index = new Index()
            {
                Name = "demoindex",
                Fields = FieldBuilder.BuildForType<DemoIndex>()
            };

            try
            {
                bool exists = serviceClient.Indexes.Exists(index.Name);

                if (exists)
                {
                    serviceClient.Indexes.Delete(index.Name);
                }

                serviceClient.Indexes.Create(index);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create the index\n Exception message: {0}\n", e.Message);
                ExitProgram("Cannot continue without an index");
            }

            return index;
        }
        private static Indexer CreateDemoIndexer(SearchServiceClient serviceClient, DataSource dataSource, Skillset skillSet, Index index)
        {
            IDictionary<string, object> config = new Dictionary<string, object>();
            config.Add(
                key: "dataToExtract",
                value: "contentAndMetadata");
            config.Add(
                key: "imageAction",
                value: "generateNormalizedImages");

            List<FieldMapping> fieldMappings = new List<FieldMapping>();
            fieldMappings.Add(new FieldMapping(
                sourceFieldName: "metadata_storage_path",
                targetFieldName: "id",
                mappingFunction: new FieldMappingFunction(
                    name: "base64Encode")));
            fieldMappings.Add(new FieldMapping(
                sourceFieldName: "content",
                targetFieldName: "content"));

            List<FieldMapping> outputMappings = new List<FieldMapping>();
            outputMappings.Add(new FieldMapping(
                sourceFieldName: "/document/pages/*/organizations/*",
                targetFieldName: "organizations"));
            outputMappings.Add(new FieldMapping(
                sourceFieldName: "/document/pages/*/keyPhrases/*",
                targetFieldName: "keyPhrases"));
            outputMappings.Add(new FieldMapping(
                sourceFieldName: "/document/languageCode",
                targetFieldName: "languageCode"));

            Indexer indexer = new Indexer(
                name: "demoindexer",
                dataSourceName: dataSource.Name,
                targetIndexName: index.Name,
                description: "Demo Indexer",
                skillsetName: skillSet.Name,
                parameters: new IndexingParameters(
                    maxFailedItems: -1,
                    maxFailedItemsPerBatch: -1,
                    configuration: config),
                fieldMappings: fieldMappings,
                outputFieldMappings: outputMappings);

            try
            {
                bool exists = serviceClient.Indexers.Exists(indexer.Name);

                if (exists)
                {
                    serviceClient.Indexers.Delete(indexer.Name);
                }

                serviceClient.Indexers.Create(indexer);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create the indexer\n Exception message: {0}\n", e.Message);
                ExitProgram("Cannot continue without creating an indexer");
            }

            return indexer;
        }
        private static void CheckIndexerOverallStatus(SearchServiceClient serviceClient, Indexer indexer)
        {
            try
            {
                IndexerExecutionInfo demoIndexerExecutionInfo = serviceClient.Indexers.GetStatus(indexer.Name);

                switch (demoIndexerExecutionInfo.Status)
                {
                    case IndexerStatus.Error:
                        ExitProgram("Indexer has error status. Check the Azure Portal to further understand the error.");
                        break;
                    case IndexerStatus.Running:
                        Console.WriteLine("Indexer is running");
                        break;
                    case IndexerStatus.Unknown:
                        Console.WriteLine("Indexer status is unknown");
                        break;
                    default:
                        Console.WriteLine("No indexer information");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get indexer overall status\n Exception message: {0}\n", e.Message);
            }
        }

        private static void ExitProgram(string message)
        {
            Console.WriteLine("{0}", message);
            Console.WriteLine("Press any key to exit the program...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
