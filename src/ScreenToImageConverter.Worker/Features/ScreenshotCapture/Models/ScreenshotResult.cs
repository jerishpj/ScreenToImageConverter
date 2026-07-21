namespace ScreenToImageConverter.Worker.Features.ScreenshotCapture.Models;

/// <summary>
/// Result of a screenshot capture operation.
/// Part of the ScreenshotCapture vertical slice.
/// </summary>
public class ScreenshotResult
{
    /// <summary>
    /// The URL that was captured.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The screenshot data as PNG bytes.
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Size of the screenshot in bytes.
    /// </summary>
    public int ImageSizeBytes { get; set; }

    /// <summary>
    /// Timestamp when the screenshot was captured.
    /// </summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Creates a ScreenshotResult from captured data.
    /// </summary>
    public static ScreenshotResult Create(
        string url,
        byte[] imageData,
        string? correlationId = null)
    {
        return new ScreenshotResult
        {
            Url = url,
            ImageData = imageData,
            ImageSizeBytes = imageData.Length,
            CorrelationId = correlationId
        };
    }
}
