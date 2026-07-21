using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Shared.Configuration;
using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Commands;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Models;

namespace ScreenToImageConverter.Worker.Features.BlobStorageUpload.Handlers;

/// <summary>
/// Handles screenshot upload commands.
/// Orchestrates the blob storage provider to upload screenshots and generate SAS URLs.
/// Part of the BlobStorageUpload vertical slice.
/// </summary>
public class UploadScreenshotHandler
{
    private readonly IBlobStorageProvider _blobStorageProvider;
    private readonly BlobStorageOptions _blobStorageOptions;
    private readonly ILogger<UploadScreenshotHandler> _logger;

    public UploadScreenshotHandler(
        IBlobStorageProvider blobStorageProvider,
        IOptions<BlobStorageOptions> blobStorageOptions,
        ILogger<UploadScreenshotHandler> logger)
    {
        _blobStorageProvider = blobStorageProvider ?? throw new ArgumentNullException(nameof(blobStorageProvider));
        _blobStorageOptions = blobStorageOptions.Value ?? throw new ArgumentNullException(nameof(blobStorageOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the screenshot upload command.
    /// Uploads the screenshot data to blob storage and generates a SAS URL.
    /// </summary>
    public async Task<BlobUploadResult> HandleAsync(UploadScreenshotCommand command, CancellationToken cancellationToken)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (command.ImageData == null || command.ImageData.Length == 0)
            throw new ArgumentException("Image data cannot be null or empty", nameof(command.ImageData));

        if (string.IsNullOrWhiteSpace(command.BlobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(command.BlobName));

        try
        {
            _logger.LogInformation(
                "Uploading screenshot to blob storage: {BlobName} in container {Container} (Size: {SizeKb} KB) [CorrelationId: {CorrelationId}]",
                command.BlobName,
                command.ContainerName,
                command.ImageData.Length / 1024,
                command.CorrelationId ?? "N/A");

            // Upload the blob
            var blobUrlInfo = await _blobStorageProvider.UploadAsync(
                command.ContainerName,
                command.BlobName,
                command.ImageData,
                command.ContentType,
                cancellationToken);

            // Generate SAS URL for time-limited access
            string? sasUrl = null;
            DateTime? sasUrlExpiresAt = null;

            try
            {
                var sasUrlInfo = await _blobStorageProvider.GenerateSasUrlAsync(
                    command.ContainerName,
                    command.BlobName,
                    _blobStorageOptions.SasUrlExpirationMinutes,
                    cancellationToken);

                sasUrl = sasUrlInfo.SasUrl;
                sasUrlExpiresAt = sasUrlInfo.SasUrlExpiresAt;

                _logger.LogInformation(
                    "✅ SAS URL generated with {Minutes} minute expiration [CorrelationId: {CorrelationId}]",
                    _blobStorageOptions.SasUrlExpirationMinutes,
                    command.CorrelationId ?? "N/A");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "⚠️ Could not generate SAS URL, returning standard blob URI [CorrelationId: {CorrelationId}]",
                    command.CorrelationId ?? "N/A");
            }

            // Create and return the result
            var result = BlobUploadResult.Create(
                command.ContainerName,
                command.BlobName,
                blobUrlInfo.Uri,
                command.ImageData.Length,
                command.CorrelationId,
                command.RequestId,
                sasUrl,
                sasUrlExpiresAt);

            _logger.LogInformation(
                "✅ Screenshot upload completed: {BlobName} (URI: {BlobUri}) [CorrelationId: {CorrelationId}]",
                command.BlobName,
                blobUrlInfo.Uri,
                command.CorrelationId ?? "N/A");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to upload screenshot: {BlobName} [CorrelationId: {CorrelationId}]",
                command.BlobName,
                command.CorrelationId ?? "N/A");
            throw;
        }
    }
}
