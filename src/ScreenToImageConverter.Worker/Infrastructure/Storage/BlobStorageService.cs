using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ScreenToImageConverter.Worker.Infrastructure.Storage;

/// <summary>
/// Implementation of IBlobStorageService using Azure Blob Storage.
/// Handles blob upload, SAS URL generation, and lifecycle management.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<BlobStorageService> _logger;
    private BlobContainerClient? _containerClient;
    private bool _disposed;

    public BlobStorageService(
        IOptions<StorageSettings> settings,
        ILogger<BlobStorageService> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads data to blob storage.
    /// </summary>
    public async Task<BlobUploadResult> UploadAsync(
        string containerName,
        string blobName,
        byte[] data,
        string contentType,
        string? correlationId,
        string? requestId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));

        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        try
        {
            _logger.LogInformation(
                "Uploading blob to container '{Container}' with name '{BlobName}' ({SizeKb} KB) [CorrelationId: {CorrelationId}]",
                containerName,
                blobName,
                data.Length / 1024,
                correlationId ?? "N/A");

            // Get or create container client
            var containerClient = await GetOrCreateContainerClientAsync(containerName, cancellationToken);

            // Get blob client
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload blob with overwrite
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await blobClient.UploadAsync(
                BinaryData.FromBytes(data),
                overwrite: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "✅ Blob uploaded successfully: {BlobName} [CorrelationId: {CorrelationId}]",
                blobName,
                correlationId ?? "N/A");

            // Generate SAS URL
            string? sasUrl = null;
            DateTime? sasUrlExpiresAt = null;

            try
            {
                var sasResult = await GenerateSasUrlAsync(containerName, blobName, _settings.SasUrlExpirationMinutes, cancellationToken);
                sasUrl = sasResult.SasUrl;
                sasUrlExpiresAt = sasResult.SasUrlExpiresAt;

                _logger.LogInformation(
                    "✅ SAS URL generated with {Minutes} minute expiration [CorrelationId: {CorrelationId}]",
                    _settings.SasUrlExpirationMinutes,
                    correlationId ?? "N/A");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "⚠️ Could not generate SAS URL [CorrelationId: {CorrelationId}]",
                    correlationId ?? "N/A");
            }

            // Create and return the result
            var result = BlobUploadResult.Create(
                containerName,
                blobName,
                blobClient.Uri.AbsoluteUri,
                data.Length,
                correlationId,
                requestId,
                sasUrl,
                sasUrlExpiresAt);

            return result;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogError(ex, "❌ Blob already exists or container conflict: {BlobName} [CorrelationId: {CorrelationId}]", blobName, correlationId ?? "N/A");
            throw new StorageException($"Conflict uploading blob '{blobName}'", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status >= 500)
        {
            _logger.LogError(ex, "❌ Server error uploading blob: {BlobName} [CorrelationId: {CorrelationId}]", blobName, correlationId ?? "N/A");
            throw new StorageException($"Server error uploading blob '{blobName}'", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to upload blob: {BlobName} [CorrelationId: {CorrelationId}]", blobName, correlationId ?? "N/A");
            throw new StorageException($"Failed to upload blob '{blobName}'", ex);
        }
    }

    /// <summary>
    /// Generates a SAS URL for time-limited access to a blob.
    /// </summary>
    public async Task<BlobSasUrlResult> GenerateSasUrlAsync(
        string containerName,
        string blobName,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));

        if (expirationMinutes <= 0)
            throw new ArgumentException("Expiration minutes must be greater than 0", nameof(expirationMinutes));

        try
        {
            _logger.LogInformation(
                "Generating SAS URL for blob '{BlobName}' in container '{Container}' (Expiration: {Minutes} minutes)",
                blobName,
                containerName,
                expirationMinutes);

            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                _logger.LogWarning("Blob does not exist: {BlobName}", blobName);
                throw new StorageException($"Blob '{blobName}' does not exist");
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            // For SAS URL generation, we need the account name and key
            if (_settings.UseManagedIdentity)
            {
                _logger.LogWarning("SAS URL generation with Managed Identity not fully supported");
                // Return blob URI without SAS for now
                return new BlobSasUrlResult
                {
                    SasUrl = blobClient.Uri.AbsoluteUri,
                    SasUrlExpiresAt = expiresAt
                };
            }

            // Generate SAS for connection string
            var sasBuilder = new BlobSasBuilder(
                BlobContainerSasPermissions.Parse<BlobContainerSasPermissions>("racwd"),
                expiresAt);

            var sasUrl = blobClient.GenerateSasUri(sasBuilder)?.AbsoluteUri;

            _logger.LogInformation(
                "✅ SAS URL generated with {Minutes} minute expiration",
                expirationMinutes);

            return new BlobSasUrlResult
            {
                SasUrl = sasUrl ?? blobClient.Uri.AbsoluteUri,
                SasUrlExpiresAt = expiresAt
            };
        }
        catch (Exception ex) when (!(ex is StorageException))
        {
            _logger.LogError(ex, "❌ Failed to generate SAS URL for blob: {BlobName}", blobName);
            throw new StorageException($"Failed to generate SAS URL for blob '{blobName}'", ex);
        }
    }

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));

        try
        {
            _logger.LogInformation("Deleting blob '{BlobName}' from container '{Container}'", blobName, containerName);

            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

            _logger.LogInformation("✅ Blob deleted successfully: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to delete blob: {BlobName}", blobName);
            throw new StorageException($"Failed to delete blob '{blobName}'", ex);
        }
    }

    /// <summary>
    /// Checks if a blob exists in storage.
    /// </summary>
    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));

        try
        {
            var containerClient = await GetContainerClientAsync(containerName, cancellationToken);
            var blobClient = containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync(cancellationToken);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to check blob existence: {BlobName}", blobName);
            throw new StorageException($"Failed to check blob existence '{blobName}'", ex);
        }
    }

    /// <summary>
    /// Checks if the blob storage service is connected and accessible.
    /// </summary>
    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Checking blob storage connectivity");

            var containerClient = await GetContainerClientAsync(_settings.ContainerName, cancellationToken);
            var properties = await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("✅ Blob storage is connected and accessible");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Blob storage connectivity check failed");
            return false;
        }
    }

    /// <summary>
    /// Gets or creates a container client.
    /// </summary>
    private async Task<BlobContainerClient> GetOrCreateContainerClientAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        var containerClient = GetBlobContainerClient(containerName);

        if (!_settings.AutoCreateContainer)
        {
            return containerClient;
        }

        try
        {
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            _logger.LogInformation("Container '{Container}' is ready", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create container '{Container}', it may already exist", containerName);
        }

        return containerClient;
    }

    /// <summary>
    /// Gets a container client.
    /// </summary>
    private async Task<BlobContainerClient> GetContainerClientAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        var containerClient = GetBlobContainerClient(containerName);

        try
        {
            await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError("Container does not exist: {Container}", containerName);
            throw new StorageException($"Container '{containerName}' does not exist", ex);
        }

        return containerClient;
    }

    /// <summary>
    /// Gets a blob container client.
    /// </summary>
    private BlobContainerClient GetBlobContainerClient(string containerName)
    {
        if (_containerClient != null && _containerClient.Name == containerName)
        {
            return _containerClient;
        }

        var blobServiceClient = GetBlobServiceClient();
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        return _containerClient;
    }

    /// <summary>
    /// Gets the blob service client based on configuration.
    /// </summary>
    private BlobServiceClient GetBlobServiceClient()
    {
        if (_settings.UseManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(_settings.AccountName))
            {
                throw new StorageException("AccountName is required when using Managed Identity");
            }

            var accountUri = new Uri($"https://{_settings.AccountName}.blob.core.windows.net");
            return new BlobServiceClient(accountUri, new DefaultAzureCredential());
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                throw new StorageException("ConnectionString is required when not using Managed Identity");
            }

            return new BlobServiceClient(_settings.ConnectionString);
        }
    }

    /// <summary>
    /// Disposes the blob storage service resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Disposing BlobStorageService");
            _disposed = true;
            _logger.LogInformation("✅ BlobStorageService disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error disposing BlobStorageService");
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Custom exception for storage-related errors.
/// </summary>
public class StorageException : Exception
{
    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}
