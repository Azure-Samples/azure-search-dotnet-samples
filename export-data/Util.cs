using System.Text.Encodings.Web;
using System.Text.Json;

namespace export_data
{
    public static class Util
    {
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            // Required to put non-ASCII characters in JSON files. To learn more, please visit https://learn.microsoft.com/dotnet/api/system.text.encodings.web.javascriptencoder.unsaferelaxedjsonescaping
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
