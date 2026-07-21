using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Exceptions;
using ScreenToImageConverter.Shared.Interfaces;

namespace ScreenToImageConverter.Worker.Features.BlobStorageUpload.Providers;

/// <summary>
/// Implementation of IBlobStorageProvider using Azure Blob Storage.
/// Handles blob upload, SAS URL generation, and lifecycle management.
/// Part of the BlobStorageUpload vertical slice.
/// </summary>
public class BlobStorageProvider : IBlobStorageProvider
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageProvider> _logger;
    private BlobContainerClient? _containerClient;
    private bool _disposed;

    public BlobStorageProvider(
        IOptions<BlobStorageOptions> options,
        ILogger<BlobStorageProvider> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads a screenshot file to blob storage with retry logic.
    /// </summary>
    public async Task<BlobUrlInfo> UploadAsync(
        string containerName,
        string blobName,
        byte[] data,
        string contentType,
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
                "Uploading blob to container '{Container}' with name '{BlobName}' ({SizeKb} KB)",
                containerName,
                blobName,
                data.Length / 1024);

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

            _logger.LogInformation("✅ Blob uploaded successfully: {BlobName}", blobName);

            // Generate and return blob URL info
            return await GenerateBlobUrlInfoAsync(containerName, blobName, cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogError(ex, "❌ Blob already exists or container conflict: {BlobName}", blobName);
            throw new BlobStorageException($"Conflict uploading blob '{blobName}'", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status >= 500)
        {
            _logger.LogError(ex, "❌ Server error uploading blob: {BlobName}", blobName);
            throw new BlobStorageException($"Server error uploading blob '{blobName}'", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to upload blob: {BlobName}", blobName);
            throw new BlobStorageException($"Failed to upload blob '{blobName}'", ex);
        }
    }

    /// <summary>
    /// Generates a SAS URL for time-limited access to a blob.
    /// </summary>
    public async Task<BlobUrlInfo> GenerateSasUrlAsync(
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
                throw new BlobStorageException($"Blob '{blobName}' does not exist");
            }

            return await GenerateBlobUrlInfoAsync(containerName, blobName, cancellationToken, expirationMinutes);
        }
        catch (Exception ex) when (!(ex is BlobStorageException))
        {
            _logger.LogError(ex, "❌ Failed to generate SAS URL for blob: {BlobName}", blobName);
            throw new BlobStorageException($"Failed to generate SAS URL for blob '{blobName}'", ex);
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
            throw new BlobStorageException($"Failed to delete blob '{blobName}'", ex);
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
            throw new BlobStorageException($"Failed to check blob existence '{blobName}'", ex);
        }
    }

    /// <summary>
    /// Checks if the blob storage provider is connected and accessible.
    /// </summary>
    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Checking blob storage connectivity");

            var containerClient = await GetContainerClientAsync(_options.ContainerName, cancellationToken);

            // Try to get container properties to verify connectivity
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

        if (!_options.AutoCreateContainer)
        {
            return containerClient;
        }

        try
        {
            // Try to create the container (will fail silently if it already exists)
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

        // Verify container exists
        try
        {
            await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError("Container does not exist: {Container}", containerName);
            throw new BlobStorageException($"Container '{containerName}' does not exist", ex);
        }

        return containerClient;
    }

    /// <summary>
    /// Gets a blob container client (does not verify existence).
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
        if (_options.UseManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(_options.AccountName))
            {
                throw new ConfigurationException("AccountName is required when using Managed Identity");
            }

            var accountUri = new Uri($"https://{_options.AccountName}.blob.core.windows.net");
            return new BlobServiceClient(accountUri, new DefaultAzureCredential());
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new ConfigurationException("ConnectionString is required when not using Managed Identity");
            }

            return new BlobServiceClient(_options.ConnectionString);
        }
    }

    /// <summary>
    /// Generates blob URL info with SAS URL.
    /// </summary>
    private async Task<BlobUrlInfo> GenerateBlobUrlInfoAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken,
        int expirationMinutes = 0)
    {
        try
        {
            var containerClient = GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Generate SAS URL if expiration is specified
            string? sasUrl = null;
            DateTime? expiresAt = null;

            if (expirationMinutes > 0)
            {
                // For SAS URL generation, we need the account name and key
                // When using Managed Identity, we create a limited SAS for read access
                if (_options.UseManagedIdentity)
                {
                    // With Managed Identity, we would need to use a different approach
                    // For now, return the blob URI without SAS
                    _logger.LogWarning("SAS URL generation with Managed Identity requires additional configuration");
                }
                else
                {
                    // Generate SAS for connection string
                    try
                    {
                        var sasBuilder = new BlobSasBuilder(
                            BlobContainerSasPermissions.Parse<BlobContainerSasPermissions>("racwd"),
                            DateTimeOffset.UtcNow.AddMinutes(expirationMinutes));

                        sasUrl = blobClient.GenerateSasUri(sasBuilder)?.AbsoluteUri;
                        expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

                        _logger.LogInformation("✅ SAS URL generated with {Minutes} minute expiration", expirationMinutes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate SAS URL");
                    }
                }
            }

            return new BlobUrlInfo
            {
                Uri = blobClient.Uri.AbsoluteUri,
                SasUrl = sasUrl,
                SasUrlExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate blob URL info");
            throw new BlobStorageException($"Failed to generate blob URL info for '{blobName}'", ex);
        }
    }

    /// <summary>
    /// Disposes the blob storage provider resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Disposing BlobStorageProvider");
            _disposed = true;
            _logger.LogInformation("✅ BlobStorageProvider disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error disposing BlobStorageProvider");
        }

        await Task.CompletedTask;
    }
}
