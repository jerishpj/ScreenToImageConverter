using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Worker.Infrastructure.Screenshots;

namespace ScreenToImageConverter.Worker.Infrastructure.Notifications;

/// <summary>
/// Implementation of IMessageConsumer using Azure Service Bus.
/// Handles receiving and deserializing HtmlScreenshotRequest messages from Service Bus topics.
/// Part of the Notifications infrastructure.
/// </summary>
public class ServiceBusConsumer : IMessageConsumer
{
    private readonly NotificationSettings _settings;
    private readonly ILogger<ServiceBusConsumer> _logger;
    private ServiceBusClient? _serviceBusClient;
    private ServiceBusProcessor? _processor;
    private bool _disposed;

    /// <summary>
    /// Callback delegate for handling received messages.
    /// </summary>
    public delegate Task MessageHandlerDelegate(HtmlScreenshotRequest message, string correlationId, CancellationToken cancellationToken);

    private MessageHandlerDelegate? _messageHandler;

    public bool IsConnected => _processor != null && !_processor.IsProcessing;

    public ServiceBusConsumer(
        IOptions<NotificationSettings> settings,
        ILogger<ServiceBusConsumer> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers the message handler callback.
    /// This must be called before StartAsync.
    /// </summary>
    public void RegisterMessageHandler(MessageHandlerDelegate handler)
    {
        _messageHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger.LogInformation("Message handler registered");
    }

    /// <summary>
    /// Starts listening for messages from the Service Bus subscription.
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
            _logger.LogInformation(
                "Starting Service Bus message consumer for topic '{Topic}' subscription '{Subscription}'",
                _settings.HtmlScreenshotRequestTopicName,
                _settings.HtmlScreenshotRequestSubscriptionName);

            // Create Service Bus client
            if (_settings.UseManagedIdentity)
            {
                var fullyQualifiedNamespace = _settings.FullyQualifiedNamespace;
                _serviceBusClient = new ServiceBusClient(
                    fullyQualifiedNamespace,
                    new DefaultAzureCredential());
            }
            else
            {
                _serviceBusClient = new ServiceBusClient(_settings.ConnectionString);
            }

            // Create processor
            var processorOptions = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _settings.MaxConcurrentCalls,
                PrefetchCount = _settings.PrefetchCount,
                AutoCompleteMessages = false
            };

            _processor = _serviceBusClient.CreateProcessor(
                _settings.HtmlScreenshotRequestTopicName,
                _settings.HtmlScreenshotRequestSubscriptionName,
                processorOptions);

            // Register message and error handlers
            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            // Start processing
            await _processor.StartProcessingAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Service Bus message consumer started (MaxConcurrent: {MaxConcurrent}, Prefetch: {Prefetch})",
                _settings.MaxConcurrentCalls,
                _settings.PrefetchCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start Service Bus message consumer");
            throw;
        }
    }

    /// <summary>
    /// Stops listening for messages.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_processor != null)
            {
                _logger.LogInformation("Stopping Service Bus message consumer");
                await _processor.StopProcessingAsync(cancellationToken);
                _logger.LogInformation("✅ Service Bus message consumer stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping Service Bus message consumer");
        }
    }

    /// <summary>
    /// Processes a received message.
    /// </summary>
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            var correlationId = args.Message.CorrelationId ?? Guid.NewGuid().ToString();

            _logger.LogInformation(
                "📨 Received message from Service Bus [CorrelationId: {CorrelationId}]",
                correlationId);

            // Deserialize the message
            var request = JsonSerializer.Deserialize<HtmlScreenshotRequest>(messageBody);
            if (request == null)
            {
                _logger.LogWarning("Failed to deserialize message [CorrelationId: {CorrelationId}]", correlationId);
                await args.AbandonMessageAsync(args.Message);
                return;
            }

            // Validate the request
            var validationErrors = request.Validate();
            if (validationErrors.Any())
            {
                _logger.LogWarning(
                    "Invalid request - Errors: {Errors} [CorrelationId: {CorrelationId}]",
                    string.Join(", ", validationErrors),
                    correlationId);
                await args.DeadLetterMessageAsync(args.Message, "ValidationFailed",
                    $"Validation errors: {string.Join("; ", validationErrors)}");
                return;
            }

            // Handle the message
            await _messageHandler!(request, correlationId, args.CancellationToken);

            // Complete the message
            await args.CompleteMessageAsync(args.Message);
            _logger.LogInformation(
                "✅ Message processed successfully [CorrelationId: {CorrelationId}]",
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing message");
            // Abandon the message so it can be retried
            await args.AbandonMessageAsync(args.Message);
        }
    }

    /// <summary>
    /// Handles processing errors.
    /// </summary>
    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "❌ Error in Service Bus processor - EntityPath: {EntityPath}, FullyQualifiedNamespace: {Namespace}",
            args.EntityPath,
            args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the consumer resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync();
                await _processor.DisposeAsync();
            }

            if (_serviceBusClient != null)
            {
                await _serviceBusClient.DisposeAsync();
            }

            _disposed = true;
            _logger.LogInformation("✅ ServiceBusConsumer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error disposing ServiceBusConsumer");
        }
    }
}
