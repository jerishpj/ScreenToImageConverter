namespace ScreenToImageConverter.Worker.AppSettings;

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public class BlobStorageOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Connection string to the Blob Storage account.
    /// Use Managed Identity in production instead of connection strings.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Account name (e.g., "myaccount" for "myaccount.blob.core.windows.net").
    /// Required when using Managed Identity or token-based authentication.
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    /// Name of the blob container where screenshots are stored.
    /// Default: "screenshots"
    /// </summary>
    public string ContainerName { get; set; } = "screenshots";

    /// <summary>
    /// Whether to use Managed Identity for authentication.
    /// If true, AccountName must be set; ConnectionString is ignored.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Duration in minutes for which SAS URLs are valid.
    /// Default: 60 minutes
    /// </summary>
    public int SasUrlExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to create the container automatically if it doesn't exist.
    /// Default: true
    /// </summary>
    public bool AutoCreateContainer { get; set; } = true;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public ICollection<string> Validate()
    {
        var errors = new List<string>();

        if (UseManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(AccountName))
                errors.Add($"{nameof(AccountName)} is required when UseManagedIdentity is true.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                errors.Add($"{nameof(ConnectionString)} is required when UseManagedIdentity is false.");
        }

        if (string.IsNullOrWhiteSpace(ContainerName))
            errors.Add($"{nameof(ContainerName)} is required.");

        if (SasUrlExpirationMinutes <= 0)
            errors.Add($"{nameof(SasUrlExpirationMinutes)} must be greater than 0.");

        return errors;
    }
}
