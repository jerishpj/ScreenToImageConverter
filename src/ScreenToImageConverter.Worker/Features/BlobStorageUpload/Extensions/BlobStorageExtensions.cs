using Microsoft.Extensions.DependencyInjection;
using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Handlers;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Health;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Providers;

namespace ScreenToImageConverter.Worker.Features.BlobStorageUpload.Extensions;

/// <summary>
/// Extension methods for registering BlobStorageUpload feature services.
/// Part of the BlobStorageUpload vertical slice.
/// </summary>
public static class BlobStorageExtensions
{
    /// <summary>
    /// Registers all BlobStorageUpload feature services.
    /// </summary>
    public static IServiceCollection AddBlobStorageUploadFeature(this IServiceCollection services)
    {
        // Register Blob Storage provider
        services.AddSingleton<IBlobStorageProvider, BlobStorageProvider>();

        // Register handlers
        services.AddScoped<UploadScreenshotHandler>();

        // Register health checks
        services.AddHealthChecks()
            .AddCheck<BlobStorageHealthCheck>("blob-storage", tags: new[] { "ready" });

        return services;
    }
}
