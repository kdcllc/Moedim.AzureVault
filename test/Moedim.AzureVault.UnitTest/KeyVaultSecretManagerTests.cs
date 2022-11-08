using Azure;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

using Moedim.AzureVault.KeyVaultSecretManagers;

using Moq;

namespace Moedim.AzureVault.UnitTest;

/// <summary>
/// Testing code was taken from: https://github.com/Azure/azure-sdk-for-net/blob/f6319e4b06a8dbc8bd6913b02c0f19f6dc09c15c/sdk/extensions/Azure.Extensions.AspNetCore.Configuration.Secrets/tests/AzureKeyVaultConfigurationTests.cs#L19.
/// </summary>
public class KeyVaultSecretManagerTests
{
    [Theory]
    [InlineData("Secret1", "Value1", "dev")]
    [InlineData("Secret2", "Value2", "prod")]
    public void Loads_All_Prefixed_Secrets_From_Vault(string key, string value, string prefix)
    {
        var client = new Mock<SecretClient>();
        SetPages(
            client,
            new[]
            {
                CreateSecret($"{prefix}--{key}", value)
            },
            new[]
            {
                CreateSecret($"{prefix}--Secret4", "Value4")
            });

        // Act
        using var provider = new AzureKeyVaultConfigurationProvider(
                                client.Object,
                                new AzureKeyVaultConfigurationOptions()
                                {
                                    Manager = new PrefixKeyVaultSecretManager(prefix)
                                });
        provider.Load();

        var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
        Assert.Equal(new[] { key, "Secret4" }, childKeys);

        Assert.True(provider.TryGet(key, out var v));

        Assert.Equal(v, value);
    }

    private void SetPages(Mock<SecretClient> mock, params KeyVaultSecret[][] pages)
    {
        SetPages(mock, null!, pages);
    }

    private void SetPages(Mock<SecretClient> mock, Func<string, Task> getSecretCallback, params KeyVaultSecret[][] pages)
    {
        getSecretCallback ??= _ => Task.CompletedTask;

        var pagesOfProperties = pages.Select(
            page => page.Select(secret => secret.Properties).ToArray()).ToArray();

        mock.Setup(m => m.GetPropertiesOfSecretsAsync(default)).Returns(new MockAsyncPageable(pagesOfProperties));

        foreach (var page in pages)
        {
            foreach (var secret in page)
            {
                mock.Setup(client => client.GetSecretAsync(secret.Name, null, default))
                    .Returns(async (string name, string label, CancellationToken token) =>
                    {
                        await getSecretCallback(name);
                        return Response.FromValue(secret, Mock.Of<Response>());
                    });
            }
        }
    }

    private KeyVaultSecret CreateSecret(string name, string value, bool? enabled = true, DateTimeOffset? updated = null)
    {
        var id = new Uri("http://azure.keyvault/" + name);

        var secretProperties = SecretModelFactory.SecretProperties(id, name: name, updatedOn: updated);
        secretProperties.Enabled = enabled;

        return SecretModelFactory.KeyVaultSecret(secretProperties, value);
    }

    private class MockAsyncPageable : AsyncPageable<SecretProperties>
    {
        private readonly SecretProperties[][] _pages;

        public MockAsyncPageable(SecretProperties[][] pages)
        {
            _pages = pages;
        }

        public override async IAsyncEnumerable<Page<SecretProperties>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            foreach (var page in _pages)
            {
                yield return Page<SecretProperties>.FromValues(page, null, Mock.Of<Response>());
            }

            await Task.CompletedTask;
        }
    }
}
