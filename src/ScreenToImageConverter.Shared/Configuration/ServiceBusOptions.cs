namespace ScreenToImageConverter.Shared.Configuration;

/// <summary>
/// Configuration options for Azure Service Bus.
/// </summary>
public class ServiceBusOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// Connection string to the Service Bus namespace.
    /// Use Managed Identity in production instead of connection strings.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Fully qualified namespace of the Service Bus (e.g., "myservice.servicebus.windows.net").
    /// Required when using Managed Identity or token-based authentication.
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Name of the topic for receiving screenshot requests.
    /// </summary>
    public string HtmlScreenshotRequestTopicName { get; set; } = "html-screenshot-requests";

    /// <summary>
    /// Name of the subscription for consuming screenshot requests.
    /// </summary>
    public string HtmlScreenshotRequestSubscriptionName { get; set; } = "screenshot-worker-subscription";

    /// <summary>
    /// Name of the topic for publishing screenshot completion events.
    /// </summary>
    public string ScreenshotCompletedEventTopicName { get; set; } = "screenshot-completed-events";

    /// <summary>
    /// Maximum number of messages to process concurrently.
    /// Default: 1 for sequential processing.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 1;

    /// <summary>
    /// Number of messages to prefetch from the queue.
    /// Default: 0 (no prefetch, messages fetched on-demand).
    /// </summary>
    public int PrefetchCount { get; set; } = 0;

    /// <summary>
    /// Whether to use Managed Identity for authentication.
    /// If true, FullyQualifiedNamespace must be set; ConnectionString is ignored.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public ICollection<string> Validate()
    {
        var errors = new List<string>();

        if (UseManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(FullyQualifiedNamespace))
                errors.Add($"{nameof(FullyQualifiedNamespace)} is required when UseManagedIdentity is true.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                errors.Add($"{nameof(ConnectionString)} is required when UseManagedIdentity is false.");
        }

        if (string.IsNullOrWhiteSpace(HtmlScreenshotRequestTopicName))
            errors.Add($"{nameof(HtmlScreenshotRequestTopicName)} is required.");

        if (string.IsNullOrWhiteSpace(HtmlScreenshotRequestSubscriptionName))
            errors.Add($"{nameof(HtmlScreenshotRequestSubscriptionName)} is required.");

        if (string.IsNullOrWhiteSpace(ScreenshotCompletedEventTopicName))
            errors.Add($"{nameof(ScreenshotCompletedEventTopicName)} is required.");

        if (MaxConcurrentCalls <= 0)
            errors.Add($"{nameof(MaxConcurrentCalls)} must be greater than 0.");

        if (PrefetchCount < 0)
            errors.Add($"{nameof(PrefetchCount)} cannot be negative.");

        return errors;
    }
}
