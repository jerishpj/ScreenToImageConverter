namespace ScreenToImageConverter.Worker.Infrastructure.Notifications;

/// <summary>
/// Represents a request to capture a screenshot of an HTML page.
/// This message is published to the Service Bus topic for processing.
/// </summary>
public class HtmlScreenshotRequest
{
    /// <summary>
    /// Unique identifier for this request. Used for correlation and idempotency.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The URL of the HTML page to screenshot.
    /// Must be a valid HTTP or HTTPS URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Identifier for the user/source making this request.
    /// Used for tracking and analytics.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Optional: Custom identifier for the business entity (e.g., document ID, report ID).
    /// Used for correlation in downstream systems.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Optional: Preferred width of the screenshot in pixels.
    /// Default: 1920
    /// </summary>
    public int? ViewportWidth { get; set; }

    /// <summary>
    /// Optional: Preferred height of the screenshot in pixels.
    /// Default: 1080
    /// </summary>
    public int? ViewportHeight { get; set; }

    /// <summary>
    /// Optional: Maximum time to wait for page load in milliseconds.
    /// Default: 30000 (30 seconds)
    /// </summary>
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Optional: Whether to wait for the page to be fully loaded.
    /// Default: true
    /// </summary>
    public bool? WaitForPageLoad { get; set; }

    /// <summary>
    /// Optional: Name/description of the screenshot for display purposes.
    /// </summary>
    public string? ScreenshotName { get; set; }

    /// <summary>
    /// Timestamp when the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Schema version for this message. Supports future evolution.
    /// Current version: 1.0
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Validates the request for required fields and constraints.
    /// </summary>
    public ICollection<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RequestId))
            errors.Add("RequestId is required.");

        if (string.IsNullOrWhiteSpace(Url))
            errors.Add("Url is required.");
        else if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            errors.Add("Url must be a valid HTTP or HTTPS URL.");

        if (ViewportWidth.HasValue && ViewportWidth <= 0)
            errors.Add("ViewportWidth must be greater than 0.");

        if (ViewportHeight.HasValue && ViewportHeight <= 0)
            errors.Add("ViewportHeight must be greater than 0.");

        if (TimeoutMs.HasValue && TimeoutMs <= 0)
            errors.Add("TimeoutMs must be greater than 0.");

        return errors;
    }
}
