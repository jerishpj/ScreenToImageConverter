using Microsoft.Extensions.DependencyInjection;
using ScreenToImageConverter.Infrastructure.Providers;
using ScreenToImageConverter.Shared.Interfaces;

namespace ScreenToImageConverter.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Playwright screenshot provider as a singleton.
    /// </summary>
    public static IServiceCollection AddPlaywrightScreenshotProvider(
        this IServiceCollection services)
    {
        services.AddSingleton<IScreenshotProvider, PlaywrightScreenshotProvider>();
        return services;
    }

    /// <summary>
    /// Registers Playwright screenshot provider and initializes it at startup.
    /// </summary>
    public static IServiceCollection AddPlaywrightScreenshotProviderWithInitialization(
        this IServiceCollection services)
    {
        services.AddSingleton<IScreenshotProvider, PlaywrightScreenshotProvider>();

        // Note: Initialization should be called explicitly in Program.cs
        // after DI container is built, to ensure proper logging setup

        return services;
    }

    /// <summary>
    /// Registers Azure Blob Storage provider as a singleton.
    /// </summary>
    public static IServiceCollection AddBlobStorageProvider(
        this IServiceCollection services)
    {
        services.AddSingleton<IBlobStorageProvider, BlobStorageProvider>();
        return services;
    }

    /// <summary>
    /// Registers all infrastructure providers at once.
    /// </summary>
    public static IServiceCollection AddInfrastructureProviders(
        this IServiceCollection services)
    {
        services.AddPlaywrightScreenshotProvider();
        services.AddBlobStorageProvider();
        return services;
    }
}
