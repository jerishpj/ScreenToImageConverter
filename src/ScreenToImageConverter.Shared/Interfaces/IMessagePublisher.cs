namespace ScreenToImageConverter.Shared.Interfaces;

/// <summary>
/// Interface for publishing messages to Service Bus.
/// Implementations handle serialization and publishing to topics/queues.
/// </summary>
public interface IMessagePublisher : IAsyncDisposable
{
    /// <summary>
    /// Publishes a message to the Service Bus topic.
    /// </summary>
    /// <typeparam name="T">Type of the message to publish.</typeparam>
    /// <param name="message">The message object to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// Publishes a message with a correlation ID for end-to-end tracking.
    /// </summary>
    /// <typeparam name="T">Type of the message to publish.</typeparam>
    /// <param name="message">The message object to publish.</param>
    /// <param name="correlationId">Correlation ID for tracking across services.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// Checks if the publisher is currently connected.
    /// </summary>
    bool IsConnected { get; }
}
