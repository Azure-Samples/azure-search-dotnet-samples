using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;

namespace AzureSearchBackupRestoreIndex;

internal static class OfflineTests
{
    private static int assertions;

    public static async Task Run()
    {
        // All HTTP requests terminate in this in-memory handler. No credentials or network are used.
        var root = Path.Combine(Directory.GetCurrentDirectory(), $".self-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var handler = new FakeSearch(300);
            using var http = new HttpClient(handler);
            using var source = new SearchApi(http, new Uri("https://offline.invalid"), "source", "test-version", new OfflineCredential());
            using var target = new SearchApi(http, new Uri("https://offline.invalid"), "target", "test-version", new OfflineCredential());
            var backup = Path.Combine(root, "roundtrip");
            await BackupRestore.Backup(source, backup);
            var manifest = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(Path.Combine(backup, "manifest.json")))!;
            Assert(manifest.Count == 300 && manifest.Files.Sum(f => f.Count) == 300, "all 300 docs backed up");
            Assert(handler.Filters.Count > 1 && handler.Filters[1]!.Contains("gt '000036'"), "cursor uses actual truncated page length");
            await BackupRestore.Restore(target, backup);
            Assert(handler.Target.Count == 300, "all docs restored");
            Assert(JsonNode.DeepEquals(handler.Source[42], handler.Target["000042"]), "vectors, nested collections and GeoJSON preserved");
            Assert(handler.TargetSchema!["futureProperty"]!["nested"]!.GetValue<bool>(), "unknown REST schema property preserved");
            Assert(handler.TargetSchema["vectorSearch"]!["vectorizers"]![0]!["kind"]!.GetValue<string>() == "azureOpenAI", "integrated vectorizer preserved");
            await Throws(() => BackupRestore.Restore(target, backup), "existing target is never overwritten");
            Assert(handler.Target.Count == 300, "existing target data remains");
            await Throws(() => BackupRestore.Backup(source, backup), "nonempty backup rejected");

            var first = Path.Combine(backup, manifest.Files[0].Name);
            await File.AppendAllTextAsync(first, " ");
            await Throws(() => BackupRestore.Restore(target, backup), "tampered file rejected before create");

            var key = JsonNode.Parse("""{"name":"id","type":"Edm.String","key":true,"retrievable":true,"filterable":false,"sortable":false}""")!;
            Throws(() => BackupRestore.CheckPaging(key, 100_000), "non-sortable paging rejected");
            key["sortable"] = true;
            BackupRestore.CheckPaging(key, 100_000);
            Assert(true, "sortable key allows deterministic skip paging up to 100k");
            key["filterable"] = true;
            BackupRestore.CheckPaging(key, 100_001);
            Assert(BackupRestore.CanKeyset(key), "keyset supports more than 100k");
            key["normalizer"] = "lowercase";
            Throws(() => BackupRestore.CheckPaging(key, 100_001), "normalized key rejected for large exports");

            Assert(Program.IsValidIndexName("ab"), "two-character index name accepted");
            Assert(Program.IsValidIndexName("products_v1"), "underscore index name accepted");
            Assert(!Program.IsValidIndexName("_products"), "index name cannot start with underscore");
            Assert(!Program.IsValidIndexName("products_"), "index name cannot end with underscore");

            var submitted = JsonNode.Parse("""[{"id":"one","@search.action":"upload"}]""")!.AsArray();
            Throws(() => BackupRestore.CheckIndexResults(JsonNode.Parse("""{"value":[{"key":"one","status":false,"statusCode":400}]}""")!, submitted, "id"), "HTTP success with per-document failure rejected");
            Throws(() => BackupRestore.CheckIndexResults(JsonNode.Parse("""{"value":[]}""")!, submitted, "id"), "missing result rejected");
            BackupRestore.CheckIndexResults(JsonNode.Parse("""{"value":[{"key":"one","status":true,"statusCode":201}]}""")!, submitted, "id");
            Assert(true, "successful per-document result accepted");

            var unsortedSchema = JsonNode.Parse("""{"futureProperty":{"keep":true},"@odata.etag":"old","fields":[],"name":"old"}""")!.AsObject();
            var prepared = BackupRestore.PrepareSchema(unsortedSchema, "new");
            Assert(prepared["futureProperty"]!["keep"]!.GetValue<bool>() && prepared["name"]!.GetValue<string>() == "new" && !prepared.ContainsKey("@odata.etag"), "schema rename is structural and strips only OData metadata");
            Assert(unsortedSchema["name"]!.GetValue<string>() == "old", "source schema not mutated");

            Assert(Hash("""{"v":1.0,"id":"x"}""") == Hash("""{"id":"x","v":1e0}"""), "numeric spelling and property order normalized");
            Assert(Hash("""{"v":1e-40}""") != Hash("""{"v":0}"""), "tiny vector numbers not rounded to zero");
            Assert(Hash("""{"v":9223372036854775807}""") != Hash("""{"v":9223372036854775806}"""), "64-bit integers not rounded");
            Assert(Hash("""{"v":[1,2]}""") != Hash("""{"v":[2,1]}"""), "vector order significant");
            var vectorSchema = JsonNode.Parse("""[{"name":"v","type":"Collection(Edm.Single)"}]""")!.AsArray();
            string StoredVectorHash(string json) => BackupRestore.DocumentHash(JsonNode.Parse(json)!.AsObject(), vectorSchema);
            Assert(StoredVectorHash("""{"v":[0.1]}""") == StoredVectorHash("""{"v":[0.10000000149011612]}"""),
                "all Single bits compared independent of shortest/full decimal spelling");
            Assert(StoredVectorHash("""{"v":[0.1]}""") != StoredVectorHash("""{"v":[0.10000000894069672]}"""),
                "adjacent Single values are not accepted as approximately equal");
            Assert(StoredVectorHash("""{"v":[1,2]}""") != StoredVectorHash("""{"v":[2,1]}"""),
                "schema-aware verification compares every vector position");
            Assert(StoredVectorHash("""{"v":[0.1],"price":0.1}""") != StoredVectorHash("""{"v":[0.1],"price":0.10000000149011612}"""),
                "Single vector normalization does not weaken other numeric field checks");

            var largeDocs = new JsonArray();
            for (var i = 0; i < 3; i++)
                largeDocs.Add(new JsonObject { ["id"] = i.ToString(), ["text"] = new string('x', 6 * 1024 * 1024) });
            var batches = BackupRestore.UploadBatches(largeDocs).ToArray();
            Assert(batches.Length == 2 && batches.Sum(b => b.Count) == 3, "byte-sized batches split below API payload limit");
            Assert(batches.All(b => Encoding.UTF8.GetByteCount(new JsonObject { ["value"] = b.DeepClone() }.ToJsonString()) < BackupRestore.MaximumBatchBytes), "serialized batch fits payload budget");
            Throws(() => BackupRestore.UploadBatches(new JsonArray(new JsonObject { ["text"] = new string('x', BackupRestore.MaximumBatchBytes) })).ToArray(), "oversized single doc fails");
            var many = new JsonArray(Enumerable.Range(0, 1001).Select(i => (JsonNode)new JsonObject { ["id"] = i.ToString() }).ToArray());
            Assert(BackupRestore.UploadBatches(many).Select(b => b.Count).SequenceEqual(new[] { 1000, 1 }), "1000 action batch limit respected");

            var brokenOriginal = JsonSerializer.Serialize(new { id = "one" }) + ",\n";
            brokenOriginal = "{\"value\":[" + brokenOriginal[..^3] + "]}";
            Throws(() => JsonNode.Parse(brokenOriginal), "original trailing-comma removal corrupts JSON");

            var partialHandler = new FakeSearch(1);
            partialHandler.Schema["fields"]![1]!["retrievable"] = false;
            using var partialHttp = new HttpClient(partialHandler);
            using var partialApi = new SearchApi(partialHttp, new Uri("https://offline.invalid"), "source", "test-version", new OfflineCredential());
            await Throws(() => BackupRestore.Backup(partialApi, Path.Combine(root, "nonretrievable")), "nonretrievable vectors fail strict backup");
            partialHandler.Schema["fields"]![1]!["retrievable"] = true;
            partialHandler.Schema["vectorSearch"]!["vectorizers"]![0]!["azureOpenAIParameters"]!["apiKey"] = "fake-schema-secret";
            var credentialDirectory = Path.Combine(root, "credential");
            await Throws(() => BackupRestore.Backup(partialApi, credentialDirectory), "credential-bearing schema rejected before persistence");
            Assert(!Directory.EnumerateFiles(credentialDirectory).Any(), "no schema credential reaches disk");
            partialHandler.Schema["vectorSearch"]!["vectorizers"]![0]!["azureOpenAIParameters"]!["apiKey"] = null;
            partialHandler.Schema["vectorSearch"]!["vectorizers"]![0]!["azureOpenAIParameters"]!["resourceUri"] = "https://offline.invalid/function?code=fake-key";
            await Throws(() => BackupRestore.Backup(partialApi, Path.Combine(root, "credential-url")), "function URL credentials rejected before persistence");
            partialHandler.Schema["vectorSearch"]!["vectorizers"]![0]!["azureOpenAIParameters"]!["resourceUri"] = "https://offline.invalid";
            partialHandler.Schema["vectorSearch"]!["vectorizers"]![0]!["httpHeaders"] = new JsonObject { ["x-custom-secret"] = "fake-header-value" };
            await Throws(() => BackupRestore.Backup(partialApi, Path.Combine(root, "credential-header")), "static vectorizer headers rejected conservatively");

            var emptyHandler = new FakeSearch(0);
            using var emptyHttp = new HttpClient(emptyHandler);
            using var emptySource = new SearchApi(emptyHttp, new Uri("https://offline.invalid"), "source", "test-version", new OfflineCredential());
            using var emptyTarget = new SearchApi(emptyHttp, new Uri("https://offline.invalid"), "target", "test-version", new OfflineCredential());
            var emptyDirectory = Path.Combine(root, "empty");
            await BackupRestore.Backup(emptySource, emptyDirectory);
            await BackupRestore.Restore(emptyTarget, emptyDirectory);
            Assert(emptyHandler.Target.Count == 0, "empty index roundtrips");

            var skipHandler = new FakeSearch(101);
            skipHandler.Schema["fields"]![0]!["filterable"] = false;
            using var skipHttp = new HttpClient(skipHandler);
            using var skipSource = new SearchApi(skipHttp, new Uri("https://offline.invalid"), "source", "test-version", new OfflineCredential());
            using var skipTarget = new SearchApi(skipHttp, new Uri("https://offline.invalid"), "target", "test-version", new OfflineCredential());
            var skipDirectory = Path.Combine(root, "skip");
            await BackupRestore.Backup(skipSource, skipDirectory);
            await BackupRestore.Restore(skipTarget, skipDirectory);
            Assert(skipHandler.Target.Count == 101 && skipHandler.Skips.Contains(37), "bounded skip fallback handles server-truncated pages");

            Console.WriteLine($"PASS: {assertions} offline assertions; no network requests.");
        }
        finally { Directory.Delete(root, true); }
    }

    private static string Hash(string json) => BackupRestore.DocumentHash(JsonNode.Parse(json)!.AsObject());
    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException($"Offline assertion failed: {name}");
        assertions++;
    }
    private static void Throws(Action action, string name)
    {
        try { action(); } catch { assertions++; return; }
        throw new InvalidOperationException($"Offline assertion failed (expected failure): {name}");
    }
    private static async Task Throws(Func<Task> action, string name)
    {
        try { await action(); } catch { assertions++; return; }
        throw new InvalidOperationException($"Offline assertion failed (expected failure): {name}");
    }

    private sealed class FakeSearch : HttpMessageHandler
    {
        public JsonObject Schema { get; } = JsonNode.Parse("""
            {
              "@odata.etag":"original",
              "futureProperty":{"nested":true},
              "name":"source",
              "fields":[
                {"name":"id","type":"Edm.String","key":true,"retrievable":true,"filterable":true,"sortable":true},
                {"name":"vector","type":"Collection(Edm.Single)","retrievable":true,"stored":true,"searchable":true,"dimensions":3,"vectorSearchProfile":"profile"},
                {"name":"text","type":"Edm.String","retrievable":true},
                {"name":"geo","type":"Edm.GeographyPoint","retrievable":true},
                {"name":"nested","type":"Edm.ComplexType","fields":[{"name":"values","type":"Collection(Edm.Int32)","retrievable":true}]}
              ],
              "vectorSearch":{
                "profiles":[{"name":"profile","algorithm":"hnsw","vectorizer":"integrated"}],
                "algorithms":[{"name":"hnsw","kind":"hnsw","hnswParameters":{"metric":"cosine","m":4}}],
                "vectorizers":[{"name":"integrated","kind":"azureOpenAI","azureOpenAIParameters":{"resourceUri":"https://offline.invalid","deploymentId":"embedding","modelName":"text-embedding-3-small"}}]
              },
              "semantic":{"configurations":[{"name":"semantic","prioritizedFields":{"prioritizedContentFields":[{"fieldName":"text"}]}}]}
            }
            """)!.AsObject();
        public List<JsonObject> Source { get; }
        public Dictionary<string, JsonObject> Target { get; } = new(StringComparer.Ordinal);
        public JsonObject? TargetSchema { get; private set; }
        public List<string?> Filters { get; } = new();
        public List<int> Skips { get; } = new();

        public FakeSearch(int count)
        {
            Source = Enumerable.Range(0, count).Select(i => new JsonObject
            {
                ["id"] = i.ToString("D6"),
                ["text"] = "Latitude Longitude are plain text; \u2603",
                ["vector"] = new JsonArray(0.125, -0.5, 0.0000000000000000000000000000000000000001),
                ["geo"] = new JsonObject { ["type"] = "Point", ["coordinates"] = new JsonArray(-122.12, 47.67) },
                ["nested"] = new JsonObject { ["values"] = new JsonArray(1, 2, 3) }
            }).ToList();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert(request.Headers.Authorization?.Scheme == "Bearer" && !request.Headers.Contains("api-key"), "Search uses bearer identity and no API key");
            var path = request.RequestUri!.AbsolutePath;
            var target = path.StartsWith("/indexes/target", StringComparison.Ordinal);
            var body = request.Content == null ? null : JsonNode.Parse(await request.Content.ReadAsStringAsync(cancellationToken))!;
            JsonNode result;
            if (request.Method == HttpMethod.Put)
            {
                Assert(request.Headers.IfNoneMatch.Any(h => h.Tag == "*"), "create uses If-None-Match");
                if (TargetSchema != null) return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
                TargetSchema = body!.DeepClone().AsObject();
                result = TargetSchema.DeepClone();
            }
            else if (path.EndsWith("/docs/$count", StringComparison.Ordinal))
                result = JsonValue.Create(target ? Target.Count : Source.Count)!;
            else if (path.EndsWith("/docs/search", StringComparison.Ordinal))
            {
                Assert(body!["select"]!.GetValue<string>() == "*", "all retrievable fields explicitly selected");
                var filter = body["filter"]?.GetValue<string>();
                Filters.Add(filter);
                var skip = body["skip"]?.GetValue<int>() ?? 0;
                Skips.Add(skip);
                var docs = target ? Target.Values.OrderBy(d => d["id"]!.GetValue<string>(), StringComparer.Ordinal) : Source.AsEnumerable();
                if (filter != null)
                {
                    var cursor = filter["id gt '".Length..^1].Replace("''", "'");
                    docs = docs.Where(d => string.CompareOrdinal(d["id"]!.GetValue<string>(), cursor) > 0);
                }
                var values = new JsonArray();
                foreach (var doc in docs.Skip(skip).Take(Math.Min(37, body["top"]!.GetValue<int>())))
                {
                    var copy = doc.DeepClone().AsObject();
                    copy["@search.score"] = 1.0;
                    values.Add(copy);
                }
                result = new JsonObject { ["value"] = values };
            }
            else if (path.EndsWith("/docs/index", StringComparison.Ordinal))
            {
                var results = new JsonArray();
                foreach (var doc in body!["value"]!.AsArray())
                {
                    var copy = doc!.DeepClone().AsObject();
                    Assert(copy["@search.action"]!.GetValue<string>() == "upload", "explicit upload action");
                    copy.Remove("@search.action");
                    var id = copy["id"]!.GetValue<string>();
                    Target[id] = copy;
                    results.Add(new JsonObject { ["key"] = id, ["status"] = true, ["statusCode"] = 201 });
                }
                result = new JsonObject { ["value"] = results };
            }
            else if (request.Method == HttpMethod.Get)
            {
                result = (target ? TargetSchema! : Schema).DeepClone();
                result["@odata.etag"] = "changed";
            }
            else throw new InvalidOperationException("Unexpected fake HTTP request.");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(result.ToJsonString(), Encoding.UTF8, "application/json") };
        }

    }

    private sealed class OfflineCredential : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                Assert(requestContext.Scopes.SequenceEqual(new[] { "https://search.azure.com/.default" }), "Search token audience");
                return new AccessToken("offline-token", DateTimeOffset.UtcNow.AddHours(1));
            }

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
                ValueTask.FromResult(GetToken(requestContext, cancellationToken));
        }
}
