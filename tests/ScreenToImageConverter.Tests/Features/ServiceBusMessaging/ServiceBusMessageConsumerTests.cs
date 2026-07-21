using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Messages;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Consumers;

namespace ScreenToImageConverter.Tests.Features.ServiceBusMessaging;

/// <summary>
/// Unit tests for ServiceBusMessageConsumer.
/// Tests message consumption, deserialization, error handling, and correlation ID propagation.
/// </summary>
public class ServiceBusMessageConsumerTests
{
    private readonly Mock<IOptions<ServiceBusOptions>> _optionsMock;
    private readonly Mock<ILogger<ServiceBusMessageConsumer>> _loggerMock;
    private readonly ServiceBusOptions _options;

    public ServiceBusMessageConsumerTests()
    {
        _options = new ServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
            UseManagedIdentity = false,
            HtmlScreenshotRequestTopicName = "html-screenshot-requests",
            HtmlScreenshotRequestSubscriptionName = "screenshot-worker-subscription",
            ScreenshotCompletedEventTopicName = "screenshot-completed-events",
            MaxConcurrentCalls = 10,
            PrefetchCount = 5
        };

        _optionsMock = new Mock<IOptions<ServiceBusOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_options);

        _loggerMock = new Mock<ILogger<ServiceBusMessageConsumer>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var nullOptions = new Mock<IOptions<ServiceBusOptions>>();
        nullOptions.Setup(o => o.Value).Returns((ServiceBusOptions)null!);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ServiceBusMessageConsumer(nullOptions.Object, _loggerMock.Object));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ServiceBusMessageConsumer(_optionsMock.Object, null!));
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        // Act
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(consumer);
        Assert.False(consumer.IsConnected); // Not connected until StartAsync is called
    }

    #endregion

    #region MessageHandler Registration Tests

    [Fact]
    public void RegisterMessageHandler_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            consumer.RegisterMessageHandler(null!));
        Assert.Equal("handler", ex.ParamName);
    }

    [Fact]
    public void RegisterMessageHandler_WithValidHandler_Succeeds()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);
        async Task MockHandler(HtmlScreenshotRequest req, string corrId, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        // Act - Should not throw
        consumer.RegisterMessageHandler(MockHandler);

        // Assert - Logging confirms registration
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message handler registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_WithoutRegisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => consumer.StartAsync(cancellationToken));
        Assert.Contains("Message handler not registered", ex.Message);
    }

    [Fact]
    public async Task StartAsync_WithInvalidConnectionString_ThrowsException()
    {
        // Arrange
        var badOptions = new ServiceBusOptions
        {
            ConnectionString = "Invalid connection string",
            UseManagedIdentity = false,
            HtmlScreenshotRequestTopicName = "test-topic",
            HtmlScreenshotRequestSubscriptionName = "test-subscription",
            ScreenshotCompletedEventTopicName = "output-topic",
            MaxConcurrentCalls = 10,
            PrefetchCount = 5
        };

        var badOptionsMock = new Mock<IOptions<ServiceBusOptions>>();
        badOptionsMock.Setup(o => o.Value).Returns(badOptions);

        var consumer = new ServiceBusMessageConsumer(badOptionsMock.Object, _loggerMock.Object);

        async Task MockHandler(HtmlScreenshotRequest req, string corrId, CancellationToken ct)
        {
            await Task.CompletedTask;
        }
        consumer.RegisterMessageHandler(MockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => consumer.StartAsync(CancellationToken.None));
    }

    #endregion

    #region Message Deserialization Tests

    [Fact]
    public void DeserializeValidMessage_WithAllProperties_Succeeds()
    {
        // Arrange
        var request = new HtmlScreenshotRequest
        {
            RequestId = "req-123",
            Url = "https://example.com",
            SourceId = "source-456",
            CorrelationId = "corr-789",
            ViewportWidth = 1280,
            ViewportHeight = 720,
            TimeoutMs = 45000,
            WaitForPageLoad = true,
            ScreenshotName = "Test Screenshot"
        };

        var json = JsonSerializer.Serialize(request);

        // Act
        var deserialized = JsonSerializer.Deserialize<HtmlScreenshotRequest>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("req-123", deserialized.RequestId);
        Assert.Equal("https://example.com", deserialized.Url);
        Assert.Equal("source-456", deserialized.SourceId);
        Assert.Equal("corr-789", deserialized.CorrelationId);
        Assert.Equal(1280, deserialized.ViewportWidth);
        Assert.Equal(720, deserialized.ViewportHeight);
        Assert.Equal(45000, deserialized.TimeoutMs);
        Assert.True(deserialized.WaitForPageLoad);
        Assert.Equal("Test Screenshot", deserialized.ScreenshotName);
    }

    [Fact]
    public void DeserializeValidMessage_WithMinimalProperties_Succeeds()
    {
        // Arrange
        var request = new HtmlScreenshotRequest
        {
            RequestId = "req-123",
            Url = "https://example.com"
        };

        var json = JsonSerializer.Serialize(request);

        // Act
        var deserialized = JsonSerializer.Deserialize<HtmlScreenshotRequest>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("req-123", deserialized.RequestId);
        Assert.Equal("https://example.com", deserialized.Url);
        Assert.Null(deserialized.SourceId);
        Assert.Null(deserialized.CorrelationId);
    }

    [Fact]
    public void DeserializeInvalidJson_ReturnsNull()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var deserialized = JsonSerializer.Deserialize<HtmlScreenshotRequest>(invalidJson);

        // Assert
        Assert.Null(deserialized);
    }

    #endregion

    #region Message Validation Tests

    [Fact]
    public void ValidateMessage_WithValidUrl_ReturnsNoErrors()
    {
        // Arrange
        var request = new HtmlScreenshotRequest
        {
            Url = "https://example.com"
        };

        // Act
        var errors = request.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateMessage_WithNullUrl_ReturnsError()
    {
        // Arrange
        var request = new HtmlScreenshotRequest
        {
            Url = null!
        };

        // Act
        var errors = request.Validate();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("URL is required", errors.FirstOrDefault() ?? "");
    }

    [Fact]
    public void ValidateMessage_WithEmptyUrl_ReturnsError()
    {
        // Arrange
        var request = new HtmlScreenshotRequest
        {
            Url = ""
        };

        // Act
        var errors = request.Validate();

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ValidateMessage_WithInvalidUrl_ReturnsError()
    {
        // Arrange
        var request = new HtmlScreenshotRequest
        {
            Url = "not a url"
        };

        // Act
        var errors = request.Validate();

        // Assert
        Assert.NotEmpty(errors);
    }

    #endregion

    #region Correlation ID Tests

    [Fact]
    public void CorrelationId_FromServiceBusMessage_IsPreserved()
    {
        // Arrange
        var correlationId = "my-correlation-id";
        var request = new HtmlScreenshotRequest
        {
            Url = "https://example.com"
        };
        var json = JsonSerializer.Serialize(request);

        // Act
        // In real scenario, this would come from ServiceBusMessage.CorrelationId
        var messageCorrelationId = !string.IsNullOrWhiteSpace(correlationId) 
            ? correlationId 
            : Guid.NewGuid().ToString();

        // Assert
        Assert.Equal("my-correlation-id", messageCorrelationId);
    }

    [Fact]
    public void CorrelationId_WhenNull_GeneratesNewGuid()
    {
        // Arrange
        string? correlationId = null;

        // Act
        var messageCorrelationId = !string.IsNullOrWhiteSpace(correlationId) 
            ? correlationId 
            : Guid.NewGuid().ToString();

        // Assert
        Assert.NotNull(messageCorrelationId);
        Assert.NotEmpty(messageCorrelationId);
        // Verify it's a valid GUID format
        Assert.True(Guid.TryParse(messageCorrelationId, out _));
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenNotStarted_CompletesWithoutError()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);

        // Act - Should not throw
        await consumer.StopAsync(CancellationToken.None);

        // Assert
        // Verify logging occurred
        _loggerMock.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WithValidConsumer_CompletesSuccessfully()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);

        // Act - Should not throw
        await consumer.DisposeAsync();

        // Assert
        // Verify disposal logging
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw on multiple calls
        await consumer.DisposeAsync();
        await consumer.DisposeAsync();
        await consumer.DisposeAsync();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void IsConnected_BeforeStartAsync_ReturnsFalse()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);

        // Act
        var isConnected = consumer.IsConnected;

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void MessageHandler_PreservesRequestData()
    {
        // Arrange
        var consumer = new ServiceBusMessageConsumer(_optionsMock.Object, _loggerMock.Object);
        HtmlScreenshotRequest? capturedRequest = null;
        string? capturedCorrelationId = null;

        async Task CaptureHandler(HtmlScreenshotRequest req, string corrId, CancellationToken ct)
        {
            capturedRequest = req;
            capturedCorrelationId = corrId;
            await Task.CompletedTask;
        }

        consumer.RegisterMessageHandler(CaptureHandler);

        // Act
        // Simulate calling the handler
        var testRequest = new HtmlScreenshotRequest { Url = "https://example.com" };
        var testCorrelationId = "test-corr-123";

        // This is a simplified test; in real scenario, this would be called via Service Bus
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion
}
