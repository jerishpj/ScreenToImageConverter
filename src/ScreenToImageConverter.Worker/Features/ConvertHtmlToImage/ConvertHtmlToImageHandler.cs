using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenToImageConverter.Worker.AppSettings;
using ScreenToImageConverter.Worker.Infrastructure.Storage;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;
using ScreenToImageConverter.Worker.Infrastructure.Screenshots;

namespace ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

/// <summary>
/// Handles the complete HTML to image conversion workflow.
/// Orchestrates: Validation → Capture → Upload → Event Publishing
/// Core business logic for the ConvertHtmlToImage feature.
/// </summary>
public class ConvertHtmlToImageHandler
{
    private readonly IScreenshotProvider _screenshotProvider;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly PlaywrightOptions _playwrightOptions;
    private readonly StorageSettings _storageSettings;
    private readonly ILogger<ConvertHtmlToImageHandler> _logger;

    public ConvertHtmlToImageHandler(
        IScreenshotProvider screenshotProvider,
        IBlobStorageService blobStorageService,
        IMessagePublisher messagePublisher,
        IOptions<PlaywrightOptions> playwrightOptions,
        IOptions<StorageSettings> storageSettings,
        ILogger<ConvertHtmlToImageHandler> logger)
    {
        _screenshotProvider = screenshotProvider ?? throw new ArgumentNullException(nameof(screenshotProvider));
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _playwrightOptions = playwrightOptions?.Value ?? throw new ArgumentNullException(nameof(playwrightOptions));
        _storageSettings = storageSettings?.Value ?? throw new ArgumentNullException(nameof(storageSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the HTML to image conversion command.
    /// Orchestrates the complete workflow and returns metadata about the result.
    /// </summary>
    public async Task<ImageMetadataResponse> HandleAsync(
        ConvertHtmlToImageCommand command,
        CancellationToken cancellationToken)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var startTime = DateTime.UtcNow;
        var correlationId = command.CorrelationId ?? Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation(
                "🚀 Starting HTML to image conversion [RequestId: {RequestId}, CorrelationId: {CorrelationId}, URL: {Url}]",
                command.RequestId,
                correlationId,
                command.Url);

            // Step 1: Validate the command
            _logger.LogInformation(
                "📋 Step 1/3: Validating request [CorrelationId: {CorrelationId}]",
                correlationId);

            var validationErrors = HtmlRequestValidator.Validate(command);
            if (validationErrors.Any())
            {
                var errorMessage = $"Validation errors: {string.Join("; ", validationErrors)}";
                _logger.LogWarning(
                    "❌ Validation failed: {Errors} [CorrelationId: {CorrelationId}]",
                    errorMessage,
                    correlationId);

                return ImageMetadataResponse.CreateFailure(
                    command.RequestId,
                    command.Url,
                    errorMessage,
                    correlationId,
                    command.SourceId);
            }

            // Step 2: Capture screenshot
            _logger.LogInformation(
                "📸 Step 2/3: Capturing screenshot [CorrelationId: {CorrelationId}]",
                correlationId);

            int viewportWidth = command.ViewportWidth ?? _playwrightOptions.DefaultViewportWidth;
            int viewportHeight = command.ViewportHeight ?? _playwrightOptions.DefaultViewportHeight;
            int timeoutMs = command.TimeoutMs ?? _playwrightOptions.DefaultTimeoutMs;

            byte[] imageData = await _screenshotProvider.CaptureScreenshotAsync(
                command.Url,
                viewportWidth,
                viewportHeight,
                timeoutMs,
                cancellationToken);

            _logger.LogInformation(
                "✅ Screenshot captured: {SizeKb} KB [CorrelationId: {CorrelationId}]",
                imageData.Length / 1024,
                correlationId);

            // Step 2.5: Save locally for verification
            _logger.LogInformation(
                "💾 Step 2.5/3: Saving image locally for verification [CorrelationId: {CorrelationId}]",
                correlationId);

            var localFilePath = await SaveImageLocallyAsync(
                imageData,
                command.RequestId,
                correlationId,
                cancellationToken);

            _logger.LogInformation(
                "✅ Image saved locally: {LocalPath} [CorrelationId: {CorrelationId}]",
                localFilePath,
                correlationId);

            // Step 3: Upload to blob storage
            _logger.LogInformation(
                "☁️ Step 3/3: Uploading to blob storage [CorrelationId: {CorrelationId}]",
                correlationId);

            var blobName = GenerateBlobName(command.RequestId);
            var uploadResult = await _blobStorageService.UploadAsync(
                _storageSettings.ContainerName,
                blobName,
                imageData,
                "image/png",
                correlationId,
                command.RequestId,
                cancellationToken);

            _logger.LogInformation(
                "✅ Image uploaded to blob storage [BlobUri: {BlobUri}, CorrelationId: {CorrelationId}]",
                uploadResult.BlobUri,
                correlationId);

            var processingDuration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Build success response
            var response = ImageMetadataResponse.CreateSuccess(
                command.RequestId,
                command.Url,
                blobName,
                uploadResult.ContainerName,
                uploadResult.BlobUri,
                uploadResult.SasUrl,
                uploadResult.SasUrlExpiresAt,
                imageData.Length,
                correlationId,
                command.SourceId,
                processingDuration,
                Environment.MachineName);

            _logger.LogInformation(
                "🎉 HTML to image conversion completed successfully [RequestId: {RequestId}, Duration: {DurationMs}ms, CorrelationId: {CorrelationId}]",
                command.RequestId,
                processingDuration,
                correlationId);

            // Publish completion event (fire and forget for async notification)
            _ = PublishCompletionEventAsync(response, correlationId, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            var processingDuration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogError(
                ex,
                "❌ HTML to image conversion failed [RequestId: {RequestId}, Duration: {DurationMs}ms, CorrelationId: {CorrelationId}]",
                command.RequestId,
                processingDuration,
                correlationId);

            var failureResponse = ImageMetadataResponse.CreateFailure(
                command.RequestId,
                command.Url,
                ex.Message,
                correlationId,
                command.SourceId,
                processingDuration);

            // Attempt to publish failure event
            _ = PublishCompletionEventAsync(failureResponse, correlationId, cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// Publishes a completion event to notify downstream systems.
    /// Fire-and-forget pattern to avoid blocking the main response.
    /// </summary>
    private async Task PublishCompletionEventAsync(
        ImageMetadataResponse response,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "📤 Publishing completion event [CorrelationId: {CorrelationId}]",
                correlationId);

            // Convert response to event format (matching ScreenshotCompletedEvent structure)
            var completionEvent = new
            {
                response.RequestId,
                CorrelationId = correlationId,
                response.SourceId,
                response.Url,
                response.IsSuccessful,
                response.ErrorMessage,
                response.BlobFileName,
                response.BlobContainerName,
                response.BlobUri,
                response.BlobSasUrl,
                response.SasUrlExpiresAt,
                response.FileSizeBytes,
                response.ContentType,
                response.ProcessedAt,
                response.ProcessingDurationMs,
                response.ProcessedByInstanceId,
                response.RetryAttempts,
                response.SchemaVersion
            };

            await _messagePublisher.PublishAsync(completionEvent, correlationId, cancellationToken);

            _logger.LogInformation(
                "✅ Completion event published [CorrelationId: {CorrelationId}]",
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "⚠️ Failed to publish completion event [CorrelationId: {CorrelationId}]",
                correlationId);
            // Don't throw - this is fire-and-forget
        }
    }

    /// <summary>
    /// Generates a unique blob name for the screenshot.
    /// </summary>
    private static string GenerateBlobName(string requestId)
    {
        var now = DateTime.UtcNow;
        return $"screenshots/{now:yyyy/MM/dd}/{requestId}_{now:HHmmss}.png";
    }

    /// <summary>
    /// Saves the image data locally to disk for verification purposes.
    /// Creates a local folder structure: ./output/screenshots/yyyy/MM/dd/
    /// </summary>
    private async Task<string> SaveImageLocallyAsync(
        byte[] imageData,
        string requestId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create local output directory structure
            var baseOutputPath = Path.Combine(AppContext.BaseDirectory, "output", "screenshots");
            var now = DateTime.UtcNow;
            var dateFolder = Path.Combine(baseOutputPath, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));

            // Ensure directory exists
            if (!Directory.Exists(dateFolder))
            {
                Directory.CreateDirectory(dateFolder);
                _logger.LogDebug(
                    "📁 Created local directory: {Directory} [CorrelationId: {CorrelationId}]",
                    dateFolder,
                    correlationId);
            }

            // Generate unique filename
            var filename = $"{requestId}_{now:HHmmss_fff}.png";
            var localFilePath = Path.Combine(dateFolder, filename);

            // Write file asynchronously
            await File.WriteAllBytesAsync(localFilePath, imageData, cancellationToken);

            _logger.LogDebug(
                "📝 Image file written: {Filename}, Size: {SizeKb} KB [CorrelationId: {CorrelationId}]",
                filename,
                imageData.Length / 1024,
                correlationId);

            return localFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "⚠️ Failed to save image locally. Continuing with blob upload. [CorrelationId: {CorrelationId}]",
                correlationId);

            // Don't throw - local save is optional for verification
            // Continue with blob upload anyway
            return $"(Failed to save locally: {ex.Message})";
        }
    }
}
