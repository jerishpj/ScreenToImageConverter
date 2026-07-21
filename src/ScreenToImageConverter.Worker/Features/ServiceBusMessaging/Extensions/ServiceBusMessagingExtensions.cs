using Microsoft.Extensions.DependencyInjection;
using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Consumers;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Publishers;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Validators;

namespace ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Extensions;

/// <summary>
/// Extension methods for registering ServiceBusMessaging feature services.
/// Part of the ServiceBusMessaging vertical slice.
/// </summary>
public static class ServiceBusMessagingExtensions
{
    /// <summary>
    /// Registers all ServiceBusMessaging feature services.
    /// </summary>
    public static IServiceCollection AddServiceBusMessagingFeature(this IServiceCollection services)
    {
        // Register message consumer and publisher
        services.AddSingleton<IMessageConsumer, ServiceBusMessageConsumer>();
        services.AddSingleton<IMessagePublisher, ServiceBusEventPublisher>();

        // Register validators
        services.AddSingleton<HtmlScreenshotRequestValidator>();

        return services;
    }
}
