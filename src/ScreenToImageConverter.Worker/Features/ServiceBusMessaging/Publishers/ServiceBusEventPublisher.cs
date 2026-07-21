using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Interfaces;

namespace ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Publishers;

/// <summary>
/// Implementation of IMessagePublisher using Azure Service Bus.
/// Handles publishing messages to Service Bus topics.
/// Part of the ServiceBusMessaging vertical slice.
/// </summary>
public class ServiceBusEventPublisher : IMessagePublisher
{
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusEventPublisher> _logger;
    private ServiceBusClient? _serviceBusClient;
    private ServiceBusSender? _sender;
    private bool _disposed;

    public bool IsConnected => _sender != null;

    public ServiceBusEventPublisher(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusEventPublisher> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeClient();
    }

    /// <summary>
    /// Initializes the Service Bus client and sender.
    /// </summary>
    private void InitializeClient()
    {
        try
        {
            _logger.LogInformation("Initializing Service Bus event publisher");

            if (_options.UseManagedIdentity)
            {
                var fullyQualifiedNamespace = _options.FullyQualifiedNamespace;
                _serviceBusClient = new ServiceBusClient(
                    fullyQualifiedNamespace,
                    new DefaultAzureCredential());
            }
            else
            {
                _serviceBusClient = new ServiceBusClient(_options.ConnectionString);
            }

            _sender = _serviceBusClient.CreateSender(_options.ScreenshotCompletedEventTopicName);

            _logger.LogInformation(
                "✅ Service Bus event publisher initialized for topic '{Topic}'",
                _options.ScreenshotCompletedEventTopicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize Service Bus event publisher");
            throw;
        }
    }

    /// <summary>
    /// Publishes a message to the Service Bus topic.
    /// </summary>
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        await PublishAsync(message, null, cancellationToken);
    }

    /// <summary>
    /// Publishes a message with a correlation ID for end-to-end tracking.
    /// </summary>
    public async Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken) where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (_sender == null)
            throw new InvalidOperationException("Publisher not initialized");

        try
        {
            _logger.LogInformation(
                "Publishing message of type '{MessageType}' to topic '{Topic}' [CorrelationId: {CorrelationId}]",
                typeof(T).Name,
                _options.ScreenshotCompletedEventTopicName,
                correlationId ?? "N/A");

            // Serialize the message
            var json = JsonSerializer.Serialize(message);
            var body = new BinaryData(json);

            // Create the Service Bus message
            var serviceBusMessage = new ServiceBusMessage(body)
            {
                ContentType = "application/json"
            };

            // Set correlation ID if provided
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                serviceBusMessage.CorrelationId = correlationId;
            }

            // Add message type to properties for consumer routing
            serviceBusMessage.ApplicationProperties.Add("MessageType", typeof(T).Name);
            serviceBusMessage.ApplicationProperties.Add("PublishedAt", DateTime.UtcNow);

            // Send the message
            await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation(
                "✅ Message published successfully to topic '{Topic}' [CorrelationId: {CorrelationId}]",
                _options.ScreenshotCompletedEventTopicName,
                correlationId ?? "N/A");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to publish message to topic '{Topic}' [CorrelationId: {CorrelationId}]",
                _options.ScreenshotCompletedEventTopicName,
                correlationId ?? "N/A");
            throw;
        }
    }

    /// <summary>
    /// Disposes the publisher resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_sender != null)
            {
                await _sender.DisposeAsync();
            }

            if (_serviceBusClient != null)
            {
                await _serviceBusClient.DisposeAsync();
            }

            _disposed = true;
            _logger.LogInformation("✅ ServiceBusEventPublisher disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error disposing ServiceBusEventPublisher");
        }
    }
}
