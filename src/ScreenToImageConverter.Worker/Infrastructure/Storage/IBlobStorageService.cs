namespace ScreenToImageConverter.Worker.Infrastructure.Storage;

/// <summary>
/// Service interface for managing blob storage operations.
/// Provides abstraction for uploading, managing, and accessing blob storage resources.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads data to blob storage and returns metadata including SAS URL.
    /// </summary>
    Task<BlobUploadResult> UploadAsync(
        string containerName,
        string blobName,
        byte[] data,
        string contentType,
        string? correlationId,
        string? requestId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates a SAS URL for time-limited access to a blob.
    /// </summary>
    Task<BlobSasUrlResult> GenerateSasUrlAsync(
        string containerName,
        string blobName,
        int expirationMinutes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a blob exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the blob storage service is connected and accessible.
    /// </summary>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Result of a blob upload operation.
/// </summary>
public class BlobUploadResult
{
    /// <summary>
    /// The blob name that was uploaded.
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// The container name where blob was uploaded.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Public URI to access the blob.
    /// </summary>
    public string BlobUri { get; set; } = string.Empty;

    /// <summary>
    /// SAS URL for time-limited access to the blob.
    /// </summary>
    public string? SasUrl { get; set; }

    /// <summary>
    /// When the SAS URL expires.
    /// </summary>
    public DateTime? SasUrlExpiresAt { get; set; }

    /// <summary>
    /// Size of the uploaded blob in bytes.
    /// </summary>
    public int BlobSizeBytes { get; set; }

    /// <summary>
    /// Timestamp when the blob was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Request ID that originated this upload.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Creates a BlobUploadResult from upload response.
    /// </summary>
    public static BlobUploadResult Create(
        string containerName,
        string blobName,
        string blobUri,
        int blobSizeBytes,
        string? correlationId = null,
        string? requestId = null,
        string? sasUrl = null,
        DateTime? sasUrlExpiresAt = null)
    {
        return new BlobUploadResult
        {
            ContainerName = containerName,
            BlobName = blobName,
            BlobUri = blobUri,
            BlobSizeBytes = blobSizeBytes,
            CorrelationId = correlationId,
            RequestId = requestId,
            SasUrl = sasUrl,
            SasUrlExpiresAt = sasUrlExpiresAt
        };
    }
}

/// <summary>
/// Result of a SAS URL generation operation.
/// </summary>
public class BlobSasUrlResult
{
    /// <summary>
    /// The SAS URL for time-limited access.
    /// </summary>
    public string SasUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the SAS URL expires.
    /// </summary>
    public DateTime SasUrlExpiresAt { get; set; }
}
