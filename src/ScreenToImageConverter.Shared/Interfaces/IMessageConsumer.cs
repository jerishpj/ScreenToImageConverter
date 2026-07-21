namespace ScreenToImageConverter.Shared.Interfaces;

/// <summary>
/// Interface for consuming messages from Service Bus.
/// Implementations handle receiving and deserializing messages.
/// </summary>
public interface IMessageConsumer : IAsyncDisposable
{
    /// <summary>
    /// Starts listening for messages from the Service Bus subscription.
    /// This method should be called once during application startup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop listening.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops listening for messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the consumer is currently connected and listening.
    /// </summary>
    bool IsConnected { get; }
}
