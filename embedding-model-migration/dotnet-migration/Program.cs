// Copyright Microsoft. Licensed under the MIT license in ..\dotnet-fixed\LICENSE.txt.
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using AzureSearchBackupRestoreIndex;

namespace DotnetEmbeddingMigration;

internal static class Program
{
    internal const int HttpTimeoutSeconds = 240;

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args is ["--self-test"]) { await OfflineTests.Run(); return 0; }
            if (args is ["--help"])
            {
                Console.WriteLine("prepare | reembed | --self-test. Configure process environment using settings.example.ps1 and README.md.");
                return 0;
            }
            if (args.Length != 1 || args[0] is not ("prepare" or "reembed"))
                throw new InvalidOperationException("Usage: dotnet run -- prepare|reembed|--self-test|--help.");
            var settings = Settings.FromEnvironment();
            var targetEndpoint = args[0] == "reembed" ? Settings.Https(Required("TARGET_SEARCH_ENDPOINT"), rootOnly: true) : null;
            var targetIndex = args[0] == "reembed" ? Required("TARGET_INDEX_NAME") : null;
            if (targetIndex != null && !System.Text.RegularExpressions.Regex.IsMatch(targetIndex, "^[a-z0-9][a-z0-9-]{1,126}[a-z0-9]$"))
                throw new InvalidOperationException("Use a valid new target index name.");
            var credential = IdentityConfiguration.Create();
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds) };
            var function = new FunctionClient(http, credential, settings.FunctionBase, settings.FunctionAudience, settings.Model, settings.Dimensions);
            await Migration.Prepare(settings, function);
            if (args[0] == "reembed")
            {
                using var api = new SearchApi(http, targetEndpoint!, targetIndex!, settings.ApiVersion, credential);
                await BackupRestore.Restore(api, settings.OutputDirectory);
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAILED: {ex.Message}");
            return 1;
        }
    }

    internal static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value :
            throw new InvalidOperationException($"Set environment variable {name}.");
}

internal sealed record Settings(string InputDirectory, string OutputDirectory, string ApiVersion,
    string TextField, string VectorField, string Model, int Dimensions, Uri FunctionBase,
    string FunctionAudience, string? SearchFunctionIdentity, int BatchSize = Migration.DocumentBatchSize,
    int HttpTimeoutSeconds = Program.HttpTimeoutSeconds)
{
    public static Settings FromEnvironment()
    {
        if (!int.TryParse(Program.Required("VECTOR_DIMENSIONS"), out var dimensions) || dimensions is < 1 or > 65536)
            throw new InvalidOperationException("VECTOR_DIMENSIONS must be an actual supported output size, 1-65536; no model-specific dimension is assumed.");
        var model = Program.Required("MODEL_VARIANT");
        if (model is not ("fast" or "pro")) throw new InvalidOperationException("MODEL_VARIANT must be fast or pro.");
        var audience = Program.Required("FUNCTION_AUTH_RESOURCE_ID").TrimEnd('/');
        if ((!audience.StartsWith("api://", StringComparison.Ordinal) && !audience.StartsWith("https://", StringComparison.Ordinal)) ||
            !Uri.TryCreate(audience, UriKind.Absolute, out var audienceUri) || audienceUri.UserInfo.Length != 0 ||
            audience.Contains('?') || audience.Contains('#') || audience.EndsWith("/.default", StringComparison.Ordinal))
            throw new InvalidOperationException("FUNCTION_AUTH_RESOURCE_ID must be the configured Entra audience, not a token scope or URL with query.");
        var identity = Environment.GetEnvironmentVariable("SEARCH_FUNCTION_IDENTITY_RESOURCE_ID");
        if (!string.IsNullOrEmpty(identity) && (!identity.StartsWith("/subscriptions/", StringComparison.OrdinalIgnoreCase) ||
            !identity.Contains("/providers/Microsoft.ManagedIdentity/userAssignedIdentities/", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("SEARCH_FUNCTION_IDENTITY_RESOURCE_ID must be a user-assigned managed identity ARM resource ID.");
        return new Settings(Path.GetFullPath(Program.Required("BACKUP_DIRECTORY")), Path.GetFullPath(Program.Required("MIGRATION_DIRECTORY")),
            Program.Required("SEARCH_API_VERSION"), Program.Required("TEXT_FIELD"), Program.Required("VECTOR_FIELD"), model, dimensions,
            Https(Program.Required("FUNCTION_BASE_ENDPOINT"), rootOnly: true), audience, string.IsNullOrEmpty(identity) ? null : identity,
            BoundedInteger(Environment.GetEnvironmentVariable("EMBEDDING_BATCH_SIZE"), "EMBEDDING_BATCH_SIZE", Migration.DocumentBatchSize, 1, 16),
            BoundedInteger(Environment.GetEnvironmentVariable("FUNCTION_HTTP_TIMEOUT_SECONDS"), "FUNCTION_HTTP_TIMEOUT_SECONDS", Program.HttpTimeoutSeconds, 240, 1800));
    }

    internal static int BoundedInteger(string? value, string name, int defaultValue, int minimum, int maximum)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (!int.TryParse(value, out var parsed) || parsed < minimum || parsed > maximum)
            throw new InvalidOperationException($"{name} must be an integer from {minimum} to {maximum}.");
        return parsed;
    }

    internal static Uri Https(string value, bool rootOnly)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            (rootOnly && uri.AbsolutePath != "/"))
            throw new InvalidOperationException("Endpoint must be an HTTPS root URL without credentials, query or fragment.");
        return uri;
    }
}

internal sealed class FunctionClient(HttpClient http, TokenCredential credential, Uri endpoint, string audience, string model, int dimensions)
{
    public async Task<Dictionary<string, JsonArray>> Embed(string role, IReadOnlyList<(string Id, string Text)> records)
    {
        if (role is not ("document" or "query")) throw new InvalidOperationException("Embedding role must be document or query.");
        var values = new JsonArray();
        foreach (var (id, text) in records)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("Embedding input must contain nonempty text.");
            values.Add(new JsonObject { ["recordId"] = id, ["data"] = new JsonObject { ["text"] = text } });
        }
        var body = new JsonObject { ["values"] = values }.ToJsonString();
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, $"api/embed/{model}/{role}"));
            var token = await credential.GetTokenAsync(new TokenRequestContext([$"{audience.TrimEnd('/')}/.default"]), default);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request);
            if ((response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
                (int)response.StatusCode is >= 500 and <= 599) && attempt < 4)
            {
                var delay = response.Headers.RetryAfter?.Delta ??
                    (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : TimeSpan.FromSeconds(1 << attempt));
                await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay > TimeSpan.FromMinutes(2) ? TimeSpan.FromMinutes(2) : delay);
                continue;
            }
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Embedding Function returned HTTP {(int)response.StatusCode}; no keys or fallback provider are supported.");
            var result = JsonNode.Parse(await response.Content.ReadAsStringAsync())?["values"]?.AsArray()
                ?? throw new InvalidDataException("Embedding Function response has no values array.");
            var wanted = records.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
            if (wanted.Count != records.Count) throw new InvalidDataException("Embedding record IDs must be unique.");
            var vectors = new Dictionary<string, JsonArray>(StringComparer.Ordinal);
            foreach (var item in result)
            {
                var id = item?["recordId"]?.GetValue<string>();
                if (id == null || !wanted.Contains(id) || vectors.ContainsKey(id) ||
                    item?["errors"] is JsonArray { Count: > 0 } || item?["warnings"] is JsonArray { Count: > 0 })
                    throw new InvalidDataException("Embedding Function returned errors, warnings, duplicate or unexpected record IDs.");
                var vector = item?["data"]?["vector"] as JsonArray ?? throw new InvalidDataException("Embedding response lacks data.vector.");
                vectors.Add(id, ValidateVector(vector, dimensions));
            }
            if (vectors.Count != wanted.Count) throw new InvalidDataException("Embedding Function omitted records.");
            return vectors;
        }
    }

    internal static JsonArray ValidateVector(JsonArray vector, int expected)
    {
        if (vector.Count != expected) throw new InvalidDataException("Embedding dimension does not match VECTOR_DIMENSIONS.");
        var converted = new JsonArray();
        foreach (var component in vector)
        {
            var number = component?.GetValue<double>() ?? throw new InvalidDataException("Embedding contains null.");
            if (!double.IsFinite(number) || Math.Abs(number) > float.MaxValue)
                throw new InvalidDataException("Embedding contains a nonfinite or out-of-range Edm.Single component.");
            converted.Add((float)number);
        }
        return converted;
    }
}

internal static class Migration
{
    internal const int DocumentBatchSize = 4;

    public static async Task Prepare(Settings settings, FunctionClient function)
    {
        if (settings.BatchSize is < 1 or > 16 || settings.HttpTimeoutSeconds is < 240 or > 1800)
            throw new InvalidOperationException("Embedding batch size must be 1-16; HTTP timeout must be 240-1800 seconds.");
        if (settings.InputDirectory.Equals(settings.OutputDirectory, StringComparison.OrdinalIgnoreCase) ||
            (Directory.Exists(settings.OutputDirectory) && Directory.EnumerateFileSystemEntries(settings.OutputDirectory).Any()))
            throw new InvalidOperationException("MIGRATION_DIRECTORY must be a new empty directory, different from BACKUP_DIRECTORY.");
        var manifest = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(Path.Combine(settings.InputDirectory, "manifest.json")))
            ?? throw new InvalidDataException("Missing backup manifest.");
        if (manifest.Format != 1 || manifest.Partial || manifest.ApiVersion != settings.ApiVersion)
            throw new InvalidDataException("A complete format-1 .NET backup and matching SEARCH_API_VERSION are required.");
        var originalSchema = await File.ReadAllBytesAsync(Path.Combine(settings.InputDirectory, "schema.json"));
        if (BackupRestore.Hash(originalSchema) != manifest.SchemaSha256) throw new InvalidDataException("Backup schema checksum mismatch.");
        var schema = JsonNode.Parse(originalSchema)!.AsObject();
        BackupRestore.RejectCredentials(schema);
        if (BackupRestore.HasNonretrievable(schema["fields"]!.AsArray()))
            throw new InvalidDataException("Nonretrievable fields prevent a complete migration.");
        var changedSchema = ChangeSchema(schema, settings);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files)
        {
            if (!fileNames.Add(file.Name)) throw new InvalidDataException("Duplicate backup filename.");
            foreach (var doc in await BackupRestore.ReadFile(settings.InputDirectory, file))
            {
                if (doc is not JsonObject obj || obj.ContainsKey("@search.action") ||
                    !keys.Add(doc[manifest.KeyField]!.GetValue<string>()) ||
                    string.IsNullOrWhiteSpace(doc[settings.TextField]?.GetValue<string>()))
                    throw new InvalidDataException("Backup contains invalid/duplicate keys, indexing actions or missing/empty text.");
            }
        }
        if (keys.Count != manifest.Count) throw new InvalidDataException("Backup document count mismatch.");
        // Validate query deployment dimensions before generating document embeddings or creating an index.
        await function.Embed("query", [("dimension-probe", "Validate embedding dimension compatibility.")]);
        Directory.CreateDirectory(settings.OutputDirectory);
        var schemaBytes = Encoding.UTF8.GetBytes(changedSchema.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllBytesAsync(Path.Combine(settings.OutputDirectory, "schema.json"), schemaBytes);
        var files = new List<BackupFile>();
        foreach (var file in manifest.Files)
        {
            var docs = await BackupRestore.ReadFile(settings.InputDirectory, file);
            for (var start = 0; start < docs.Count; start += settings.BatchSize)
            {
                var batch = docs.Skip(start).Take(settings.BatchSize).Select(d => (
                    d![manifest.KeyField]!.GetValue<string>(), d[settings.TextField]!.GetValue<string>())).ToArray();
                var vectors = await function.Embed("document", batch);
                foreach (var doc in docs.Skip(start).Take(settings.BatchSize))
                    doc![settings.VectorField] = vectors[doc[manifest.KeyField]!.GetValue<string>()].DeepClone();
            }
            var bytes = Encoding.UTF8.GetBytes(new JsonObject { ["value"] = docs.DeepClone() }.ToJsonString());
            await File.WriteAllBytesAsync(Path.Combine(settings.OutputDirectory, file.Name), bytes);
            files.Add(new BackupFile(file.Name, BackupRestore.Hash(bytes), docs.Count));
        }
        var migrated = manifest with { SchemaSha256 = BackupRestore.Hash(schemaBytes), Files = files };
        await File.WriteAllTextAsync(Path.Combine(settings.OutputDirectory, "manifest.json"),
            JsonSerializer.Serialize(migrated, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Prepared {manifest.Count} documents using {settings.Model}, {settings.Dimensions} dimensions; original backup untouched.");
    }

    internal static JsonObject ChangeSchema(JsonObject original, Settings settings)
    {
        var schema = original.DeepClone().AsObject();
        var fields = schema["fields"]!.AsArray();
        var text = fields.SingleOrDefault(f => f?["name"]?.GetValue<string>() == settings.TextField);
        var vector = fields.SingleOrDefault(f => f?["name"]?.GetValue<string>() == settings.VectorField);
        if (settings.TextField == settings.VectorField || text?["type"]?.GetValue<string>() != "Edm.String" ||
            vector?["type"]?.GetValue<string>() != "Collection(Edm.Single)")
            throw new InvalidDataException("Select distinct existing top-level Edm.String text and Collection(Edm.Single) vector fields.");
        var search = schema["vectorSearch"] as JsonObject ?? throw new InvalidDataException("Source schema has no vectorSearch definition.");
        var prefix = $"migration-{settings.Model}";
        var algorithmName = $"{prefix}-hnsw";
        var profileName = $"{prefix}-profile";
        var vectorizerName = $"{prefix}-vectorizer";
        var algorithms = EnsureArray(search, "algorithms");
        var profiles = EnsureArray(search, "profiles");
        var vectorizers = EnsureArray(search, "vectorizers");
        if (algorithms.Any(a => a?["name"]?.GetValue<string>() == algorithmName) ||
            profiles.Any(a => a?["name"]?.GetValue<string>() == profileName) ||
            vectorizers.Any(a => a?["name"]?.GetValue<string>() == vectorizerName))
            throw new InvalidDataException("Source already uses generated migration profile names; choose a fresh baseline backup.");
        vector!["dimensions"] = settings.Dimensions;
        vector["vectorSearchProfile"] = profileName;
        vector["retrievable"] = true;
        vector["stored"] = true;
        algorithms.Add(new JsonObject { ["name"] = algorithmName, ["kind"] = "hnsw",
            ["hnswParameters"] = new JsonObject { ["metric"] = "cosine", ["m"] = 4, ["efConstruction"] = 400, ["efSearch"] = 500 },
            ["exhaustiveKnnParameters"] = null });
        profiles.Add(new JsonObject { ["name"] = profileName, ["algorithm"] = algorithmName, ["vectorizer"] = vectorizerName, ["compression"] = null });
        var parameters = new JsonObject
        {
            ["uri"] = new Uri(settings.FunctionBase, $"api/embed/{settings.Model}/query").ToString(),
            ["authResourceId"] = settings.FunctionAudience,
            ["timeout"] = "PT3M50S",
            ["httpMethod"] = "POST",
            ["httpHeaders"] = new JsonObject(),
            ["authIdentity"] = null
        };
        if (settings.SearchFunctionIdentity != null)
            parameters["authIdentity"] = new JsonObject
            {
                ["@odata.type"] = "#Microsoft.Azure.Search.DataUserAssignedIdentity",
                ["userAssignedIdentity"] = settings.SearchFunctionIdentity
            };
        vectorizers.Add(new JsonObject { ["name"] = vectorizerName, ["kind"] = "customWebApi",
            ["azureOpenAIParameters"] = null, ["customWebApiParameters"] = parameters });
        BackupRestore.RejectCredentials(schema);
        return schema;
    }

    private static JsonArray EnsureArray(JsonObject obj, string name)
    {
        if (obj[name] == null) obj[name] = new JsonArray();
        return obj[name]!.AsArray();
    }
}
