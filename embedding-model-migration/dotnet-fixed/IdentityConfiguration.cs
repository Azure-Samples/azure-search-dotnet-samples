using Azure.Core;
using Azure.Identity;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DotnetEmbeddingMigration")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DotnetInPlaceMigration")]

namespace AzureSearchBackupRestoreIndex;

internal static class IdentityConfiguration
{
    public static TokenCredential Create() => Environment.GetEnvironmentVariable("AZURE_AUTH_MODE") switch
    {
        "cli" => new AzureCliCredential(),
        null or "" or "default" => new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
            ExcludeInteractiveBrowserCredential = true,
            ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")
        }),
        _ => throw new InvalidOperationException("AZURE_AUTH_MODE must be default or cli. No key/secret authentication is supported.")
    };
}
