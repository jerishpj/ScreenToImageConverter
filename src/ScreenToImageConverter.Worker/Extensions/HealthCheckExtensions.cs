using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Worker.AppSettings;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Infrastructure.Screenshots;
using ScreenToImageConverter.Worker.Infrastructure.Storage;

namespace ScreenToImageConverter.Worker.Extensions;

/// <summary>
/// Extension methods for registering health checks.
/// Health checks monitor the readiness and liveness of infrastructure dependencies.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds custom health checks for screenshot provider, blob storage, messaging, and configuration validation.
    /// </summary>
    public static IHealthChecksBuilder AddApplicationHealthChecks(this IServiceCollection services)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add Playwright screenshot provider health check
        healthChecksBuilder
            .AddCheck<PlaywrightHealthCheck>("playwright", tags: new[] { "ready", "live" });

        // Add Blob Storage health check
        healthChecksBuilder
            .AddCheck<BlobStorageHealthCheck>("blob-storage", tags: new[] { "ready" });

        // Add message consumer (RabbitMQ/Service Bus) health check
        healthChecksBuilder
            .AddCheck<MessageConsumerHealthCheck>("message-consumer", tags: new[] { "ready", "live" });

        // Add configuration validation check
        healthChecksBuilder
            .AddCheck<ConfigurationHealthCheck>("configuration", tags: new[] { "ready" });

        return healthChecksBuilder;
    }
}

/// <summary>
/// Health check for Playwright screenshot provider initialization.
/// </summary>
internal class PlaywrightHealthCheck : IHealthCheck
{
    private readonly IScreenshotProvider _screenshotProvider;

    public PlaywrightHealthCheck(IScreenshotProvider screenshotProvider)
    {
        _screenshotProvider = screenshotProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_screenshotProvider.IsInitialized
                ? HealthCheckResult.Healthy("Playwright provider is initialized and ready.")
                : HealthCheckResult.Degraded("Playwright provider is not yet initialized."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Playwright provider check failed.", ex));
        }
    }
}

/// <summary>
/// Health check for Azure Blob Storage connectivity.
/// </summary>
internal class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IBlobStorageService _blobStorageService;

    public BlobStorageHealthCheck(IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _blobStorageService.IsConnectedAsync(cancellationToken);
            return isConnected
                ? HealthCheckResult.Healthy("Blob Storage is accessible.")
                : HealthCheckResult.Unhealthy("Blob Storage is not accessible.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob Storage check failed.", ex);
        }
    }
}

/// <summary>
/// Health check for configuration options validation.
/// </summary>
internal class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IOptionsSnapshot<ServiceBusOptions> _serviceBusOptions;
    private readonly IOptionsSnapshot<StorageSettings> _storageSettings;
    private readonly IOptionsSnapshot<PlaywrightOptions> _playwrightOptions;

    public ConfigurationHealthCheck(
        IOptionsSnapshot<ServiceBusOptions> serviceBusOptions,
        IOptionsSnapshot<StorageSettings> storageSettings,
        IOptionsSnapshot<PlaywrightOptions> playwrightOptions)
    {
        _serviceBusOptions = serviceBusOptions;
        _storageSettings = storageSettings;
        _playwrightOptions = playwrightOptions;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var errors = new List<string>();
            errors.AddRange(_serviceBusOptions.Value.Validate());
            errors.AddRange(_storageSettings.Value.Validate());
            errors.AddRange(_playwrightOptions.Value.Validate());

            return Task.FromResult(errors.Count == 0
                ? HealthCheckResult.Healthy("All configuration options are valid.")
                : HealthCheckResult.Unhealthy($"Configuration validation failed: {string.Join("; ", errors)}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Configuration check failed.", ex));
        }
    }
}

/// <summary>
/// Health check for message consumer (RabbitMQ or Service Bus) connectivity.
/// Reports the current connection status and detailed diagnostic information.
/// </summary>
internal class MessageConsumerHealthCheck : IHealthCheck
{
    private readonly IMessageConsumer _messageConsumer;
    private readonly ILogger<MessageConsumerHealthCheck> _logger;

    public MessageConsumerHealthCheck(
        IMessageConsumer messageConsumer,
        ILogger<MessageConsumerHealthCheck> logger)
    {
        _messageConsumer = messageConsumer;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var consumerType = _messageConsumer.GetType().Name;

            // Check RabbitMQ consumer
            if (_messageConsumer is RabbitMqConsumer rabbitConsumer)
            {
                var isConnected = rabbitConsumer.IsConnected;

                if (isConnected)
                {
                    return Task.FromResult(HealthCheckResult.Healthy(
                        $"RabbitMQ consumer is connected and ready to process messages."));
                }
                else
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "RabbitMQ consumer is not currently connected. " +
                        "The worker will attempt to reconnect with exponential backoff. " +
                        "Check logs for connection retry attempts. " +
                        "Ensure RabbitMQ is running and accessible."));
                }
            }

            // Check Service Bus consumer (always assumed connected if registered)
            if (_messageConsumer is ServiceBusConsumer)
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    "Service Bus consumer initialized and ready."));
            }

            // Unknown consumer type
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Message consumer ({consumerType}) initialized."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Message consumer health check encountered an error");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Message consumer health check failed.", ex));
        }
    }
}
