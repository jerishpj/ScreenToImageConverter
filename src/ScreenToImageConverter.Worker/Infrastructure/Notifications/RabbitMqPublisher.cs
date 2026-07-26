using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ScreenToImageConverter.Worker.AppSettings;

namespace ScreenToImageConverter.Worker.Infrastructure.Notifications;

/// <summary>
/// RabbitMQ implementation of IMessagePublisher for local development testing.
/// Simulates Azure Service Bus behavior using RabbitMQ.
/// </summary>
public class RabbitMqPublisher : IMessagePublisher, IDisposable, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public bool IsConnected => _channel?.IsOpen == true;

    public RabbitMqPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeConnection();
    }

    /// <summary>
    /// Initializes the RabbitMQ connection.
    /// </summary>
    private void InitializeConnection()
    {
        try
        {
            _logger.LogInformation("🐰 Initializing RabbitMQ publisher...");

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare exchange (create if doesn't exist)
            _channel.ExchangeDeclareAsync(
                exchange: _options.CompletionEventExchange,
                type: ExchangeType.Topic,
                durable: true).GetAwaiter().GetResult();

            _logger.LogInformation("✅ RabbitMQ publisher initialized for exchange '{Exchange}'",
                _options.CompletionEventExchange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize RabbitMQ publisher");
            throw;
        }
    }

    /// <summary>
    /// Publishes a message to RabbitMQ.
    /// </summary>
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        await PublishAsync(message, null, cancellationToken);
    }

    /// <summary>
    /// Publishes a message with correlation ID.
    /// </summary>
    public async Task PublishAsync<T>(T message, string? correlationId, CancellationToken cancellationToken) where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (_channel == null || !_channel.IsOpen)
        {
            throw new InvalidOperationException("RabbitMQ channel is not open");
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                CorrelationId = correlationId ?? Guid.NewGuid().ToString()
            };

            await _channel.BasicPublishAsync(
                exchange: _options.CompletionEventExchange,
                routingKey: _options.CompletionEventRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("✅ Message published to RabbitMQ [CorrelationId: {CorrelationId}]",
                properties.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish message to RabbitMQ");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _connection?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _connection?.Dispose();

        _disposed = true;
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}
