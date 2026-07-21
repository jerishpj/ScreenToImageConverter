using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Commands;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Models;

namespace ScreenToImageConverter.Worker.Features.ScreenshotCapture.Handlers;

/// <summary>
/// Handles screenshot capture commands.
/// Orchestrates the Playwright screenshot provider to capture page screenshots.
/// Part of the ScreenshotCapture vertical slice.
/// </summary>
public class CaptureScreenshotHandler
{
    private readonly IScreenshotProvider _screenshotProvider;
    private readonly PlaywrightOptions _playwrightOptions;
    private readonly ILogger<CaptureScreenshotHandler> _logger;

    public CaptureScreenshotHandler(
        IScreenshotProvider screenshotProvider,
        IOptions<PlaywrightOptions> playwrightOptions,
        ILogger<CaptureScreenshotHandler> logger)
    {
        _screenshotProvider = screenshotProvider ?? throw new ArgumentNullException(nameof(screenshotProvider));

        if (playwrightOptions == null)
            throw new ArgumentNullException(nameof(playwrightOptions));

        _playwrightOptions = playwrightOptions.Value ?? throw new ArgumentNullException(nameof(playwrightOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the screenshot capture command.
    /// </summary>
    public async Task<ScreenshotResult> HandleAsync(CaptureScreenshotCommand command, CancellationToken cancellationToken)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.Url))
            throw new ArgumentException("URL cannot be null or empty", nameof(command.Url));

        try
        {
            _logger.LogInformation(
                "Handling screenshot capture for URL: {Url} [CorrelationId: {CorrelationId}]",
                command.Url,
                command.CorrelationId ?? "N/A");

            // Use custom dimensions or fall back to defaults
            int viewportWidth = command.ViewportWidth ?? _playwrightOptions.DefaultViewportWidth;
            int viewportHeight = command.ViewportHeight ?? _playwrightOptions.DefaultViewportHeight;
            int timeoutMs = command.TimeoutMs ?? _playwrightOptions.DefaultTimeoutMs;

            // Capture the screenshot
            byte[] imageData = await _screenshotProvider.CaptureScreenshotAsync(
                command.Url,
                viewportWidth,
                viewportHeight,
                timeoutMs,
                cancellationToken);

            // Create and return the result
            var result = ScreenshotResult.Create(
                command.Url,
                imageData,
                command.CorrelationId);

            _logger.LogInformation(
                "✅ Screenshot capture completed for URL: {Url} (Size: {SizeKb} KB) [CorrelationId: {CorrelationId}]",
                command.Url,
                imageData.Length / 1024,
                command.CorrelationId ?? "N/A");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to capture screenshot for URL: {Url} [CorrelationId: {CorrelationId}]",
                command.Url,
                command.CorrelationId ?? "N/A");
            throw;
        }
    }
}
