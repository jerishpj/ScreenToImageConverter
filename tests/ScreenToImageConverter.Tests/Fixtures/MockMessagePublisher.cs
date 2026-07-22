using Microsoft.Extensions.Logging;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;

namespace ScreenToImageConverter.Tests.Fixtures;

/// <summary>
/// Mock implementation of IMessagePublisher for testing purposes.
/// Captures published messages for verification without actual broker connectivity.
/// </summary>
public class MockMessagePublisher : IMessagePublisher
{
    private readonly ILogger<MockMessagePublisher> _logger;
    private bool _isConnected;
    private bool _disposed;
    private readonly List<PublishedMessage> _publishedMessages = new();

    public bool IsConnected => _isConnected;

    public IReadOnlyList<PublishedMessage> PublishedMessages => _publishedMessages.AsReadOnly();

    public MockMessagePublisher(ILogger<MockMessagePublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isConnected = true;
    }

    /// <summary>
    /// Clears the history of published messages.
    /// </summary>
    public void ClearPublishedMessages()
    {
        _publishedMessages.Clear();
    }

    /// <summary>
    /// Gets published messages of a specific type.
    /// </summary>
    public List<PublishedMessage> GetPublishedMessages<T>() where T : class
    {
        return _publishedMessages.Where(m => m.MessageType == typeof(T).Name).ToList();
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("MockMessagePublisher is not connected");
        }

        var publishedMsg = new PublishedMessage
        {
            MessageType = typeof(T).Name,
            Message = message,
            CorrelationId = null,
            PublishedAt = DateTime.UtcNow
        };

        _publishedMessages.Add(publishedMsg);
        _logger.LogInformation("📤 MockMessagePublisher published message of type {MessageType}", typeof(T).Name);

        await Task.CompletedTask;
    }

    public async Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("MockMessagePublisher is not connected");
        }

        var publishedMsg = new PublishedMessage
        {
            MessageType = typeof(T).Name,
            Message = message,
            CorrelationId = correlationId,
            PublishedAt = DateTime.UtcNow
        };

        _publishedMessages.Add(publishedMsg);
        _logger.LogInformation(
            "📤 MockMessagePublisher published message of type {MessageType} with CorrelationId {CorrelationId}",
            typeof(T).Name, correlationId);

        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("MockMessagePublisher disposing");
        _isConnected = false;
        _disposed = true;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Represents a published message for testing.
    /// </summary>
    public class PublishedMessage
    {
        public required string MessageType { get; init; }
        public required object Message { get; init; }
        public string? CorrelationId { get; init; }
        public DateTime PublishedAt { get; init; }
    }
}
