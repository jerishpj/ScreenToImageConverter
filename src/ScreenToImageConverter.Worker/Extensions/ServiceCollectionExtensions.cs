using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Exceptions;

namespace ScreenToImageConverter.Worker.Extensions;

/// <summary>
/// Extension methods for registering services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers and validates configuration options from appsettings.
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register and validate ServiceBusOptions
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        var serviceBusOptions = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>();
        ValidateOptions(serviceBusOptions, nameof(ServiceBusOptions));

        // Register and validate BlobStorageOptions
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
        var blobStorageOptions = configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>();
        ValidateOptions(blobStorageOptions, nameof(BlobStorageOptions));

        // Register and validate PlaywrightOptions
        services.Configure<PlaywrightOptions>(configuration.GetSection(PlaywrightOptions.SectionName));
        var playwrightOptions = configuration.GetSection(PlaywrightOptions.SectionName).Get<PlaywrightOptions>();
        ValidateOptions(playwrightOptions, nameof(PlaywrightOptions));

        return services;
    }

    /// <summary>
    /// Registers resilience policies (retry, circuit breaker, timeouts).
    /// These policies protect the application from cascading failures.
    /// </summary>
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        // TODO: Register Polly policies for:
        // - Service Bus: exponential backoff retry (3 attempts, 1-5 second delays)
        // - Blob Storage: exponential backoff retry (3 attempts, 1-5 second delays)
        // - Playwright: timeout policy (30 second per request) with circuit breaker
        // - Overall timeout for entire screenshot pipeline (60 seconds)
        // These will be integrated into the Consumer and Provider implementations

        return services;
    }

    /// <summary>
    /// Validates configuration options and throws if invalid.
    /// </summary>
    private static void ValidateOptions(object? options, string optionName)
    {
        if (options == null)
        {
            throw new ConfigurationException(
                $"Configuration section '{optionName}' is missing from appsettings. " +
                $"Please add the required configuration.");
        }

        // Call Validate method if it exists
        var validateMethod = options.GetType().GetMethod("Validate");
        if (validateMethod != null)
        {
            var errors = validateMethod.Invoke(options, null) as ICollection<string>;
            if (errors?.Count > 0)
            {
                var errorMessage = string.Join("; ", errors);
                throw new ConfigurationException(
                    $"Configuration validation failed for '{optionName}': {errorMessage}");
            }
        }
    }
}


