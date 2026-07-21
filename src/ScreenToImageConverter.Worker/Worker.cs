namespace ScreenToImageConverter.Worker;

/// <summary>
/// Main background service for the screenshot processing worker.
/// Coordinates message consumption, screenshot capture, blob storage upload, and event publishing.
/// 
/// Workflow:
/// 1. Listens for HtmlScreenshotRequest messages on Service Bus topic subscription
/// 2. Validates the incoming request
/// 3. Uses PlaywrightScreenshotProvider to capture the page screenshot
/// 4. Uploads the PNG to Azure Blob Storage via BlobStorageProvider
/// 5. Publishes ScreenshotCompletedEvent to Service Bus topic for downstream consumers
/// 6. Handles errors and retries with resilience policies
/// 
/// TODO: Implement these steps in ExecuteAsync once Service Bus consumer is ready
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    /// <summary>
    /// Entry point for the background service.
    /// Keeps the service running until cancellation is requested.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎯 Worker service started. Waiting for screenshot requests from Service Bus...");

        try
        {
            // TODO: Step 6 Implementation:
            // 1. Inject IMessageConsumer for Service Bus topic subscription
            // 2. Call messageConsumer.StartAsync(stoppingToken)
            // 3. Implement message handler callback with:
            //    - Request validation
            //    - Screenshot capture orchestration
            //    - Blob upload and SAS URL generation
            //    - Completion event publishing
            //    - Error handling and retries
            // 4. Keep the service running by awaiting the consumer

            // Placeholder: Keep the service running until cancellation is requested
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
    /// Called when the service is starting.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏱️ Worker service starting...");
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the service is stopping.
    /// Allows for graceful shutdown of resources.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏹️ Worker service stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✅ Worker service stopped");
    }

    /// <summary>
    /// Called when the service is disposed.
    /// </summary>
    public override void Dispose()
    {
        _logger.LogInformation("🧹 Worker service disposing");
        base.Dispose();
    }
}
