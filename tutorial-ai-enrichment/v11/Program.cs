using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrichwithAI
{
    class Program
    {
        public static void Main(string[] args)
        {
            // Create service client
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            string searchServiceUri = configuration["SearchServiceUri"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];
            string cognitiveServicesKey = configuration["CognitiveServicesKey"];

            SearchIndexClient indexClient = new SearchIndexClient(new Uri(searchServiceUri), new AzureKeyCredential(adminApiKey));
            SearchIndexerClient indexerClient = new SearchIndexerClient(new Uri(searchServiceUri), new AzureKeyCredential(adminApiKey));

            // Create or Update the data source
            Console.WriteLine("Creating or updating the data source...");
            SearchIndexerDataSourceConnection dataSource = CreateOrUpdateDataSource(indexerClient, configuration);

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
            List<SearchIndexerSkill> skills = new List<SearchIndexerSkill>();
            skills.Add(ocrSkill);
            skills.Add(mergeSkill);
            skills.Add(languageDetectionSkill);
            skills.Add(splitSkill);
            skills.Add(entityRecognitionSkill);
            skills.Add(keyPhraseExtractionSkill);

            SearchIndexerSkillset skillset = CreateOrUpdateDemoSkillSet(indexerClient, skills, cognitiveServicesKey);

            // Create the index
            Console.WriteLine("Creating the index...");
            SearchIndex demoIndex = CreateDemoIndex(indexClient);

            // Create the indexer, map fields, and execute transformations
            Console.WriteLine("Creating the indexer and executing the pipeline...");
            SearchIndexer demoIndexer = CreateDemoIndexer(indexerClient, dataSource, skillset, demoIndex);

            // Check indexer overall status
            Console.WriteLine("Check the indexer overall status...");
            CheckIndexerOverallStatus(indexerClient, demoIndexer);
        }

        private static SearchIndexerDataSourceConnection CreateOrUpdateDataSource(SearchIndexerClient indexerClient, IConfigurationRoot configuration)
        {
            SearchIndexerDataSourceConnection dataSource = new SearchIndexerDataSourceConnection(
                name: "demodata",
                type: SearchIndexerDataSourceType.AzureBlob,
                connectionString: configuration["AzureBlobConnectionString"],
                container: new SearchIndexerDataContainer("cog-search-demo"))
            {
                Description = "Demo files to demonstrate cognitive search capabilities."
            };

            // The data source does not need to be deleted if it was already created
            // since we are using the CreateOrUpdate method
            try
            {
                indexerClient.CreateOrUpdateDataSourceConnection(dataSource);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create or update the data source\n Exception message: {0}\n", ex.Message);
                ExitProgram("Cannot continue without a data source");
            }

            return dataSource;
        }

        private static OcrSkill CreateOcrSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry("image")
            {
                Source = "/document/normalized_images/*"
            });

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry("text")
            {
                TargetName = "text"
            });

            OcrSkill ocrSkill = new OcrSkill(inputMappings, outputMappings)
            {
                Description = "Extract text (plain and structured) from image",
                Context = "/document/normalized_images/*",
                DefaultLanguageCode = OcrSkillLanguage.En,
                ShouldDetectOrientation = true
            };

            return ocrSkill;
        }

        private static MergeSkill CreateMergeSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry("text")
            {
                Source = "/document/content"
            });
            inputMappings.Add(new InputFieldMappingEntry("itemsToInsert")
            {
                Source = "/document/normalized_images/*/text"
            });
            inputMappings.Add(new InputFieldMappingEntry("offsets")
            {
                Source = "/document/normalized_images/*/contentOffset"
            });

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry("mergedText")
            {
                TargetName = "merged_text"
            });

            MergeSkill mergeSkill = new MergeSkill(inputMappings, outputMappings)
            {
                Description = "Create merged_text which includes all the textual representation of each image inserted at the right location in the content field.",
                Context = "/document",
                InsertPreTag = " ",
                InsertPostTag = " "
            };

            return mergeSkill;
        }

        private static LanguageDetectionSkill CreateLanguageDetectionSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry("text")
            {
                Source = "/document/merged_text"
            });

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry("languageCode")
            {
                TargetName = "languageCode"
            });

            LanguageDetectionSkill languageDetectionSkill = new LanguageDetectionSkill(inputMappings, outputMappings)
            {
                Description = "Detect the language used in the document",
                Context = "/document"
            };

            return languageDetectionSkill;
        }

        private static SplitSkill CreateSplitSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry("text")
            {
                Source = "/document/merged_text"
            });
            inputMappings.Add(new InputFieldMappingEntry("languageCode")
            {
                Source = "/document/languageCode"
            });

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry("textItems")
            {
                TargetName = "pages",
            });

            SplitSkill splitSkill = new SplitSkill(inputMappings, outputMappings)
            {
                Description = "Split content into pages",
                Context = "/document",
                TextSplitMode = TextSplitMode.Pages,
                MaximumPageLength = 4000,
                DefaultLanguageCode = SplitSkillLanguage.En
            };

            return splitSkill;
        }

        private static EntityRecognitionSkill CreateEntityRecognitionSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry("text")
            {
                Source = "/document/pages/*"
            });

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry("organizations")
            {
                TargetName = "organizations"
            });

            EntityRecognitionSkill entityRecognitionSkill = new EntityRecognitionSkill(inputMappings, outputMappings)
            {
                Description = "Recognize organizations",
                Context = "/document/pages/*",
                DefaultLanguageCode = EntityRecognitionSkillLanguage.En
            };
            entityRecognitionSkill.Categories.Add(EntityCategory.Organization);

            return entityRecognitionSkill;
        }

        private static KeyPhraseExtractionSkill CreateKeyPhraseExtractionSkill()
        {
            List<InputFieldMappingEntry> inputMappings = new List<InputFieldMappingEntry>();
            inputMappings.Add(new InputFieldMappingEntry("text")
            {
                Source = "/document/pages/*"
            });
            inputMappings.Add(new InputFieldMappingEntry("languageCode")
            {
                Source = "/document/languageCode"
            });

            List<OutputFieldMappingEntry> outputMappings = new List<OutputFieldMappingEntry>();
            outputMappings.Add(new OutputFieldMappingEntry("keyPhrases")
            {
                TargetName = "keyPhrases"
            });

            KeyPhraseExtractionSkill keyPhraseExtractionSkill = new KeyPhraseExtractionSkill(inputMappings, outputMappings)
            {
                Description = "Extract the key phrases",
                Context = "/document/pages/*",
                DefaultLanguageCode = KeyPhraseExtractionSkillLanguage.En
            };

            return keyPhraseExtractionSkill;
        }

        private static SearchIndexerSkillset CreateOrUpdateDemoSkillSet(SearchIndexerClient indexerClient, IList<SearchIndexerSkill> skills,string cognitiveServicesKey)
        {
            SearchIndexerSkillset skillset = new SearchIndexerSkillset("demoskillset", skills)
            {
                Description = "Demo skillset",
                CognitiveServicesAccount = new CognitiveServicesAccountKey(cognitiveServicesKey)
            };

            // Create the skillset in your search service.
            // The skillset does not need to be deleted if it was already created
            // since we are using the CreateOrUpdate method
            try
            {
                indexerClient.CreateOrUpdateSkillset(skillset);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Failed to create the skillset\n Exception message: {0}\n", ex.Message);
                ExitProgram("Cannot continue without a skillset");
            }

            return skillset;
        }

        private static SearchIndex CreateDemoIndex(SearchIndexClient indexClient)
        {
            FieldBuilder builder = new FieldBuilder();
            var index = new SearchIndex("demoindex")
            {
                Fields = builder.Build(typeof(DemoIndex))
            };

            try
            {
                indexClient.GetIndex(index.Name);
                indexClient.DeleteIndex(index.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                //if the specified index not exist, 404 will be thrown.
            }

            try
            {
                indexClient.CreateIndex(index);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Failed to create the index\n Exception message: {0}\n", ex.Message);
                ExitProgram("Cannot continue without an index");
            }

            return index;
        }

        private static SearchIndexer CreateDemoIndexer(SearchIndexerClient indexerClient, SearchIndexerDataSourceConnection dataSource, SearchIndexerSkillset skillSet, SearchIndex index)
        {
            IndexingParameters indexingParameters = new IndexingParameters()
            {
                MaxFailedItems = -1,
                MaxFailedItemsPerBatch = -1,
            };
            indexingParameters.Configuration.Add("dataToExtract", "contentAndMetadata");
            indexingParameters.Configuration.Add("imageAction", "generateNormalizedImages");

            SearchIndexer indexer = new SearchIndexer("demoindexer", dataSource.Name, index.Name)
            {
                Description = "Demo Indexer",
                SkillsetName = skillSet.Name,
                Parameters = indexingParameters
            };

            FieldMappingFunction mappingFunction = new FieldMappingFunction("base64Encode");
            mappingFunction.Parameters.Add("useHttpServerUtilityUrlTokenEncode", true);

            indexer.FieldMappings.Add(new FieldMapping("metadata_storage_path")
            {
                TargetFieldName = "id",
                MappingFunction = mappingFunction

            });
            indexer.FieldMappings.Add(new FieldMapping("content")
            {
                TargetFieldName = "content"
            });

            indexer.OutputFieldMappings.Add(new FieldMapping("/document/pages/*/organizations/*")
            {
                TargetFieldName = "organizations"
            });
            indexer.OutputFieldMappings.Add(new FieldMapping("/document/pages/*/keyPhrases/*")
            {
                TargetFieldName = "keyPhrases"
            });
            indexer.OutputFieldMappings.Add(new FieldMapping("/document/languageCode")
            {
                TargetFieldName = "languageCode"
            });

            try
            {
                indexerClient.GetIndexer(indexer.Name);
                indexerClient.DeleteIndexer(indexer.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                //if the specified indexer not exist, 404 will be thrown.
            }

            try
            {
                indexerClient.CreateIndexer(indexer);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Failed to create the indexer\n Exception message: {0}\n", ex.Message);
                ExitProgram("Cannot continue without creating an indexer");
            }

            return indexer;
        }

        private static void CheckIndexerOverallStatus(SearchIndexerClient indexerClient, SearchIndexer indexer)
        {
            try
            {
                var demoIndexerExecutionInfo = indexerClient.GetIndexerStatus(indexer.Name);

                switch (demoIndexerExecutionInfo.Value.Status)
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
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Failed to get indexer overall status\n Exception message: {0}\n", ex.Message);
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
