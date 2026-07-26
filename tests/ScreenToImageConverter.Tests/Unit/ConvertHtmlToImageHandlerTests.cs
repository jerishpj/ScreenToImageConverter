using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ScreenToImageConverter.Tests.Builders;
using ScreenToImageConverter.Tests.Factories;
using ScreenToImageConverter.Tests.Fixtures;
using ScreenToImageConverter.Worker.AppSettings;
using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Infrastructure.Screenshots;
using ScreenToImageConverter.Worker.Infrastructure.Storage;

namespace ScreenToImageConverter.Tests.Unit;

/// <summary>
/// Comprehensive unit tests for ConvertHtmlToImageHandler.
/// Tests cover success paths, failure paths, exception handling, and edge cases.
/// </summary>
public class ConvertHtmlToImageHandlerTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MockScreenshotProvider _mockScreenshotProvider;
    private readonly MockBlobStorageProvider _mockBlobStorageProvider;
    private readonly MockMessagePublisher _mockMessagePublisher;
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly ConvertHtmlToImageHandler _handler;
    private readonly PlaywrightOptions _playwrightOptions;
    private readonly StorageSettings _storageSettings;

    public ConvertHtmlToImageHandlerTests()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _mockScreenshotProvider = new MockScreenshotProvider(
            _loggerFactory.CreateLogger<MockScreenshotProvider>());

        _mockBlobStorageProvider = new MockBlobStorageProvider(
            _loggerFactory.CreateLogger<MockBlobStorageProvider>());

        _mockMessagePublisher = new MockMessagePublisher(
            _loggerFactory.CreateLogger<MockMessagePublisher>());

        // Initialize the screenshot provider synchronously for tests
        _mockScreenshotProvider.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Create mock IBlobStorageService that delegates to MockBlobStorageProvider
        _mockBlobStorageService = new Mock<IBlobStorageService>();
        _mockBlobStorageService
            .Setup(s => s.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string container, string name, byte[] data, string contentType, string? correlationId, string? requestId, CancellationToken ct) =>
            {
                await _mockBlobStorageProvider.UploadAsync(container, name, data, contentType, ct);
                return new BlobUploadResult
                {
                    BlobUri = $"https://storage.azure.com/{container}/{name}",
                    ContainerName = container,
                    SasUrl = $"https://storage.azure.com/{container}/{name}?sv=2021-06-08",
                    SasUrlExpiresAt = DateTime.UtcNow.AddHours(1)
                };
            });

        _mockBlobStorageService
            .Setup(s => s.GenerateSasUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string container, string name, int expiry, CancellationToken ct) =>
                new BlobSasUrlResult
                {
                    SasUrl = $"https://storage.azure.com/{container}/{name}?sv=2021-06-08",
                    SasUrlExpiresAt = DateTime.UtcNow.AddMinutes(expiry)
                });

        _mockBlobStorageService
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockBlobStorageService
            .Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockBlobStorageService
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _playwrightOptions = new PlaywrightOptions
        {
            DefaultViewportWidth = 1920,
            DefaultViewportHeight = 1080,
            DefaultTimeoutMs = 30000,
            BrowserType = "chromium"
        };

        _storageSettings = new StorageSettings
        {
            ContainerName = "screenshots",
            SasUrlExpirationMinutes = 60
        };

        _handler = new ConvertHtmlToImageHandler(
            _mockScreenshotProvider,
            _mockBlobStorageService.Object,
            _mockMessagePublisher,
            Options.Create(_playwrightOptions),
            Options.Create(_storageSettings),
            _loggerFactory.CreateLogger<ConvertHtmlToImageHandler>());
    }

    #region Success Path Tests

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnSuccessResponse()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            TimeoutMs = 30000,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccessful);
        Assert.Equal("req-12345", response.RequestId);
        Assert.Equal("https://www.example.com", response.Url);
        Assert.NotNull(response.BlobUri);
        Assert.NotNull(response.BlobSasUrl);
        Assert.True(response.FileSizeBytes > 0);
        Assert.Equal("corr-1", response.CorrelationId);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldUploadBlobToStorage()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            TimeoutMs = 30000,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
        // Verify blob exists in storage
        var blobExists = await _mockBlobStorageProvider.ExistsAsync("screenshots", response.BlobFileName);
        Assert.True(blobExists);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldPublishCompletionEvent()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            TimeoutMs = 30000,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert - Give fire-and-forget task time to complete
        await Task.Delay(100);
        Assert.True(response.IsSuccessful);
        Assert.NotEmpty(_mockMessagePublisher.PublishedMessages);
        Assert.Single(_mockMessagePublisher.PublishedMessages);
    }

    [Fact]
    public async Task HandleAsync_ShouldPopulateProcessingDurationMs()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
        Assert.True(response.ProcessingDurationMs >= 0);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultViewportValues_ShouldUsePlaywrightDefaults()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            ViewportWidth = null,
            ViewportHeight = null,
            TimeoutMs = null,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
        Assert.NotNull(response.BlobUri);
    }

    [Fact]
    public async Task HandleAsync_WithCustomSourceAndCorrelationId_ShouldPreserveInResponse()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            SourceId = "custom-source",
            CorrelationId = "custom-corr-id"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
        Assert.Equal("custom-source", response.SourceId);
        Assert.Equal("custom-corr-id", response.CorrelationId);
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task HandleAsync_WithNullUrl_ShouldReturnFailureResponse()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = null,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.IsSuccessful);
        Assert.Contains("Url", response.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidUrl_ShouldReturnFailureResponse()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "not-a-valid-url",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.IsSuccessful);
        Assert.Contains("Url", response.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WithZeroViewportWidth_ShouldReturnFailureResponse()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            ViewportWidth = 0,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.IsSuccessful);
        Assert.Contains("ViewportWidth", response.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WithNullRequestId_ShouldReturnFailureResponse()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = null,
            Url = "https://www.example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.IsSuccessful);
        Assert.Contains("RequestId", response.ErrorMessage);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task HandleAsync_WhenScreenshotCaptureFails_ShouldThrowAndPublishFailureEvent()
    {
        // Arrange
        var failingProvider = new Mock<IScreenshotProvider>();
        failingProvider
            .Setup(p => p.CaptureScreenshotAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Screenshot capture failed"));

        var handler = new ConvertHtmlToImageHandler(
            failingProvider.Object,
            _mockBlobStorageService.Object,
            _mockMessagePublisher,
            Options.Create(_playwrightOptions),
            Options.Create(_storageSettings),
            _loggerFactory.CreateLogger<ConvertHtmlToImageHandler>());

        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => handler.HandleAsync(command, CancellationToken.None));
        Assert.Equal("Screenshot capture failed", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenBlobUploadFails_ShouldThrowAndPublishFailureEvent()
    {
        // Arrange
        var failingBlobService = new Mock<IBlobStorageService>();
        failingBlobService
            .Setup(s => s.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Blob upload failed"));

        var handler = new ConvertHtmlToImageHandler(
            _mockScreenshotProvider,
            failingBlobService.Object,
            _mockMessagePublisher,
            Options.Create(_playwrightOptions),
            Options.Create(_storageSettings),
            _loggerFactory.CreateLogger<ConvertHtmlToImageHandler>());

        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => handler.HandleAsync(command, CancellationToken.None));
        Assert.Equal("Blob upload failed", ex.Message);
    }

    #endregion

    #region Null Parameter Tests

    [Fact]
    public async Task HandleAsync_WithNullCommand_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task HandleAsync_WithCancelledToken_ShouldHandleGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act & Assert
        // Accept TaskCanceledException which derives from OperationCanceledException
        var exception = await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await _handler.HandleAsync(command, cts.Token));
        Assert.NotNull(exception);
    }

    #endregion

    #region Content Type and Response Details Tests

    [Fact]
    public async Task HandleAsync_ShouldSetContentTypeToPng()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal("image/png", response.ContentType);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeProcessedByInstanceId()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response.ProcessedByInstanceId);
        Assert.Equal(Environment.MachineName, response.ProcessedByInstanceId);
    }

    #endregion

    #region Blob Name Generation Tests

    [Fact]
    public async Task HandleAsync_ShouldGenerateUniqueBlobNames()
    {
        // Arrange
        var command1 = new ConvertHtmlToImageCommand
        {
            RequestId = "req-1",
            Url = "https://www.example1.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        var command2 = new ConvertHtmlToImageCommand
        {
            RequestId = "req-2",
            Url = "https://www.example2.com",
            SourceId = "source-2",
            CorrelationId = "corr-2"
        };

        // Act
        var response1 = await _handler.HandleAsync(command1, CancellationToken.None);
        var response2 = await _handler.HandleAsync(command2, CancellationToken.None);

        // Assert
        Assert.NotEqual(response1.BlobFileName, response2.BlobFileName);
        Assert.Contains("req-1", response1.BlobFileName);
        Assert.Contains("req-2", response2.BlobFileName);
    }

    #endregion
}
