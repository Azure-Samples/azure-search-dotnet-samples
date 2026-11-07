// Copyright Microsoft. Licensed under the MIT license in ..\dotnet-fixed\LICENSE.txt.
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Azure.Core;
using AzureSearchBackupRestoreIndex;

namespace DotnetInPlaceMigration;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args is ["--self-test"])
            {
                OfflineTests.Run();
                return 0;
            }
            if (args is ["--help"])
            {
                Console.WriteLine("migrate | --self-test. Configure process environment using settings.example.ps1.");
                return 0;
            }
            if (args.Length != 1 || args[0] != "migrate")
                throw new InvalidOperationException("Usage: dotnet run -- migrate|--self-test|--help.");

            var settings = Settings.FromEnvironment();
            var credential = IdentityConfiguration.Create();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds) };
            using var search = new SearchApi(http, settings.SearchEndpoint, settings.IndexName, settings.ApiVersion, credential);
            var schema = await search.GetSchema();
            var plan = SchemaPlanner.Build(schema, settings);

            var beforeCount = await search.Count();
            var beforeSample = await search.TopDocuments(plan.KeyField, settings.TextField, 3);
            Console.WriteLine($"Before: index={settings.IndexName}, count={beforeCount}, sampleKeys={string.Join(", ", beforeSample)}");

            if (plan.RequiresSchemaUpdate)
                await search.UpdateSchema(plan.UpdatedSchema);

            var pendingBefore = await search.CountPending(settings.MigrationMarkerField, settings.MigrationMarkerValue);
            Console.WriteLine($"Backfill target set: pendingBackfill={pendingBefore} for field '{settings.NewVectorField}'.");

            var embedder = new FunctionEmbedder(
                http, credential, settings.FunctionBaseEndpoint, settings.FunctionAudience,
                settings.ModelVariant, settings.Dimensions, settings.BatchSize);
            await InPlaceMigrator.Backfill(search, plan, settings, embedder);

            var afterCount = await search.Count();
            var pendingAfter = await search.CountPending(settings.MigrationMarkerField, settings.MigrationMarkerValue);
            var afterSample = await search.TopDocuments(plan.KeyField, settings.NewVectorField, 3);
            Console.WriteLine($"After: index={settings.IndexName}, count={afterCount}, pendingBackfill={pendingAfter}, sampleKeysWithNewField={string.Join(", ", afterSample)}");

            var queryHits = await search.TextVectorQuery(plan.KeyField, settings.NewVectorField, settings.QueryProbeText, 3);
            Console.WriteLine($"After query probe: returned={queryHits} results using field '{settings.NewVectorField}'.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAILED: {ex.Message}");
            return 1;
        }
    }
}

internal sealed record Settings(
    Uri SearchEndpoint,
    string IndexName,
    string ApiVersion,
    string TextField,
    string SourceVectorField,
    string NewVectorField,
    int Dimensions,
    string ModelVariant,
    Uri FunctionBaseEndpoint,
    string FunctionAudience,
    int BatchSize,
    int HttpTimeoutSeconds,
    string QueryProbeText,
    string MigrationMarkerField,
    string MigrationMarkerValue,
    bool SourceWritesFrozen)
{
    public static Settings FromEnvironment()
    {
        var endpoint = HttpsRoot(Required("SEARCH_ENDPOINT"));
        var indexName = Required("INDEX_NAME");
        if (!AzureSearchBackupRestoreIndex.Program.IsValidIndexName(indexName))
            throw new InvalidOperationException("INDEX_NAME is not a valid Azure AI Search index name.");

        var model = Required("MODEL_VARIANT");
        if (model is not ("fast" or "pro"))
            throw new InvalidOperationException("MODEL_VARIANT must be fast or pro.");

        var dimensions = Integer(Required("VECTOR_DIMENSIONS"), "VECTOR_DIMENSIONS", 1, 65536);
        var newVectorField = Required("NEW_VECTOR_FIELD");
        var markerField = Required("MIGRATION_MARKER_FIELD");

        var sourceFrozen = Environment.GetEnvironmentVariable("SOURCE_WRITES_FROZEN");
        if (!bool.TryParse(sourceFrozen, out var frozen) || !frozen)
            throw new InvalidOperationException("Set SOURCE_WRITES_FROZEN=true only after pausing ingestion and updates for the source index.");

        var audience = Required("FUNCTION_AUTH_RESOURCE_ID").TrimEnd('/');
        if ((!audience.StartsWith("api://", StringComparison.Ordinal) &&
             !audience.StartsWith("https://", StringComparison.Ordinal)) ||
            audience.EndsWith("/.default", StringComparison.Ordinal))
            throw new InvalidOperationException("FUNCTION_AUTH_RESOURCE_ID must be the Entra audience, not a token scope.");

        return new Settings(
            endpoint,
            indexName,
            Required("SEARCH_API_VERSION"),
            Required("TEXT_FIELD"),
            Required("SOURCE_VECTOR_FIELD"),
            newVectorField,
            dimensions,
            model,
            HttpsRoot(Required("FUNCTION_BASE_ENDPOINT")),
            audience,
            Integer(Environment.GetEnvironmentVariable("EMBEDDING_BATCH_SIZE") ?? "4", "EMBEDDING_BATCH_SIZE", 1, 16),
            Integer(Environment.GetEnvironmentVariable("FUNCTION_HTTP_TIMEOUT_SECONDS") ?? "240", "FUNCTION_HTTP_TIMEOUT_SECONDS", 120, 1800),
            Environment.GetEnvironmentVariable("QUERY_PROBE_TEXT") ?? "How do I recover my account?",
            markerField,
            Environment.GetEnvironmentVariable("MIGRATION_MARKER_VALUE")?.Trim()
                ?? $"{newVectorField}:{model}:{dimensions}",
            SourceWritesFrozen: true);
    }

    private static int Integer(string value, string name, int min, int max)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ||
            parsed < min || parsed > max)
            throw new InvalidOperationException($"{name} must be an integer in range [{min}, {max}].");
        return parsed;
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Set environment variable {name}.");
        return value.Trim();
    }

    private static Uri HttpsRoot(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/")
            throw new InvalidOperationException("Endpoint must be an HTTPS root URL without credentials, query, or fragment.");
        return uri;
    }
}

internal sealed class SearchApi(HttpClient http, Uri endpoint, string indexName, string apiVersion, TokenCredential credential) : IDisposable
{
    private const int MaxBatchBytes = 16 * 1024 * 1024;
    private readonly HttpClient _http = http;
    private readonly Uri _endpoint = endpoint;
    private readonly string _index = indexName;
    private readonly string _version = apiVersion;
    private readonly TokenCredential _credential = credential;

    public void Dispose() { }

    public async Task<JsonObject> GetSchema() => (await Send(HttpMethod.Get, IndexPath())).AsObject();
    public async Task UpdateSchema(JsonObject schema) => _ = await Send(HttpMethod.Put, IndexPath(), schema);
    public async Task<long> Count() => (await Send(HttpMethod.Get, IndexPath() + "/docs/$count")).GetValue<long>();

    public async Task<List<string>> TopDocuments(string keyField, string field, int top)
    {
        var body = new JsonObject
        {
            ["search"] = "*",
            ["select"] = $"{keyField},{field}",
            ["top"] = top
        };
        var response = await Send(HttpMethod.Post, IndexPath() + "/docs/search", body);
        return response["value"]?.AsArray().Select(doc => doc?[keyField]?.GetValue<string>() ?? "<missing>").ToList() ?? [];
    }

    public async Task<int> TextVectorQuery(string keyField, string vectorField, string text, int k)
    {
        var body = new JsonObject
        {
            ["top"] = k,
            ["select"] = keyField,
            ["vectorQueries"] = new JsonArray
            {
                new JsonObject
                {
                    ["kind"] = "text",
                    ["text"] = text,
                    ["fields"] = vectorField,
                    ["k"] = k
                }
            }
        };
        var response = await Send(HttpMethod.Post, IndexPath() + "/docs/search", body);
        return response["value"]?.AsArray().Count ?? 0;
    }

    public async Task<JsonArray> SearchPage(
        string keyField,
        string[] selectFields,
        string? lastKey,
        string? extraFilter,
        int top)
    {
        var body = new JsonObject
        {
            ["search"] = "*",
            ["select"] = string.Join(",", selectFields),
            ["orderby"] = $"{keyField} asc",
            ["top"] = top
        };
        var filters = new List<string>();
        if (lastKey != null)
            filters.Add($"{keyField} gt '{EscapeOData(lastKey)}'");
        if (!string.IsNullOrWhiteSpace(extraFilter))
            filters.Add(extraFilter);
        if (filters.Count > 0)
            body["filter"] = string.Join(" and ", filters);
        var response = await Send(HttpMethod.Post, IndexPath() + "/docs/search", body);
        return response["value"]?.AsArray() ?? throw new InvalidDataException("Search returned no value array.");
    }

    public async Task<long> CountPending(string markerField, string markerValue)
    {
        var body = new JsonObject
        {
            ["search"] = "*",
            ["count"] = true,
            ["top"] = 0,
            ["filter"] = $"{markerField} ne '{EscapeOData(markerValue)}'"
        };
        var response = await Send(HttpMethod.Post, IndexPath() + "/docs/search", body);
        return response["@odata.count"]?.GetValue<long>()
            ?? throw new InvalidDataException("Search count query did not return @odata.count.");
    }

    public async Task MergeUpload(JsonArray actions)
    {
        foreach (var batch in SplitIndexBatches(actions))
        {
            var response = await Send(HttpMethod.Post, IndexPath() + "/docs/index", new JsonObject { ["value"] = batch });
            var statuses = response["value"]?.AsArray() ?? throw new InvalidDataException("Upload returned no per-document results.");
            if (statuses.Any(s => s?["status"]?.GetValue<bool>() != true))
                throw new InvalidOperationException("At least one merge update document failed.");
        }
    }

    private static IEnumerable<JsonArray> SplitIndexBatches(JsonArray actions)
    {
        var current = new JsonArray();
        foreach (var node in actions)
        {
            var candidate = node?.DeepClone() ?? throw new InvalidDataException("Upload action cannot be null.");
            current.Add(candidate);
            if (PayloadSize(current) <= MaxBatchBytes)
                continue;

            if (current.Count == 1)
                throw new InvalidOperationException("A single update action exceeds Search's 16 MiB indexing request limit.");

            current.RemoveAt(current.Count - 1);
            yield return current;
            current = new JsonArray { candidate };
            if (PayloadSize(current) > MaxBatchBytes)
                throw new InvalidOperationException("A single update action exceeds Search's 16 MiB indexing request limit.");
        }

        if (current.Count > 0)
            yield return current;
    }

    private static int PayloadSize(JsonArray batch) =>
        Encoding.UTF8.GetByteCount(new JsonObject { ["value"] = batch }.ToJsonString());

    private string IndexPath() => $"indexes('{_index}')";
    private static string EscapeOData(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private async Task<JsonNode> Send(HttpMethod method, string relativePath, JsonNode? body = null)
    {
        var uri = new Uri(_endpoint, $"{relativePath}?api-version={Uri.EscapeDataString(_version)}");
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, uri);
            var token = await _credential.GetTokenAsync(new TokenRequestContext(["https://search.azure.com/.default"]), default);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            if (body != null)
                request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request);
            if ((response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout ||
                 (int)response.StatusCode is >= 500 and <= 599) && attempt < 4)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(16, 1 << attempt)));
                continue;
            }
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Search {method} {relativePath} failed (HTTP {(int)response.StatusCode}).");

            var text = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(text))
                return new JsonObject();
            return JsonNode.Parse(text) ?? throw new InvalidDataException("Search returned invalid JSON.");
        }
    }
}

internal sealed class FunctionEmbedder(HttpClient http, TokenCredential credential, Uri functionBase, string audience, string model, int dimensions, int batchSize)
{
    public int BatchSize { get; } = batchSize;

    public async Task<Dictionary<string, JsonArray>> EmbedDocuments(IReadOnlyList<(string Key, string Text)> items)
    {
        var payload = new JsonObject
        {
            ["values"] = new JsonArray(items.Select(item =>
                (JsonNode)new JsonObject
                {
                    ["recordId"] = item.Key,
                    ["data"] = new JsonObject { ["text"] = item.Text }
                }).ToArray())
        };
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(functionBase, $"api/embed/{model}/document"));
            var token = await credential.GetTokenAsync(new TokenRequestContext([$"{audience}/.default"]), default);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request);
            if ((response.StatusCode is HttpStatusCode.TooManyRequests || (int)response.StatusCode is >= 500 and <= 599) && attempt < 4)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(16, 1 << attempt)));
                continue;
            }
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Embedding Function failed (HTTP {(int)response.StatusCode}).");

            var root = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            var values = root?["values"]?.AsArray() ?? throw new InvalidDataException("Embedding response missing values.");
            var vectors = new Dictionary<string, JsonArray>(StringComparer.Ordinal);
            foreach (var value in values)
            {
                var id = value?["recordId"]?.GetValue<string>();
                var vector = value?["data"]?["vector"]?.AsArray();
                if (id == null || vector == null || value?["errors"] is JsonArray { Count: > 0 })
                    throw new InvalidDataException("Embedding response contains malformed records.");
                vectors[id] = ValidateVector(vector);
            }
            if (vectors.Count != items.Count)
                throw new InvalidDataException("Embedding response record count mismatch.");
            return vectors;
        }
    }

    private JsonArray ValidateVector(JsonArray input)
    {
        if (input.Count != dimensions)
            throw new InvalidDataException("Embedding vector dimension mismatch.");
        var result = new JsonArray();
        foreach (var item in input)
        {
            var number = item?.GetValue<double>() ?? throw new InvalidDataException("Embedding vector contains null.");
            if (!double.IsFinite(number) || Math.Abs(number) > float.MaxValue)
                throw new InvalidDataException("Embedding vector contains invalid Edm.Single values.");
            result.Add((float)number);
        }
        return result;
    }
}

internal sealed record SchemaPlan(
    string KeyField,
    string TextField,
    string SourceVectorField,
    string NewVectorField,
    int Dimensions,
    JsonObject UpdatedSchema,
    bool RequiresSchemaUpdate);

internal static class SchemaPlanner
{
    public static SchemaPlan Build(JsonObject schema, Settings settings)
    {
        var fields = schema["fields"]?.AsArray() ?? throw new InvalidDataException("Schema missing fields.");
        var key = fields.SingleOrDefault(f => f?["key"]?.GetValue<bool>() == true)
            ?? throw new InvalidDataException("Schema key field missing.");
        var keyName = key["name"]?.GetValue<string>() ?? throw new InvalidDataException("Schema key name missing.");
        if (key["type"]?.GetValue<string>() != "Edm.String" ||
            key["filterable"]?.GetValue<bool>() != true ||
            key["sortable"]?.GetValue<bool>() != true ||
            key["retrievable"]?.GetValue<bool>() == false ||
            key["normalizer"] != null)
            throw new InvalidDataException("In-place migration requires a non-normalized, sortable, filterable, retrievable Edm.String key.");

        if (settings.SourceVectorField == settings.NewVectorField)
            throw new InvalidDataException("SOURCE_VECTOR_FIELD and NEW_VECTOR_FIELD must be different.");
        if (settings.MigrationMarkerField == keyName ||
            settings.MigrationMarkerField == settings.TextField ||
            settings.MigrationMarkerField == settings.SourceVectorField ||
            settings.MigrationMarkerField == settings.NewVectorField)
            throw new InvalidDataException("MIGRATION_MARKER_FIELD must not alias key/text/source-vector/destination-vector fields.");

        var text = fields.SingleOrDefault(f => f?["name"]?.GetValue<string>() == settings.TextField)
            ?? throw new InvalidDataException($"Text field '{settings.TextField}' not found.");
        if (text["type"]?.GetValue<string>() != "Edm.String")
            throw new InvalidDataException("TEXT_FIELD must be Edm.String.");

        var sourceVector = fields.SingleOrDefault(f => f?["name"]?.GetValue<string>() == settings.SourceVectorField)
            ?? throw new InvalidDataException($"Source vector field '{settings.SourceVectorField}' not found.");
        if (sourceVector["type"]?.GetValue<string>() != "Collection(Edm.Single)")
            throw new InvalidDataException("SOURCE_VECTOR_FIELD must be Collection(Edm.Single).");

        var nonStored = fields.Where(f =>
            f?["type"]?.GetValue<string>() == "Collection(Edm.Single)" &&
            f?["stored"]?.GetValue<bool>() == false)
            .Select(f => f!["name"]?.GetValue<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        if (nonStored.Count > 0)
            throw new InvalidDataException($"In-place backfill does not support nonstored vector fields: {string.Join(", ", nonStored)}.");

        var updated = JsonNode.Parse(schema.ToJsonString())?.AsObject()
            ?? throw new InvalidDataException("Schema clone failed.");
        var updatedFields = updated["fields"]!.AsArray();
        var existingTarget = updatedFields.SingleOrDefault(f => f?["name"]?.GetValue<string>() == settings.NewVectorField);
        var vectorSearch = updated["vectorSearch"]?.AsObject() ?? throw new InvalidDataException("Schema is missing vectorSearch.");
        var profiles = vectorSearch["profiles"]?.AsArray() ?? throw new InvalidDataException("Schema is missing vectorSearch.profiles.");
        var vectorizers = vectorSearch["vectorizers"] as JsonArray ?? new JsonArray();
        if (vectorSearch["vectorizers"] == null) vectorSearch["vectorizers"] = vectorizers;

        var schemaChanged = false;
        var expectedQueryUri = new Uri(settings.FunctionBaseEndpoint, $"api/embed/{settings.ModelVariant}/query").ToString().TrimEnd('/');
        var expectedAudience = settings.FunctionAudience.TrimEnd('/');
        var targetProfileName = $"migration-{settings.NewVectorField}-profile";
        var targetVectorizerName = $"migration-{settings.NewVectorField}-vectorizer";

        if (existingTarget == null)
        {
            var newField = JsonNode.Parse(sourceVector.ToJsonString())?.AsObject()
                ?? throw new InvalidDataException("Unable to clone source vector field.");
            var sourceProfileName = sourceVector["vectorSearchProfile"]?.GetValue<string>()
                ?? throw new InvalidDataException("Source vector field is missing vectorSearchProfile.");
            var sourceProfile = profiles.SingleOrDefault(p => p?["name"]?.GetValue<string>() == sourceProfileName)
                ?? throw new InvalidDataException("Source vector search profile was not found.");
            var algorithms = vectorSearch["algorithms"]?.AsArray() ?? throw new InvalidDataException("Schema is missing vectorSearch.algorithms.");
            var algorithmName = sourceProfile["algorithm"]?.GetValue<string>()
                ?? throw new InvalidDataException("Source vector search profile is missing algorithm.");
            if (!algorithms.Any(a => a?["name"]?.GetValue<string>() == algorithmName))
                throw new InvalidDataException("Source vector search algorithm was not found.");
            if (profiles.Any(p => p?["name"]?.GetValue<string>() == targetProfileName))
                throw new InvalidDataException("Target vector search profile name already exists.");
            if (vectorizers.Any(v => v?["name"]?.GetValue<string>() == targetVectorizerName))
                throw new InvalidDataException("Target vectorizer name already exists.");

            newField["name"] = settings.NewVectorField;
            newField["dimensions"] = settings.Dimensions;
            newField["vectorSearchProfile"] = targetProfileName;
            newField["retrievable"] = true;
            newField["stored"] = true;
            updatedFields.Add(newField);

            var targetProfile = new JsonObject
            {
                ["name"] = targetProfileName,
                ["algorithm"] = algorithmName,
                ["vectorizer"] = targetVectorizerName
            };
            if (sourceProfile["compression"] != null)
                targetProfile["compression"] = sourceProfile["compression"]!.DeepClone();
            profiles.Add(targetProfile);

            vectorizers.Add(new JsonObject
            {
                ["name"] = targetVectorizerName,
                ["kind"] = "customWebApi",
                ["customWebApiParameters"] = new JsonObject
                {
                    ["uri"] = expectedQueryUri,
                    ["httpMethod"] = "POST",
                    ["timeout"] = "PT1M",
                    ["authResourceId"] = expectedAudience
                }
            });
            schemaChanged = true;
        }
        else
        {
            if (existingTarget["type"]?.GetValue<string>() != "Collection(Edm.Single)")
                throw new InvalidDataException($"NEW_VECTOR_FIELD '{settings.NewVectorField}' must be Collection(Edm.Single).");
            if (existingTarget["dimensions"]?.GetValue<int>() != settings.Dimensions)
                throw new InvalidDataException($"NEW_VECTOR_FIELD '{settings.NewVectorField}' dimensions do not match VECTOR_DIMENSIONS.");
            if (existingTarget["retrievable"]?.GetValue<bool>() == false)
                throw new InvalidDataException("NEW_VECTOR_FIELD must be retrievable for verification.");
            if (existingTarget["stored"]?.GetValue<bool>() == false)
                throw new InvalidDataException("NEW_VECTOR_FIELD must be stored for in-place verification and updates.");

            var profileName = existingTarget["vectorSearchProfile"]?.GetValue<string>()
                ?? throw new InvalidDataException("NEW_VECTOR_FIELD is missing vectorSearchProfile.");
            var profile = profiles.SingleOrDefault(p => p?["name"]?.GetValue<string>() == profileName)
                ?? throw new InvalidDataException("NEW_VECTOR_FIELD vector search profile was not found.");
            var vectorizerName = profile["vectorizer"]?.GetValue<string>()
                ?? throw new InvalidDataException("NEW_VECTOR_FIELD profile is missing vectorizer.");
            var vectorizer = vectorizers.SingleOrDefault(v => v?["name"]?.GetValue<string>() == vectorizerName)
                ?? throw new InvalidDataException("NEW_VECTOR_FIELD vectorizer was not found.");
            if (vectorizer["kind"]?.GetValue<string>() != "customWebApi")
                throw new InvalidDataException("NEW_VECTOR_FIELD must use a customWebApi vectorizer compatible with the migration Function.");
            var parameters = vectorizer["customWebApiParameters"]?.AsObject()
                ?? throw new InvalidDataException("NEW_VECTOR_FIELD customWebApi vectorizer parameters are missing.");
            var uri = parameters["uri"]?.GetValue<string>()?.TrimEnd('/')
                ?? throw new InvalidDataException("NEW_VECTOR_FIELD customWebApi vectorizer uri is missing.");
            var authResourceId = parameters["authResourceId"]?.GetValue<string>()?.TrimEnd('/')
                ?? throw new InvalidDataException("NEW_VECTOR_FIELD customWebApi vectorizer authResourceId is missing.");
            if (!string.Equals(uri, expectedQueryUri, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(authResourceId, expectedAudience, StringComparison.Ordinal))
                throw new InvalidDataException("NEW_VECTOR_FIELD vectorizer must match FUNCTION_BASE_ENDPOINT, MODEL_VARIANT, and FUNCTION_AUTH_RESOURCE_ID.");
        }

        var marker = updatedFields.SingleOrDefault(f => f?["name"]?.GetValue<string>() == settings.MigrationMarkerField);
        if (marker == null)
        {
            updatedFields.Add(new JsonObject
            {
                ["name"] = settings.MigrationMarkerField,
                ["type"] = "Edm.String",
                ["searchable"] = false,
                ["filterable"] = true,
                ["retrievable"] = true,
                ["sortable"] = false,
                ["facetable"] = false
            });
            schemaChanged = true;
        }
        else
        {
            if (marker["type"]?.GetValue<string>() != "Edm.String")
                throw new InvalidDataException("MIGRATION_MARKER_FIELD must resolve to Edm.String.");
            if (marker["filterable"]?.GetValue<bool>() != true)
                throw new InvalidDataException("MIGRATION_MARKER_FIELD must be filterable.");
        }

        return new SchemaPlan(
            keyName,
            settings.TextField,
            settings.SourceVectorField,
            settings.NewVectorField,
            settings.Dimensions,
            updated,
            schemaChanged);
    }
}

internal static class InPlaceMigrator
{
    public static async Task Backfill(SearchApi search, SchemaPlan plan, Settings settings, FunctionEmbedder embedder)
    {
        var expected = await search.Count();
        var pending = await search.CountPending(settings.MigrationMarkerField, settings.MigrationMarkerValue);
        if (pending == 0)
        {
            Console.WriteLine($"No pending documents to backfill for '{plan.NewVectorField}'.");
            await WaitForConvergence(search, plan, settings, expected);
            return;
        }

        long processed = 0;
        string? cursor = null;
        var pendingFilter = $"{settings.MigrationMarkerField} ne '{settings.MigrationMarkerValue.Replace("'", "''", StringComparison.Ordinal)}'";

        while (true)
        {
            var page = await search.SearchPage(
                plan.KeyField,
                [plan.KeyField, plan.TextField],
                cursor,
                pendingFilter,
                500);
            if (page.Count == 0) break;

            cursor = page[^1]?[plan.KeyField]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(cursor))
                throw new InvalidDataException("Search page is missing key values required for keyset paging.");

            var records = page.Select(row =>
            {
                var key = row?[plan.KeyField]?.GetValue<string>();
                var text = row?[plan.TextField]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(text))
                    throw new InvalidDataException("Index document is missing key/text values required for migration.");
                return (Key: key!, Text: text!);
            }).ToList();

            var vectors = new Dictionary<string, JsonArray>(StringComparer.Ordinal);
            for (var start = 0; start < records.Count; start += embedder.BatchSize)
            {
                var batch = records.Skip(start).Take(embedder.BatchSize).ToArray();
                foreach (var pair in await embedder.EmbedDocuments(batch))
                    vectors[pair.Key] = pair.Value;
            }

            var uploads = new JsonArray();
            foreach (var record in records)
            {
                var action = new JsonObject
                {
                    ["@search.action"] = "merge",
                    [plan.KeyField] = record.Key,
                    [plan.NewVectorField] = vectors[record.Key].DeepClone(),
                    [settings.MigrationMarkerField] = settings.MigrationMarkerValue
                };
                uploads.Add(action);
            }
            await search.MergeUpload(uploads);
            processed += records.Count;
            Console.WriteLine($"Backfilled {processed}/{pending} pending documents into '{plan.NewVectorField}'.");
        }

        await WaitForConvergence(search, plan, settings, expected);
    }

    private static async Task WaitForConvergence(SearchApi search, SchemaPlan plan, Settings settings, long expectedCount)
    {
        Exception? lastVerifyError = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var remaining = await search.CountPending(settings.MigrationMarkerField, settings.MigrationMarkerValue);
            var count = await search.Count();
            if (remaining == 0 && count == expectedCount)
            {
                try
                {
                    await VerifyNewField(search, plan, expectedCount);
                    return;
                }
                catch (Exception ex)
                {
                    lastVerifyError = ex;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 1 << attempt)));
        }

        if (lastVerifyError != null)
            throw new InvalidOperationException("In-place migration timed out waiting for verification convergence.", lastVerifyError);
        throw new InvalidOperationException("In-place migration did not complete the expected pending backfill.");
    }

    private static async Task VerifyNewField(SearchApi search, SchemaPlan plan, long expectedCount)
    {
        long seen = 0;
        string? cursor = null;
        while (seen < expectedCount)
        {
            var page = await search.SearchPage(
                plan.KeyField,
                [plan.KeyField, plan.NewVectorField],
                cursor,
                null,
                500);
            if (page.Count == 0) break;

            cursor = page[^1]?[plan.KeyField]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(cursor))
                throw new InvalidOperationException("Destination verification page is missing key values.");

            foreach (var row in page)
            {
                var vector = row?[plan.NewVectorField]?.AsArray();
                if (vector == null || vector.Count != plan.Dimensions)
                    throw new InvalidOperationException($"Destination row is missing '{plan.NewVectorField}' or has wrong dimensions.");
            }
            seen += page.Count;
        }
        if (seen != expectedCount)
            throw new InvalidOperationException("Destination verification did not read all documents.");
    }
}
