using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using AzureSearchBackupRestoreIndex;

namespace DotnetEmbeddingMigration;

internal static class OfflineTests
{
    private static int assertions;

    public static async Task Run()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), $".self-test-migration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var input = Path.Combine(root, "baseline");
            Directory.CreateDirectory(input);
            var schema = JsonNode.Parse("""
                {"@odata.etag":"old","name":"baseline","future":{"preserve":true},
                "fields":[{"name":"id","key":true,"type":"Edm.String","retrievable":true,"sortable":true,"filterable":true},
                {"name":"text","type":"Edm.String","retrievable":true},
                {"name":"vector","type":"Collection(Edm.Single)","retrievable":true,"stored":true,"dimensions":2,"vectorSearchProfile":"original"}],
                "vectorSearch":{"algorithms":[{"name":"old","kind":"hnsw"}],
                "profiles":[{"name":"original","algorithm":"old","vectorizer":"original"}],
                "vectorizers":[{"name":"original","kind":"azureOpenAI","azureOpenAIParameters":{"resourceUri":"https://offline.invalid","deploymentId":"old","modelName":"text-embedding-3-small"}}]},
                "semantic":{"configurations":[{"name":"old","prioritizedFields":{"prioritizedContentFields":[{"fieldName":"text"}]}}]}}
                """)!.AsObject();
            var originalSchema = Encoding.UTF8.GetBytes(schema.ToJsonString());
            await File.WriteAllBytesAsync(Path.Combine(input, "schema.json"), originalSchema);
            var docs = new JsonArray(Enumerable.Range(0, 19).Select(i => (JsonNode)new JsonObject
            {
                ["id"] = i.ToString("D3"), ["text"] = $"document-{i}", ["vector"] = new JsonArray(0.1, 0.2)
            }).ToArray());
            var documentBytes = Encoding.UTF8.GetBytes(new JsonObject { ["value"] = docs }.ToJsonString());
            await File.WriteAllBytesAsync(Path.Combine(input, "documents-00000001.json"), documentBytes);
            var manifest = new Manifest(1, "baseline", "test", "id", 19, false, BackupRestore.Hash(originalSchema),
                [new BackupFile("documents-00000001.json", BackupRestore.Hash(documentBytes), 19)]);
            await File.WriteAllTextAsync(Path.Combine(input, "manifest.json"), JsonSerializer.Serialize(manifest));
            var credential = new FakeCredential();
            var handler = new FakeFunction();
            using var http = new HttpClient(handler);
            foreach (var model in new[] { "fast", "pro" })
            {
                var settings = new Settings(input, Path.Combine(root, model), "test", "text", "vector",
                    model, 3, new Uri("https://offline.invalid"), "api://example-app", null);
                var function = new FunctionClient(http, credential, settings.FunctionBase, settings.FunctionAudience, model, 3);
                await Migration.Prepare(settings, function);
                var migrated = JsonSerializer.Deserialize<Manifest>(await File.ReadAllTextAsync(Path.Combine(settings.OutputDirectory, "manifest.json")))!;
                var output = await BackupRestore.ReadFile(settings.OutputDirectory, migrated.Files.Single());
                Assert(output.Count == 19 && output[0]!["vector"]!.AsArray().Count == 3, $"{model} regenerated all docs");
                Assert(output[0]!["text"]!.GetValue<string>() == "document-0", "text preserved");
                Assert(output[18]!["id"]!.GetValue<string>() == "018", "keys preserved");
                var changed = JsonNode.Parse(await File.ReadAllBytesAsync(Path.Combine(settings.OutputDirectory, "schema.json")))!;
                Assert(changed["future"]!["preserve"]!.GetValue<bool>(), "future schema property preserved");
                Assert(JsonNode.DeepEquals(changed["semantic"], schema["semantic"]), "semantic config preserved");
                Assert(changed["fields"]![2]!["dimensions"]!.GetValue<int>() == 3, "new dimensions recorded");
                var parameters = changed["vectorSearch"]!["vectorizers"]!.AsArray()[^1]!["customWebApiParameters"]!;
                Assert(parameters["uri"]!.GetValue<string>().EndsWith($"/api/embed/{model}/query", StringComparison.Ordinal), "query route differs from document route");
                Assert(parameters["authResourceId"]!.GetValue<string>() == "api://example-app" && parameters["httpHeaders"] is JsonObject { Count: 0 }, "keyless vectorizer");
                Assert(parameters["timeout"]!.GetValue<string>() == "PT3M50S", "query vectorizer uses canonical 230-second duration");
                await Throws(() => Migration.Prepare(settings, function), "nonempty migration directory rejected");
                Assert(BackupRestore.Hash(await File.ReadAllBytesAsync(Path.Combine(input, "schema.json"))) == manifest.SchemaSha256, "original schema unchanged");
                Assert(BackupRestore.Hash(await File.ReadAllBytesAsync(Path.Combine(input, "documents-00000001.json"))) == manifest.Files[0].Sha256, "original documents unchanged");
                var targetHandler = new FakeTarget();
                using var targetHttp = new HttpClient(targetHandler);
                using var targetApi = new SearchApi(targetHttp, new Uri("https://offline.invalid"), $"target-{model}", "test", new FakeCredential());
                await BackupRestore.Restore(targetApi, settings.OutputDirectory);
                Assert(targetHandler.Documents.Count == 19 && targetHandler.Documents["000"]["vector"]!.AsArray().Count == 3,
                    $"{model} migrated artifact passes complete shared restore and content verification");
                var legacyDirectory = Path.Combine(root, $"legacy-{model}");
                Directory.CreateDirectory(legacyDirectory);
                foreach (var file in migrated.Files)
                    File.Copy(Path.Combine(settings.OutputDirectory, file.Name), Path.Combine(legacyDirectory, file.Name));
                var legacySchema = Encoding.UTF8.GetBytes(LegacySchema(changed.AsObject()).ToJsonString());
                await File.WriteAllBytesAsync(Path.Combine(legacyDirectory, "schema.json"), legacySchema);
                await File.WriteAllTextAsync(Path.Combine(legacyDirectory, "manifest.json"),
                    JsonSerializer.Serialize(migrated with { SchemaSha256 = BackupRestore.Hash(legacySchema) }));
                var legacyHandler = new FakeTarget();
                using var legacyHttp = new HttpClient(legacyHandler);
                using var legacyApi = new SearchApi(legacyHttp, new Uri("https://offline.invalid"), $"legacy-{model}", "test", new FakeCredential());
                await BackupRestore.Restore(legacyApi, legacyDirectory);
                Assert(legacyHandler.Documents.Count == 19, $"{model} existing sparse backup restores against normalized GET without regeneration");
            }
            var observed = new JsonObject { ["name"] = "observed", ["vectorSearch"] = ObservedVectorSearch("fast") };
            var sparse = LegacySchema(observed);
            var sparseBefore = sparse.DeepClone();
            Assert(!JsonNode.DeepEquals(sparse, observed), "fixture captures the live failure of raw structural comparison");
            Assert(BackupRestore.SchemasEquivalent(sparse, observed), "seven confirmed GET default/duration expansions compare equal");
            Assert(JsonNode.DeepEquals(sparseBefore, sparse), "schema comparison does not rewrite saved source schema");
            void RejectMutation(Action<JsonObject> mutate, string message)
            {
                var altered = observed.DeepClone().AsObject();
                mutate(altered);
                Assert(!BackupRestore.SchemasEquivalent(sparse, altered), message);
            }
            RejectMutation(s => s["vectorSearch"]!["profiles"]![0]!["compression"] = "different", "nondefault compression remains strict");
            RejectMutation(s => s["vectorSearch"]!["algorithms"]![0]!["hnswParameters"]!["m"] = 8, "algorithm parameter changes remain strict");
            RejectMutation(s => s["vectorSearch"]!["algorithms"]![0]!["exhaustiveKnnParameters"] = new JsonObject { ["metric"] = "cosine" },
                "nondefault optional algorithm properties remain strict");
            RejectMutation(s => s["vectorSearch"]!["vectorizers"]![0]!["futureUnknownFeature"] = null, "unknown null properties are not discarded");
            RejectMutation(s => s["vectorSearch"]!["vectorizers"]![0]!["customWebApiParameters"]!["httpMethod"] = "GET", "nondefault HTTP method remains strict");
            RejectMutation(s => s["vectorSearch"]!["vectorizers"]![0]!["customWebApiParameters"]!["timeout"] = "PT229S", "different duration remains strict");
            RejectMutation(s => s["vectorSearch"]!["vectorizers"]![0]!["customWebApiParameters"]!["authResourceId"] = "api://different", "audience changes remain strict");
            RejectMutation(s => s["vectorSearch"]!["vectorizers"]![0]!["customWebApiParameters"]!["uri"] = "https://different.invalid/query", "endpoint changes remain strict");
            RejectMutation(s => s["vectorSearch"]!["vectorizers"]![0]!["customWebApiParameters"]!["httpHeaders"] = new JsonObject { ["header"] = "changed" },
                "nonempty headers remain strict");
            RejectMutation(s => s["vectorSearch"]!["vectorizers"]![0]!["customWebApiParameters"]!["authIdentity"] = new JsonObject { ["identity"] = "changed" },
                "identity changes remain strict");
            Assert(handler.DocumentBatchSizes.SequenceEqual(new[] { 4, 4, 4, 4, 3, 4, 4, 4, 4, 3 }), "bounded four-record document batches");
            Assert(handler.Roles[0] == "query" && handler.Roles[6] == "query", "query route probed before each document run");
            Assert(credential.Scopes.All(s => s == "api://example-app/.default"), "Entra audience requested");
            var userIdentity = "/subscriptions/example/resourceGroups/example/providers/Microsoft.ManagedIdentity/userAssignedIdentities/example";
            var identitySettings = new Settings(input, Path.Combine(root, "identity"), "test", "text", "vector",
                "fast", 3, new Uri("https://offline.invalid"), "api://example-app", userIdentity);
            var withIdentity = Migration.ChangeSchema(schema, identitySettings);
            Assert(withIdentity["vectorSearch"]!["vectorizers"]!.AsArray()[^1]!["customWebApiParameters"]!["authIdentity"]!["userAssignedIdentity"]!.GetValue<string>() == userIdentity,
                "optional Search caller user-assigned identity preserved");
            Assert(schema["fields"]![2]!["dimensions"]!.GetValue<int>() == 2, "schema builder does not mutate baseline");
            Throws(() => Migration.ChangeSchema(schema, identitySettings with { TextField = "missing" }), "missing text field rejected");
            Throws(() => FunctionClient.ValidateVector(JsonNode.Parse("[1,2]")!.AsArray(), 3), "wrong vector dimensions rejected");
            Throws(() => FunctionClient.ValidateVector(new JsonArray(double.PositiveInfinity), 1), "nonfinite values rejected");
            Throws(() => FunctionClient.ValidateVector(JsonNode.Parse("[1e100]")!.AsArray(), 1), "values outside Single range rejected");
            Throws(() => FunctionClient.ValidateVector(JsonNode.Parse("[null]")!.AsArray(), 1), "null components rejected");
            Throws(() => Settings.Https("https://offline.invalid?code=not-a-key", true), "key query URL rejected");

            var wrong = new FunctionClient(http, credential, new Uri("https://offline.invalid"), "api://example-app", "fast", 4);
            var wrongOutput = identitySettings with { OutputDirectory = Path.Combine(root, "wrong-dimensions"), Dimensions = 4 };
            await Throws(() => Migration.Prepare(wrongOutput, wrong), "query dimension mismatch aborts");
            Assert(!Directory.Exists(wrongOutput.OutputDirectory), "query failure creates no migration artifacts");
            handler.ReturnErrors = true;
            var bad = new FunctionClient(http, credential, new Uri("https://offline.invalid"), "api://example-app", "fast", 3);
            await Throws(() => bad.Embed("document", [("one", "text")]), "HTTP 200 per-record errors rejected");
            handler.ReturnErrors = false;
            handler.OmitResults = true;
            await Throws(() => bad.Embed("document", [("one", "text")]), "missing Function records rejected");
            handler.OmitResults = false;
            handler.FailureStatus = HttpStatusCode.Unauthorized;
            handler.FailuresRemaining = 3;
            var requestsBefore = handler.Roles.Count;
            await Throws(() => bad.Embed("document", [("one", "text")]), "HTTP 401 fails immediately");
            Assert(handler.Roles.Count == requestsBefore + 1, "HTTP 401 is not retried");
            handler.FailureStatus = HttpStatusCode.TooManyRequests;
            handler.FailuresRemaining = 1;
            requestsBefore = handler.Roles.Count;
            await bad.Embed("document", [("one", "text")]);
            Assert(handler.Roles.Count == requestsBefore + 2, "HTTP 429 retries after Retry-After HTTP date");
            handler.FailuresRemaining = 5;
            requestsBefore = handler.Roles.Count;
            try
            {
                await bad.Embed("document", [("one", "text")]);
                throw new InvalidOperationException("Exhausted throttling must fail.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("HTTP 429", StringComparison.Ordinal))
            {
                Assert(handler.Roles.Count == requestsBefore + 5, "HTTP 429 exhaustion surfaces after five attempts");
            }
            Assert(Program.HttpTimeoutSeconds >= 240, "Function timeout accommodates bounded server retries");
            Assert(Settings.BoundedInteger(null, "EMBEDDING_BATCH_SIZE", 4, 1, 16) == 4, "default embedding batch size is four");
            Assert(Settings.BoundedInteger("1", "EMBEDDING_BATCH_SIZE", 4, 1, 16) == 1, "single-record batch supported");
            Assert(Settings.BoundedInteger("16", "EMBEDDING_BATCH_SIZE", 4, 1, 16) == 16, "maximum batch supported");
            Throws(() => Settings.BoundedInteger("0", "EMBEDDING_BATCH_SIZE", 4, 1, 16), "zero batch rejected");
            Throws(() => Settings.BoundedInteger("17", "EMBEDDING_BATCH_SIZE", 4, 1, 16), "oversized batch rejected");
            Throws(() => Settings.BoundedInteger("invalid", "EMBEDDING_BATCH_SIZE", 4, 1, 16), "nonnumeric batch rejected");
            Assert(Settings.BoundedInteger(null, "FUNCTION_HTTP_TIMEOUT_SECONDS", Program.HttpTimeoutSeconds, 240, 1800) == 240,
                "default HTTP timeout is 240 seconds");
            Assert(Settings.BoundedInteger("300", "FUNCTION_HTTP_TIMEOUT_SECONDS", Program.HttpTimeoutSeconds, 240, 1800) == 300,
                "HTTP timeout can be extended");
            Throws(() => Settings.BoundedInteger("239", "FUNCTION_HTTP_TIMEOUT_SECONDS", Program.HttpTimeoutSeconds, 240, 1800),
                "HTTP timeout below server retry budget rejected");
            var configuredHandler = new FakeFunction();
            using var configuredHttp = new HttpClient(configuredHandler);
            var configuredFunction = new FunctionClient(configuredHttp, credential, new Uri("https://offline.invalid"), "api://example-app", "fast", 3);
            await Migration.Prepare(identitySettings with { OutputDirectory = Path.Combine(root, "configured-batch"), BatchSize = 16 }, configuredFunction);
            Assert(configuredHandler.DocumentBatchSizes.SequenceEqual(new[] { 16, 3 }), "configured batch size controls actual Function requests");
            Console.WriteLine($"PASS: {assertions} .NET migration offline assertions; no network requests.");
        }
        finally { Directory.Delete(root, true); }
    }

    private static JsonObject ObservedVectorSearch(string model)
    {
        var fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "service-normalized-vector-search.json"));
        return JsonNode.Parse(fixture.Replace("migration-fast-", $"migration-{model}-", StringComparison.Ordinal)
            .Replace("/fast/query", $"/{model}/query", StringComparison.Ordinal))!.AsObject();
    }

    private static JsonObject LegacySchema(JsonObject original)
    {
        var schema = original.DeepClone().AsObject();
        var search = schema["vectorSearch"]!;
        search["algorithms"]!.AsArray()[^1]!.AsObject().Remove("exhaustiveKnnParameters");
        search["profiles"]!.AsArray()[^1]!.AsObject().Remove("compression");
        var vectorizer = search["vectorizers"]!.AsArray()[^1]!.AsObject();
        vectorizer.Remove("azureOpenAIParameters");
        var parameters = vectorizer["customWebApiParameters"]!.AsObject();
        foreach (var name in new[] { "authIdentity", "httpHeaders", "httpMethod" }) parameters.Remove(name);
        parameters["timeout"] = "PT230S";
        return schema;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
        assertions++;
    }

    private static void Throws(Action action, string message)
    {
        try { action(); } catch { assertions++; return; }
        throw new InvalidOperationException($"Expected failure: {message}");
    }

    private static async Task Throws(Func<Task> action, string message)
    {
        try { await action(); } catch { assertions++; return; }
        throw new InvalidOperationException($"Expected failure: {message}");
    }

    private sealed class FakeCredential : TokenCredential
    {
        public List<string> Scopes { get; } = new();
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Scopes.AddRange(requestContext.Scopes);
            return new AccessToken("offline-token", DateTimeOffset.UtcNow.AddHours(1));
        }
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    private sealed class FakeFunction : HttpMessageHandler
    {
        public List<string> Roles { get; } = new();
        public List<int> DocumentBatchSizes { get; } = new();
        public bool ReturnErrors { get; set; }
        public bool OmitResults { get; set; }
        public HttpStatusCode FailureStatus { get; set; }
        public int FailuresRemaining { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert(request.Headers.Authorization?.Scheme == "Bearer" && !request.Headers.Contains("x-functions-key") &&
                !request.Headers.Contains("api-key") && request.RequestUri!.Query.Length == 0, "Function calls are bearer-only");
            var role = request.RequestUri!.Segments[^1];
            Roles.Add(role);
            if (FailuresRemaining > 0)
            {
                FailuresRemaining--;
                var failure = new HttpResponseMessage(FailureStatus);
                failure.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(-1));
                return failure;
            }
            var records = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!["values"]!.AsArray();
            if (role == "document") DocumentBatchSizes.Add(records.Count);
            var values = new JsonArray();
            if (!OmitResults)
                foreach (var record in records)
                {
                    if (ReturnErrors)
                        values.Add(new JsonObject { ["recordId"] = record!["recordId"]!.GetValue<string>(),
                            ["errors"] = new JsonArray(new JsonObject { ["message"] = "mock provider rejection" }) });
                    else
                        values.Add(new JsonObject { ["recordId"] = record!["recordId"]!.GetValue<string>(),
                            ["data"] = new JsonObject { ["vector"] = new JsonArray(0.1, -0.5, 0.25) } });
                }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(new JsonObject { ["values"] = values }.ToJsonString(), Encoding.UTF8, "application/json")
            };
        }

    }

    private sealed class FakeTarget : HttpMessageHandler
        {
            private JsonObject? schema;
            public Dictionary<string, JsonObject> Documents { get; } = new(StringComparer.Ordinal);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert(request.Headers.Authorization?.Scheme == "Bearer" && !request.Headers.Contains("api-key"), "migration target is keyless");
                var path = request.RequestUri!.AbsolutePath;
                var body = request.Content == null ? null : JsonNode.Parse(await request.Content.ReadAsStringAsync(cancellationToken))!;
                JsonNode result;
                if (request.Method == HttpMethod.Put)
                {
                    Assert(request.Headers.IfNoneMatch.Any(h => h.Tag == "*"), "migration target is create-only");
                    schema = body!.DeepClone().AsObject();
                    result = schema.DeepClone();
                }
                else if (path.EndsWith("/docs/$count", StringComparison.Ordinal))
                    result = JsonValue.Create(Documents.Count)!;
                else if (path.EndsWith("/docs/index", StringComparison.Ordinal))
                {
                    var results = new JsonArray();
                    foreach (var doc in body!["value"]!.AsArray())
                    {
                        var copy = doc!.DeepClone().AsObject();
                        var id = copy["id"]!.GetValue<string>();
                        copy.Remove("@search.action");
                        Documents[id] = copy;
                        results.Add(new JsonObject { ["key"] = id, ["status"] = true, ["statusCode"] = 201 });
                    }
                    result = new JsonObject { ["value"] = results };
                }
                else if (path.EndsWith("/docs/search", StringComparison.Ordinal))
                    result = new JsonObject { ["value"] = new JsonArray(Documents.OrderBy(p => p.Key, StringComparer.Ordinal)
                        .Select(p => p.Value.DeepClone()).ToArray()) };
                else
                {
                    result = schema!.DeepClone();
                    var search = result["vectorSearch"]!;
                    var model = search["vectorizers"]!.AsArray()[^1]!["name"]!.GetValue<string>().StartsWith("migration-pro-", StringComparison.Ordinal) ? "pro" : "fast";
                    var observed = ObservedVectorSearch(model);
                    foreach (var section in new[] { "algorithms", "profiles", "vectorizers" })
                        search[section]!.AsArray()[^1] = observed[section]![0]!.DeepClone();
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(result.ToJsonString(), Encoding.UTF8, "application/json")
                };
            }
    }
}
