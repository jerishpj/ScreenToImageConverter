namespace ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

/// <summary>
/// Command to convert an HTML page to an image screenshot.
/// Consolidates the screenshot capture and storage workflow.
/// </summary>
public class ConvertHtmlToImageCommand
{
    /// <summary>
    /// The URL of the HTML page to convert to an image.
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
    /// Unique identifier for this request. Used for correlation and idempotency.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Optional: Identifier for the user/source making this request.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Correlation ID for tracing across services.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Optional: Whether to wait for the page to be fully loaded.
    /// Default: true
    /// </summary>
    public bool? WaitForPageLoad { get; set; }

    /// <summary>
    /// Optional: Name/description of the screenshot for display purposes.
    /// </summary>
    public string? ScreenshotName { get; set; }
}
