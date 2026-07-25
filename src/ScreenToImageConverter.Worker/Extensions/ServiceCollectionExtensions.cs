using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ScreenToImageConverter.Worker.AppSettings;
using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Infrastructure.Screenshots;
using ScreenToImageConverter.Worker.Infrastructure.Storage;

namespace ScreenToImageConverter.Worker.Extensions;

/// <summary>
/// Extension methods for registering services in the dependency injection container.
/// Consolidates vertical slice architecture registration.
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

        // Register and validate Notification (ServiceBus) settings
        services.Configure<NotificationSettings>(configuration.GetSection(NotificationSettings.SectionName));
        var notificationSettings = configuration.GetSection(NotificationSettings.SectionName).Get<NotificationSettings>();
        ValidateOptions(notificationSettings, nameof(NotificationSettings));

        // Add RabbitMQ configuration
        services.Configure<RabbitMqOptions>(
            services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>()
                .GetSection("RabbitMq"));

        // Validate RabbitMQ options if development environment
        var serviceProvider = services.BuildServiceProvider();
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

        if (environment.IsDevelopment())
        {
            var rabbitMqOptions = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>();
            ValidateOptions(rabbitMqOptions, nameof(RabbitMqOptions));

            // Use RabbitMQ for local development (free, no costs)
            services.AddScoped<IMessageConsumer, RabbitMqConsumer>();
            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        }
        else
        {
            // Use Azure Service Bus for production
            services.AddScoped<IMessageConsumer, ServiceBusConsumer>();
            services.AddScoped<IMessagePublisher, ServiceBusPublisher>();
        }

        // Register and validate Storage (BlobStorage) settings
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        var storageSettings = configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>();
        ValidateOptions(storageSettings, nameof(StorageSettings));

        // Register and validate PlaywrightOptions
        services.Configure<PlaywrightOptions>(configuration.GetSection(PlaywrightOptions.SectionName));
        var playwrightOptions = configuration.GetSection(PlaywrightOptions.SectionName).Get<PlaywrightOptions>();
        ValidateOptions(playwrightOptions, nameof(PlaywrightOptions));

        return services;
    }

    /// <summary>
    /// Registers the ConvertHtmlToImage feature with all its dependencies.
    /// </summary>
    public static IServiceCollection AddConvertHtmlToImageFeature(
        this IServiceCollection services)
    {
        // Register the feature handler
        services.AddScoped<ConvertHtmlToImageHandler>();

        // Register infrastructure services
        services.AddInfrastructureNotifications();
        services.AddInfrastructureStorage();

        // Register screenshot provider
        services.AddSingleton<IScreenshotProvider, PlaywrightScreenshotProvider>();

        return services;
    }

    /// <summary>
    /// Registers notification (Service Bus) infrastructure components.
    /// </summary>
    public static IServiceCollection AddInfrastructureNotifications(
        this IServiceCollection services)
    {
        services.AddScoped<IMessageConsumer, ServiceBusConsumer>();
        services.AddScoped<IMessagePublisher, ServiceBusPublisher>();

        return services;
    }

    /// <summary>
    /// Registers storage (Blob Storage) infrastructure components.
    /// </summary>
    public static IServiceCollection AddInfrastructureStorage(
        this IServiceCollection services)
    {
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        return services;
    }

    /// <summary>
    /// Validates configuration options and throws if invalid.
    /// </summary>
    private static void ValidateOptions(object? options, string optionName)
    {
        if (options == null)
        {
            throw new Exception(
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
                throw new Exception(
                    $"Configuration validation failed for '{optionName}': {errorMessage}");
            }
        }
    }

    /// <summary>
    /// Initializes the Playwright screenshot provider.
    /// Must be called after the host is built.
    /// </summary>
    public static async Task InitializePlaywrightAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var screenshotProvider = services.GetRequiredService<IScreenshotProvider>();
        if (screenshotProvider is PlaywrightScreenshotProvider playwrightProvider)
        {
            await playwrightProvider.InitializeAsync(cancellationToken);
        }
    }
}


