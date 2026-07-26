using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ScreenToImageConverter.Worker.AppSettings;
using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;
using ScreenToImageConverter.Worker.Infrastructure.Diagnostics;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Infrastructure.Resilience;
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
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        RegisterAndValidateNotificationSettings(services, configuration);
        RegisterAndValidateMessagingProvider(services, configuration);
        RegisterAndValidateStorageSettings(services, configuration);
        RegisterAndValidatePlaywrightOptions(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers and validates notification settings.
    /// </summary>
    private static void RegisterAndValidateNotificationSettings(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NotificationSettings.SectionName);
        services.Configure<NotificationSettings>(section);

        var notificationSettings = section.Get<NotificationSettings>();
        ValidateOptions(notificationSettings, nameof(NotificationSettings));
    }

    /// <summary>
    /// Registers appropriate messaging provider based on environment (RabbitMQ for dev, Service Bus for prod).
    /// </summary>
    private static void RegisterAndValidateMessagingProvider(IServiceCollection services, IConfiguration configuration)
    {
        // Register RabbitMQ options first
        var rabbitMqSection = configuration.GetSection("RabbitMq");
        services.Configure<RabbitMqOptions>(rabbitMqSection);

        // Build temporary provider to check environment
        var tempProvider = services.BuildServiceProvider();
        var environment = tempProvider.GetRequiredService<IHostEnvironment>();

        if (environment.IsDevelopment())
        {
            var rabbitMqOptions = rabbitMqSection.Get<RabbitMqOptions>();
            ValidateOptions(rabbitMqOptions, nameof(RabbitMqOptions));

            services.AddScoped<IMessageConsumer, RabbitMqConsumer>();
            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        }
        else
        {
            services.AddScoped<IMessageConsumer, ServiceBusConsumer>();
            services.AddScoped<IMessagePublisher, ServiceBusPublisher>();
        }
    }

    /// <summary>
    /// Registers and validates storage settings.
    /// </summary>
    private static void RegisterAndValidateStorageSettings(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageSettings.SectionName);
        services.Configure<StorageSettings>(section);

        var storageSettings = section.Get<StorageSettings>();
        ValidateOptions(storageSettings, nameof(StorageSettings));
    }

    /// <summary>
    /// Registers and validates Playwright options.
    /// </summary>
    private static void RegisterAndValidatePlaywrightOptions(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(PlaywrightOptions.SectionName);
        services.Configure<PlaywrightOptions>(section);

        var playwrightOptions = section.Get<PlaywrightOptions>();
        ValidateOptions(playwrightOptions, nameof(PlaywrightOptions));
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
        // Note: IMessageConsumer and IMessagePublisher are already registered in AddApplicationConfiguration
        // based on the environment, so we don't need to call AddInfrastructureNotifications here
        services.AddInfrastructureStorage();

        // Register screenshot provider
        services.AddSingleton<IScreenshotProvider, PlaywrightScreenshotProvider>();

        return services;
    }

    /// <summary>
    /// Registers resilience and diagnostics services for the worker.
    /// Includes startup diagnostics and connection policies.
    /// </summary>
    public static IServiceCollection AddApplicationResilience(
        this IServiceCollection services)
    {
        services.AddScoped<IStartupDiagnostics, StartupDiagnostics>();

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
        services.AddScoped<BlobStorageDiagnostics>();

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


