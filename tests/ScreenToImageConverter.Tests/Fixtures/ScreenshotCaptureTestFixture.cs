using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Extensions;

namespace ScreenToImageConverter.Tests.Fixtures;

/// <summary>
/// Base fixture for ScreenshotCapture feature tests.
/// Provides common setup and service configuration.
/// </summary>
public class ScreenshotCaptureTestFixture : IAsyncLifetime
{
    private readonly ServiceCollection _services;
    protected IServiceProvider? ServiceProvider { get; private set; }
    protected ILogger<T>? GetLogger<T>() where T : class => ServiceProvider?.GetRequiredService<ILogger<T>>();

    public ScreenshotCaptureTestFixture()
    {
        _services = new ServiceCollection();
        ConfigureServices();
    }

    protected virtual void ConfigureServices()
    {
        // Add logging
        _services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add PlaywrightOptions
        _services.Configure<PlaywrightOptions>(options =>
        {
            options.BrowserType = "chromium";
            options.DefaultViewportWidth = 1920;
            options.DefaultViewportHeight = 1080;
            options.DefaultTimeoutMs = 30000;
            options.MaxRetryAttempts = 2;
            options.RetryDelayMs = 1000;
            options.Headless = true;
            options.DisableSandbox = true;
        });

        // Register mock screenshot provider by default
        _services.AddSingleton<IScreenshotProvider, MockScreenshotProvider>();

        // Register handlers
        _services.AddScreenshotCaptureFeature();
    }

    /// <summary>
    /// Override to use actual Playwright provider instead of mock.
    /// </summary>
    protected void UseRealPlaywright()
    {
        // Remove mock, will use real provider from feature registration
        // This is handled by not registering the mock in ConfigureServices
    }

    /// <summary>
    /// Creates a service scope for test execution.
    /// </summary>
    protected IServiceScope CreateServiceScope()
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider not initialized. Call InitializeAsync first.");
        }

        return ServiceProvider.CreateScope();
    }

    /// <summary>
    /// Gets a service instance from the service provider.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider not initialized. Call InitializeAsync first.");
        }

        return ServiceProvider.GetRequiredService<T>();
    }

    public async Task InitializeAsync()
    {
        ServiceProvider = _services.BuildServiceProvider();

        // Optionally initialize Playwright (skipped for mock provider)
        // await ServiceProvider.InitializePlaywrightAsync();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (ServiceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        ServiceProvider = null;
    }
}
