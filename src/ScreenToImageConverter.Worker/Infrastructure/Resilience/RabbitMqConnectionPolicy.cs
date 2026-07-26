using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace ScreenToImageConverter.Worker.Infrastructure.Resilience;

/// <summary>
/// Defines resilience policies for RabbitMQ connection operations.
/// Implements industry-standard patterns for handling transient failures with exponential backoff
/// and circuit breaker pattern to prevent cascading failures.
/// 
/// Patterns used:
/// - Exponential Backoff: Gradually increases wait time between retries
/// - Circuit Breaker: Stops attempting connections after repeated failures to prevent resource exhaustion
/// - Timeout: Prevents indefinite waits on connection operations
/// 
/// Reference: https://github.com/App-vNext/Polly
/// </summary>
public static class RabbitMqConnectionPolicy
{
    /// <summary>
    /// Creates a retry policy with exponential backoff for RabbitMQ connection attempts.
    /// 
    /// Behavior:
    /// - Retries on BrokerUnreachableException (connection refused)
    /// - Retries on IOException (network errors)
    /// - Exponential backoff: 1s, 2s, 4s, 8s, etc.
    /// - Maximum 5 retry attempts
    /// - Logs each retry attempt
    /// </summary>
    /// <param name="logger">Logger for retry diagnostics</param>
    /// <returns>Async retry policy for RabbitMQ operations</returns>
    public static IAsyncPolicy<T> CreateExponentialBackoffPolicy<T>(ILogger logger)
    {
        // Create exponential backoff delays: 1s, 2s, 4s, 8s, 16s
        var retryDelays = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromSeconds(16)
        };

        return Policy
            .Handle<BrokerUnreachableException>()
            .Or<IOException>()
            .Or<OperationCanceledException>()
            .OrResult<T>(r => r == null)
            .WaitAndRetryAsync(
                retryCount: retryDelays.Length,
                sleepDurationProvider: (retryCount, context) =>
                {
                    return retryCount < retryDelays.Length
                        ? retryDelays[retryCount]
                        : retryDelays[^1]; // Return last delay if exceeds array length
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    if (outcome.Exception != null)
                    {
                        logger.LogWarning(
                            "⚠️ RabbitMQ connection attempt {RetryCount}/5 failed. Retrying in {DelayMs}ms. Error: {ErrorMessage}",
                            retryCount,
                            (int)timespan.TotalMilliseconds,
                            outcome.Exception.Message);
                    }
                    else
                    {
                        logger.LogWarning(
                            "⚠️ RabbitMQ connection attempt {RetryCount}/5 returned null. Retrying in {DelayMs}ms.",
                            retryCount,
                            (int)timespan.TotalMilliseconds);
                    }
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures.
    /// 
    /// Behavior:
    /// - Opens circuit after 3 consecutive failures
    /// - Stays open (rejecting requests) for 30 seconds
    /// - Allows one test request after timeout to check if service recovered
    /// - Logs circuit state changes
    /// 
    /// This prevents hammering RabbitMQ with connection attempts when it's clearly unavailable.
    /// </summary>
    /// <param name="logger">Logger for circuit breaker diagnostics</param>
    /// <returns>Async circuit breaker policy for RabbitMQ operations</returns>
    public static IAsyncPolicy<T> CreateCircuitBreakerPolicy<T>(ILogger logger)
    {
        return Policy
            .Handle<BrokerUnreachableException>()
            .Or<IOException>()
            .Or<OperationCanceledException>()
            .OrResult<T>(r => r == null)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan) =>
                {
                    logger.LogError(
                        "🔴 RabbitMQ circuit breaker opened. Service is unavailable. " +
                        "Will retry connection in {DurationSeconds}s. Last error: {ErrorMessage}",
                        (int)timespan.TotalSeconds,
                        outcome.Exception?.Message ?? "Unknown error");
                },
                onReset: () =>
                {
                    logger.LogInformation(
                        "🟢 RabbitMQ circuit breaker reset. Attempting to reconnect...");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation(
                        "🟡 RabbitMQ circuit breaker half-open. Testing connection...");
                });
    }

    /// <summary>
    /// Creates a combined policy that wraps retry and circuit breaker.
    /// 
    /// Strategy:
    /// 1. First attempt with circuit breaker (quick fail if service is down)
    /// 2. If circuit open, fail fast
    /// 3. If circuit closed, attempt with exponential backoff retry
    /// 
    /// This provides both short-term resilience (retries) and long-term protection (circuit breaker).
    /// </summary>
    /// <param name="logger">Logger for policy diagnostics</param>
    /// <returns>Wrapped policy combining retry and circuit breaker</returns>
    public static IAsyncPolicy<T> CreateCombinedPolicy<T>(ILogger logger)
    {
        var retryPolicy = CreateExponentialBackoffPolicy<T>(logger);
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy<T>(logger);

        // Wrap retry inside circuit breaker: circuit breaker checks first
        return Policy.WrapAsync(circuitBreakerPolicy, retryPolicy);
    }

    /// <summary>
    /// Creates a timeout policy to prevent indefinite waits on connection operations.
    /// 
    /// Behavior:
    /// - Throws TimeoutRejectedException if operation takes longer than timeout
    /// - Default timeout: 10 seconds
    /// 
    /// This is useful for connection operations that might hang indefinitely
    /// when the network is unreachable but not actively rejecting connections.
    /// </summary>
    /// <param name="timeoutSeconds">Timeout duration in seconds (default: 10)</param>
    /// <param name="logger">Logger for timeout events</param>
    /// <returns>Async timeout policy</returns>
    public static IAsyncPolicy<T> CreateTimeoutPolicy<T>(int timeoutSeconds = 10, ILogger? logger = null)
    {
        return Policy.TimeoutAsync<T>(
            TimeSpan.FromSeconds(timeoutSeconds),
            onTimeoutAsync: (context, timespan, task, ex) =>
            {
                logger?.LogWarning(
                    "⏱️ RabbitMQ connection operation timed out after {TimeoutSeconds}s",
                    (int)timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }
}
