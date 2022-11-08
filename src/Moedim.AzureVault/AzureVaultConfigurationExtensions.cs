using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Options;

using Moedim.AzureVault;
using Moedim.AzureVault.KeyVaultSecretManagers;
using Moedim.AzureVault.Options;

namespace Microsoft.Extensions.Configuration;

public static class AzureVaultConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault client based on enviroment prefix.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> builder.</param>
    /// <param name="hostingEnviromentName">The hosting enviroment name.</param>
    /// <param name="sectionName">The configucation section name.</param>
    /// <param name="configure">The configuration of the <see cref="AzureVaultOptions"/>.</param>
    /// <param name="reloadInterval">The reload interval for the Azure Key Vault. The default is null.</param>
    /// <param name="tokenAuthRetry">The number of retries for the client. The default is 2 times.</param>
    /// <returns></returns>
    /// <exception cref="OptionsValidationException">The validation exception for <see cref="AzureVaultOptions"/>.</exception>
    /// <exception cref="ArgumentException">The validatione exception for maping between enviroment and prefix.</exception>
    public static IConfigurationBuilder AddAzureKeyVault(
        this IConfigurationBuilder builder,
        string hostingEnviromentName,
        string sectionName = "AzureVault",
        Action<AzureVaultOptions>? configure = null,
        TimeSpan? reloadInterval = null,
        int tokenAuthRetry = 2)
    {
        var config = builder.Build();
        var options = new AzureVaultOptions();

        config.GetSection(sectionName).Bind(options);

        configure?.Invoke(options);

        var optionsName = typeof(AzureVaultOptions).Name;

        var validator = new DataAnnotationValidateOptions<AzureVaultOptions>(optionsName);

        var result = validator.Validate(optionsName, options);
        if (result.Failed)
        {
            throw new OptionsValidationException(optionsName, options.GetType(), result.Failures);
        }

        var defaultMap = new DefaultSecretPrefixes();
        if (!defaultMap.TryGetValue(hostingEnviromentName, out var prefix))
        {
            throw new ArgumentException("Hosting Enviroment Name doesn't match any default prefixes");
        }

        if (!string.IsNullOrWhiteSpace(options?.TenantId)
            && !string.IsNullOrWhiteSpace(options?.ClientId)
            && !string.IsNullOrWhiteSpace(options?.ClientSecret))
        {
            var clientSecret = options?.ClientSecret.FromBase64String();
            var cred = new ClientSecretCredential(options?.TenantId, options?.ClientId, clientSecret);

            return builder.AddAzureKeyVault(
                options?.BaseUrl ?? string.Empty,
                cred,
                defaultMap.Values.ToArray(),
                prefix,
                reloadInterval,
                tokenAuthRetry);
        }

        return builder.AddAzureKeyVault(
            options?.BaseUrl ?? string.Empty,
            new DefaultAzureCredential(),
            defaultMap.Values.ToArray(),
            prefix,
            reloadInterval,
            tokenAuthRetry);
    }

    /// <summary>
    /// Adds Azure Key Vault client based on prefix.
    /// Allow to include or exclude them from the reading from configuration provider.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> builder.</param>
    /// <param name="keyVaultEndpoints">The default Azure Key Vaults values separated by ';'.</param>
    /// <param name="credential">The Credential with the details needed to authenticate against Azure Active Directory with a client secret.</param>
    /// <param name="excludePrefix">The prefixes to exclude from the vault.</param>
    /// <param name="prefix">The prefix to be used to add the client with. The default is empty.</param>
    /// <param name="reloadInterval">The reload interval for the Azure Key Vault.</param>
    /// <param name="tokenAuthRetry">The number of retries for the client. The default is 2 times.</param>
    /// <returns></returns>
    public static IConfigurationBuilder AddAzureKeyVault(
        this IConfigurationBuilder builder,
        string keyVaultEndpoints,
        TokenCredential credential,
        string[] excludePrefix,
        string prefix = "",
        TimeSpan? reloadInterval = null,
        int tokenAuthRetry = 2)
    {
        foreach (var keyvault in keyVaultEndpoints.Split(';'))
        {
            var clientOptions = new SecretClientOptions()
            {
                Retry =
                    {
                        Delay = TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                        MaxRetries = tokenAuthRetry,
                        Mode = RetryMode.Exponential
                    }
            };

            var client = new SecretClient(new Uri(keyvault), credential, clientOptions);

            var noPrefix = new AzureKeyVaultConfigurationOptions
            {
                Manager = new PrefixExcludingKeyVaultSecretManager(excludePrefix),
                ReloadInterval = reloadInterval,
            };

            builder.AddAzureKeyVault(client, noPrefix);

            if (!string.IsNullOrEmpty(prefix))
            {
                var prefixOptions = new AzureKeyVaultConfigurationOptions
                {
                    Manager = new PrefixKeyVaultSecretManager(prefix),
                    ReloadInterval = reloadInterval,
                };

                builder.AddAzureKeyVault(client, prefixOptions);
            }
        }

        return builder;
    }
}
