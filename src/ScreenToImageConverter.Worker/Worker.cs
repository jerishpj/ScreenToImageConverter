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
/// 
/// IMPORTANT: This class uses IServiceProvider to create scopes for scoped services.
/// BackgroundService (IHostedService) is a singleton and cannot directly inject scoped dependencies.
/// Solution: Use _serviceProvider.CreateScope() to get scoped instances when needed.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IServiceProvider _serviceProvider;

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Entry point for the background service.
    /// Initializes the message consumer and keeps the service running until cancellation.
    /// Creates scoped services within a using statement to ensure proper disposal.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎯 Worker service started. Initializing ConvertHtmlToImage feature...");

        // Create a scope for scoped services (IMessageConsumer, ConvertHtmlToImageHandler)
        // This scope lives for the duration of the ExecuteAsync method
        using var scope = _serviceProvider.CreateScope();
        var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();

        try
        {
            // Register message handler with the consumer
            if (messageConsumer is ServiceBusConsumer serviceBusConsumer)
            {
                serviceBusConsumer.RegisterMessageHandler(ProcessMessageAsync);
            }

            _logger.LogInformation("📢 Starting Service Bus message consumer...");
            await messageConsumer.StartAsync(stoppingToken);

            _logger.LogInformation("✅ Worker service ready. Listening for HTML to image conversion requests...");

            // Keep the service running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("🛑 Shutting down worker service gracefully...");
            await messageConsumer.StopAsync(stoppingToken);
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
    /// Creates a new scope for scoped services (ConvertHtmlToImageHandler, IBlobStorageService, IMessagePublisher).
    /// </summary>
    private async Task ProcessMessageAsync(
        HtmlScreenshotRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        // Create a new scope for this message processing
        // Each message gets its own scope with fresh instances of scoped services
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ConvertHtmlToImageHandler>();

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

            // Process the command using the scoped handler
            await handler.HandleAsync(command, cancellationToken);
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
}
