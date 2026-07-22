using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ScreenToImageConverter.Worker.AppSettings;
using ScreenToImageConverter.Worker.Infrastructure.Exceptions;

namespace ScreenToImageConverter.Worker.Infrastructure.Screenshots;

/// <summary>
/// Implementation of IScreenshotProvider using Microsoft Playwright.
/// Handles browser automation, page navigation, and screenshot capture.
/// Infrastructure component for cross-cutting screenshot functionality.
/// </summary>
public class PlaywrightScreenshotProvider : IScreenshotProvider
{
    private readonly PlaywrightOptions _options;
    private readonly ILogger<PlaywrightScreenshotProvider> _logger;
    private IBrowser? _browser;
    private bool _initialized;
    private bool _disposed;

    public bool IsInitialized => _initialized;

    public PlaywrightScreenshotProvider(
        IOptions<PlaywrightOptions> options,
        ILogger<PlaywrightScreenshotProvider> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes Playwright and downloads browser binaries.
    /// Should be called once during application startup.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            _logger.LogInformation("Playwright already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing Playwright with browser type: {BrowserType}", _options.BrowserType);

            // Install browser dependencies
            await PlaywrightInstaller.Install(_options.BrowserType);

            // Launch the browser
            _browser = await GetBrowserAsync(cancellationToken);

            _initialized = true;
            _logger.LogInformation("✅ Playwright initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize Playwright");
            throw new ScreenshotCapturException("Failed to initialize Playwright", ex);
        }
    }

    /// <summary>
    /// Captures a screenshot of the HTML page at the specified URL using default settings.
    /// </summary>
    public async Task<byte[]> CaptureScreenshotAsync(string url, CancellationToken cancellationToken)
    {
        return await CaptureScreenshotAsync(
            url,
            _options.DefaultViewportWidth,
            _options.DefaultViewportHeight,
            _options.DefaultTimeoutMs,
            cancellationToken);
    }

    /// <summary>
    /// Captures a screenshot with custom viewport dimensions and timeout.
    /// </summary>
    public async Task<byte[]> CaptureScreenshotAsync(
        string url,
        int viewportWidth,
        int viewportHeight,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (!_initialized || _browser == null)
        {
            throw new ScreenshotCapturException("Playwright is not initialized. Call InitializeAsync first.");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        int attemptCount = 0;
        Exception? lastException = null;

        while (attemptCount < _options.MaxRetryAttempts)
        {
            IPage? page = null;
            try
            {
                _logger.LogInformation(
                    "Capturing screenshot for URL: {Url} (Attempt {Attempt}/{MaxAttempts})",
                    url,
                    attemptCount + 1,
                    _options.MaxRetryAttempts);

                // Create new browser context with custom viewport
                var contextOptions = new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize
                    {
                        Width = viewportWidth,
                        Height = viewportHeight
                    },
                    DeviceScaleFactor = (float)_options.DeviceScaleFactor,
                    Locale = "en-US"
                };

                await using var context = await _browser.NewContextAsync(contextOptions);
                page = await context.NewPageAsync();

                // Set timeout for navigation
                page.SetDefaultTimeout(timeoutMs);
                page.SetDefaultNavigationTimeout(timeoutMs);

                // Navigate to the URL and wait for page load
                _logger.LogDebug("Navigating to URL: {Url}", url);
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                // Optional: Wait for additional content to load
                await Task.Delay(500, cancellationToken);

                // Capture the screenshot
                _logger.LogDebug("Capturing screenshot");
                var screenshotBuffer = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = _options.FullPage,
                    Type = ScreenshotType.Png
                });

                _logger.LogInformation(
                    "✅ Screenshot captured successfully ({SizeKb} KB)",
                    screenshotBuffer.Length / 1024);

                return screenshotBuffer;
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                // Timeout exception - retryable
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "⚠️ Screenshot capture timeout for URL: {Url}. Retrying... ({Attempt}/{MaxAttempts})",
                    url,
                    attemptCount + 1,
                    _options.MaxRetryAttempts);

                attemptCount++;
                if (attemptCount < _options.MaxRetryAttempts)
                {
                    await Task.Delay(_options.RetryDelayMs, cancellationToken);
                }
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("net::", StringComparison.OrdinalIgnoreCase))
            {
                // Network error - retryable
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "⚠️ Network error capturing screenshot for URL: {Url}. Retrying... ({Attempt}/{MaxAttempts})",
                    url,
                    attemptCount + 1,
                    _options.MaxRetryAttempts);

                attemptCount++;
                if (attemptCount < _options.MaxRetryAttempts)
                {
                    await Task.Delay(_options.RetryDelayMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Non-retryable error
                _logger.LogError(ex, "❌ Non-retryable error capturing screenshot for URL: {Url}", url);
                throw new ScreenshotCapturException($"Failed to capture screenshot for URL: {url}", ex);
            }
            finally
            {
                if (page != null)
                {
                    await page.CloseAsync();
                }
            }
        }

        // All retry attempts exhausted
        _logger.LogError(
            "❌ Failed to capture screenshot after {MaxAttempts} attempts for URL: {Url}",
            _options.MaxRetryAttempts,
            url);

        throw new ScreenshotCapturException(
            $"Failed to capture screenshot after {_options.MaxRetryAttempts} attempts for URL: {url}",
            lastException);
    }

    /// <summary>
    /// Gets or creates a browser instance based on configuration.
    /// </summary>
    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser != null)
        {
            return _browser;
        }

        var playwright = await Playwright.CreateAsync();

        return _options.BrowserType.ToLower() switch
        {
            "chromium" => await LaunchChromiumAsync(playwright, cancellationToken),
            "firefox" => await LaunchFirefoxAsync(playwright, cancellationToken),
            "webkit" => await LaunchWebkitAsync(playwright, cancellationToken),
            _ => throw new ScreenshotCapturException($"Unsupported browser type: {_options.BrowserType}")
        };
    }

    private async Task<IBrowser> LaunchChromiumAsync(IPlaywright playwright, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Launching Chromium browser");

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        };

        if (_options.DisableSandbox)
        {
            launchOptions.Args = launchOptions.Args?.Append("--no-sandbox").ToArray();
        }

        return await playwright.Chromium.LaunchAsync(launchOptions);
    }

    private async Task<IBrowser> LaunchFirefoxAsync(IPlaywright playwright, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Launching Firefox browser");

        return await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless
        });
    }

    private async Task<IBrowser> LaunchWebkitAsync(IPlaywright playwright, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Launching WebKit browser");

        return await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless
        });
    }

    /// <summary>
    /// Disposes browser resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_browser != null)
            {
                _logger.LogInformation("Closing Playwright browser");
                await _browser.CloseAsync();
            }

            _disposed = true;
            _logger.LogInformation("✅ PlaywrightScreenshotProvider disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error disposing PlaywrightScreenshotProvider");
        }
    }
}

/// <summary>
/// Helper class for installing Playwright browser binaries.
/// </summary>
internal static class PlaywrightInstaller
{
    public static async Task Install(string browserType)
    {
        // Playwright automatically downloads browser binaries on first run
        // This method is a placeholder for future explicit installation logic
        await Task.CompletedTask;
    }
}
