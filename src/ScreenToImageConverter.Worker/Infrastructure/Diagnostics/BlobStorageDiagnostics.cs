using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Worker.Infrastructure.Storage;

namespace ScreenToImageConverter.Worker.Infrastructure.Diagnostics;

/// <summary>
/// Diagnostic helper for Azure Blob Storage configuration and connectivity.
/// Use this to troubleshoot blob storage issues during development/testing.
/// </summary>
public class BlobStorageDiagnostics
{
    private readonly StorageSettings _settings;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<BlobStorageDiagnostics> _logger;

    public BlobStorageDiagnostics(
        IOptions<StorageSettings> settings,
        IBlobStorageService blobStorageService,
        ILogger<BlobStorageDiagnostics> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs comprehensive diagnostics on Blob Storage configuration and connectivity.
    /// </summary>
    public async Task<BlobStorageDiagnosticsResult> RunDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var result = new BlobStorageDiagnosticsResult();

        _logger.LogInformation("\n╔════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║         Azure Blob Storage Diagnostics                       ║");
        _logger.LogInformation("╚════════════════════════════════════════════════════════════╝");

        // 1. Configuration Validation
        _logger.LogInformation("\n[1/4] Configuration Validation");
        _logger.LogInformation("─────────────────────────────────────────────────────────────");
        result.ConfigurationValid = ValidateConfiguration();

        // 2. Authentication Method
        _logger.LogInformation("\n[2/4] Authentication Method");
        _logger.LogInformation("─────────────────────────────────────────────────────────────");
        LogAuthenticationMethod();

        // 3. Connectivity Test
        _logger.LogInformation("\n[3/4] Connectivity Test");
        _logger.LogInformation("─────────────────────────────────────────────────────────────");
        result.IsConnected = await TestConnectivityAsync(cancellationToken);

        // 4. Summary
        _logger.LogInformation("\n[4/4] Summary");
        _logger.LogInformation("─────────────────────────────────────────────────────────────");
        PrintSummary(result);

        _logger.LogInformation("\n╚════════════════════════════════════════════════════════════╝\n");

        return result;
    }

    private bool ValidateConfiguration()
    {
        try
        {
            var errors = _settings.Validate();

            if (errors.Count == 0)
            {
                _logger.LogInformation("✅ Configuration is valid");
                return true;
            }

            _logger.LogError("❌ Configuration validation failed:");
            foreach (var error in errors)
            {
                _logger.LogError("   • {Error}", error);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error validating configuration: {Message}", ex.Message);
            return false;
        }
    }

    private void LogAuthenticationMethod()
    {
        if (_settings.UseManagedIdentity)
        {
            _logger.LogInformation("📋 Authentication: Managed Identity");
            _logger.LogInformation("   Account Name: {AccountName}", _settings.AccountName ?? "(not set)");
            _logger.LogInformation("   URI: https://{AccountName}.blob.core.windows.net", 
                _settings.AccountName ?? "(not set)");
            _logger.LogInformation("   Credential: DefaultAzureCredential (Azure CLI, VS, or MSI)");
        }
        else
        {
            _logger.LogInformation("📋 Authentication: Connection String");

            if (!string.IsNullOrEmpty(_settings.ConnectionString))
            {
                var accountName = ExtractAccountNameFromConnectionString(_settings.ConnectionString);
                _logger.LogInformation("   Account Name: {AccountName}", accountName ?? "(unable to extract)");
                _logger.LogInformation("   Connection String: ***{Suffix}", 
                    _settings.ConnectionString.Length > 20 
                        ? _settings.ConnectionString.Substring(_settings.ConnectionString.Length - 20) 
                        : "***");
            }
            else
            {
                _logger.LogWarning("   Connection String: (not set) ⚠️");
            }
        }

        _logger.LogInformation("   Container: {Container}", _settings.ContainerName);
        _logger.LogInformation("   Auto-create Container: {AutoCreate}", _settings.AutoCreateContainer);
        _logger.LogInformation("   SAS URL Expiration: {Minutes} minutes", _settings.SasUrlExpirationMinutes);
    }

    private async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Testing connectivity to blob storage...");
            var isConnected = await _blobStorageService.IsConnectedAsync(cancellationToken);

            if (isConnected)
            {
                _logger.LogInformation("✅ Connectivity Test: PASSED");
                _logger.LogInformation("   → Container '{Container}' is accessible", _settings.ContainerName);
                return true;
            }
            else
            {
                _logger.LogWarning("⚠️  Connectivity Test: INCONCLUSIVE");
                _logger.LogWarning("   → Unable to verify container access");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Connectivity Test: FAILED");
            _logger.LogError("   Error Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("   Message: {Message}", ex.Message);

            // Provide helpful context based on error
            if (ex.Message.Contains("AccountName") || ex.Message.Contains("account-name"))
            {
                _logger.LogError("   💡 Hint: Check that AccountName is set correctly (without .blob.core.windows.net)");
            }
            else if (ex.Message.Contains("authorization") || ex.Message.Contains("permission") || ex.Message.Contains("Unauthorized"))
            {
                _logger.LogError("   💡 Hint: Check RBAC permissions. You may need 'Storage Blob Data Contributor' role");
            }
            else if (ex.Message.Contains("connection") || ex.Message.Contains("Connection"))
            {
                _logger.LogError("   💡 Hint: Verify connection string is correct. Get it from: Azure Portal → Storage Account → Access Keys");
            }
            else if (ex.Message.Contains("container") || ex.Message.Contains("Container"))
            {
                _logger.LogError("   💡 Hint: The container may not exist. Set 'AutoCreateContainer: true' or create it manually");
            }

            return false;
        }
    }

    private void PrintSummary(BlobStorageDiagnosticsResult result)
    {
        _logger.LogInformation("Configuration Valid:  {Status}", result.ConfigurationValid ? "✅ Yes" : "❌ No");
        _logger.LogInformation("Connected to Blob:    {Status}", result.IsConnected ? "✅ Yes" : "❌ No");

        var overallStatus = result.ConfigurationValid && result.IsConnected;
        if (overallStatus)
        {
            _logger.LogInformation("\n🎉 All checks passed! Blob storage is ready to use.");
        }
        else
        {
            _logger.LogWarning("\n⚠️  Some checks failed. Review the output above for details.");
            _logger.LogWarning("   See: Docs/AZURE_BLOB_SETUP_GUIDE.md for troubleshooting steps");
        }
    }

    private string? ExtractAccountNameFromConnectionString(string connectionString)
    {
        try
        {
            var accountNamePart = connectionString
                .Split(';')
                .FirstOrDefault(p => p.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase));

            return accountNamePart?.Substring("AccountName=".Length);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Exports diagnostics result as formatted string.
    /// </summary>
    public string ExportAsString(BlobStorageDiagnosticsResult result)
    {
        return $"""
            Azure Blob Storage Diagnostics Report
            ======================================
            Configuration Valid: {(result.ConfigurationValid ? "✅ Yes" : "❌ No")}
            Connected to Blob:   {(result.IsConnected ? "✅ Yes" : "❌ No")}
            Status: {(result.IsHealthy ? "🟢 Healthy" : "🔴 Unhealthy")}
            """;
    }
}

/// <summary>
/// Result of blob storage diagnostics.
/// </summary>
public class BlobStorageDiagnosticsResult
{
    public bool ConfigurationValid { get; set; }
    public bool IsConnected { get; set; }

    public bool IsHealthy => ConfigurationValid && IsConnected;
}

