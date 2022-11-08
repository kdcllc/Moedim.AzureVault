using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Moedim.AzureVault.Options;

/// <summary>
/// Provides a place holder for validation and creation of Azure Vault.
/// </summary>
public class AzureVaultOptions
{
    /// <summary>
    /// Url for Azure Vault 'https://{name}.vault.azure.net/'.
    /// </summary>
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The Azure Active Directory tenant (directory) Id of the service principal.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The client (application) ID of the service principal.
    /// </summary>
    [RegularExpression(@"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$", ErrorMessage = "Must be valid Guid Id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Client Secret must be Base64String.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });
    }
}
