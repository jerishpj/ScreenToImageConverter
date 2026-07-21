namespace ScreenToImageConverter.Worker.Features.ScreenshotCapture.Commands;

/// <summary>
/// Command to capture a screenshot from a given URL.
/// Part of the ScreenshotCapture vertical slice.
/// </summary>
public class CaptureScreenshotCommand
{
    /// <summary>
    /// The URL to capture a screenshot from.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Custom viewport width in pixels.
    /// If not specified, uses default from configuration.
    /// </summary>
    public int? ViewportWidth { get; set; }

    /// <summary>
    /// Optional: Custom viewport height in pixels.
    /// If not specified, uses default from configuration.
    /// </summary>
    public int? ViewportHeight { get; set; }

    /// <summary>
    /// Optional: Custom timeout in milliseconds.
    /// If not specified, uses default from configuration.
    /// </summary>
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Correlation ID for tracing across services.
    /// </summary>
    public string? CorrelationId { get; set; }
}
