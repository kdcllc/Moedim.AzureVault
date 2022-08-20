using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Moedim.AzureVault.KeyVaultSecretManagers
{
    /// <summary>
    /// Manager for loading all keys that are not prefixed with an environment.
    /// </summary>
    public class PrefixExcludingKeyVaultSecretManager : KeyVaultSecretManager
    {
        private readonly string[] _envronments;

        public PrefixExcludingKeyVaultSecretManager(string[] envronments)
        {
            _envronments = envronments;
        }

        public override bool Load(SecretProperties secret)
        {
            // Load a vault secret when its secret name starts with the
            // prefix. Other secrets won't be loaded.
            var secretName = secret.Name;

            var envIndex = secretName.IndexOf("--");

            if (envIndex > -1)
            {
                var env = secretName.Substring(0, envIndex);

                return !_envronments.Contains(env);
            }

            return true;
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            // Remove the prefix from the secret name and replace two
            // dashes in any name with the KeyDelimiter, which is the
            // delimiter used in configuration (usually a colon). Azure
            // Key Vault doesn't allow a colon in secret names.
            return secret.Name.Replace("--", ConfigurationPath.KeyDelimiter);
        }
    }
}
