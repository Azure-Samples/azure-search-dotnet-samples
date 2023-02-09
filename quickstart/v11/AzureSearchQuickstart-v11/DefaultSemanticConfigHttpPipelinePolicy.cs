using Azure.Core;
using Azure.Core.Pipeline;
using System.IO;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using System;

namespace AzureSearchQuickstart_v11
{
    public sealed class DefaultSemanticConfigHttpPipelinePolicy : HttpPipelineSynchronousPolicy
    {
        public override void OnSendingRequest(HttpMessage message)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // investigate the request content
            var requestContent = message.Request.Content;

            if (requestContent is not null)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    requestContent.WriteTo(stream, token);
                    stream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();


                    JsonObject contentJson = JsonSerializer.Deserialize<JsonObject>(content);

                    if (contentJson["semantic"] is not null && contentJson["semantic"]!["configurations"] is not null)
                    {
                        string str = "hello";
                        JsonNode semantic = contentJson["semantic"]!;
                        semantic["defaultConfiguration"] = "semconfig";
                        contentJson["semantic"] = semantic;
                        string text = contentJson.ToJsonString();
                        message.Request.Content = RequestContent.Create(text);
                    }
                }
            }

        }
    }
}
