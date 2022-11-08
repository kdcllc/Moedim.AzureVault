using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Moedim.AzureVault.Options;

namespace Moedim.AzureVault.UnitTest;

public class ConfigurationBuilderTests
{
    [Fact]
    public void Validating_Options_ThrowsOptions_ValidationException()
    {
        var builder = new ConfigurationBuilder();

        var dict = new Dictionary<string, string>
        {
            { "AzureVault:BaseUrl", string.Empty }
        };

        builder.AddInMemoryCollection(dict!);

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(builder.Build());

        services.AddOptions<AzureVaultOptions>().ValidateDataAnnotations();

        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<AzureVaultOptions>>();

        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Create_AzureVault_Client_Per_Enviroment_Successfully(string env)
    {
        var builder = new ConfigurationBuilder();

        var dict = new Dictionary<string, string>
        {
            { "AzureVault:BaseUrl", "https://bet.vault.azure.net/;https://kdcllc.vault.azure.net/" }
        };

        builder.AddInMemoryCollection(dict!);

        builder.AddAzureKeyVault(env, tokenAuthRetry: 1);

        var list = builder.Sources.ToList();
        var found = list.Where(x => x.GetType()?.FullName?.Contains("AzureKeyVaultConfigurationSource") ?? false);

        Assert.Equal(4, found.Count());

        // var config = builder.Build();
        // Assert.NotNull(config);
    }
}