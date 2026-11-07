// Derived from the index-backup-restore v11 sample in azure-search-dotnet-utilities.
// Copyright Microsoft. Licensed under the MIT license; see LICENSE.txt.
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using Azure.Core;

namespace AzureSearchBackupRestoreIndex;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args is ["--self-test"])
            {
                await OfflineTests.Run();
                return 0;
            }
            if (args is ["--help"])
            {
                Console.WriteLine("backup | restore | --self-test. Configure keyless process environment using settings.example.ps1 and README.md.");
                return 0;
            }
            if (args.Length != 1 || args[0] is not ("backup" or "restore"))
                throw new InvalidOperationException("Usage: dotnet run -- backup|restore|--self-test. See README.md.");

            var backup = args[0] == "backup";
            var prefix = backup ? "SOURCE" : "TARGET";
            var endpoint = new Uri(Required($"{prefix}_SEARCH_ENDPOINT"));
            if (endpoint.Scheme != "https" || !string.IsNullOrEmpty(endpoint.UserInfo) ||
                !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) || endpoint.AbsolutePath != "/")
                throw new InvalidOperationException("Use an HTTPS search service root URL, without credentials or a query.");
            var index = Required($"{prefix}_INDEX_NAME");
            if (!System.Text.RegularExpressions.Regex.IsMatch(index, "^[a-z0-9][a-z0-9-]{1,126}[a-z0-9]$"))
                throw new InvalidOperationException("Index name must be 3-128 lowercase letters, digits or hyphens.");
            var directory = Path.GetFullPath(Required("BACKUP_DIRECTORY"));
            var apiVersion = Required("SEARCH_API_VERSION");
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            using var api = new SearchApi(http, endpoint, index, apiVersion, IdentityConfiguration.Create());
            if (backup)
                await BackupRestore.Backup(api, directory, Environment.GetEnvironmentVariable("ALLOW_PARTIAL_BACKUP") == "true");
            else
                await BackupRestore.Restore(api, directory);
            return 0;
        }
        catch (Exception ex)
        {
            // Do not echo service response bodies, request URIs or configuration secrets.
            Console.Error.WriteLine($"FAILED: {ex.Message}");
            return 1;
        }
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Set environment variable {name}.");
}

internal sealed class SearchApi : IDisposable
{
    private readonly HttpClient http;
    private readonly Uri endpoint;
    private readonly TokenCredential credential;
    public string Index { get; }
    public string Version { get; }

    public SearchApi(HttpClient http, Uri endpoint, string index, string version, TokenCredential credential)
    {
        this.http = http;
        this.endpoint = endpoint;
        this.credential = credential;
        Index = index;
        Version = version;
    }

    public async Task<JsonNode> Send(HttpMethod method, string suffix = "", JsonNode? body = null, bool createOnly = false)
    {
        var uri = new Uri(endpoint, $"indexes/{Uri.EscapeDataString(Index)}{suffix}?api-version={Uri.EscapeDataString(Version)}");
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, uri);
            var token = await credential.GetTokenAsync(new TokenRequestContext(["https://search.azure.com/.default"]), default);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            if (createOnly) request.Headers.TryAddWithoutValidation("If-None-Match", "*");
            if (body != null) request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request);
            var transient = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
                or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;
            // A conditional schema creation is never replayed: a lost response must be reconciled by an operator.
            if (transient && !createOnly && attempt < 4)
            {
                var delay = response.Headers.RetryAfter?.Delta ??
                    (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : TimeSpan.FromSeconds(1 << attempt));
                await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay > TimeSpan.FromMinutes(2) ? TimeSpan.FromMinutes(2) : delay);
                continue;
            }
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Search {method} failed (HTTP {(int)response.StatusCode}); no further steps were performed.");
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())
                ?? throw new InvalidDataException("Search returned an empty JSON response.");
        }
    }

    public async Task<long> Count() => (await Send(HttpMethod.Get, "/docs/$count")).GetValue<long>();
    public void Dispose() { /* HttpClient is owned by the caller. */ }
}

internal sealed record BackupFile(string Name, string Sha256, int Count);
internal sealed record Manifest(int Format, string SourceIndex, string ApiVersion, string KeyField,
    long Count, bool Partial, string SchemaSha256, List<BackupFile> Files);

internal static class BackupRestore
{
    internal const int MaximumBatchBytes = 15 * 1024 * 1024;
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static async Task Backup(SearchApi api, string directory, bool allowPartial = false)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new InvalidOperationException("Backup directory must be empty; use a new directory for every backup.");
        Directory.CreateDirectory(directory);
        var schema = (await api.Send(HttpMethod.Get)).AsObject();
        RejectCredentials(schema);
        var fields = schema["fields"]!.AsArray();
        var key = fields.Single(f => f?["key"]?.GetValue<bool>() == true)!;
        var keyName = key["name"]!.GetValue<string>();
        var partial = HasNonretrievable(fields);
        if (partial && !allowPartial)
            throw new InvalidOperationException("Nonretrievable fields cannot be backed up. For a knowingly incomplete export only, set ALLOW_PARTIAL_BACKUP=true. Nonstored vectors cannot be recovered.");
        var count = await api.Count();
        CheckPaging(key, count);
        var schemaBytes = Encoding.UTF8.GetBytes(schema.ToJsonString(Pretty));
        await File.WriteAllBytesAsync(Path.Combine(directory, "schema.json"), schemaBytes);
        var files = new List<BackupFile>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var page in ReadPages(api, key, count))
        {
            foreach (var doc in page)
                if (!keys.Add(doc![keyName]!.GetValue<string>()))
                    throw new InvalidDataException("Duplicate document key during extraction; backup is incomplete.");
            var bytes = Encoding.UTF8.GetBytes(new JsonObject { ["value"] = page }.ToJsonString());
            var name = $"documents-{files.Count + 1:D8}.json";
            await File.WriteAllBytesAsync(Path.Combine(directory, name), bytes);
            files.Add(new BackupFile(name, Hash(bytes), page.Count));
        }
        if (keys.Count != count || await api.Count() != count)
            throw new InvalidDataException("Source count changed or extraction count mismatched; no complete manifest was written.");
        var manifest = new Manifest(1, api.Index, api.Version, keyName, count, partial, Hash(schemaBytes), files);
        await File.WriteAllTextAsync(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(manifest, Pretty));
        Console.WriteLine($"Backup complete: {count} documents. Partial={partial}. Freeze source writes until verification.");
    }

    public static async Task Restore(SearchApi api, string directory)
    {
        var manifest = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(Path.Combine(directory, "manifest.json")))
            ?? throw new InvalidDataException("Missing manifest.");
        if (manifest.Format != 1 || manifest.ApiVersion != api.Version)
            throw new InvalidDataException("Use manifest format 1 and the same SEARCH_API_VERSION as the backup.");
        if (manifest.Partial)
            throw new InvalidDataException("This backup is partial and cannot satisfy an identical restore.");
        var schemaBytes = await File.ReadAllBytesAsync(Path.Combine(directory, "schema.json"));
        if (Hash(schemaBytes) != manifest.SchemaSha256) throw new InvalidDataException("Schema checksum mismatch.");
        var schema = JsonNode.Parse(schemaBytes)!.AsObject();
        RejectCredentials(schema);
        var key = schema["fields"]!.AsArray().Single(f => f?["key"]?.GetValue<bool>() == true)!;
        if (key["name"]!.GetValue<string>() != manifest.KeyField)
            throw new InvalidDataException("Manifest key does not match schema.");
        CheckPaging(key, manifest.Count);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal);
        var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files)
        {
            if (!fileNames.Add(file.Name)) throw new InvalidDataException("Duplicate manifest filename.");
            var docs = await ReadFile(directory, file);
            foreach (var doc in docs)
            {
                if (doc is not JsonObject obj || obj.ContainsKey("@search.action"))
                    throw new InvalidDataException("Backup documents must be objects without indexing actions.");
                if (!expected.TryAdd(doc[manifest.KeyField]!.GetValue<string>(), DocumentHash(obj, schema["fields"]!.AsArray())))
                    throw new InvalidDataException("Duplicate backup key.");
            }
        }
        if (expected.Count != manifest.Count) throw new InvalidDataException("Manifest document count mismatch.");
        var targetSchema = PrepareSchema(schema, api.Index);
        // Never delete/overwrite an index. HTTP 412 also protects against a concurrent create.
        await api.Send(HttpMethod.Put, body: targetSchema, createOnly: true);
        foreach (var file in manifest.Files)
        {
            var docs = await ReadFile(directory, file);
            foreach (var batch in UploadBatches(docs))
                CheckIndexResults(await api.Send(HttpMethod.Post, "/docs/index", new JsonObject { ["value"] = batch }), batch, manifest.KeyField);
        }
        var matched = false;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (await api.Count() == manifest.Count) { matched = true; break; }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        if (!matched) throw new InvalidDataException("Target count did not converge within 60 seconds; inspect the target before retrying.");
        var actualSchema = PrepareSchema((await api.Send(HttpMethod.Get)).AsObject(), api.Index);
        if (!SchemasEquivalent(targetSchema, actualSchema))
            throw new InvalidDataException("Target schema differs from backup after normalization of known service defaults and equivalent durations.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var page in ReadPages(api, key, manifest.Count))
        {
            foreach (var doc in page)
            {
                var id = doc![manifest.KeyField]!.GetValue<string>();
                if (!seen.Add(id) || !expected.TryGetValue(id, out var hash) || hash != DocumentHash(doc.AsObject(), schema["fields"]!.AsArray()))
                    throw new InvalidDataException("Target document content differs from backup, including retrievable vectors.");
            }
        }
        if (seen.Count != manifest.Count || await api.Count() != manifest.Count)
            throw new InvalidDataException("Target changed during verification.");
        Console.WriteLine($"Restore verified: schema and all retrievable fields of {seen.Count} documents, including vectors.");
    }

    internal static JsonObject PrepareSchema(JsonObject original, string target)
    {
        var schema = original.DeepClone().AsObject();
        foreach (var property in schema.Select(p => p.Key).Where(k => k.StartsWith("@odata.", StringComparison.Ordinal)).ToArray())
            schema.Remove(property);
        schema["name"] = target;
        return schema;
    }

    internal static bool SchemasEquivalent(JsonObject expected, JsonObject actual) =>
        JsonNode.DeepEquals(CanonicalSchema(expected), CanonicalSchema(actual));

    private static JsonObject CanonicalSchema(JsonObject original)
    {
        var schema = PrepareSchema(original, "comparison-index");
        if (schema["vectorSearch"] is not JsonObject search) return schema;
        if (search["algorithms"] is JsonArray algorithms)
            foreach (var algorithm in algorithms.OfType<JsonObject>())
                if (algorithm["kind"]?.GetValue<string>() == "hnsw")
                    Default(algorithm, "exhaustiveKnnParameters", null);
        if (search["profiles"] is JsonArray profiles)
            foreach (var profile in profiles.OfType<JsonObject>())
                Default(profile, "compression", null);
        if (search["vectorizers"] is JsonArray vectorizers)
            foreach (var vectorizer in vectorizers.OfType<JsonObject>())
            {
                if (vectorizer["kind"]?.GetValue<string>() != "customWebApi") continue;
                Default(vectorizer, "azureOpenAIParameters", null);
                if (vectorizer["customWebApiParameters"] is not JsonObject parameters) continue;
                Default(parameters, "authIdentity", null);
                Default(parameters, "httpHeaders", new JsonObject());
                Default(parameters, "httpMethod", JsonValue.Create("POST"));
                if (parameters["timeout"] is JsonValue duration && duration.TryGetValue<string>(out var text))
                {
                    try { parameters["timeout"] = System.Xml.XmlConvert.ToString(System.Xml.XmlConvert.ToTimeSpan(text)); }
                    catch (FormatException) { throw new InvalidDataException("Custom Web API vectorizer timeout is not a valid ISO 8601 duration."); }
                }
            }
        return schema;
    }

    private static void Default(JsonObject obj, string name, JsonNode? value)
    {
        if (!obj.ContainsKey(name)) obj[name] = value;
    }

    internal static bool HasNonretrievable(JsonArray fields) => fields.Any(field =>
        field?["retrievable"]?.GetValue<bool>() == false ||
        (field?["fields"] is JsonArray children && HasNonretrievable(children)));

    internal static void RejectCredentials(JsonNode node)
    {
        if (node is JsonObject obj)
            foreach (var (name, value) in obj)
            {
                if (new[] { "apiKey", "api-key", "x-api-key", "authenticationKey", "connectionString",
                    "applicationSecret", "clientSecret", "accessToken", "sasToken", "sharedAccessSignature",
                    "authorization", "password", "httpHeaders" }
                    .Contains(name, StringComparer.OrdinalIgnoreCase) && value != null && value.ToJsonString() != "\"\"" &&
                    value is not JsonObject { Count: 0 } && value is not JsonArray { Count: 0 })
                    throw new InvalidDataException("Schema contains a credential; it was not written. Use managed identity or a separately sanitized schema workflow.");
                if (value != null) RejectCredentials(value);
            }
        else if (node is JsonArray array)
        {
            foreach (var value in array) if (value != null) RejectCredentials(value);
        }
        else if (node is JsonValue scalar && scalar.TryGetValue<string>(out var text) &&
            Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" &&
            (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)))
            throw new InvalidDataException("Schema contains a URL with user information or query parameters; it was not written because these can contain credentials.");
    }

    internal static bool CanKeyset(JsonNode key) => key["type"]?.GetValue<string>() == "Edm.String" &&
        key["filterable"]?.GetValue<bool>() == true && key["sortable"]?.GetValue<bool>() == true &&
        key["normalizer"] == null;

    internal static void CheckPaging(JsonNode key, long count)
    {
        if (count < 0 || (!CanKeyset(key) && count > 100_000))
            throw new InvalidDataException("Over 100,000 documents requires a filterable, sortable string key without a normalizer. Otherwise only <=100,000 are supported.");
        if (key["retrievable"]?.GetValue<bool>() == false)
            throw new InvalidDataException("Document key must be retrievable.");
    }

    private static async IAsyncEnumerable<JsonArray> ReadPages(SearchApi api, JsonNode key, long expected)
    {
        var keyName = key["name"]!.GetValue<string>();
        var keyset = CanKeyset(key);
        long read = 0;
        string? last = null;
        while (read < expected)
        {
            var query = new JsonObject { ["search"] = "*", ["select"] = "*", ["top"] = (int)Math.Min(100, expected - read) };
            if (keyset)
            {
                query["orderby"] = $"{keyName} asc";
                if (last != null) query["filter"] = $"{keyName} gt '{last.Replace("'", "''")}'";
            }
            else
            {
                query["skip"] = read;
                if (key["sortable"]?.GetValue<bool>() == true) query["orderby"] = $"{keyName} asc";
            }
            var response = await api.Send(HttpMethod.Post, "/docs/search", query);
            var page = response["value"]?.DeepClone().AsArray() ?? throw new InvalidDataException("Search response has no value array.");
            if (page.Count == 0) throw new InvalidDataException("Search ended before the expected document count.");
            foreach (var item in page)
            {
                var doc = item!.AsObject();
                foreach (var property in doc.Select(p => p.Key).Where(k => k.StartsWith("@search.", StringComparison.Ordinal) || k.StartsWith("@odata.", StringComparison.Ordinal)).ToArray())
                    doc.Remove(property);
            }
            read += page.Count;
            if (read > expected) throw new InvalidDataException("Source grew during extraction.");
            var next = page[^1]![keyName]!.GetValue<string>();
            if (keyset && next == last) throw new InvalidDataException("Keyset cursor did not advance.");
            last = next;
            yield return page;
        }
    }

    internal static async Task<JsonArray> ReadFile(string directory, BackupFile file)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(file.Name, "^documents-[0-9]{8}\\.json$"))
            throw new InvalidDataException("Invalid document filename in manifest.");
        var bytes = await File.ReadAllBytesAsync(Path.Combine(directory, file.Name));
        if (Hash(bytes) != file.Sha256) throw new InvalidDataException("Document file checksum mismatch.");
        var docs = JsonNode.Parse(bytes)?["value"]?.AsArray() ?? throw new InvalidDataException("Invalid document file.");
        if (docs.Count != file.Count) throw new InvalidDataException("Document file count mismatch.");
        return docs;
    }

    internal static IEnumerable<JsonArray> UploadBatches(JsonArray docs)
    {
        var batch = new JsonArray();
        var size = Encoding.UTF8.GetByteCount("{\"value\":[]}");
        foreach (var doc in docs)
        {
            var action = doc!.DeepClone().AsObject();
            action["@search.action"] = "upload";
            var bytes = Encoding.UTF8.GetByteCount(action.ToJsonString()) + 1;
            if (bytes + 12 > MaximumBatchBytes) throw new InvalidDataException("One document exceeds the safe indexing payload size.");
            if (batch.Count > 0 && (size + bytes > MaximumBatchBytes || batch.Count == 1000))
            {
                yield return batch;
                batch = new JsonArray();
                size = 12;
            }
            batch.Add(action);
            size += bytes;
        }
        if (batch.Count > 0) yield return batch;
    }

    internal static void CheckIndexResults(JsonNode response, JsonArray submitted, string key)
    {
        var results = response["value"]?.AsArray() ?? throw new InvalidDataException("Missing indexing results.");
        var wanted = submitted.Select(d => d![key]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in results)
        {
            var id = item?["key"]?.GetValue<string>();
            var statusCode = item?["statusCode"]?.GetValue<int>() ?? 0;
            if (id == null || !wanted.Contains(id) || !seen.Add(id) || item?["status"]?.GetValue<bool>() != true || statusCode is < 200 or >= 300)
                throw new InvalidDataException($"Indexing failed or returned invalid per-document results (status {statusCode}). Target may be partially populated; it was not deleted.");
        }
        if (seen.Count != wanted.Count) throw new InvalidDataException("Missing per-document indexing results.");
    }

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static string DocumentHash(JsonObject doc, JsonArray? fields = null)
    {
        // Compare Single vectors by every stored component, not provider-specific decimal formatting.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) Canonical(writer, doc, fields);
        return Hash(buffer.ToArray());
    }

    private static void Canonical(Utf8JsonWriter writer, JsonNode? node, JsonArray? fields = null, string? type = null)
    {
        switch (node)
        {
            case JsonObject obj:
                writer.WriteStartObject();
                foreach (var (name, value) in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    var field = fields?.SingleOrDefault(f => f?["name"]?.GetValue<string>() == name);
                    writer.WritePropertyName(name);
                    Canonical(writer, value, field?["fields"] as JsonArray, field?["type"]?.GetValue<string>());
                }
                writer.WriteEndObject();
                break;
            case JsonArray array:
                writer.WriteStartArray();
                foreach (var value in array)
                {
                    if (type == "Collection(Edm.Single)")
                    {
                        var vectorElement = JsonSerializer.SerializeToElement(value);
                        if (vectorElement.ValueKind != JsonValueKind.Number || !vectorElement.TryGetSingle(out var single) || !float.IsFinite(single))
                            throw new InvalidDataException("A Single collection contains an invalid component.");
                        writer.WriteNumberValue(BitConverter.SingleToUInt32Bits(single == 0 ? 0 : single));
                    }
                    else Canonical(writer, value, fields);
                }
                writer.WriteEndArray();
                break;
            case null: writer.WriteNullValue(); break;
            default:
                var element = JsonSerializer.SerializeToElement(node);
                if (element.ValueKind == JsonValueKind.Number)
                    writer.WriteRawValue(NormalizeNumber(element.GetRawText()));
                else node.WriteTo(writer);
                break;
        }
    }

    private static string NormalizeNumber(string text)
    {
        var parts = text.ToLowerInvariant().Split('e');
        var exponent = parts.Length == 2 ? long.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
        var coefficient = parts[0];
        var negative = coefficient.StartsWith('-');
        if (negative) coefficient = coefficient[1..];
        var dot = coefficient.IndexOf('.');
        if (dot >= 0)
        {
            exponent -= coefficient.Length - dot - 1;
            coefficient = coefficient.Remove(dot, 1);
        }
        coefficient = coefficient.TrimStart('0');
        if (coefficient.Length == 0) return "0";
        var trimmed = coefficient.TrimEnd('0');
        exponent += coefficient.Length - trimmed.Length;
        return $"{(negative ? "-" : "")}{trimmed}e{exponent}";
    }
}
