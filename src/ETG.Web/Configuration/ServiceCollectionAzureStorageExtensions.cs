using CMS.IO;
using Kentico.Xperience.AzureStorage;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the Kentico Xperience Azure Blob Storage provider.
///
/// Mirrors the K13 AzureStorageModule setup:
///   - Account name  → CMSAzureAccountName  (appsettings / env var)
///   - Shared key    → CMSAzureSharedKey    (appsettings / env var / Key Vault)
///   - Container     → CMSAzureContainerName (default "etg", lowercase — required by Azure)
///
/// The XbyK package picks up CMSAzureAccountName and CMSAzureSharedKey automatically
/// from IConfiguration (same key names Kentico uses internally via AccountInfo).
/// 
/// IMPORTANT: For Content Item Assets to work with Azure Blob Storage:
/// 1. Azure storage must be enabled in configuration (AzureStorage:Enabled = true)
/// 2. The assets are stored at path: ~/assets/contentitems/{Identifier}/{FileName}
/// 3. The Kentico.Xperience.AzureStorage package handles the mapping automatically
/// 4. Asset metadata (Identifier, Name, Size) is already in the database
/// 5. This configuration tells Kentico to fetch the actual binary from Azure Blob Storage
/// </summary>
public static class ServiceCollectionAzureStorageExtensions
{
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        var isEnabled = config.GetValue<bool>("AzureStorage:Enabled");

        if (!isEnabled)
        {
            return services;
        }

        // Register Azure Blob Storage path mapping for Kentico Xperience
        // This maps ALL CMS storage paths (including Content Item Assets) to Azure Blob Storage
        // The assets will be accessible at: https://{accountname}.blob.core.windows.net/{container}/assets/contentitems/{Identifier}/{Name}
        services.AddAppServiceStoragePathMapping(options =>
        {
            options.IsMappingEnabled = true;
            options.ContainerName = config["CMSAzureContainerName"] ?? "etgfilescontainer";
        });

        // Sets the cleanup threshold to 10 GB to prevent disk bloating (Kentico XbyK official standard)
        services.Configure<AzureStorageCleanerOptions>(options =>
        {
            options.Treshold = 10L * 1024L * 1024L * 1024L; // 10 GB
        });

        return services;
    }
}
