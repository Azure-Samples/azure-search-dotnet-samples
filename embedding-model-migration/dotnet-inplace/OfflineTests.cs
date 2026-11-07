using System.Text.Json.Nodes;

namespace DotnetInPlaceMigration;

internal static class OfflineTests
{
    private static int _pass;
    private static int _total;

    public static void Run()
    {
        TestRejectsNormalizedKey();
        TestRejectsEqualSourceAndDestinationFields();
        TestAcceptsExistingDestinationField();
        TestRejectsMarkerAlias();
        TestAddsDestinationFieldAndVectorizer();
        TestAddsMarkerFieldWhenConfigured();
        Console.WriteLine($"PASS: {_pass}/{_total}");
    }

    private static void TestRejectsNormalizedKey()
    {
        _total++;
        var schema = MinimalSchema();
        schema["fields"]![0]!["normalizer"] = "lowercase";
        try
        {
            _ = SchemaPlanner.Build(schema, SettingsForTests());
            throw new InvalidOperationException("Expected normalized key rejection.");
        }
        catch (InvalidDataException)
        {
            _pass++;
        }
    }

    private static void TestRejectsEqualSourceAndDestinationFields()
    {
        _total++;
        try
        {
            _ = SchemaPlanner.Build(MinimalSchema(), SettingsForTests(sourceVectorField: "contentVector", newVectorField: "contentVector"));
            throw new InvalidOperationException("Expected equal source/destination rejection.");
        }
        catch (InvalidDataException)
        {
            _pass++;
        }
    }

    private static void TestAcceptsExistingDestinationField()
    {
        _total++;
        var schema = MinimalSchema();
        schema["fields"]!.AsArray().Add(new JsonObject
        {
            ["name"] = "contentVectorV2",
            ["type"] = "Collection(Edm.Single)",
            ["searchable"] = true,
            ["retrievable"] = true,
            ["stored"] = true,
            ["dimensions"] = 1024,
            ["vectorSearchProfile"] = "migration-contentVectorV2-profile"
        });
        schema["fields"]!.AsArray().Add(new JsonObject
        {
            ["name"] = "migrationMarker",
            ["type"] = "Edm.String",
            ["filterable"] = true,
            ["retrievable"] = true,
            ["searchable"] = false
        });
        schema["vectorSearch"]!["profiles"]!.AsArray().Add(new JsonObject
        {
            ["name"] = "migration-contentVectorV2-profile",
            ["algorithm"] = "hnsw-default",
            ["vectorizer"] = "migration-contentVectorV2-vectorizer"
        });
        schema["vectorSearch"]!["vectorizers"]!.AsArray().Add(new JsonObject
        {
            ["name"] = "migration-contentVectorV2-vectorizer",
            ["kind"] = "customWebApi",
            ["customWebApiParameters"] = new JsonObject
            {
                ["uri"] = "https://unit-func.azurewebsites.net/api/embed/fast/query",
                ["httpMethod"] = "POST",
                ["authResourceId"] = "api://00000000-0000-0000-0000-000000000000"
            }
        });
        var plan = SchemaPlanner.Build(schema, SettingsForTests());
        Assert(plan.RequiresSchemaUpdate == false, "Schema update should not be required when destination field already exists.");
        _pass++;
    }

    private static void TestRejectsMarkerAlias()
    {
        _total++;
        try
        {
            _ = SchemaPlanner.Build(MinimalSchema(), SettingsForTests(markerField: "id"));
            throw new InvalidOperationException("Expected marker alias rejection.");
        }
        catch (InvalidDataException)
        {
            _pass++;
        }
    }

    private static void TestAddsDestinationFieldAndVectorizer()
    {
        _total++;
        var plan = SchemaPlanner.Build(MinimalSchema(), SettingsForTests());
        var fields = plan.UpdatedSchema["fields"]!.AsArray();
        var newField = fields.Single(f => f?["name"]?.GetValue<string>() == "contentVectorV2");
        var vectorizers = plan.UpdatedSchema["vectorSearch"]?["vectorizers"]?.AsArray()
            ?? throw new InvalidDataException("Vectorizers missing.");
        var profile = plan.UpdatedSchema["vectorSearch"]?["profiles"]?.AsArray()
            ?.Single(p => p?["name"]?.GetValue<string>() == "migration-contentVectorV2-profile")
            ?? throw new InvalidDataException("Profile missing.");

        Assert(newField?["dimensions"]?.GetValue<int>() == 1024, "Dimensions should be updated.");
        Assert(profile["vectorizer"]?.GetValue<string>() == "migration-contentVectorV2-vectorizer", "Profile should point to new vectorizer.");
        Assert(vectorizers.Any(v => v?["name"]?.GetValue<string>() == "migration-contentVectorV2-vectorizer"), "Vectorizer should be added.");
        _pass++;
    }

    private static void TestAddsMarkerFieldWhenConfigured()
    {
        _total++;
        var plan = SchemaPlanner.Build(MinimalSchema(), SettingsForTests(markerField: "migrationMarker"));
        var marker = plan.UpdatedSchema["fields"]!.AsArray().Single(f => f?["name"]?.GetValue<string>() == "migrationMarker");
        Assert(marker?["type"]?.GetValue<string>() == "Edm.String", "Marker field must be Edm.String.");
        Assert(marker?["filterable"]?.GetValue<bool>() == true, "Marker field must be filterable.");
        _pass++;
    }

    private static Settings SettingsForTests(
        string markerField = "migrationMarker",
        string sourceVectorField = "contentVector",
        string newVectorField = "contentVectorV2") =>
        new(
            new Uri("https://unit.search.windows.net/"),
            "products-v1",
            "2025-09-01",
            "content",
            sourceVectorField,
            newVectorField,
            1024,
            "fast",
            new Uri("https://unit-func.azurewebsites.net/"),
            "api://00000000-0000-0000-0000-000000000000",
            4,
            240,
            "query",
            markerField,
            "contentVectorV2:fast:1024",
            true);

    private static JsonObject MinimalSchema() => JsonNode.Parse("""
{
  "name": "products-v1",
  "fields": [
    {
      "name": "id",
      "type": "Edm.String",
      "key": true,
      "filterable": true,
      "sortable": true,
      "retrievable": true
    },
    {
      "name": "content",
      "type": "Edm.String",
      "searchable": true,
      "retrievable": true
    },
    {
      "name": "contentVector",
      "type": "Collection(Edm.Single)",
      "searchable": true,
      "retrievable": true,
      "dimensions": 1536,
      "vectorSearchProfile": "profile-v1"
    }
  ],
  "vectorSearch": {
    "algorithms": [
      { "name": "hnsw-default", "kind": "hnsw" }
    ],
    "profiles": [
      {
        "name": "profile-v1",
        "algorithm": "hnsw-default",
        "vectorizer": "vectorizer-v1"
      }
    ],
    "vectorizers": [
      {
        "name": "vectorizer-v1",
        "kind": "customWebApi",
        "customWebApiParameters": {
          "uri": "https://old-func.azurewebsites.net/api/embed/fast/query",
          "httpMethod": "POST",
          "authResourceId": "api://11111111-1111-1111-1111-111111111111"
        }
      }
    ]
  }
}
""")!.AsObject();

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
