using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Consumers;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Handlers;

namespace ScreenToImageConverter.Worker;

/// <summary>
/// Main background service for the screenshot processing worker.
/// Orchestrates vertical slice features:
/// - ServiceBusMessaging: Listens for HtmlScreenshotRequest messages
/// - ScreenshotCapture: Captures screenshots using Playwright
/// - BlobStorageUpload: Uploads screenshots to Azure Blob Storage
/// - Event Publishing: Publishes ScreenshotCompletedEvent to downstream consumers
/// 
/// Workflow:
/// 1. Starts Service Bus consumer to listen for HtmlScreenshotRequest messages
/// 2. For each incoming message:
///    a. Passes to ScreenshotProcessingOrchestrator
///    b. Orchestrator: Validate → Capture → Upload → Publish completion event
///    c. Handles errors and publishes failure events
/// 3. Keeps the service running until cancellation is requested
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IMessageConsumer _messageConsumer;
    private readonly ScreenshotProcessingOrchestrator _orchestrator;

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IMessageConsumer messageConsumer,
        ScreenshotProcessingOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
        _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    /// <summary>
    /// Entry point for the background service.
    /// Initializes the message consumer and keeps the service running until cancellation.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎯 Worker service started. Initializing vertical slice features...");

        try
        {
            // Register message handler with the consumer
            if (_messageConsumer is ServiceBusMessageConsumer serviceBusConsumer)
            {
                serviceBusConsumer.RegisterMessageHandler(ProcessMessageAsync);
            }

            _logger.LogInformation("📢 Starting Service Bus message consumer...");
            await _messageConsumer.StartAsync(stoppingToken);

            _logger.LogInformation("✅ Worker service ready. Listening for screenshot requests...");

            // Keep the service running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("🛑 Shutting down worker service gracefully...");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏸️ Worker service cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Worker service encountered an unhandled error");
            _hostApplicationLifetime.StopApplication();
        }
    }

    /// <summary>
    /// Message handler callback for processing incoming Service Bus messages.
    /// Delegates to the ScreenshotProcessingOrchestrator.
    /// </summary>
    private async Task ProcessMessageAsync(
        ScreenToImageConverter.Shared.Messages.HtmlScreenshotRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _orchestrator.ProcessScreenshotAsync(request, correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Message processing failed [RequestId: {RequestId}, CorrelationId: {CorrelationId}]",
                request.RequestId,
                correlationId);
            throw;
        }
    }

    /// <summary>
    /// Called when the service is starting.
    /// Initializes infrastructure.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏱️ Worker service starting...");
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the service is stopping.
    /// Gracefully shuts down the message consumer.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏹️ Worker service stopping...");

        try
        {
            if (_messageConsumer != null)
            {
                await _messageConsumer.StopAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message consumer");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✅ Worker service stopped");
    }

    /// <summary>
    /// Called when the service is disposed.
    /// Cleans up resources.
    /// </summary>
    public override async void Dispose()
    {
        _logger.LogInformation("🧹 Worker service disposing");

        try
        {
            if (_messageConsumer != null)
            {
                await _messageConsumer.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing message consumer");
        }

        base.Dispose();
    }
}
