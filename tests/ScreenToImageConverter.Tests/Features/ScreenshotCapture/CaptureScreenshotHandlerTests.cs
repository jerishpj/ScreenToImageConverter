using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Language.Flow;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Commands;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Handlers;

namespace ScreenToImageConverter.Tests.Features.ScreenshotCapture;

/// <summary>
/// Tests for the CaptureScreenshotHandler.
/// Part of the ScreenshotCapture feature test suite.
/// </summary>
public class CaptureScreenshotHandlerTests
{
    private readonly Mock<IScreenshotProvider> _mockScreenshotProvider;
    private readonly Mock<ILogger<CaptureScreenshotHandler>> _mockLogger;
    private readonly IOptions<PlaywrightOptions> _playwrightOptions;
    private readonly CaptureScreenshotHandler _handler;

    public CaptureScreenshotHandlerTests()
    {
        _mockScreenshotProvider = new Mock<IScreenshotProvider>();
        _mockLogger = new Mock<ILogger<CaptureScreenshotHandler>>();

        _playwrightOptions = Options.Create(new PlaywrightOptions
        {
            BrowserType = "chromium",
            DefaultViewportWidth = 1920,
            DefaultViewportHeight = 1080,
            DefaultTimeoutMs = 30000,
            MaxRetryAttempts = 2,
            RetryDelayMs = 1000
        });

        _handler = new CaptureScreenshotHandler(
            _mockScreenshotProvider.Object,
            _playwrightOptions,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullScreenshotProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CaptureScreenshotHandler(null!, _playwrightOptions, _mockLogger.Object));

        Assert.Equal("screenshotProvider", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CaptureScreenshotHandler(_mockScreenshotProvider.Object, null!, _mockLogger.Object));

        Assert.Equal("playwrightOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CaptureScreenshotHandler(_mockScreenshotProvider.Object, _playwrightOptions, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsScreenshotResult()
    {
        // Arrange
        var command = new CaptureScreenshotCommand
        {
            Url = "https://example.com",
            CorrelationId = "test-correlation-123"
        };

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(
                command.Url,
                _playwrightOptions.Value.DefaultViewportWidth,
                _playwrightOptions.Value.DefaultViewportHeight,
                _playwrightOptions.Value.DefaultTimeoutMs,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(imageData);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Url, result.Url);
        Assert.Equal(imageData, result.ImageData);
        Assert.Equal(imageData.Length, result.ImageSizeBytes);
        Assert.Equal(command.CorrelationId, result.CorrelationId);

        _mockScreenshotProvider.Verify(
            x => x.CaptureScreenshotAsync(
                command.Url,
                _playwrightOptions.Value.DefaultViewportWidth,
                _playwrightOptions.Value.DefaultViewportHeight,
                _playwrightOptions.Value.DefaultTimeoutMs,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCustomViewport_UsesCustomDimensions()
    {
        // Arrange
        var command = new CaptureScreenshotCommand
        {
            Url = "https://example.com",
            ViewportWidth = 1280,
            ViewportHeight = 720,
            CorrelationId = "test-123"
        };

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(
                command.Url,
                1280,
                720,
                _playwrightOptions.Value.DefaultTimeoutMs,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(imageData);

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _mockScreenshotProvider.Verify(
            x => x.CaptureScreenshotAsync(
                command.Url,
                1280,
                720,
                _playwrightOptions.Value.DefaultTimeoutMs,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCustomTimeout_UsesCustomValue()
    {
        // Arrange
        var command = new CaptureScreenshotCommand
        {
            Url = "https://example.com",
            TimeoutMs = 60000,
            CorrelationId = "test-123"
        };

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(
                command.Url,
                _playwrightOptions.Value.DefaultViewportWidth,
                _playwrightOptions.Value.DefaultViewportHeight,
                60000,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(imageData);

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _mockScreenshotProvider.Verify(
            x => x.CaptureScreenshotAsync(
                command.Url,
                _playwrightOptions.Value.DefaultViewportWidth,
                _playwrightOptions.Value.DefaultViewportHeight,
                60000,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));

        Assert.Equal("command", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WithNullUrl_ThrowsArgumentException()
    {
        // Arrange
        var command = new CaptureScreenshotCommand { Url = null! };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        Assert.Contains("URL cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var command = new CaptureScreenshotCommand { Url = "" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        Assert.Contains("URL cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderThrows_PropagatesException()
    {
        // Arrange
        var command = new CaptureScreenshotCommand
        {
            Url = "https://example.com",
            CorrelationId = "test-123"
        };

        var expectedException = new InvalidOperationException("Screenshot capture failed");
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal(expectedException, exception);
    }

    [Fact]
    public async Task HandleAsync_WithoutCorrelationId_CreatesResultWithNullCorrelationId()
    {
        // Arrange
        var command = new CaptureScreenshotCommand
        {
            Url = "https://example.com"
            // CorrelationId intentionally omitted
        };

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(imageData);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Null(result.CorrelationId);
    }

    [Fact]
    public async Task HandleAsync_CancellationToken_PropagatedToProvider()
    {
        // Arrange
        var command = new CaptureScreenshotCommand { Url = "https://example.com" };
        var cts = new CancellationTokenSource();
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        _mockScreenshotProvider
            .Setup(x => x.CaptureScreenshotAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(imageData);

        // Act
        await _handler.HandleAsync(command, cts.Token);

        // Assert
        _mockScreenshotProvider.Verify(
            x => x.CaptureScreenshotAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                cts.Token),
            Times.Once);
    }
}
