# Moedim.AzureVault

[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://raw.githubusercontent.com/kdcllc/Moedim.AzureVault/master/LICENSE)
![master workflow](https://github.com/kdcllc/Moedim.AzureVault/actions/workflows/master.yml/badge.svg)[![NuGet](https://img.shields.io/nuget/v/Moedim.AzureVault.svg)](https://www.nuget.org/packages?q=Moedim.AzureVault)
![Nuget](https://img.shields.io/nuget/dt/Moedim.AzureVault)
[![feedz.io](https://img.shields.io/badge/endpoint.svg?url=https://f.feedz.io/kdcllc/moedim/shield/Moedim.AzureVault/latest)](https://f.feedz.io/kdcllc/moedim/packages/Moedim.AzureVault/latest/download)

> This is a Hebrew word that translates "feast" or "appointed time."
> "Appointed times" refers to HaSham's festivals in Vayikra/Leviticus 23rd.
> The feasts are "signals and signs" to help us know what is on the heart of HaShem.

_Note: Pre-release packages are distributed via [feedz.io](https://f.feedz.io/kdcllc/moedim/nuget/index.json)._

This goal of this repo is to provide with a reusable Azure Vault Key functionality for developing Microservice.

On application start, the secrets are loaded from Azure Key Vault based on hosting environment or other prefixes i.e. `local--mykey`.

## Hire me

Please send [email](mailto:kingdavidconsulting@gmail.com) if you consider to **hire me**.

[![buymeacoffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/vyve0og)

## Give a Star! :star:

If you like or are using this project to learn or start your solution, please give it a star. Thanks!

## Install

```csharp
    dotnet add package Moedim.AzureVault
```

## Usage

If Azure AD App credentials are used than `TenantId`, `ClientId` and `ClientSecret` are also required values.

For Azure AD `DefaultCredential` provider to be used only `BaseUrl` is required. The extension method validates `BaseUrl` value.

```json
"AzureVault": {
    "BaseUrl": "https://kdcllc.vault.azure.net/",
    "TenantId" : "",
    "ClientId": "",
    "ClientSecret": ""
  }
```

We configure our host to use Azure Key Vault with reload based on hosting environment and 10 seconds interval for reloading the secrets.

```csharp
     Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, configBuilder) =>
        {
            // based on environment Development = dev; Production = prod prefix in Azure Vault.
            var envName = hostingContext.HostingEnvironment.EnvironmentName;
            var configuration = configBuilder.AddAzureKeyVault(
                envName,
                reloadInterval: TimeSpan.FromSeconds(10));

            // helpful to see what was retrieved from all of the configuration providers.
            if (hostingContext.HostingEnvironment.IsDevelopment())
            {
                configuration.DebugConfigurations();
            }
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.AddOptions<SampleOptions>().Bind(hostContext.Configuration.GetSection("Sample"));

            services.AddHostedService<Worker>();
        });
```

Then

```csharp

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private SampleOptions _options;

        public Worker(ILogger<Worker> logger, IOptionsMonitor<SampleOptions> options)
        {
            _logger = logger;

            _options = options.CurrentValue;

            // makes the update to the options based on 10 seconds.
            options.OnChange((opt) =>
            {
                _options = opt;
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time} - {name}", DateTimeOffset.Now, _options.Name);

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
```

## References

- [Bet.Extensions.AzureVault](https://github.com/kdcllc/Bet.Extensions/tree/master/src/Bet.Extensions.AzureVault)
- [Azure Key Vault Secrets configuration provider for Microsoft.Extensions.Configuration](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/extensions.aspnetcore.configuration.secrets-readme)
- [Tutorial: Use a managed identity to connect Key Vault to an Azure web app in .NET](https://docs.microsoft.com/en-us/azure/key-vault/general/tutorial-net-create-vault-azure-web-app)
- [Dependency injection with the Azure SDK for .NET](https://docs.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection)


```csharp
        private void CheckConfiguration(IApplicationBuilder app, IServiceCollection services)
        {
            var optionsServiceDescriptors = services.Where(s => s.ServiceType.Name.Contains("IOptionsChangeTokenSource"));

            foreach (var service in optionsServiceDescriptors)
            {
                var genericTypes = service.ServiceType.GenericTypeArguments;

                if (genericTypes.Length > 0)
                {
                    var optionsType = genericTypes[0];
                    var genericOptions = typeof(IOptions<>).MakeGenericType(optionsType);

                    dynamic instance = app.ApplicationServices.GetService(genericOptions);
                    var options = instance.Value;
                    var results = new List<ValidationResult>();

                    var isValid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);
                    if (!isValid)
                    {
                        var messages = new List<string> { "Configuration issues" };
                        messages.AddRange(results.Select(r => r.ErrorMessage));
                        throw new Exception(string.Join("\n", messages));
                    }
                }
            }
        }
```