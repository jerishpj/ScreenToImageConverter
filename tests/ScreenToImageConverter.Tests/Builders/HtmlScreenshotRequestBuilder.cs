using ScreenToImageConverter.Worker.Infrastructure.Notifications;

namespace ScreenToImageConverter.Tests.Builders;

/// <summary>
/// Builder for creating test instances of HtmlScreenshotRequest with sensible defaults.
/// </summary>
public class HtmlScreenshotRequestBuilder
{
    private string _requestId = Guid.NewGuid().ToString();
    private string _url = "https://example.com";
    private string _sourceId = "test-source";
    private string _correlationId = Guid.NewGuid().ToString();
    private int _viewportWidth = 1920;
    private int _viewportHeight = 1080;
    private int _timeoutMs = 30000;
    private bool _waitForPageLoad = true;
    private string _screenshotName = "test-screenshot.png";

    public HtmlScreenshotRequestBuilder WithRequestId(string requestId)
    {
        _requestId = requestId;
        return this;
    }

    public HtmlScreenshotRequestBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    public HtmlScreenshotRequestBuilder WithSourceId(string sourceId)
    {
        _sourceId = sourceId;
        return this;
    }

    public HtmlScreenshotRequestBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    public HtmlScreenshotRequestBuilder WithViewport(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        return this;
    }

    public HtmlScreenshotRequestBuilder WithTimeout(int milliseconds)
    {
        _timeoutMs = milliseconds;
        return this;
    }

    public HtmlScreenshotRequestBuilder WithWaitForPageLoad(bool wait)
    {
        _waitForPageLoad = wait;
        return this;
    }

    public HtmlScreenshotRequestBuilder WithScreenshotName(string name)
    {
        _screenshotName = name;
        return this;
    }

    public HtmlScreenshotRequest Build()
    {
        return new HtmlScreenshotRequest
        {
            RequestId = _requestId,
            Url = _url,
            SourceId = _sourceId,
            CorrelationId = _correlationId,
            ViewportWidth = _viewportWidth,
            ViewportHeight = _viewportHeight,
            TimeoutMs = _timeoutMs,
            WaitForPageLoad = _waitForPageLoad,
            ScreenshotName = _screenshotName,
            CreatedAt = DateTime.UtcNow,
            SchemaVersion = "1.0"
        };
    }
}
