namespace ScreenToImageConverter.Shared.Interfaces;

/// <summary>
/// Interface for capturing screenshots of HTML pages using browser automation.
/// Implementations handle browser lifecycle, page navigation, and screenshot capture.
/// </summary>
public interface IScreenshotProvider : IAsyncDisposable
{
    /// <summary>
    /// Captures a screenshot of the HTML page at the specified URL.
    /// </summary>
    /// <param name="url">The URL of the HTML page to screenshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Byte array containing the screenshot image data (PNG format).</returns>
    Task<byte[]> CaptureScreenshotAsync(string url, CancellationToken cancellationToken);

    /// <summary>
    /// Captures a screenshot with custom viewport dimensions and timeout.
    /// </summary>
    /// <param name="url">The URL of the HTML page to screenshot.</param>
    /// <param name="viewportWidth">Width of the viewport in pixels.</param>
    /// <param name="viewportHeight">Height of the viewport in pixels.</param>
    /// <param name="timeoutMs">Maximum time to wait for page load in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Byte array containing the screenshot image data (PNG format).</returns>
    Task<byte[]> CaptureScreenshotAsync(
        string url,
        int viewportWidth,
        int viewportHeight,
        int timeoutMs,
        CancellationToken cancellationToken);

    /// <summary>
    /// Initializes the screenshot provider (e.g., downloads browser binaries).
    /// Should be called once during application startup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the provider is ready to capture screenshots.
    /// </summary>
    bool IsInitialized { get; }
}
