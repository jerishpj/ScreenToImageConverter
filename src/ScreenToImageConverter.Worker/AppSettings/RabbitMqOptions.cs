namespace ScreenToImageConverter.Worker.AppSettings;

/// <summary>
/// Configuration options for RabbitMQ local development.
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

        return errors;
    }
}