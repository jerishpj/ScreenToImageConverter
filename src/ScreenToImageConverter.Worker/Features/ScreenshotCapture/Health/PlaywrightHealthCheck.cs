using Microsoft.Extensions.Diagnostics.HealthChecks;
using ScreenToImageConverter.Shared.Interfaces;

namespace ScreenToImageConverter.Worker.Features.ScreenshotCapture.Health;

/// <summary>
/// Health check for Playwright screenshot provider initialization.
/// Part of the ScreenshotCapture vertical slice.
/// </summary>
public class PlaywrightHealthCheck : IHealthCheck
{
    private readonly IScreenshotProvider _screenshotProvider;

    public PlaywrightHealthCheck(IScreenshotProvider screenshotProvider)
    {
        _screenshotProvider = screenshotProvider ?? throw new ArgumentNullException(nameof(screenshotProvider));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_screenshotProvider.IsInitialized
                ? HealthCheckResult.Healthy("Playwright provider is initialized and ready.")
                : HealthCheckResult.Degraded("Playwright provider is not yet initialized."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Playwright provider check failed.", ex));
        }
    }
}
