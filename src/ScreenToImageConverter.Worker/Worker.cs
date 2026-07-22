using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

namespace ScreenToImageConverter.Worker;

/// <summary>
/// Main background service for the HTML to Image conversion worker.
/// Orchestrates the ConvertHtmlToImage feature:
/// - ServiceBusConsumer: Listens for HtmlScreenshotRequest messages from Service Bus
/// - ConvertHtmlToImageHandler: Processes requests and manages the conversion workflow
/// 
/// Workflow:
/// 1. Starts Service Bus consumer to listen for HtmlScreenshotRequest messages
/// 2. For each incoming message:
///    a. Passes to ConvertHtmlToImageHandler
///    b. Handler: Validate → Capture → Upload → Publish completion event
///    c. Handles errors gracefully
/// 3. Keeps the service running until cancellation is requested
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IMessageConsumer _messageConsumer;
    private readonly ConvertHtmlToImageHandler _handler;

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IMessageConsumer messageConsumer,
        ConvertHtmlToImageHandler handler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
        _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Entry point for the background service.
    /// Initializes the message consumer and keeps the service running until cancellation.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎯 Worker service started. Initializing ConvertHtmlToImage feature...");

        try
        {
            // Register message handler with the consumer
            if (_messageConsumer is ServiceBusConsumer serviceBusConsumer)
            {
                serviceBusConsumer.RegisterMessageHandler(ProcessMessageAsync);
            }

            _logger.LogInformation("📢 Starting Service Bus message consumer...");
            await _messageConsumer.StartAsync(stoppingToken);

            _logger.LogInformation("✅ Worker service ready. Listening for HTML to image conversion requests...");

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
    /// Delegates to the ConvertHtmlToImageHandler.
    /// </summary>
    private async Task ProcessMessageAsync(
        HtmlScreenshotRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "📨 Processing message [RequestId: {RequestId}, CorrelationId: {CorrelationId}]",
                request.RequestId,
                correlationId);

            // Convert the incoming request to a command
            var command = new ConvertHtmlToImageCommand
            {
                Url = request.Url,
                ViewportWidth = request.ViewportWidth,
                ViewportHeight = request.ViewportHeight,
                TimeoutMs = request.TimeoutMs,
                RequestId = request.RequestId,
                SourceId = request.SourceId,
                CorrelationId = correlationId,
                WaitForPageLoad = request.WaitForPageLoad,
                ScreenshotName = request.ScreenshotName
            };

            // Process the command
            await _handler.HandleAsync(command, cancellationToken);
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
