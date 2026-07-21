using ScreenToImageConverter.Shared.Interfaces;
using ScreenToImageConverter.Shared.Messages;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Commands;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Handlers;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Commands;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Handlers;
using Microsoft.Extensions.Logging;

namespace ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Handlers;

/// <summary>
/// Orchestrates the complete screenshot processing workflow.
/// Coordinates between ScreenshotCapture, BlobStorageUpload, and event publishing.
/// Part of the ServiceBusMessaging vertical slice.
/// </summary>
public class ScreenshotProcessingOrchestrator
{
    private readonly CaptureScreenshotHandler _captureHandler;
    private readonly UploadScreenshotHandler _uploadHandler;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<ScreenshotProcessingOrchestrator> _logger;

    public ScreenshotProcessingOrchestrator(
        CaptureScreenshotHandler captureHandler,
        UploadScreenshotHandler uploadHandler,
        IMessagePublisher messagePublisher,
        ILogger<ScreenshotProcessingOrchestrator> logger)
    {
        _captureHandler = captureHandler ?? throw new ArgumentNullException(nameof(captureHandler));
        _uploadHandler = uploadHandler ?? throw new ArgumentNullException(nameof(uploadHandler));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Orchestrates the complete screenshot processing workflow.
    /// Steps:
    /// 1. Validate request
    /// 2. Capture screenshot from URL
    /// 3. Upload screenshot to blob storage
    /// 4. Generate SAS URL for time-limited access
    /// 5. Publish completion event
    /// </summary>
    public async Task ProcessScreenshotAsync(
        HtmlScreenshotRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "🚀 Starting screenshot processing workflow [RequestId: {RequestId}, CorrelationId: {CorrelationId}]",
                request.RequestId,
                correlationId);

            // Step 1: Capture screenshot
            _logger.LogInformation(
                "📸 Step 1/3: Capturing screenshot for URL: {Url} [CorrelationId: {CorrelationId}]",
                request.Url,
                correlationId);

            var captureCommand = new CaptureScreenshotCommand
            {
                Url = request.Url,
                ViewportWidth = request.ViewportWidth,
                ViewportHeight = request.ViewportHeight,
                TimeoutMs = request.TimeoutMs,
                CorrelationId = correlationId
            };

            var screenshotResult = await _captureHandler.HandleAsync(captureCommand, cancellationToken);

            _logger.LogInformation(
                "✅ Screenshot captured: {SizeKb} KB [CorrelationId: {CorrelationId}]",
                screenshotResult.ImageSizeBytes / 1024,
                correlationId);

            // Step 2: Upload to blob storage
            _logger.LogInformation(
                "☁️ Step 2/3: Uploading screenshot to blob storage [CorrelationId: {CorrelationId}]",
                correlationId);

            var blobName = GenerateBlobName(request.RequestId);
            var uploadCommand = new UploadScreenshotCommand
            {
                ImageData = screenshotResult.ImageData,
                BlobName = blobName,
                ContainerName = "screenshots",
                ContentType = "image/png",
                SourceUrl = request.Url,
                CorrelationId = correlationId,
                RequestId = request.RequestId
            };

            var uploadResult = await _uploadHandler.HandleAsync(uploadCommand, cancellationToken);

            _logger.LogInformation(
                "✅ Screenshot uploaded to blob storage [BlobUri: {BlobUri}, CorrelationId: {CorrelationId}]",
                uploadResult.BlobUri,
                correlationId);

            // Step 3: Publish completion event
            _logger.LogInformation(
                "📤 Step 3/3: Publishing completion event [CorrelationId: {CorrelationId}]",
                correlationId);

            var processingDuration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var completionEvent = new ScreenshotCompletedEvent
            {
                RequestId = request.RequestId,
                CorrelationId = correlationId,
                SourceId = request.SourceId,
                Url = request.Url,
                IsSuccessful = true,
                BlobFileName = blobName,
                BlobContainerName = uploadResult.ContainerName,
                BlobUri = uploadResult.BlobUri,
                BlobSasUrl = uploadResult.SasUrl,
                SasUrlExpiresAt = uploadResult.SasUrlExpiresAt,
                FileSizeBytes = screenshotResult.ImageSizeBytes,
                ContentType = "image/png",
                ProcessedAt = DateTime.UtcNow,
                ProcessingDurationMs = processingDuration,
                ProcessedByInstanceId = Environment.MachineName
            };

            await _messagePublisher.PublishAsync(completionEvent, correlationId, cancellationToken);

            _logger.LogInformation(
                "✅ Completion event published [CorrelationId: {CorrelationId}]",
                correlationId);

            _logger.LogInformation(
                "🎉 Screenshot processing workflow completed successfully [RequestId: {RequestId}, Duration: {DurationMs}ms, CorrelationId: {CorrelationId}]",
                request.RequestId,
                processingDuration,
                correlationId);
        }
        catch (Exception ex)
        {
            var processingDuration = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogError(
                ex,
                "❌ Screenshot processing failed [RequestId: {RequestId}, Duration: {DurationMs}ms, CorrelationId: {CorrelationId}]",
                request.RequestId,
                processingDuration,
                correlationId);

            // Try to publish failure event
            try
            {
                var failureEvent = new ScreenshotCompletedEvent
                {
                    RequestId = request.RequestId,
                    CorrelationId = correlationId,
                    SourceId = request.SourceId,
                    Url = request.Url,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingDurationMs = processingDuration,
                    ProcessedByInstanceId = Environment.MachineName
                };

                await _messagePublisher.PublishAsync(failureEvent, correlationId, cancellationToken);
            }
            catch (Exception publishEx)
            {
                _logger.LogError(
                    publishEx,
                    "❌ Failed to publish failure event [CorrelationId: {CorrelationId}]",
                    correlationId);
            }

            throw;
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
}
