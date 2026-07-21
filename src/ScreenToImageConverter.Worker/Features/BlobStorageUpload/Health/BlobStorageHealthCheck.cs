using Microsoft.Extensions.Diagnostics.HealthChecks;
using ScreenToImageConverter.Shared.Interfaces;

namespace ScreenToImageConverter.Worker.Features.BlobStorageUpload.Health;

/// <summary>
/// Health check for Azure Blob Storage connectivity.
/// Part of the BlobStorageUpload vertical slice.
/// </summary>
public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IBlobStorageProvider _blobStorageProvider;

    public BlobStorageHealthCheck(IBlobStorageProvider blobStorageProvider)
    {
        _blobStorageProvider = blobStorageProvider ?? throw new ArgumentNullException(nameof(blobStorageProvider));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _blobStorageProvider.IsConnectedAsync(cancellationToken);
            return isConnected
                ? HealthCheckResult.Healthy("Blob Storage is accessible.")
                : HealthCheckResult.Unhealthy("Blob Storage is not accessible.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob Storage check failed.", ex);
        }
    }
}
