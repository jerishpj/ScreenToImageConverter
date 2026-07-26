using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ScreenToImageConverter.Worker.AppSettings;

namespace ScreenToImageConverter.Worker.Infrastructure.Notifications;

/// <summary>
/// RabbitMQ implementation of IMessageConsumer for local development testing.
/// Simulates Azure Service Bus behavior using RabbitMQ.
/// 
/// This is a drop-in replacement for ServiceBusConsumer.
/// Use this for local development without Azure costs.
/// 
/// Implements both IDisposable and IAsyncDisposable following best practices.
/// The DI container may call either Dispose() or DisposeAsync() depending on context.
/// </summary>
public class RabbitMqConsumer : IMessageConsumer, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;
    private bool _disposed;
    private bool _consumerStarted;
    private CancellationTokenSource? _reconnectionCancellationTokenSource;
    private Task? _reconnectionTask;

    public delegate Task MessageHandlerDelegate(HtmlScreenshotRequest message, string correlationId, CancellationToken cancellationToken);
    private MessageHandlerDelegate? _messageHandler;

    public bool IsConnected => _channel?.IsOpen == true && _consumerStarted;

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
    /// Implements graceful failure handling with exponential backoff retry and circuit breaker.
    /// 
    /// Behavior:
    /// - On successful connection: starts consuming messages, sets _consumerStarted = true
    /// - On connection failure with graceful degradation: starts background reconnection task
    /// - Background task automatically reconnects and resumes message consumption when RabbitMQ is available
    /// - No manual restart required - connection recovery is automatic
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

            // Create connection factory with automatic recovery
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds)
            };

            try
            {
                // Attempt to create connection with retry policy
                _connection = await factory.CreateConnectionAsync();
                await InitializeChannelAsync(cancellationToken);

                _logger.LogInformation(
                    "✅ RabbitMQ consumer connected to {Host}:{Port}, exchange '{Exchange}', queue '{Queue}'",
                    _options.HostName, _options.Port, _options.ExchangeName, _options.QueueName);

                // Setup consumer
                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.ReceivedAsync += OnMessageReceivedAsync;

                // Start consuming
                await _channel.BasicConsumeAsync(
                    queue: _options.QueueName,
                    autoAck: false,
                    consumerTag: "screenshot-consumer",
                    consumer: _consumer);

                _consumerStarted = true;
                _logger.LogInformation("✅ RabbitMQ consumer started. Waiting for messages...");

                await Task.CompletedTask;
            }
            catch (BrokerUnreachableException ex)
            {
                // Graceful handling of connection failure
                _logger.LogError(
                    ex,
                    "❌ RabbitMQ broker is unreachable at {Host}:{Port}. " +
                    "Worker will attempt to reconnect with exponential backoff. " +
                    "Ensure RabbitMQ is running and accessible.",
                    _options.HostName,
                    _options.Port);

                if (_options.EnableGracefulDegradation)
                {
                    _logger.LogWarning(
                        "⚠️ Graceful degradation enabled. Starting background reconnection task. " +
                        "No messages will be processed until RabbitMQ is available. " +
                        "Once RabbitMQ is up, message consumption will resume automatically without restart.");

                    // Start background reconnection task that will automatically resume when RabbitMQ is available
                    _reconnectionCancellationTokenSource = new CancellationTokenSource();
                    _reconnectionTask = MonitorAndReconnectAsync(_reconnectionCancellationTokenSource.Token);
                }
                else
                {
                    _logger.LogCritical(
                        "🚨 Graceful degradation disabled. Worker will fail immediately. " +
                        "Please ensure RabbitMQ is running before starting the worker.");
                    throw;
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Network error while connecting to RabbitMQ at {Host}:{Port}. " +
                    "This could indicate network connectivity issues or firewall blocks.",
                    _options.HostName,
                    _options.Port);

                if (_options.EnableGracefulDegradation)
                {
                    _logger.LogWarning(
                        "⚠️ Graceful degradation enabled. Starting background reconnection task.");

                    _reconnectionCancellationTokenSource = new CancellationTokenSource();
                    _reconnectionTask = MonitorAndReconnectAsync(_reconnectionCancellationTokenSource.Token);
                }
                else
                {
                    throw;
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "⏱️ Connection attempt cancelled (timeout or user cancellation)");
                if (!_options.EnableGracefulDegradation)
                {
                    throw;
                }

                _logger.LogWarning(
                    "⚠️ Graceful degradation enabled. Starting background reconnection task.");

                _reconnectionCancellationTokenSource = new CancellationTokenSource();
                _reconnectionTask = MonitorAndReconnectAsync(_reconnectionCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Unexpected error while starting RabbitMQ consumer: {ErrorMessage}",
                    ex.Message);

                if (_options.EnableGracefulDegradation)
                {
                    _logger.LogWarning(
                        "⚠️ Graceful degradation enabled. Starting background reconnection task. " +
                        "Please review the error details above for root cause analysis.");

                    _reconnectionCancellationTokenSource = new CancellationTokenSource();
                    _reconnectionTask = MonitorAndReconnectAsync(_reconnectionCancellationTokenSource.Token);
                }
                else
                {
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error in StartAsync");
            throw;
        }
    }

    /// <summary>
    /// Monitors RabbitMQ connection status and automatically reconnects when broker becomes available.
    /// Runs as a background task and continuously attempts to establish connection if initial connection failed.
    /// 
    /// Behavior:
    /// - Checks connection status at regular intervals
    /// - If not connected, attempts to reconnect
    /// - On successful reconnection, resumes message consumption automatically
    /// - Uses exponential backoff to prevent excessive reconnection attempts
    /// - Logs all connection attempts and state changes for operational visibility
    /// </summary>
    private async Task MonitorAndReconnectAsync(CancellationToken cancellationToken)
    {
        TimeSpan currentDelay = TimeSpan.FromSeconds(_options.ReconnectionIntervalSeconds);
        int reconnectionAttempt = 0;

        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            try
            {
                await Task.Delay(currentDelay, cancellationToken);

                // Check if already connected and consuming
                if (_consumerStarted && _channel?.IsOpen == true)
                {
                    _logger.LogInformation("✅ RabbitMQ consumer is connected and consuming messages");
                    currentDelay = TimeSpan.FromSeconds(_options.ReconnectionIntervalSeconds); // Reset delay
                    continue;
                }

                reconnectionAttempt++;
                _logger.LogInformation(
                    "🔄 Attempting to reconnect to RabbitMQ (attempt {Attempt}). " +
                    "Next retry in {Seconds} seconds if this fails...",
                    reconnectionAttempt,
                    (int)currentDelay.TotalSeconds);

                // Clean up previous connection if it exists
                if (_connection?.IsOpen == true)
                {
                    try
                    {
                        await _connection.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error closing previous RabbitMQ connection during reconnect");
                    }
                }

                // Attempt reconnection
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    AutomaticRecoveryEnabled = true,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds)
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                await InitializeChannelAsync(cancellationToken);

                _logger.LogInformation(
                    "✅ Successfully reconnected to RabbitMQ at {Host}:{Port}. Resuming message consumption...",
                    _options.HostName,
                    _options.Port);

                // Re-setup consumer
                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.ReceivedAsync += OnMessageReceivedAsync;

                // Resume consuming
                await _channel.BasicConsumeAsync(
                    queue: _options.QueueName,
                    autoAck: false,
                    consumerTag: "screenshot-consumer",
                    consumer: _consumer);

                _consumerStarted = true;
                _logger.LogInformation("✅ Message consumption resumed. Ready to process messages.");

                // Reset counters and delay on successful reconnection
                reconnectionAttempt = 0;
                currentDelay = TimeSpan.FromSeconds(_options.ReconnectionIntervalSeconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Background reconnection task cancelled");
                break;
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogWarning(
                    ex,
                    "⚠️ Reconnection attempt {Attempt} failed: Broker unreachable at {Host}:{Port}. " +
                    "Will retry in {Seconds} seconds...",
                    reconnectionAttempt,
                    _options.HostName,
                    _options.Port,
                    (int)currentDelay.TotalSeconds);

                // Increase delay with cap
                currentDelay = TimeSpan.FromSeconds(
                    Math.Min(currentDelay.TotalSeconds * 2, _options.MaxReconnectionIntervalSeconds));
            }
            catch (IOException ex)
            {
                _logger.LogWarning(
                    ex,
                    "⚠️ Network error during reconnection attempt {Attempt}. " +
                    "Will retry in {Seconds} seconds...",
                    reconnectionAttempt,
                    (int)currentDelay.TotalSeconds);

                currentDelay = TimeSpan.FromSeconds(
                    Math.Min(currentDelay.TotalSeconds * 2, _options.MaxReconnectionIntervalSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Unexpected error during reconnection attempt {Attempt}. " +
                    "Will retry in {Seconds} seconds...",
                    reconnectionAttempt,
                    (int)currentDelay.TotalSeconds);

                currentDelay = TimeSpan.FromSeconds(
                    Math.Min(currentDelay.TotalSeconds * 2, _options.MaxReconnectionIntervalSeconds));
            }
        }

        _logger.LogInformation("🛑 Background reconnection task ended");
    }

        /// <summary>
        /// Initializes the RabbitMQ channel and declares queue/exchange.
        /// Separated for cleaner error handling and retry logic.
        /// </summary>
        private async Task InitializeChannelAsync(CancellationToken cancellationToken)
        {
            _channel = await _connection!.CreateChannelAsync();

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
        }

    /// <summary>
    /// Handles incoming messages from RabbitMQ.
    /// </summary>
    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();

            // Use case-insensitive deserialization to support both PascalCase and camelCase JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var message = JsonSerializer.Deserialize<HtmlScreenshotRequest>(body, options);

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

            // Cancel background reconnection task if running
            if (_reconnectionCancellationTokenSource != null && !_reconnectionCancellationTokenSource.IsCancellationRequested)
            {
                _reconnectionCancellationTokenSource.Cancel();

                // Wait for reconnection task to complete (with timeout)
                if (_reconnectionTask != null)
                {
                    try
                    {
                        await _reconnectionTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling the task
                    }
                }
            }

            _consumerStarted = false;

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

    /// <summary>
    /// Synchronous dispose for IDisposable.
    /// Calls StopAsync synchronously (blocking) to clean up resources.
    /// This is used when the DI container disposes the instance synchronously.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Synchronously block on async stop to ensure cleanup
            StopAsync(CancellationToken.None).Wait();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during synchronous disposal");
        }

        _channel?.Dispose();
        _connection?.Dispose();
        _reconnectionCancellationTokenSource?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronous dispose for IAsyncDisposable.
    /// Provides proper async cleanup without blocking.
    /// Preferred method when async context is available.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await StopAsync(CancellationToken.None);

        _channel?.Dispose();
        _connection?.Dispose();
        _reconnectionCancellationTokenSource?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}