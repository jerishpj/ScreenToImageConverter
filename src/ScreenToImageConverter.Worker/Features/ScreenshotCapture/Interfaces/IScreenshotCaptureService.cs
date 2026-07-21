namespace ScreenToImageConverter.Worker.Features.ScreenshotCapture.Interfaces;

/// <summary>
/// Marker interface for the ScreenshotCapture feature.
/// Routes to IScreenshotProvider in Shared for the actual implementation.
/// Used for feature-specific DI registration and organization.
/// </summary>
/// <remarks>
/// This is a local interface to maintain the vertical slice boundary.
/// The actual implementation details are in Shared.Interfaces.IScreenshotProvider
/// </remarks>
public interface IScreenshotCaptureService
{
}
