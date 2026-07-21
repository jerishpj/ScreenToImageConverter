using Microsoft.Extensions.DependencyInjection;
using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Handlers;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Health;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Providers;

namespace ScreenToImageConverter.Worker.Features.ScreenshotCapture.Extensions;

/// <summary>
/// Extension methods for registering ScreenshotCapture feature services.
/// Part of the ScreenshotCapture vertical slice.
/// </summary>
public static class ScreenshotCaptureExtensions
{
    /// <summary>
    /// Registers all ScreenshotCapture feature services.
    /// </summary>
    public static IServiceCollection AddScreenshotCaptureFeature(this IServiceCollection services)
    {
        // Register Playwright screenshot provider
        services.AddSingleton<IScreenshotProvider, PlaywrightScreenshotProvider>();

        // Register handlers
        services.AddScoped<CaptureScreenshotHandler>();

        // Register health checks
        services.AddHealthChecks()
            .AddCheck<PlaywrightHealthCheck>("playwright", tags: new[] { "ready", "live" });

        return services;
    }

    /// <summary>
    /// Initializes the Playwright screenshot provider.
    /// Should be called during application startup.
    /// </summary>
    public static async Task InitializePlaywrightAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var screenshotProvider = scope.ServiceProvider.GetRequiredService<IScreenshotProvider>();
        await screenshotProvider.InitializeAsync(cancellationToken);
    }
}
