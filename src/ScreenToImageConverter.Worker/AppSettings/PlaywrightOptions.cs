namespace ScreenToImageConverter.Worker.AppSettings;

/// <summary>
/// Configuration options for Playwright browser automation.
/// </summary>
public class PlaywrightOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Playwright";

    /// <summary>
    /// Browser type to use: "chromium", "firefox", or "webkit".
    /// Default: "chromium" (most common and well-tested)
    /// </summary>
    public string BrowserType { get; set; } = "chromium";

    /// <summary>
    /// Default viewport width in pixels.
    /// Default: 1920
    /// </summary>
    public int DefaultViewportWidth { get; set; } = 1920;

    /// <summary>
    /// Default viewport height in pixels.
    /// Default: 1080
    /// </summary>
    public int DefaultViewportHeight { get; set; } = 1080;

    /// <summary>
    /// Default page load timeout in milliseconds.
    /// Default: 30000 (30 seconds)
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Wait event for page navigation: "load", "domcontentloaded", or "networkidle".
    /// Default: "networkidle" - waits for network to be idle
    /// </summary>
    public string WaitUntilEvent { get; set; } = "networkidle";

    /// <summary>
    /// Whether to run the browser in headless mode.
    /// Default: true (no GUI, suitable for production)
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Whether to disable browser sandbox for Docker environments.
    /// Default: true (required in containerized environments)
    /// </summary>
    public bool DisableSandbox { get; set; } = true;

    /// <summary>
    /// Device scale factor for higher resolution screenshots.
    /// Default: 1.0 (no scaling)
    /// </summary>
    public decimal DeviceScaleFactor { get; set; } = 1.0m;

    /// <summary>
    /// Whether to take screenshots of full page or viewport only.
    /// Default: true (full page)
    /// </summary>
    public bool FullPage { get; set; } = true;

    /// <summary>
    /// Number of retry attempts for failed screenshot captures.
    /// Default: 2
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>
    /// Delay in milliseconds between retry attempts.
    /// Default: 1000 (1 second)
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to emulate the device user agent.
    /// If true, uses a desktop browser user agent.
    /// </summary>
    public bool EmulateDeviceUserAgent { get; set; } = true;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public ICollection<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BrowserType))
            errors.Add($"{nameof(BrowserType)} is required.");
        else if (!new[] { "chromium", "firefox", "webkit" }.Contains(BrowserType.ToLower()))
            errors.Add($"{nameof(BrowserType)} must be 'chromium', 'firefox', or 'webkit'.");

        if (DefaultViewportWidth <= 0)
            errors.Add($"{nameof(DefaultViewportWidth)} must be greater than 0.");

        if (DefaultViewportHeight <= 0)
            errors.Add($"{nameof(DefaultViewportHeight)} must be greater than 0.");

        if (DefaultTimeoutMs <= 0)
            errors.Add($"{nameof(DefaultTimeoutMs)} must be greater than 0.");

        if (string.IsNullOrWhiteSpace(WaitUntilEvent))
            errors.Add($"{nameof(WaitUntilEvent)} is required.");
        else if (!new[] { "load", "domcontentloaded", "networkidle" }.Contains(WaitUntilEvent.ToLower()))
            errors.Add($"{nameof(WaitUntilEvent)} must be 'load', 'domcontentloaded', or 'networkidle'.");

        if (DeviceScaleFactor <= 0)
            errors.Add($"{nameof(DeviceScaleFactor)} must be greater than 0.");

        if (MaxRetryAttempts < 0)
            errors.Add($"{nameof(MaxRetryAttempts)} cannot be negative.");

        if (RetryDelayMs < 0)
            errors.Add($"{nameof(RetryDelayMs)} cannot be negative.");

        return errors;
    }
}
