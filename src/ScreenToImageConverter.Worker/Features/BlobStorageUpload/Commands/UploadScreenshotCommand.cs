namespace ScreenToImageConverter.Worker.Features.BlobStorageUpload.Commands;

/// <summary>
/// Command to upload a screenshot to blob storage.
/// Part of the BlobStorageUpload vertical slice.
/// </summary>
public class UploadScreenshotCommand
{
    /// <summary>
    /// The screenshot data as PNG bytes.
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The name/path for the blob in storage.
    /// Example: screenshots/2024/01/15/request-12345.png
    /// </summary>
    public string BlobName { get; set; } = string.Empty;

    /// <summary>
    /// The container name for the blob.
    /// Default: "screenshots"
    /// </summary>
    public string ContainerName { get; set; } = "screenshots";

    /// <summary>
    /// Content type of the blob.
    /// Default: "image/png"
    /// </summary>
    public string ContentType { get; set; } = "image/png";

    /// <summary>
    /// The source URL that was captured.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Correlation ID for tracing across services.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Request ID that originated this upload.
    /// </summary>
    public string? RequestId { get; set; }
}
