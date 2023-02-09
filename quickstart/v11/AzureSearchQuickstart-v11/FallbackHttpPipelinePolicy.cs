using Azure.Core;
using Azure.Core.Pipeline;
using System.IO;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using System;

namespace AzureSearchQuickstart_v11
{
    public sealed class FallbackHttpPipelinePolicy : HttpPipelineSynchronousPolicy
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

                    JsonNode contentJson = JsonNode.Parse(content);
                    contentJson!["semanticErrorHandling"] = "partial";
                    contentJson!["SemanticMaxWaitInMilliseconds"] = "1500";

                    string text = contentJson.ToJsonString();
                    message.Request.Content = RequestContent.Create(text);
                }
            }

        }

        public override void OnReceivedResponse(HttpMessage message)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // investigate the request content
            var responseContent = message.Response.Content;

            if (message.HasResponse)
            {
                var response = message.Response.Content;
                Console.WriteLine("Search request Response Contents");
                Console.WriteLine(response.ToString());
            }

        }
    }
}
