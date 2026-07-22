namespace ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

/// <summary>
/// Response containing metadata about a converted HTML to image.
/// Includes storage location and access information.
/// </summary>
public class ImageMetadataResponse
{
    /// <summary>
    /// Unique identifier linking to the original request.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Business correlation ID from the original request.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Identifier for the user/source who made the original request.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// The URL of the HTML page that was converted.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the conversion was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message if IsSuccessful is false.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The filename of the stored image in Blob Storage.
    /// </summary>
    public string? BlobFileName { get; set; }

    /// <summary>
    /// The container name in Blob Storage where the image is stored.
    /// </summary>
    public string? BlobContainerName { get; set; }

    /// <summary>
    /// The full blob URI (without SAS token).
    /// </summary>
    public string? BlobUri { get; set; }

    /// <summary>
    /// SAS URL for time-limited access to the blob.
    /// </summary>
    public string? BlobSasUrl { get; set; }

    /// <summary>
    /// Expiration time (UTC) for the SAS URL.
    /// </summary>
    public DateTime? SasUrlExpiresAt { get; set; }

    /// <summary>
    /// File size of the generated image in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Content type of the blob (e.g., "image/png").
    /// </summary>
    public string ContentType { get; set; } = "image/png";

    /// <summary>
    /// Timestamp when the image was created (UTC).
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration taken to process the conversion in milliseconds.
    /// </summary>
    public long ProcessingDurationMs { get; set; }

    /// <summary>
    /// The worker instance ID that processed this request.
    /// </summary>
    public string? ProcessedByInstanceId { get; set; }

    /// <summary>
    /// Number of retry attempts made before success.
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Schema version for this message.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Creates a failure response from an exception.
    /// </summary>
    public static ImageMetadataResponse CreateFailure(
        string requestId,
        string url,
        string errorMessage,
        string? correlationId = null,
        string? sourceId = null,
        long processingDurationMs = 0)
    {
        return new ImageMetadataResponse
        {
            RequestId = requestId,
            Url = url,
            CorrelationId = correlationId,
            SourceId = sourceId,
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            ProcessingDurationMs = processingDurationMs,
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a success response with blob storage details.
    /// </summary>
    public static ImageMetadataResponse CreateSuccess(
        string requestId,
        string url,
        string blobFileName,
        string blobContainerName,
        string blobUri,
        string? sasUrl = null,
        DateTime? sasUrlExpiresAt = null,
        long? fileSizeBytes = null,
        string? correlationId = null,
        string? sourceId = null,
        long processingDurationMs = 0,
        string? processedByInstanceId = null)
    {
        return new ImageMetadataResponse
        {
            RequestId = requestId,
            Url = url,
            CorrelationId = correlationId,
            SourceId = sourceId,
            IsSuccessful = true,
            BlobFileName = blobFileName,
            BlobContainerName = blobContainerName,
            BlobUri = blobUri,
            BlobSasUrl = sasUrl,
            SasUrlExpiresAt = sasUrlExpiresAt,
            FileSizeBytes = fileSizeBytes,
            ProcessingDurationMs = processingDurationMs,
            ProcessedByInstanceId = processedByInstanceId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
