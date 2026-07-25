using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ScreenToImageConverter.Worker.AppSettings;

namespace ScreenToImageConverter.Worker.Infrastructure.Notifications;

/// <summary>
/// RabbitMQ implementation of IMessageConsumer for local development testing.
/// Simulates Azure Service Bus behavior using RabbitMQ.
/// 
/// This is a drop-in replacement for ServiceBusConsumer.
/// Use this for local development without Azure costs.
/// </summary>
public class RabbitMqConsumer : IMessageConsumer
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;
    private bool _disposed;

    public delegate Task MessageHandlerDelegate(HtmlScreenshotRequest message, string correlationId, CancellationToken cancellationToken);
    private MessageHandlerDelegate? _messageHandler;

    public bool IsConnected => _channel?.IsOpen == true;

    public RabbitMqConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConsumer> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers the message handler callback.
    /// </summary>
    public void RegisterMessageHandler(MessageHandlerDelegate handler)
    {
        _messageHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger.LogInformation("Message handler registered for RabbitMQ consumer");
    }

    /// <summary>
    /// Starts listening for messages from RabbitMQ.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_messageHandler == null)
        {
            throw new InvalidOperationException(
                "Message handler not registered. Call RegisterMessageHandler before starting.");
        }

        try
        {
            _logger.LogInformation("🐰 Initializing RabbitMQ consumer...");

            // Create connection factory
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true
            };

            // Create connection and channel
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Declare queue and exchange (create if doesn't exist)
            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true);

            await _channel.QueueDeclareAsync(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange
            await _channel.QueueBindAsync(
                queue: _options.QueueName,
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey);

            _logger.LogInformation("✅ RabbitMQ consumer connected to exchange '{Exchange}' and queue '{Queue}'",
                _options.ExchangeName, _options.QueueName);

            // Setup consumer
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += OnMessageReceivedAsync;

            // Start consuming
            await _channel.BasicConsumeAsync(
                queue: _options.QueueName,
                autoAck: false,
                consumerTag: "screenshot-consumer",
                consumer: _consumer);

            _logger.LogInformation("✅ RabbitMQ consumer started. Waiting for messages...");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start RabbitMQ consumer");
            throw;
        }
    }

    /// <summary>
    /// Handles incoming messages from RabbitMQ.
    /// </summary>
    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = JsonSerializer.Deserialize<HtmlScreenshotRequest>(body);

            if (message == null)
            {
                _logger.LogWarning("Received null message from RabbitMQ");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            var correlationId = ea.BasicProperties?.CorrelationId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("📨 Received message [RequestId: {RequestId}, CorrelationId: {CorrelationId}]",
                message.RequestId, correlationId);

            // Invoke the registered message handler
            if (_messageHandler != null)
            {
                await _messageHandler(message, correlationId, CancellationToken.None);
            }

            // Acknowledge the message (remove from queue)
            await _channel!.BasicAckAsync(ea.DeliveryTag, false);

            _logger.LogInformation("✅ Message processed and acknowledged");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing message from RabbitMQ");
            // Negative acknowledge (requeue the message)
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, true);
        }
    }

    /// <summary>
    /// Stops the consumer and closes the connection.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🛑 Stopping RabbitMQ consumer...");

            if (_channel?.IsOpen == true)
            {
                await _channel.CloseAsync();
            }

            if (_connection?.IsOpen == true)
            {
                await _connection.CloseAsync();
            }

            _logger.LogInformation("✅ RabbitMQ consumer stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping RabbitMQ consumer");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await StopAsync(CancellationToken.None);

        _channel?.Dispose();
        _connection?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}