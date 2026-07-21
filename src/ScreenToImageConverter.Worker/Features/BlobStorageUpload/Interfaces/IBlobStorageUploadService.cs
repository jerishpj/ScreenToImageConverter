namespace ScreenToImageConverter.Worker.Features.BlobStorageUpload.Interfaces;

/// <summary>
/// Marker interface for the BlobStorageUpload feature.
/// Routes to IBlobStorageProvider in Shared for the actual implementation.
/// Used for feature-specific DI registration and organization.
/// </summary>
/// <remarks>
/// This is a local interface to maintain the vertical slice boundary.
/// The actual implementation details are in Shared.Interfaces.IBlobStorageProvider
/// </remarks>
public interface IBlobStorageUploadService
{
}
