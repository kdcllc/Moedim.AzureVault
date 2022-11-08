namespace Moedim.AzureVault;

/// <summary>
/// Default secret prefixes based on application enviroment.
/// </summary>
public class DefaultSecretPrefixes : Dictionary<string, string>
{
    public DefaultSecretPrefixes() : base(new Dictionary<string, string>
        {
             { "Development", "dev" },
             { "Staging", "qa" },
             { "Production", "prod" }
        })
    {
    }
}
