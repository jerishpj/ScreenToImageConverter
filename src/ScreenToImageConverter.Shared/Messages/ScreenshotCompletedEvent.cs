namespace ScreenToImageConverter.Shared.Messages;

/// <summary>
/// Represents a completed screenshot processing event.
/// This message is published to the Service Bus topic after successful processing.
/// Consumed by downstream systems (Notification Service, PDF Generator, etc.).
/// </summary>
public class ScreenshotCompletedEvent
{
    /// <summary>
    /// Unique identifier linking to the original HtmlScreenshotRequest.
    /// Used for correlation and message idempotency.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Business correlation ID from the original request.
    /// Used by downstream systems for tracking.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Identifier for the user/source who made the original request.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// The URL of the HTML page that was screenshotted.
    /// Included for reference and audit purposes.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the screenshot was successfully captured.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message if IsSuccessful is false.
    /// Null if processing was successful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The filename of the stored screenshot in Blob Storage.
    /// Format: {requestId}_{timestamp}.png
    /// </summary>
    public string? BlobFileName { get; set; }

    /// <summary>
    /// The container name in Blob Storage where the screenshot is stored.
    /// </summary>
    public string? BlobContainerName { get; set; }

    /// <summary>
    /// The full blob URI (without SAS token).
    /// Example: https://account.blob.core.windows.net/container/filename.png
    /// </summary>
    public string? BlobUri { get; set; }

    /// <summary>
    /// SAS URL for time-limited access to the blob.
    /// Includes authentication token; use this for public sharing.
    /// </summary>
    public string? BlobSasUrl { get; set; }

    /// <summary>
    /// Expiration time (UTC) for the SAS URL.
    /// After this time, the SAS URL will no longer be valid.
    /// </summary>
    public DateTime? SasUrlExpiresAt { get; set; }

    /// <summary>
    /// File size of the generated screenshot in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Content type of the blob (e.g., "image/png").
    /// </summary>
    public string ContentType { get; set; } = "image/png";

    /// <summary>
    /// Timestamp when the screenshot was captured (UTC).
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration taken to process the screenshot in milliseconds.
    /// Useful for performance monitoring.
    /// </summary>
    public long ProcessingDurationMs { get; set; }

    /// <summary>
    /// The worker instance ID that processed this request.
    /// Useful for debugging and monitoring.
    /// </summary>
    public string? ProcessedByInstanceId { get; set; }

    /// <summary>
    /// Number of retry attempts made before success.
    /// 0 if successful on first attempt.
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Schema version for this message. Supports future evolution.
    /// Current version: 1.0
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Creates a failure event from an exception.
    /// </summary>
    public static ScreenshotCompletedEvent CreateFailure(
        string requestId,
        string url,
        string errorMessage,
        string? correlationId = null,
        string? sourceId = null,
        int retryAttempts = 0,
        long processingDurationMs = 0)
    {
        return new ScreenshotCompletedEvent
        {
            RequestId = requestId,
            Url = url,
            CorrelationId = correlationId,
            SourceId = sourceId,
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            RetryAttempts = retryAttempts,
            ProcessingDurationMs = processingDurationMs,
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a success event with blob storage details.
    /// </summary>
    public static ScreenshotCompletedEvent CreateSuccess(
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
        int retryAttempts = 0,
        long processingDurationMs = 0,
        string? processedByInstanceId = null)
    {
        return new ScreenshotCompletedEvent
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
            RetryAttempts = retryAttempts,
            ProcessingDurationMs = processingDurationMs,
            ProcessedByInstanceId = processedByInstanceId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
