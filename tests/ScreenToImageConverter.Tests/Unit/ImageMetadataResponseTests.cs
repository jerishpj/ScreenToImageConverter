using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

namespace ScreenToImageConverter.Tests.Unit;

/// <summary>
/// Unit tests for ImageMetadataResponse.
/// Tests cover factory methods, property initialization, and response state.
/// </summary>
public class ImageMetadataResponseTests
{
    #region Constructor and Default Values Tests

    [Fact]
    public void Constructor_WithDefaults_ShouldInitializeDefaultValues()
    {
        // Act
        var response = new ImageMetadataResponse();

        // Assert
        Assert.NotNull(response.RequestId);
        Assert.Equal("image/png", response.ContentType);
        Assert.Equal("1.0", response.SchemaVersion);
        Assert.True(response.ProcessedAt > DateTime.MinValue);
        Assert.False(response.IsSuccessful); // Default
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueRequestIds()
    {
        // Act
        var response1 = new ImageMetadataResponse();
        var response2 = new ImageMetadataResponse();

        // Assert
        Assert.NotEqual(response1.RequestId, response2.RequestId);
    }

    #endregion

    #region CreateFailure Factory Method Tests

    [Fact]
    public void CreateFailure_WithMinimalParameters_ShouldCreateFailureResponse()
    {
        // Act
        var response = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            "Test error message");

        // Assert
        Assert.Equal("req-123", response.RequestId);
        Assert.Equal("https://example.com", response.Url);
        Assert.False(response.IsSuccessful);
        Assert.Equal("Test error message", response.ErrorMessage);
        Assert.Null(response.BlobUri);
        Assert.Null(response.BlobSasUrl);
    }

    [Fact]
    public void CreateFailure_WithAllParameters_ShouldPopulateAllFields()
    {
        // Act
        var response = ImageMetadataResponse.CreateFailure(
            requestId: "req-123",
            url: "https://example.com",
            errorMessage: "Conversion failed",
            correlationId: "corr-123",
            sourceId: "source-1",
            processingDurationMs: 5000);

        // Assert
        Assert.Equal("req-123", response.RequestId);
        Assert.Equal("https://example.com", response.Url);
        Assert.Equal("corr-123", response.CorrelationId);
        Assert.Equal("source-1", response.SourceId);
        Assert.False(response.IsSuccessful);
        Assert.Equal("Conversion failed", response.ErrorMessage);
        Assert.Equal(5000, response.ProcessingDurationMs);
    }

    [Fact]
    public void CreateFailure_ShouldSetIsSuccessfulToFalse()
    {
        // Act
        var response = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            "Error");

        // Assert
        Assert.False(response.IsSuccessful);
    }

    [Fact]
    public void CreateFailure_ShouldSetProcessedAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var response = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            "Error");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(response.ProcessedAt >= beforeCreation);
        Assert.True(response.ProcessedAt <= afterCreation);
    }

    [Fact]
    public void CreateFailure_WithNullCorrelationAndSourceId_ShouldHandleNullValues()
    {
        // Act
        var response = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            "Error",
            correlationId: null,
            sourceId: null);

        // Assert
        Assert.Null(response.CorrelationId);
        Assert.Null(response.SourceId);
    }

    [Fact]
    public void CreateFailure_WithZeroProcessingDuration_ShouldSetZero()
    {
        // Act
        var response = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            "Error",
            processingDurationMs: 0);

        // Assert
        Assert.Equal(0, response.ProcessingDurationMs);
    }

    #endregion

    #region CreateSuccess Factory Method Tests

    [Fact]
    public void CreateSuccess_WithMinimalParameters_ShouldCreateSuccessResponse()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            requestId: "req-123",
            url: "https://example.com",
            blobFileName: "test-image.png",
            blobContainerName: "screenshots",
            blobUri: "https://storage.azure.com/screenshots/test-image.png");

        // Assert
        Assert.Equal("req-123", response.RequestId);
        Assert.Equal("https://example.com", response.Url);
        Assert.True(response.IsSuccessful);
        Assert.Equal("test-image.png", response.BlobFileName);
        Assert.Equal("screenshots", response.BlobContainerName);
        Assert.Equal("https://storage.azure.com/screenshots/test-image.png", response.BlobUri);
    }

    [Fact]
    public void CreateSuccess_WithAllParameters_ShouldPopulateAllFields()
    {
        // Arrange
        var sasExpiry = DateTime.UtcNow.AddHours(1);

        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            requestId: "req-123",
            url: "https://example.com",
            blobFileName: "test-image.png",
            blobContainerName: "screenshots",
            blobUri: "https://storage.azure.com/screenshots/test-image.png",
            sasUrl: "https://storage.azure.com/screenshots/test-image.png?sv=2021-06-08&...",
            sasUrlExpiresAt: sasExpiry,
            fileSizeBytes: 102400,
            correlationId: "corr-123",
            sourceId: "source-1",
            processingDurationMs: 3000,
            processedByInstanceId: "instance-1");

        // Assert
        Assert.Equal("req-123", response.RequestId);
        Assert.Equal("https://example.com", response.Url);
        Assert.Equal("corr-123", response.CorrelationId);
        Assert.Equal("source-1", response.SourceId);
        Assert.True(response.IsSuccessful);
        Assert.Equal("test-image.png", response.BlobFileName);
        Assert.Equal("screenshots", response.BlobContainerName);
        Assert.Equal("https://storage.azure.com/screenshots/test-image.png", response.BlobUri);
        Assert.NotNull(response.BlobSasUrl);
        Assert.Equal(sasExpiry, response.SasUrlExpiresAt);
        Assert.Equal(102400, response.FileSizeBytes);
        Assert.Equal(3000, response.ProcessingDurationMs);
        Assert.Equal("instance-1", response.ProcessedByInstanceId);
    }

    [Fact]
    public void CreateSuccess_ShouldSetIsSuccessfulToTrue()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png");

        // Assert
        Assert.True(response.IsSuccessful);
    }

    [Fact]
    public void CreateSuccess_ShouldNotSetErrorMessage()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png");

        // Assert
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public void CreateSuccess_ShouldSetProcessedAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(response.ProcessedAt >= beforeCreation);
        Assert.True(response.ProcessedAt <= afterCreation);
    }

    [Fact]
    public void CreateSuccess_WithNullOptionalParameters_ShouldHandleNullValues()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            requestId: "req-123",
            url: "https://example.com",
            blobFileName: "test.png",
            blobContainerName: "screenshots",
            blobUri: "https://storage.azure.com/screenshots/test.png",
            sasUrl: null,
            sasUrlExpiresAt: null,
            fileSizeBytes: null,
            correlationId: null,
            sourceId: null,
            processingDurationMs: 0,
            processedByInstanceId: null);

        // Assert
        Assert.Null(response.BlobSasUrl);
        Assert.Null(response.SasUrlExpiresAt);
        Assert.Null(response.FileSizeBytes);
        Assert.Null(response.CorrelationId);
        Assert.Null(response.SourceId);
        Assert.Null(response.ProcessedByInstanceId);
    }

    [Fact]
    public void CreateSuccess_ShouldSetDefaultContentType()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png");

        // Assert
        Assert.Equal("image/png", response.ContentType);
    }

    [Fact]
    public void CreateSuccess_ShouldSetDefaultSchemaVersion()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png");

        // Assert
        Assert.Equal("1.0", response.SchemaVersion);
    }

    [Fact]
    public void CreateSuccess_WithLargeFileSize_ShouldPreserveValue()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png",
            fileSizeBytes: 10485760); // 10MB

        // Assert
        Assert.Equal(10485760, response.FileSizeBytes);
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        // Arrange
        var response = new ImageMetadataResponse();

        // Act
        response.RequestId = "req-999";
        response.CorrelationId = "corr-999";
        response.SourceId = "source-999";
        response.Url = "https://custom.url";
        response.IsSuccessful = true;
        response.ErrorMessage = "Custom error";
        response.BlobFileName = "custom.png";
        response.BlobContainerName = "custom-container";
        response.BlobUri = "https://custom.uri";
        response.BlobSasUrl = "https://custom.sas";
        response.SasUrlExpiresAt = DateTime.UtcNow.AddHours(2);
        response.FileSizeBytes = 999999;
        response.ContentType = "image/jpeg";
        response.ProcessingDurationMs = 9999;
        response.ProcessedByInstanceId = "custom-instance";
        response.RetryAttempts = 5;
        response.SchemaVersion = "2.0";

        // Assert
        Assert.Equal("req-999", response.RequestId);
        Assert.Equal("corr-999", response.CorrelationId);
        Assert.Equal("source-999", response.SourceId);
        Assert.Equal("https://custom.url", response.Url);
        Assert.True(response.IsSuccessful);
        Assert.Equal("Custom error", response.ErrorMessage);
        Assert.Equal("custom.png", response.BlobFileName);
        Assert.Equal("custom-container", response.BlobContainerName);
        Assert.Equal("https://custom.uri", response.BlobUri);
        Assert.Equal("https://custom.sas", response.BlobSasUrl);
        Assert.True(response.SasUrlExpiresAt > DateTime.UtcNow);
        Assert.Equal(999999, response.FileSizeBytes);
        Assert.Equal("image/jpeg", response.ContentType);
        Assert.Equal(9999, response.ProcessingDurationMs);
        Assert.Equal("custom-instance", response.ProcessedByInstanceId);
        Assert.Equal(5, response.RetryAttempts);
        Assert.Equal("2.0", response.SchemaVersion);
    }

    #endregion

    #region Failure vs Success Comparison Tests

    [Fact]
    public void FailureAndSuccessResponses_ShouldBeDifferent()
    {
        // Act
        var failureResponse = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            "Error occurred");

        var successResponse = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png");

        // Assert
        Assert.False(failureResponse.IsSuccessful);
        Assert.True(successResponse.IsSuccessful);
        Assert.NotNull(failureResponse.ErrorMessage);
        Assert.Null(successResponse.ErrorMessage);
        Assert.Null(failureResponse.BlobUri);
        Assert.NotNull(successResponse.BlobUri);
    }

    #endregion
}
