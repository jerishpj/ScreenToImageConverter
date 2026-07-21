# Playwright Screenshot Functionality - Developer Guide

## Overview

The ScreenshotCapture feature provides automated HTML-to-image conversion using Microsoft Playwright with C#. It handles browser automation, page navigation, and screenshot capture with built-in retry logic, error handling, and comprehensive logging.

## Architecture

```
┌─────────────────────────┐
│  CaptureScreenshotCommand   │
│  - Url                  │
│  - ViewportWidth        │
│  - ViewportHeight       │
│  - TimeoutMs            │
│  - CorrelationId        │
└────────────┬────────────┘
			 │
			 ▼
┌─────────────────────────────────┐
│  CaptureScreenshotHandler       │
│  - Orchestrates provider call   │
│  - Applies defaults             │
│  - Creates results              │
│  - Logs activity                │
└────────────┬────────────────────┘
			 │
			 ▼
┌─────────────────────────────────────────┐
│  IScreenshotProvider                    │
│  - PlaywrightScreenshotProvider         │
│  - MockScreenshotProvider (testing)     │
└────────────┬────────────────────────────┘
			 │
			 ▼
┌──────────────────────────────┐
│  PlaywrightScreenshotProvider│
│  - Initialize Playwright     │
│  - Launch browser            │
│  - Navigate & capture        │
│  - Retry on timeout/network  │
│  - Resource cleanup          │
└──────────────────────────────┘
			 │
			 ▼
┌──────────────────────────┐
│  Microsoft.Playwright    │
│  - Browser automation    │
│  - Page navigation       │
│  - Screenshot capture    │
└──────────────────────────┘
			 │
			 ▼
┌──────────────────────────────────┐
│  ScreenshotResult                │
│  - Url                           │
│  - ImageData (PNG bytes)         │
│  - ImageSizeBytes               │
│  - CapturedAt                   │
│  - CorrelationId                │
└──────────────────────────────────┘
```

## Configuration

### PlaywrightOptions (appsettings.json)

```json
{
  "Playwright": {
	"BrowserType": "chromium",           // "chromium", "firefox", or "webkit"
	"DefaultViewportWidth": 1920,        // Pixels
	"DefaultViewportHeight": 1080,       // Pixels
	"DefaultTimeoutMs": 30000,           // 30 seconds
	"WaitUntilEvent": "networkidle",    // "load", "domcontentloaded", "networkidle"
	"Headless": true,                    // Run without GUI
	"DisableSandbox": true,              // Required in Docker
	"DeviceScaleFactor": 1.0,           // For high-DPI screens
	"FullPage": true,                    // Capture full page or viewport
	"MaxRetryAttempts": 2,               // Retry on timeout/network error
	"RetryDelayMs": 1000,                // Wait between retries
	"EmulateDeviceUserAgent": true       // Use desktop user agent
  }
}
```

### Environment-Specific Configuration

**Development (appsettings.Development.json)**:
- `Headless`: true
- `DisableSandbox`: false (disable sandbox is optional)
- `MaxRetryAttempts`: 2
- `DefaultTimeoutMs`: 30000

**Production (appsettings.Production.json)**:
- `Headless`: true (always)
- `DisableSandbox`: true (required in Docker)
- `MaxRetryAttempts`: 3
- `DefaultTimeoutMs`: 45000
- More aggressive retry settings

**Docker**:
```dockerfile
# Install Playwright dependencies
RUN apt-get update && apt-get install -y \
	libgconf-2-4 \
	libx11-6 \
	libxss1 \
	libxtst6 \
	fonts-liberation \
	libnss3 \
	libappindicator1 \
	libxrender1 \
	xdg-utils

# Set DisableSandbox: true in appsettings
```

## Basic Usage

### 1. Simple Screenshot Capture

```csharp
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Handlers;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Commands;
using Microsoft.Extensions.DependencyInjection;

// In your service/handler
private readonly CaptureScreenshotHandler _handler;

public MyService(CaptureScreenshotHandler handler)
{
	_handler = handler;
}

public async Task CaptureAsync(string url, CancellationToken cancellationToken)
{
	var command = new CaptureScreenshotCommand
	{
		Url = url,
		CorrelationId = Guid.NewGuid().ToString()
	};

	var result = await _handler.HandleAsync(command, cancellationToken);

	// Use result.ImageData (PNG bytes)
	// Use result.ImageSizeBytes
	// Use result.CapturedAt
}
```

### 2. Custom Viewport and Timeout

```csharp
var command = new CaptureScreenshotCommand
{
	Url = "https://example.com",
	ViewportWidth = 1280,      // Mobile view
	ViewportHeight = 720,
	TimeoutMs = 60000,         // 60 seconds
	CorrelationId = requestId
};

var result = await _handler.HandleAsync(command, cancellationToken);
```

### 3. Direct Provider Usage (Advanced)

```csharp
private readonly IScreenshotProvider _provider;

public MyService(IScreenshotProvider provider)
{
	_provider = provider;
}

public async Task InitializeAsync(CancellationToken cancellationToken)
{
	// Initialize once during startup
	await _provider.InitializeAsync(cancellationToken);
}

public async Task CaptureAsync(string url, CancellationToken cancellationToken)
{
	byte[] imageData = await _provider.CaptureScreenshotAsync(
		url,
		viewportWidth: 1920,
		viewportHeight: 1080,
		timeoutMs: 30000,
		cancellationToken);

	// Use imageData
}
```

## Error Handling

### Common Error Scenarios

| Scenario | Exception | Handling |
|----------|-----------|----------|
| Invalid URL | ArgumentException | Validate URLs before passing |
| Timeout | PlaywrightException (timeout) | Automatic retry with backoff |
| Network error | PlaywrightException (net::) | Automatic retry with backoff |
| Page crash | PlaywrightException | Non-retryable, thrown immediately |
| Not initialized | ScreenshotCapturException | Call InitializeAsync first |
| Max retries exceeded | ScreenshotCapturException | All attempts exhausted, unrecoverable |

### Error Handling Pattern

```csharp
try
{
	var result = await _handler.HandleAsync(command, cancellationToken);
	// Save to blob storage
}
catch (ArgumentException ex) when (ex.Message.Contains("URL"))
{
	// Log validation error
	_logger.LogError("Invalid URL: {Message}", ex.Message);
	// Publish failure event
}
catch (ScreenshotCapturException ex) when (ex.Message.Contains("timeout"))
{
	// Log timeout
	_logger.LogWarning("Screenshot capture timed out");
	// Could retry with longer timeout
}
catch (ScreenshotCapturException ex)
{
	// Log other capture errors
	_logger.LogError(ex, "Screenshot capture failed");
	// Publish failure event with error details
}
catch (OperationCanceledException)
{
	// Handle cancellation
	_logger.LogInformation("Screenshot capture cancelled");
}
```

## Health Checks

### Playwright Health Check

The system includes a health check for Playwright initialization:

```csharp
// Registered automatically in AddScreenshotCaptureFeature()
services.AddHealthChecks()
	.AddCheck<PlaywrightHealthCheck>("playwright", tags: new[] { "ready", "live" });
```

### Checking Health

```bash
# Get all health checks
GET /health

# Get specific check
GET /health/playwright

# Response example:
{
  "status": "Healthy",
  "checks": {
	"playwright": {
	  "status": "Healthy",
	  "description": "Playwright provider is initialized and ready."
	}
  }
}
```

## Testing

### Unit Tests with Mocks

```csharp
using ScreenToImageConverter.Tests.Fixtures;

public class MyScreenshotTests
{
	private readonly Mock<IScreenshotProvider> _mockProvider;
	private readonly CaptureScreenshotHandler _handler;

	public MyScreenshotTests()
	{
		_mockProvider = new Mock<IScreenshotProvider>();
		var options = Options.Create(new PlaywrightOptions { /* ... */ });
		var logger = new Mock<ILogger<CaptureScreenshotHandler>>();

		_handler = new CaptureScreenshotHandler(_mockProvider.Object, options, logger.Object);
	}

	[Fact]
	public async Task Should_Capture_Successfully()
	{
		// Arrange
		var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
		_mockProvider.Setup(x => x.CaptureScreenshotAsync(
			It.IsAny<string>(),
			It.IsAny<int>(),
			It.IsAny<int>(),
			It.IsAny<int>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(imageData);

		var command = new CaptureScreenshotCommand { Url = "https://example.com" };

		// Act
		var result = await _handler.HandleAsync(command, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(imageData.Length, result.ImageSizeBytes);
	}
}
```

### Integration Tests with Mock Provider

```csharp
using ScreenToImageConverter.Tests.Fixtures;

public class ScreenshotIntegrationTests : ScreenshotCaptureTestFixture
{
	[Fact]
	public async Task Should_Capture_With_Mock_Provider()
	{
		// Arrange
		var handler = GetService<CaptureScreenshotHandler>();
		var command = new CaptureScreenshotCommand { Url = "https://example.com" };

		// Act
		var result = await handler.HandleAsync(command, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("https://example.com", result.Url);
		Assert.True(result.ImageSizeBytes > 0);
	}

	[Fact]
	public async Task Should_Handle_Mock_Failures()
	{
		// Arrange
		var provider = GetService<MockScreenshotProvider>();
		provider.Config.FailCapture = true;
		provider.Config.ErrorMessage = "Simulated capture failure";

		var handler = GetService<CaptureScreenshotHandler>();
		var command = new CaptureScreenshotCommand { Url = "https://example.com" };

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => handler.HandleAsync(command, CancellationToken.None));
	}
}
```

## Performance Considerations

### Browser Reuse
- The provider maintains a single browser instance (singleton)
- Each capture creates a new context and page
- Pages are properly closed after capture

### Resource Management
```csharp
// Browser reuse (efficient)
// ✅ Good
var result1 = await provider.CaptureScreenshotAsync(url1, ct);
var result2 = await provider.CaptureScreenshotAsync(url2, ct);

// ✅ Also good - contexts are created/destroyed per capture
// No memory leak - pages properly disposed
```

### Memory Usage
- Each screenshot context uses ~30-50MB memory
- With `MaxConcurrentCalls: 1`, only one is active at a time
- For higher throughput, increase `MaxConcurrentCalls` (consider memory)

### Timeout Recommendations
- **Simple pages**: 10-15 seconds
- **Complex pages**: 30-45 seconds
- **Media-heavy pages**: 45-60 seconds
- Default: 30 seconds (production: 45 seconds)

## Troubleshooting

### Issue: "Playwright is not initialized. Call InitializeAsync first."

**Cause**: Provider not initialized before use

**Solution**:
```csharp
// In Program.cs
await host.Services.InitializePlaywrightAsync(CancellationToken.None);
```

### Issue: "Failed to launch chromium"

**Cause**: Missing dependencies or permissions

**Solution**:
1. Ensure Playwright dependencies installed
2. Check `DisableSandbox` is true in Docker
3. Verify browser binary permissions: `chmod +x ~/.cache/ms-playwright/*`

### Issue: Screenshot timeout on complex pages

**Cause**: Page needs more time to load

**Solution**:
1. Increase `DefaultTimeoutMs` in appsettings
2. Pass custom timeout in command: `TimeoutMs = 60000`
3. Check page for infinite loops or pending requests

### Issue: High memory usage

**Cause**: Browser instance or page leak

**Solution**:
1. Ensure pages closed in finally block (already done)
2. Check `MaxConcurrentCalls` setting (default: 1)
3. Monitor screenshot sizes (very large = memory intensive)

## Best Practices

1. **Always use CorrelationId for tracing**
   ```csharp
   var command = new CaptureScreenshotCommand
   {
	   Url = url,
	   CorrelationId = Guid.NewGuid().ToString() // or from request context
   };
   ```

2. **Handle timeouts gracefully**
   ```csharp
   // Use reasonable timeout for your use case
   TimeoutMs = url.Contains("complex") ? 60000 : 30000
   ```

3. **Validate URLs before passing**
   ```csharp
   if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
	   (uri.Scheme != "http" && uri.Scheme != "https"))
   {
	   throw new ArgumentException("Invalid URL");
   }
   ```

4. **Log thoroughly**
   ```csharp
   _logger.LogInformation(
	   "Capturing screenshot [RequestId: {RequestId}, Url: {Url}, Size: {SizeKb} KB]",
	   correlationId, url, imageSize / 1024);
   ```

5. **Test error scenarios**
   - Use mock provider to test without real browser
   - Test with invalid URLs
   - Test with long timeouts
   - Test cancellation scenarios

## Related Components

- **UploadScreenshotHandler**: Saves captured images to blob storage
- **ScreenshotProcessingOrchestrator**: Coordinates capture and upload
- **ServiceBusMessageConsumer**: Receives screenshot requests
- **ServiceBusEventPublisher**: Publishes completion events

## See Also

- [Playwright Documentation](https://playwright.dev/dotnet/)
- [Microsoft.Playwright NuGet Package](https://www.nuget.org/packages/Microsoft.Playwright/)
- [SOLUTION_OVERVIEW.md](../SOLUTION_OVERVIEW.md)
- [STEP6_IMPLEMENTATION_GUIDE.md](../STEP6_IMPLEMENTATION_GUIDE.md)
