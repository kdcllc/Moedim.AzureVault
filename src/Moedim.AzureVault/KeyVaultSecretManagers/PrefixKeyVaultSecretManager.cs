using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Configuration;

namespace Moedim.AzureVault.KeyVaultSecretManagers;

/// <summary>
/// Custom Azure Vault Implementation for the prefix dev2--Key--Value syntax.
/// </summary>
public class PrefixKeyVaultSecretManager : KeyVaultSecretManager
{
    private readonly string _prefix;

    public PrefixKeyVaultSecretManager(string prefix)
    {
        _prefix = $"{prefix}--";
    }

    public override bool Load(SecretProperties secret)
    {
        // Load a vault secret when its secret name starts with the
        // prefix. Other secrets won't be loaded.
        return secret.Name.StartsWith(_prefix);
    }

    public override string GetKey(KeyVaultSecret secret)
    {
        // Remove the prefix from the secret name and replace two
        // dashes in any name with the KeyDelimiter, which is the
        // delimiter used in configuration (usually a colon). Azure
        // Key Vault doesn't allow a colon in secret names.
        return secret.Name.Substring(_prefix.Length).Replace("--", ConfigurationPath.KeyDelimiter);
    }
}
