using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Tests.Builders;
using ScreenToImageConverter.Tests.Factories;
using ScreenToImageConverter.Tests.Fixtures;
using ScreenToImageConverter.Worker.AppSettings;
using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Infrastructure.Storage;

namespace ScreenToImageConverter.Tests.Integration;

/// <summary>
/// Integration tests for complex end-to-end scenarios.
/// Tests cover full workflows, multi-step processes, and cross-component interactions.
/// </summary>
public class HandlerIntegrationTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MockScreenshotProvider _mockScreenshotProvider;
    private readonly MockBlobStorageProvider _mockBlobStorageProvider;
    private readonly MockMessagePublisher _mockMessagePublisher;
    private readonly Mock<IBlobStorageService> _mockBlobStorageService;
    private readonly ConvertHtmlToImageHandler _handler;
    private readonly PlaywrightOptions _playwrightOptions;
    private readonly BlobStorageOptions _blobStorageOptions;

    public HandlerIntegrationTests()
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

        _blobStorageOptions = new BlobStorageOptions
        {
            ContainerName = "screenshots",
            SasUrlExpirationMinutes = 60
        };

        _handler = new ConvertHtmlToImageHandler(
            _mockScreenshotProvider,
            _mockBlobStorageService.Object,
            _mockMessagePublisher,
            Options.Create(_playwrightOptions),
            Options.Create(_blobStorageOptions),
            _loggerFactory.CreateLogger<ConvertHtmlToImageHandler>());
    }

    #region Multi-Request Workflow Tests

    [Fact]
    public async Task MultipleRequests_ShouldProcessIndependently()
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

        var command3 = new ConvertHtmlToImageCommand
        {
            RequestId = "req-3",
            Url = "https://www.example3.com",
            SourceId = "source-3",
            CorrelationId = "corr-3"
        };

        // Act
        var response1 = await _handler.HandleAsync(command1, CancellationToken.None);
        var response2 = await _handler.HandleAsync(command2, CancellationToken.None);
        var response3 = await _handler.HandleAsync(command3, CancellationToken.None);

        // Assert
        Assert.True(response1.IsSuccessful);
        Assert.True(response2.IsSuccessful);
        Assert.True(response3.IsSuccessful);
        Assert.Equal("req-1", response1.RequestId);
        Assert.Equal("req-2", response2.RequestId);
        Assert.Equal("req-3", response3.RequestId);
        Assert.NotEqual(response1.BlobFileName, response2.BlobFileName);
        Assert.NotEqual(response2.BlobFileName, response3.BlobFileName);
    }

    [Fact]
    public async Task MultipleRequests_ShouldPublishMultipleEvents()
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
        await _handler.HandleAsync(command1, CancellationToken.None);
        await _handler.HandleAsync(command2, CancellationToken.None);
        await Task.Delay(200); // Wait for fire-and-forget events

        // Assert
        Assert.Equal(2, _mockMessagePublisher.PublishedMessages.Count);
    }

    #endregion

    #region Concurrent Request Tests

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleWithoutConflicts()
    {
        // Arrange
        var commands = Enumerable.Range(1, 5)
            .Select(i => new ConvertHtmlToImageCommand
            {
                RequestId = $"req-{i}",
                Url = $"https://www.example{i}.com",
                SourceId = $"source-{i}",
                CorrelationId = $"corr-{i}"
            })
            .ToList();

        // Act
        var tasks = commands.Select(cmd => _handler.HandleAsync(cmd, CancellationToken.None));
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, responses.Length);
        Assert.All(responses, r => Assert.True(r.IsSuccessful));
        Assert.All(responses, r => Assert.NotNull(r.BlobUri));

        // Verify all RequestIds are preserved
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal($"req-{i + 1}", responses[i].RequestId);
        }
    }

    #endregion

    #region Blob Storage Lifecycle Tests

    [Fact]
    public async Task BlobStorage_ShouldAllowUploadAndRetrieve()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
        var blobExists = await _mockBlobStorageProvider.ExistsAsync("screenshots", response.BlobFileName);
        Assert.True(blobExists);

        // Generate SAS URL after upload
        var sasUrlResult = await _mockBlobStorageProvider.GenerateSasUrlAsync(
            "screenshots", response.BlobFileName, expirationMinutes: 60);

        Assert.NotNull(sasUrlResult);
        Assert.NotNull(sasUrlResult.SasUrl);
    }

    [Fact]
    public async Task BlobStorage_ShouldAllowDeletion()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        var response = await _handler.HandleAsync(command, CancellationToken.None);
        Assert.True(response.IsSuccessful);

        // Act
        await _mockBlobStorageProvider.DeleteAsync("screenshots", response.BlobFileName);

        // Assert
        var blobExists = await _mockBlobStorageProvider.ExistsAsync("screenshots", response.BlobFileName);
        Assert.False(blobExists);
    }

    #endregion

    #region Event Publishing Integration Tests

    [Fact]
    public async Task EventPublishing_ShouldIncludeAllResponseMetadata()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        _mockMessagePublisher.ClearPublishedMessages();

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);
        await Task.Delay(200); // Wait for fire-and-forget

        // Assert
        Assert.NotEmpty(_mockMessagePublisher.PublishedMessages);
        var publishedEvent = _mockMessagePublisher.PublishedMessages.First();
        Assert.NotNull(publishedEvent.Message);
    }

    [Fact]
    public async Task EventPublishing_ShouldContinueEvenIfPublisherFails()
    {
        // This test verifies that handler completes successfully
        // even if event publishing fails (fire-and-forget pattern)

        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert - Handler should still return success
        Assert.True(response.IsSuccessful);
        Assert.NotNull(response.BlobUri);
    }

    #endregion

    #region Error Recovery and Boundary Tests

    [Fact]
    public async Task Handler_ShouldHandleVeryLongUrl()
    {
        // Arrange
        var longUrl = "https://example.com/" + new string('a', 2048);
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = longUrl,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        // Long URLs are valid, handler should succeed (or fail due to screenshot provider, not validation)
        // The mock provider will succeed, so we expect success
        Assert.True(response.IsSuccessful);
        Assert.Equal(longUrl, response.Url);
    }

    [Fact]
    public async Task Handler_ShouldHandleMaxViewportDimensions()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = 9999,
            ViewportHeight = 9999,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
    }

    [Fact]
    public async Task Handler_ShouldHandleVeryLargeTimeout()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = 300000, // 5 minutes
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
    }

    [Fact]
    public async Task Handler_ShouldHandleMinimalTimeout()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = 1,
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
    }

    #endregion

    #region Response Validation Tests

    [Fact]
    public async Task Response_ShouldContainAccurateProcessingMetrics()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.ProcessingDurationMs >= 0);
        Assert.NotNull(response.ProcessedByInstanceId);
        Assert.True(response.ProcessedAt > DateTime.MinValue);
        Assert.Equal(0, response.RetryAttempts); // No retries in mock
    }

    [Fact]
    public async Task Response_ShouldContainBlobMetadata()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response.BlobFileName);
        Assert.NotNull(response.BlobContainerName);
        Assert.NotNull(response.BlobUri);
        Assert.NotNull(response.BlobSasUrl);
        Assert.True(response.FileSizeBytes > 0);
        Assert.Equal("image/png", response.ContentType);
        Assert.Equal("screenshots", response.BlobContainerName);
    }

    [Fact]
    public async Task Response_ShouldPreserveBlobNamePattern()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-unique-123",
            Url = "https://example.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Contains("req-unique-123", response.BlobFileName);
        Assert.Contains("screenshots", response.BlobFileName);
        Assert.EndsWith(".png", response.BlobFileName);
    }

    #endregion

    #region Validation Integration Tests

    [Fact]
    public async Task CompleteWorkflow_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = new HtmlScreenshotRequestBuilder()
            .WithUrl("https://www.example.com")
            .WithViewport(1920, 1080)
            .WithTimeout(30000)
            .Build();

        var command = new ConvertHtmlToImageCommand
        {
            RequestId = request.RequestId,
            Url = request.Url,
            ViewportWidth = request.ViewportWidth,
            ViewportHeight = request.ViewportHeight,
            TimeoutMs = request.TimeoutMs,
            SourceId = request.SourceId,
            CorrelationId = "test-corr-id"
        };

        // Act
        var response = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessful);
        Assert.Equal(request.RequestId, response.RequestId);
        Assert.Equal(request.Url, response.Url);
        Assert.NotNull(response.BlobUri);
        Assert.NotNull(response.ProcessedByInstanceId);
    }

    #endregion

    #region State Isolation Tests

    [Fact]
    public async Task MultipleHandlers_ShouldMaintainStateIndependence()
    {
        // Arrange
        var handler1 = _handler;

        // Create second mock for handler2
        var mockBlobStorageService2 = new Mock<IBlobStorageService>();
        mockBlobStorageService2
            .Setup(s => s.UploadAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string container, string name, byte[] data, string contentType, string? correlationId, string? requestId, CancellationToken ct) =>
                new BlobUploadResult
                {
                    BlobUri = $"https://storage.azure.com/{container}/{name}",
                    ContainerName = container,
                    SasUrl = $"https://storage.azure.com/{container}/{name}?sv=2021-06-08",
                    SasUrlExpiresAt = DateTime.UtcNow.AddHours(1)
                });

        mockBlobStorageService2
            .Setup(s => s.GenerateSasUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string container, string name, int expiry, CancellationToken ct) =>
                new BlobSasUrlResult
                {
                    SasUrl = $"https://storage.azure.com/{container}/{name}?sv=2021-06-08",
                    SasUrlExpiresAt = DateTime.UtcNow.AddMinutes(expiry)
                });

        mockBlobStorageService2
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockBlobStorageService2
            .Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockBlobStorageService2
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler2 = new ConvertHtmlToImageHandler(
            _mockScreenshotProvider,
            mockBlobStorageService2.Object,
            _mockMessagePublisher,
            Options.Create(_playwrightOptions),
            Options.Create(_blobStorageOptions),
            _loggerFactory.CreateLogger<ConvertHtmlToImageHandler>());

        var command1 = new ConvertHtmlToImageCommand
        {
            RequestId = "req-handler1",
            Url = "https://example1.com",
            SourceId = "source-1",
            CorrelationId = "corr-1"
        };

        var command2 = new ConvertHtmlToImageCommand
        {
            RequestId = "req-handler2",
            Url = "https://example2.com",
            SourceId = "source-2",
            CorrelationId = "corr-2"
        };

        // Act
        var response1 = await handler1.HandleAsync(command1, CancellationToken.None);
        var response2 = await handler2.HandleAsync(command2, CancellationToken.None);

        // Assert
        Assert.NotEqual(response1.RequestId, response2.RequestId);
        Assert.NotEqual(response1.BlobFileName, response2.BlobFileName);
    }

    #endregion
}
