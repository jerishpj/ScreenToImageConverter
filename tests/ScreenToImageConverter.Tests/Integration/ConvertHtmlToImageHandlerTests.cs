using Microsoft.Extensions.Logging;
using ScreenToImageConverter.Tests.Builders;
using ScreenToImageConverter.Tests.Factories;
using ScreenToImageConverter.Tests.Fixtures;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Infrastructure.Storage;

namespace ScreenToImageConverter.Tests.Integration;

/// <summary>
/// Integration tests for the ConvertHtmlToImage feature.
/// Demonstrates how to use the test fixtures and builders.
/// </summary>
public class ConvertHtmlToImageHandlerTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MockBlobStorageProvider _mockBlobStorageProvider;
    private readonly ILogger<ConvertHtmlToImageHandlerTests> _logger;

    public ConvertHtmlToImageHandlerTests()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _mockBlobStorageProvider = new MockBlobStorageProvider(
            _loggerFactory.CreateLogger<MockBlobStorageProvider>());

        _logger = _loggerFactory.CreateLogger<ConvertHtmlToImageHandlerTests>();
    }

    [Fact]
    public async Task ValidateHtmlScreenshotRequest_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new HtmlScreenshotRequestBuilder()
            .WithUrl("https://www.example.com")
            .WithViewport(1920, 1080)
            .Build();

        // Act
        var errors = request.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateHtmlScreenshotRequest_WithMissingUrl_ShouldFail()
    {
        // Arrange
        var request = TestDataFactory.CreateInvalidHtmlScreenshotRequest_MissingUrl();

        // Act
        var errors = request.Validate();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("URL", string.Join("; ", errors), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateHtmlScreenshotRequest_WithInvalidViewport_ShouldFail()
    {
        // Arrange
        var request = TestDataFactory.CreateInvalidHtmlScreenshotRequest_InvalidViewport();

        // Act
        var errors = request.Validate();

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task MockBlobStorageProvider_UploadBlob_ShouldStoreInMemory()
    {
        // Arrange
        var testData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG signature
        var containerName = "screenshots";
        var blobName = "test-screenshot.png";

        // Act
        await _mockBlobStorageProvider.UploadAsync(
            containerName, blobName, testData, "image/png");

        // Assert
        var exists = await _mockBlobStorageProvider.ExistsAsync(containerName, blobName);
        Assert.True(exists);
    }

    [Fact]
    public async Task MockBlobStorageProvider_GenerateSasUrl_ShouldReturnValidUrl()
    {
        // Arrange
        var testData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var containerName = "screenshots";
        var blobName = "test-screenshot.png";

        await _mockBlobStorageProvider.UploadAsync(
            containerName, blobName, testData, "image/png");

        // Act
        var result = await _mockBlobStorageProvider.GenerateSasUrlAsync(
            containerName, blobName, expirationMinutes: 60);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Uri);
        Assert.NotNull(result.SasUrl);
        Assert.True(result.SasUrlExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task MockBlobStorageProvider_DeleteBlob_ShouldRemoveFromMemory()
    {
        // Arrange
        var testData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var containerName = "screenshots";
        var blobName = "test-screenshot.png";

        await _mockBlobStorageProvider.UploadAsync(
            containerName, blobName, testData, "image/png");

        // Act
        await _mockBlobStorageProvider.DeleteAsync(containerName, blobName);

        // Assert
        var exists = await _mockBlobStorageProvider.ExistsAsync(containerName, blobName);
        Assert.False(exists);
    }

    [Fact]
    public void TestDataFactory_CreateValidRequest_ShouldHaveAllRequiredFields()
    {
        // Act
        var request = TestDataFactory.CreateValidHtmlScreenshotRequest();

        // Assert
        Assert.NotNull(request.RequestId);
        Assert.NotNull(request.Url);
        Assert.NotNull(request.SourceId);
        Assert.NotNull(request.CorrelationId);
        Assert.True(request.ViewportWidth > 0);
        Assert.True(request.ViewportHeight > 0);
    }

    [Fact]
    public void TestDataFactory_CreateSuccessfulEvent_ShouldMarkAsSuccessful()
    {
        // Act
        var completedEvent = TestDataFactory.CreateSuccessfulScreenshotCompletedEvent();

        // Assert
        Assert.True(completedEvent.IsSuccessful);
        Assert.Null(completedEvent.ErrorMessage);
        Assert.NotNull(completedEvent.BlobUri);
        Assert.NotNull(completedEvent.BlobSasUrl);
    }

    [Fact]
    public void TestDataFactory_CreateFailedEvent_ShouldMarkAsFailed()
    {
        // Act
        var completedEvent = TestDataFactory.CreateFailedScreenshotCompletedEvent(
            errorMessage: "Test error");

        // Assert
        Assert.False(completedEvent.IsSuccessful);
        Assert.Equal("Test error", completedEvent.ErrorMessage);
        Assert.Null(completedEvent.BlobUri);
    }
}
