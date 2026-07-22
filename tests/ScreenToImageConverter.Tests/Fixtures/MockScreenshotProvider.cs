using Microsoft.Extensions.Logging;
using ScreenToImageConverter.Worker.Infrastructure.Screenshots;

namespace ScreenToImageConverter.Tests.Fixtures;

/// <summary>
/// Mock implementation of IScreenshotProvider for testing purposes.
/// Simulates screenshot capture without launching actual browsers.
/// </summary>
public class MockScreenshotProvider : IScreenshotProvider
{
    private readonly ILogger<MockScreenshotProvider> _logger;
    private bool _initialized;
    private bool _disposed;

    public bool IsInitialized => _initialized;

    /// <summary>
    /// Configuration for controlling mock behavior.
    /// </summary>
    public class MockConfig
    {
        /// <summary>
        /// Simulate initialization failure.
        /// </summary>
        public bool FailInitialization { get; set; }

        /// <summary>
        /// Simulate screenshot capture failure.
        /// </summary>
        public bool FailCapture { get; set; }

        /// <summary>
        /// Custom error message for failures.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Simulated screenshot size in bytes.
        /// </summary>
        public int SimulatedImageSizeBytes { get; set; } = 102400; // 100KB
    }

    public MockConfig Config { get; set; } = new MockConfig();

    public MockScreenshotProvider(ILogger<MockScreenshotProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            _logger.LogInformation("MockScreenshotProvider already initialized");
            return;
        }

        if (Config.FailInitialization)
        {
            var error = Config.ErrorMessage ?? "Mock initialization failed";
            _logger.LogError("❌ MockScreenshotProvider initialization failed: {Error}", error);
            throw new InvalidOperationException(error);
        }

        _logger.LogInformation("✅ MockScreenshotProvider initialized successfully");
        _initialized = true;

        // Simulate some async work
        await Task.Delay(100, cancellationToken);
    }

    public async Task<byte[]> CaptureScreenshotAsync(string url, CancellationToken cancellationToken)
    {
        return await CaptureScreenshotAsync(url, 1920, 1080, 30000, cancellationToken);
    }

    public async Task<byte[]> CaptureScreenshotAsync(
        string url,
        int viewportWidth,
        int viewportHeight,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("MockScreenshotProvider not initialized");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        if (Config.FailCapture)
        {
            var error = Config.ErrorMessage ?? "Mock screenshot capture failed";
            _logger.LogError("❌ MockScreenshotProvider capture failed: {Error}", error);
            throw new InvalidOperationException(error);
        }

        _logger.LogInformation(
            "📸 MockScreenshotProvider capturing: {Url} ({ViewportWidth}x{ViewportHeight})",
            url, viewportWidth, viewportHeight);

        // Simulate network latency
        await Task.Delay(Math.Min(timeoutMs / 10, 500), cancellationToken);

        // Generate simulated PNG data
        var imageData = new byte[Config.SimulatedImageSizeBytes];

        // PNG signature
        imageData[0] = 0x89;
        imageData[1] = 0x50;
        imageData[2] = 0x4E;
        imageData[3] = 0x47;

        // Fill with pseudo-random data
        var random = new Random(url.GetHashCode());
        random.NextBytes(imageData);

        _logger.LogInformation(
            "✅ MockScreenshotProvider captured: {SizeKb} KB",
            imageData.Length / 1024);

        return imageData;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("MockScreenshotProvider disposing");
        _disposed = true;

        await Task.CompletedTask;
    }
}
