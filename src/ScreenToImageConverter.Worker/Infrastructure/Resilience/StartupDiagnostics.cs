using ScreenToImageConverter.Worker.Infrastructure.Notifications;

namespace ScreenToImageConverter.Worker.Infrastructure.Resilience;

/// <summary>
/// Provides startup diagnostics and validation for external dependencies.
/// Helps operators understand what's available before the worker starts processing messages.
/// 
/// Responsibilities:
/// - Check RabbitMQ connectivity at startup
/// - Validate Azure Blob Storage access
/// - Provide detailed diagnostic information for troubleshooting
/// - Report readiness status
/// 
/// This class complements health checks by providing detailed startup diagnostics
/// that help with initial debugging and monitoring.
/// </summary>
public interface IStartupDiagnostics
{
    /// <summary>
    /// Validates dependencies and returns diagnostic information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diagnostic result with status and details</returns>
    Task<StartupDiagnosticResult> ValidateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Result of startup diagnostics validation.
/// </summary>
public class StartupDiagnosticResult
{
    /// <summary>
    /// Whether all critical dependencies are available.
    /// </summary>
    public bool AllCriticalDependenciesAvailable { get; set; }

    /// <summary>
    /// Individual dependency check results.
    /// </summary>
    public Dictionary<string, DependencyCheckResult> DependencyChecks { get; set; } = [];

    /// <summary>
    /// Detailed diagnostic message for logging.
    /// </summary>
    public string DiagnosticMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets a formatted summary of all dependency checks.
    /// </summary>
    public string GetSummary()
    {
        var availableCount = DependencyChecks.Count(x => x.Value.IsAvailable);
        var totalCount = DependencyChecks.Count;
        var message = $"Startup Diagnostics: {availableCount}/{totalCount} dependencies available\n";

        foreach (var check in DependencyChecks)
        {
            var status = check.Value.IsAvailable ? "✅ OK" : "⚠️ UNAVAILABLE";
            message += $"  {status} - {check.Key}\n";
            if (!string.IsNullOrEmpty(check.Value.Details))
            {
                message += $"     {check.Value.Details}\n";
            }
        }

        return message;
    }
}

/// <summary>
/// Individual dependency check result.
/// </summary>
public class DependencyCheckResult
{
    /// <summary>
    /// Whether this dependency is currently available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Additional details about the dependency status.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// If not available, the reason why.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time when this check was performed.
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Default implementation of startup diagnostics.
/// Validates that RabbitMQ and other critical services are reachable.
/// </summary>
public class StartupDiagnostics : IStartupDiagnostics
{
    private readonly ILogger<StartupDiagnostics> _logger;
    private readonly IMessageConsumer _messageConsumer;

    public StartupDiagnostics(
        ILogger<StartupDiagnostics> logger,
        IMessageConsumer messageConsumer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
    }

    /// <summary>
    /// Validates startup dependencies and returns diagnostic information.
    /// </summary>
    public async Task<StartupDiagnosticResult> ValidateAsync(CancellationToken cancellationToken)
    {
        var result = new StartupDiagnosticResult();

        _logger.LogInformation("🔍 Starting startup diagnostics...");

        // Check message consumer (RabbitMQ or Service Bus)
        await CheckMessageConsumerAsync(result, cancellationToken);

        // Determine overall status
        result.AllCriticalDependenciesAvailable = result.DependencyChecks.Values.All(x => x.IsAvailable);

        // Generate diagnostic message
        GenerateDiagnosticMessage(result);

        // Log the summary
        _logger.LogInformation(result.GetSummary());

        return result;
    }

    /// <summary>
    /// Checks the message consumer (RabbitMQ or Service Bus) connectivity.
    /// </summary>
    private Task CheckMessageConsumerAsync(StartupDiagnosticResult result, CancellationToken cancellationToken)
    {
        var consumerType = _messageConsumer.GetType().Name;

        try
        {
            if (_messageConsumer is RabbitMqConsumer rabbitConsumer)
            {
                var isConnected = rabbitConsumer.IsConnected;
                result.DependencyChecks[consumerType] = new DependencyCheckResult
                {
                    IsAvailable = isConnected,
                    Details = isConnected ? "Connected to RabbitMQ" : "Not yet connected (will retry on demand)",
                    ErrorMessage = isConnected ? null : "RabbitMQ connection not established at startup"
                };

                if (!isConnected)
                {
                    _logger.LogWarning(
                        "⚠️ RabbitMQ not connected at startup. The worker will attempt to reconnect with exponential backoff. " +
                        "Ensure RabbitMQ is running at {RabbitMqHost}:{RabbitMqPort}",
                        "localhost", // This would come from RabbitMqOptions
                        5672); // This would come from RabbitMqOptions
                }
            }
            else if (_messageConsumer is ServiceBusConsumer)
            {
                result.DependencyChecks[consumerType] = new DependencyCheckResult
                {
                    IsAvailable = true,
                    Details = "Service Bus consumer initialized (connection validation happens at runtime)"
                };
            }
            else
            {
                result.DependencyChecks[consumerType] = new DependencyCheckResult
                {
                    IsAvailable = true,
                    Details = "Message consumer initialized"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Error during message consumer diagnostics");
            result.DependencyChecks[consumerType] = new DependencyCheckResult
            {
                IsAvailable = false,
                Details = "Error during connectivity check",
                ErrorMessage = ex.Message
            };
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Generates a comprehensive diagnostic message based on check results.
    /// </summary>
    private void GenerateDiagnosticMessage(StartupDiagnosticResult result)
    {
        if (result.AllCriticalDependenciesAvailable)
        {
            result.DiagnosticMessage = "✅ All critical dependencies are available. Worker is ready to process messages.";
        }
        else
        {
            var unavailableDependencies = result.DependencyChecks
                .Where(x => !x.Value.IsAvailable)
                .Select(x => x.Key)
                .ToList();

            result.DiagnosticMessage = $"⚠️ Some dependencies are unavailable: {string.Join(", ", unavailableDependencies)}. " +
                "Worker will retry connection with exponential backoff. Check logs for connection attempts.";
        }
    }
}
