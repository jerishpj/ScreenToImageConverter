namespace ScreenToImageConverter.Worker.AppSettings;

/// <summary>
/// Configuration options for RabbitMQ local development.
/// 
/// Supports graceful connection failure handling with industry-standard patterns:
/// - Exponential backoff retry on connection failures
/// - Circuit breaker to prevent cascading failures
/// - Configurable timeout for connection operations
/// - Maximum retry attempts before logging and waiting for manual intervention
/// </summary>
public class RabbitMqOptions
{
    // Connection settings
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    // Exchange/Queue settings for consuming requests
    public string ExchangeName { get; set; } = "screenshot-requests";
    public string QueueName { get; set; } = "screenshot-requests-queue";
    public string RoutingKey { get; set; } = "screenshot.request";

    // Exchange/Topic for publishing completion events
    public string CompletionEventExchange { get; set; } = "screenshot-completed";
    public string CompletionEventRoutingKey { get; set; } = "screenshot.completed";

    // Resilience settings
    /// <summary>
    /// Maximum number of connection retry attempts before giving up.
    /// Default: 5 retries with exponential backoff (1s, 2s, 4s, 8s, 16s)
    /// </summary>
    public int MaxConnectionRetries { get; set; } = 5;

    /// <summary>
    /// Initial delay in seconds before first retry attempt.
    /// Subsequent retries use exponential backoff.
    /// Default: 1 second
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Maximum delay in seconds between retry attempts (caps exponential backoff).
    /// Default: 32 seconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 32;

    /// <summary>
    /// Timeout in seconds for individual connection operations.
    /// Prevents indefinite waits when network is unreachable but not actively rejecting.
    /// Default: 10 seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Number of consecutive failures allowed before circuit breaker opens.
    /// When circuit opens, connection attempts are paused for a duration.
    /// Default: 3 failures
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 3;

    /// <summary>
    /// Duration in seconds to keep circuit breaker open after threshold is reached.
    /// During this time, connection attempts are rejected immediately (fail fast).
    /// Default: 30 seconds
    /// </summary>
    public int CircuitBreakerOpenDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable graceful degradation mode.
    /// When true, worker starts even if RabbitMQ is unavailable and retries connection.
    /// When false, worker fails immediately if RabbitMQ is unavailable (crash fast).
    /// Default: true (graceful degradation)
    /// </summary>
    public bool EnableGracefulDegradation { get; set; } = true;

    /// <summary>
    /// Interval in seconds for background reconnection attempts when initial connection fails.
    /// The consumer will attempt to reconnect at this interval if graceful degradation is enabled.
    /// Default: 5 seconds
    /// </summary>
    public int ReconnectionIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum interval in seconds for background reconnection attempts (prevents excessive retries).
    /// Reconnection delay will be capped at this value.
    /// Default: 60 seconds
    /// </summary>
    public int MaxReconnectionIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Validates the RabbitMQ configuration.
    /// </summary>
    /// <returns>Collection of validation error messages. Empty if valid.</returns>
    public ICollection<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(HostName))
            errors.Add("HostName is required.");

        if (Port <= 0 || Port > 65535)
            errors.Add("Port must be a valid port number (1-65535).");

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required.");

        if (string.IsNullOrWhiteSpace(Password))
            errors.Add("Password is required.");

        if (string.IsNullOrWhiteSpace(ExchangeName))
            errors.Add("ExchangeName is required.");

        if (string.IsNullOrWhiteSpace(QueueName))
            errors.Add("QueueName is required.");

        if (string.IsNullOrWhiteSpace(RoutingKey))
            errors.Add("RoutingKey is required.");

        if (string.IsNullOrWhiteSpace(CompletionEventExchange))
            errors.Add("CompletionEventExchange is required.");

        if (string.IsNullOrWhiteSpace(CompletionEventRoutingKey))
            errors.Add("CompletionEventRoutingKey is required.");

        // Validate resilience settings
        if (MaxConnectionRetries <= 0)
            errors.Add("MaxConnectionRetries must be greater than 0.");

        if (InitialRetryDelaySeconds <= 0)
            errors.Add("InitialRetryDelaySeconds must be greater than 0.");

        if (MaxRetryDelaySeconds <= 0 || MaxRetryDelaySeconds < InitialRetryDelaySeconds)
            errors.Add("MaxRetryDelaySeconds must be greater than 0 and >= InitialRetryDelaySeconds.");

        if (ConnectionTimeoutSeconds <= 0)
            errors.Add("ConnectionTimeoutSeconds must be greater than 0.");

        if (CircuitBreakerFailureThreshold <= 0)
            errors.Add("CircuitBreakerFailureThreshold must be greater than 0.");

        if (CircuitBreakerOpenDurationSeconds <= 0)
            errors.Add("CircuitBreakerOpenDurationSeconds must be greater than 0.");

        if (ReconnectionIntervalSeconds <= 0)
            errors.Add("ReconnectionIntervalSeconds must be greater than 0.");

        if (MaxReconnectionIntervalSeconds <= 0 || MaxReconnectionIntervalSeconds < ReconnectionIntervalSeconds)
            errors.Add("MaxReconnectionIntervalSeconds must be greater than 0 and >= ReconnectionIntervalSeconds.");

        return errors;
    }
}