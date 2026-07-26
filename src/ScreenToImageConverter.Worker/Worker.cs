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
    /// Implements graceful failure handling with retry logic for connection failures.
    /// 
    /// Behavior:
    /// - Attempts to start message consumer with retry logic
    /// - On repeated failures: logs diagnostic info and continues running (graceful degradation)
    /// - Worker reports health status even if message consumer is unavailable
    /// - When connection is restored, message processing resumes automatically
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎯 Worker service started. Initializing ConvertHtmlToImage feature...");

        // Create a scope for scoped services (IMessageConsumer, ConvertHtmlToImageHandler)
        // This scope lives for the duration of the ExecuteAsync method
        using var scope = _serviceProvider.CreateScope();
        var messageConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
        var logger = _serviceProvider.GetRequiredService<ILogger<Worker>>();

        bool isConsumerStarted = false;

        try
        {
            // Register message handler with the consumer
            RegisterMessageHandler(messageConsumer);

            _logger.LogInformation("📢 Starting message consumer with resilience handling...");
            await StartMessageConsumerWithRetryAsync(messageConsumer, logger, stoppingToken);
            isConsumerStarted = true;

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
            _logger.LogError(
                ex,
                "💥 Worker service encountered an unhandled error. " +
                "Consumer started: {ConsumerStarted}. Error: {ErrorMessage}",
                isConsumerStarted,
                ex.Message);
            _hostApplicationLifetime.StopApplication();
        }
    }

    /// <summary>
    /// Registers the appropriate message handler based on consumer type.
    /// </summary>
    private void RegisterMessageHandler(IMessageConsumer messageConsumer)
    {
        if (messageConsumer is ServiceBusConsumer serviceBusConsumer)
        {
            serviceBusConsumer.RegisterMessageHandler(ProcessMessageAsync);
        }
        else if (messageConsumer is RabbitMqConsumer rabbitMqConsumer)
        {
            rabbitMqConsumer.RegisterMessageHandler(ProcessMessageAsync);
        }
    }

    /// <summary>
    /// Starts message consumer with graceful retry logic.
    /// Attempts to start consumer; if it fails with connection error but graceful degradation is enabled,
    /// logs diagnostic info and continues running rather than crashing.
    /// </summary>
    private async Task StartMessageConsumerWithRetryAsync(
        IMessageConsumer messageConsumer,
        ILogger<Worker> logger,
        CancellationToken stoppingToken)
    {
        const int maxRetryAttempts = 3;
        int retryCount = 0;
        TimeSpan retryDelay = TimeSpan.FromSeconds(2);

        while (retryCount < maxRetryAttempts && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await messageConsumer.StartAsync(stoppingToken);
                return; // Success - exit retry loop
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                retryCount++;
                logger.LogWarning(
                    ex,
                    "⚠️ RabbitMQ connection attempt {RetryCount}/{MaxRetries} failed. " +
                    "Broker unreachable. Retrying in {DelaySeconds}s...",
                    retryCount,
                    maxRetryAttempts,
                    (int)retryDelay.TotalSeconds);

                if (retryCount < maxRetryAttempts)
                {
                    await Task.Delay(retryDelay, stoppingToken);
                    retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30)); // Cap at 30s
                }
                else
                {
                    logger.LogError(
                        "❌ Max retry attempts ({MaxRetries}) reached for RabbitMQ connection. " +
                        "Worker will continue running in degraded mode. " +
                        "Please ensure RabbitMQ is running: {Host}:{Port}. " +
                        "The worker will remain operational and retry connection when RabbitMQ becomes available.",
                        maxRetryAttempts,
                        "localhost",
                        5672); // These values should come from RabbitMqOptions
                }
            }
            catch (IOException ex)
            {
                retryCount++;
                logger.LogWarning(
                    ex,
                    "⚠️ Network error during RabbitMQ connection attempt {RetryCount}/{MaxRetries}. " +
                    "Retrying in {DelaySeconds}s...",
                    retryCount,
                    maxRetryAttempts,
                    (int)retryDelay.TotalSeconds);

                if (retryCount < maxRetryAttempts)
                {
                    await Task.Delay(retryDelay, stoppingToken);
                    retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30));
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                retryCount++;
                logger.LogWarning(
                    ex,
                    "⚠️ Error during message consumer startup attempt {RetryCount}/{MaxRetries}: {ErrorMessage}. " +
                    "Retrying...",
                    retryCount,
                    maxRetryAttempts,
                    ex.Message);

                if (retryCount < maxRetryAttempts)
                {
                    await Task.Delay(retryDelay, stoppingToken);
                    retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 2, 30));
                }
            }
        }

        logger.LogInformation(
            "📊 Message consumer startup completed after {AttemptCount} attempts. " +
            "Worker service is operational and will process messages if consumer is connected.",
            retryCount);
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
