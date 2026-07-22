using ScreenToImageConverter.Worker.Infrastructure.Notifications;

namespace ScreenToImageConverter.Tests.Factories;

/// <summary>
/// Factory for creating consistent test data across all tests.
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a valid HtmlScreenshotRequest with default test values.
    /// </summary>
    public static HtmlScreenshotRequest CreateValidHtmlScreenshotRequest(
        string? url = null,
        string? requestId = null,
        int viewportWidth = 1920,
        int viewportHeight = 1080)
    {
        return new HtmlScreenshotRequest
        {
            RequestId = requestId ?? Guid.NewGuid().ToString(),
            Url = url ?? "https://example.com",
            SourceId = "test-source",
            CorrelationId = Guid.NewGuid().ToString(),
            ViewportWidth = viewportWidth,
            ViewportHeight = viewportHeight,
            TimeoutMs = 30000,
            WaitForPageLoad = true,
            ScreenshotName = "test-screenshot.png",
            CreatedAt = DateTime.UtcNow,
            SchemaVersion = "1.0"
        };
    }

    /// <summary>
    /// Creates an invalid HtmlScreenshotRequest with missing URL.
    /// </summary>
    public static HtmlScreenshotRequest CreateInvalidHtmlScreenshotRequest_MissingUrl()
    {
        return new HtmlScreenshotRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Url = null, // Invalid: missing URL
            SourceId = "test-source",
            CorrelationId = Guid.NewGuid().ToString(),
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            TimeoutMs = 30000,
            WaitForPageLoad = true,
            ScreenshotName = "test-screenshot.png",
            CreatedAt = DateTime.UtcNow,
            SchemaVersion = "1.0"
        };
    }

    /// <summary>
    /// Creates an invalid HtmlScreenshotRequest with invalid viewport width.
    /// </summary>
    public static HtmlScreenshotRequest CreateInvalidHtmlScreenshotRequest_InvalidViewport()
    {
        return new HtmlScreenshotRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Url = "https://example.com",
            SourceId = "test-source",
            CorrelationId = Guid.NewGuid().ToString(),
            ViewportWidth = 0, // Invalid: width must be > 0
            ViewportHeight = 1080,
            TimeoutMs = 30000,
            WaitForPageLoad = true,
            ScreenshotName = "test-screenshot.png",
            CreatedAt = DateTime.UtcNow,
            SchemaVersion = "1.0"
        };
    }

    /// <summary>
    /// Creates a valid ScreenshotCompletedEvent for a successful screenshot.
    /// </summary>
    public static ScreenshotCompletedEvent CreateSuccessfulScreenshotCompletedEvent(
        string? requestId = null,
        string? url = null,
        string? blobFileName = null,
        string? containerName = null,
        string? blobUri = null,
        string? sasUrl = null)
    {
        return ScreenshotCompletedEvent.CreateSuccess(
            requestId ?? Guid.NewGuid().ToString(),
            url ?? "https://example.com",
            blobFileName ?? "test-screenshot.png",
            containerName ?? "screenshots",
            blobUri ?? "https://storage.azure.com/screenshots/test.png",
            sasUrl: sasUrl ?? "https://storage.azure.com/screenshots/test.png?sig=xxx",
            sasUrlExpiresAt: DateTime.UtcNow.AddHours(1),
            fileSizeBytes: 102400
        );
    }

    /// <summary>
    /// Creates a ScreenshotCompletedEvent for a failed screenshot.
    /// </summary>
    public static ScreenshotCompletedEvent CreateFailedScreenshotCompletedEvent(
        string? requestId = null,
        string? url = null,
        string? errorMessage = null)
    {
        return ScreenshotCompletedEvent.CreateFailure(
            requestId ?? Guid.NewGuid().ToString(),
            url ?? "https://example.com",
            errorMessage ?? "Screenshot capture failed"
        );
    }
}
