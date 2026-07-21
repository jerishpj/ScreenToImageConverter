# Development Guide

This guide explains the architecture, code organization, and how to extend the system.

## Architecture Overview

### Vertical Slice Architecture

The solution uses **Vertical Slice Architecture**, where each feature (or "slice") contains all code needed to implement that feature across all layers:

```
Traditional Layered              Vertical Slice
(by Technical Concern)           (by Feature)

Controllers/                      Feature/
  ├─ ScreenshotController        ├─ Commands/
  ├─ BlobController              ├─ Handlers/
  └─ ...                         ├─ Models/
								 ├─ Interfaces/
Services/                        └─ Exceptions/
  ├─ ScreenshotService
  ├─ BlobService
  └─ ...

Data/
  ├─ ScreenshotRepository
  └─ ...
```

**Benefits:**
- ✅ Features are self-contained and independently understandable
- ✅ Changes are localized to a single feature
- ✅ Teams work independently without conflicts
- ✅ Easy to add/remove features
- ✅ Clear boundaries and contracts

## Solution Structure

```
ScreenToImageConverter/
├── src/
│   ├── ScreenToImageConverter.Shared/           [Shared Contracts & Interfaces]
│   │   ├── Configuration/
│   │   │   ├── PlaywrightOptions.cs            [Playwright settings]
│   │   │   ├── ServiceBusOptions.cs            [Service Bus settings]
│   │   │   └── BlobStorageOptions.cs           [Blob Storage settings]
│   │   ├── Interfaces/
│   │   │   ├── IScreenshotProvider.cs          [Screenshot abstraction]
│   │   │   ├── IBlobStorageProvider.cs         [Blob storage abstraction]
│   │   │   ├── IMessageConsumer.cs             [Message consumer abstraction]
│   │   │   └── IMessagePublisher.cs            [Message publisher abstraction]
│   │   ├── Messages/
│   │   │   ├── HtmlScreenshotRequest.cs        [Request contract]
│   │   │   └── ScreenshotCompletedEvent.cs     [Response contract]
│   │   ├── Exceptions/
│   │   │   └── ScreenshotProcessingException.cs [Domain exception]
│   │   └── Results/
│   │       └── OperationResult<T>.cs           [Result pattern]
│   │
│   └── ScreenToImageConverter.Worker/          [Worker Service]
│       ├── Program.cs                          [Composition root, DI setup]
│       ├── Worker.cs                           [BackgroundService entry point]
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs  [Feature registration helpers]
│       │
│       └── Features/                           [Vertical Slices]
│           ├── ScreenshotCapture/
│           │   ├── Commands/
│           │   │   └── CaptureScreenshotCommand.cs
│           │   ├── Handlers/
│           │   │   └── CaptureScreenshotHandler.cs
│           │   ├── Providers/
│           │   │   └── PlaywrightScreenshotProvider.cs
│           │   ├── Models/
│           │   │   └── ScreenshotResult.cs
│           │   └── ScreenshotCaptureFeature.cs [Feature registration]
│           │
│           ├── BlobStorageUpload/
│           │   ├── Commands/
│           │   │   └── UploadScreenshotCommand.cs
│           │   ├── Handlers/
│           │   │   └── UploadScreenshotHandler.cs
│           │   ├── Models/
│           │   │   └── BlobUploadResult.cs
│           │   └── BlobStorageUploadFeature.cs
│           │
│           └── ServiceBusMessaging/
│               ├── Consumers/
│               │   └── ServiceBusMessageConsumer.cs
│               ├── Publishers/
│               │   └── ScreenshotEventPublisher.cs
│               ├── Orchestrators/
│               │   └── ScreenshotProcessingOrchestrator.cs
│               └── ServiceBusMessagingFeature.cs
│
└── tests/
	└── ScreenToImageConverter.Tests/
		├── Features/
		│   ├── ScreenshotCapture/
		│   │   ├── CaptureScreenshotHandlerTests.cs
		│   │   └── MockScreenshotProvider.cs
		│   ├── BlobStorageUpload/
		│   │   └── UploadScreenshotHandlerTests.cs
		│   └── ServiceBusMessaging/
		│       ├── ServiceBusMessageConsumerTests.cs
		│       └── ScreenshotEventPublisherTests.cs
		└── Fixtures/
			├── ScreenshotCaptureTestFixture.cs
			└── ServiceBusTestFixture.cs
```

## How the System Works

### 1. Message Flow

```
Service Bus Queue
	  │
	  ├─→ HtmlScreenshotRequest message received
	  │
	  ▼
Worker.ExecuteAsync()
	  │
	  ├─→ Deserialize message
	  ├─→ Create CaptureScreenshotCommand
	  │
	  ▼
ScreenshotProcessingOrchestrator.ProcessAsync()
	  │
	  ├─→ CaptureScreenshotHandler.HandleAsync()
	  │   ├─→ PlaywrightScreenshotProvider.CaptureAsync()
	  │   │   ├─→ Launch browser (if needed)
	  │   │   ├─→ Navigate to URL
	  │   │   ├─→ Capture screenshot
	  │   │   └─→ Return ScreenshotResult
	  │   └─→ Return CaptureScreenshotResult
	  │
	  ├─→ UploadScreenshotHandler.HandleAsync()
	  │   ├─→ IBlobStorageProvider.UploadAsync()
	  │   │   ├─→ Upload to Azure Blob Storage
	  │   │   └─→ Return blob URL
	  │   └─→ Return UploadScreenshotResult
	  │
	  ├─→ ScreenshotEventPublisher.PublishAsync()
	  │   ├─→ Create ScreenshotCompletedEvent
	  │   ├─→ Publish to Service Bus Topic
	  │   └─→ Return PublishResult
	  │
	  └─→ Complete message on queue
```

### 2. Feature Registration

Each feature must be registered in `Program.cs`:

```csharp
// Program.cs
var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
	// Register features
	services.AddScreenshotCaptureFeature(context.Configuration);
	services.AddBlobStorageUploadFeature(context.Configuration);
	services.AddServiceBusMessagingFeature(context.Configuration);

	// Register orchestrator
	services.AddScoped<ScreenshotProcessingOrchestrator>();

	// Register worker
	services.AddHostedService<Worker>();
});

await builder.Build().RunAsync();
```

### 3. Dependency Injection Pattern

Each feature provides an extension method for registration:

```csharp
// In ScreenshotCaptureFeature.cs
public static class ScreenshotCaptureFeature
{
	public static IServiceCollection AddScreenshotCaptureFeature(
		this IServiceCollection services, 
		IConfiguration configuration)
	{
		// Register configuration
		services.Configure<PlaywrightOptions>(
			configuration.GetSection("Playwright"));

		// Register provider (singleton - expensive to initialize)
		services.AddSingleton<IScreenshotProvider, PlaywrightScreenshotProvider>();

		// Register handler (scoped - per request)
		services.AddScoped<CaptureScreenshotHandler>();

		return services;
	}
}
```

## Creating a New Feature

### Step 1: Define the Feature Structure

Create a folder under `Features/YourFeatureName/` with these subdirectories:
- `Commands/` – Input contracts
- `Handlers/` – Business logic
- `Models/` – Output models
- `Interfaces/` – Feature-specific interfaces (if needed)
- `Exceptions/` – Feature-specific exceptions (if needed)

### Step 2: Create Input and Output Models

```csharp
// Features/YourFeature/Commands/MyCommand.cs
public class MyCommand
{
	public string InputData { get; set; }
	public string CorrelationId { get; set; }
}

// Features/YourFeature/Models/MyResult.cs
public class MyResult
{
	public string OutputData { get; set; }
	public DateTime ProcessedAt { get; set; }
}
```

### Step 3: Implement the Handler

```csharp
// Features/YourFeature/Handlers/MyHandler.cs
public class MyHandler
{
	private readonly ILogger<MyHandler> _logger;
	private readonly MyDependency _dependency;

	public MyHandler(ILogger<MyHandler> logger, MyDependency dependency)
	{
		_logger = logger;
		_dependency = dependency;
	}

	public async Task<MyResult> HandleAsync(
		MyCommand command, 
		CancellationToken cancellationToken = default)
	{
		_logger.LogInformation(
			"Processing {CorrelationId}", 
			command.CorrelationId);

		try
		{
			var result = await _dependency.ProcessAsync(
				command.InputData, 
				cancellationToken);

			return new MyResult
			{
				OutputData = result,
				ProcessedAt = DateTime.UtcNow
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex, 
				"Error processing {CorrelationId}", 
				command.CorrelationId);
			throw;
		}
	}
}
```

### Step 4: Create Feature Registration

```csharp
// Features/YourFeature/YourFeatureExtensions.cs
public static class YourFeatureExtensions
{
	public static IServiceCollection AddYourFeature(
		this IServiceCollection services, 
		IConfiguration configuration)
	{
		// Register configuration if needed
		services.Configure<YourOptions>(
			configuration.GetSection("YourFeature"));

		// Register dependencies
		services.AddScoped<MyDependency>();

		// Register handler
		services.AddScoped<MyHandler>();

		return services;
	}
}
```

### Step 5: Register in Program.cs

```csharp
// Program.cs
builder.ConfigureServices((context, services) =>
{
	// ... existing registrations ...
	services.AddYourFeature(context.Configuration);
	// ... remaining registrations ...
});
```

### Step 6: Add Tests

```csharp
// Tests/ScreenToImageConverter.Tests/Features/YourFeature/MyHandlerTests.cs
public class MyHandlerTests
{
	private readonly MyHandler _handler;
	private readonly Mock<MyDependency> _mockDependency;

	public MyHandlerTests()
	{
		_mockDependency = new Mock<MyDependency>();
		_handler = new MyHandler(
			new Mock<ILogger<MyHandler>>().Object,
			_mockDependency.Object);
	}

	[Fact]
	public async Task HandleAsync_WithValidCommand_ReturnsResult()
	{
		// Arrange
		var command = new MyCommand { InputData = "test" };
		_mockDependency
			.Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("processed");

		// Act
		var result = await _handler.HandleAsync(command);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("processed", result.OutputData);
	}
}
```

## Shared Contracts

### Configuration Classes

Located in `ScreenToImageConverter.Shared/Configuration/`:

- **PlaywrightOptions** – Browser automation settings
- **ServiceBusOptions** – Azure Service Bus configuration
- **BlobStorageOptions** – Azure Storage configuration

All options should:
1. Be validated on startup
2. Have sensible defaults
3. Support multiple environments via `appsettings.{Environment}.json`

### Message Contracts

Located in `ScreenToImageConverter.Shared/Messages/`:

- **HtmlScreenshotRequest** – Input message from Service Bus
- **ScreenshotCompletedEvent** – Output event published to Service Bus

These contracts define the public API and should be versioned carefully.

### Interfaces

Located in `ScreenToImageConverter.Shared/Interfaces/`:

- **IScreenshotProvider** – Abstracts browser automation
- **IBlobStorageProvider** – Abstracts cloud storage
- **IMessageConsumer** – Abstracts message consumption
- **IMessagePublisher** – Abstracts message publishing

These enable:
- Testability (mock implementations for unit tests)
- Extensibility (alternative implementations)
- Loose coupling between features

## Error Handling

### Custom Exceptions

All domain exceptions inherit from `ScreenshotProcessingException`:

```csharp
try
{
	// some operation
}
catch (TimeoutException ex)
{
	throw new ScreenshotProcessingException(
		"Screenshot capture timed out after retries", 
		ex);
}
```

### Result Pattern

Use `OperationResult<T>` for operations that may fail:

```csharp
public OperationResult<MyResult> TryDoSomething()
{
	try
	{
		var result = DoSomething();
		return OperationResult<MyResult>.Success(result);
	}
	catch (Exception ex)
	{
		return OperationResult<MyResult>.Failure(ex.Message);
	}
}
```

## Testing Strategy

### Unit Tests

- Mock external dependencies
- Test single handler in isolation
- Use `CancellationToken.None` for non-async tests

### Integration Tests

- Use fixtures for setup/teardown
- Test multiple components together
- Use real or locally-running services (e.g., Azurite for blob storage)

### Test Fixtures

Create base classes for common setup:

```csharp
public class ScreenshotCaptureTestFixture : IAsyncLifetime
{
	protected Mock<IScreenshotProvider> MockProvider { get; set; }
	protected CaptureScreenshotHandler Handler { get; set; }

	public async Task InitializeAsync()
	{
		MockProvider = new Mock<IScreenshotProvider>();
		Handler = new CaptureScreenshotHandler(
			MockProvider.Object,
			new Mock<ILogger<CaptureScreenshotHandler>>().Object);

		await Task.CompletedTask;
	}

	public async Task DisposeAsync()
	{
		// Cleanup
		await Task.CompletedTask;
	}
}
```

## Logging

Use structured logging with Serilog:

```csharp
_logger.LogInformation(
	"Processing screenshot request for {Url} with {CorrelationId}",
	url,
	correlationId);

_logger.LogWarning(
	"Retry attempt {AttemptNumber} for {CorrelationId}",
	attemptNumber,
	correlationId);

_logger.LogError(
	ex,
	"Screenshot capture failed for {CorrelationId}: {ErrorMessage}",
	correlationId,
	ex.Message);
```

Benefits:
- Structured fields are queryable in Application Insights
- Correlation IDs tie related events together
- Correlation context flows through the entire request

## Performance Considerations

### Singleton vs Scoped

- **Singleton**: Browser instance (expensive to create, reuse across requests)
- **Scoped**: Handler (per-request state, proper cleanup)
- **Transient**: Avoid (creates many instances, hard to track)

### Timeout Management

Always implement timeout protection:

```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(timeoutMs);

try
{
	await someOperation(cts.Token);
}
catch (OperationCanceledException)
{
	_logger.LogWarning("Operation timed out after {Milliseconds}ms", timeoutMs);
	throw;
}
```

### Retry Strategy

Implement exponential backoff for transient failures:

```csharp
private async Task<T> RetryAsync<T>(
	Func<Task<T>> operation,
	int maxAttempts = 3)
{
	for (int i = 1; i <= maxAttempts; i++)
	{
		try
		{
			return await operation();
		}
		catch (Exception ex) when (i < maxAttempts && IsTransient(ex))
		{
			var delay = TimeSpan.FromMilliseconds(Math.Pow(2, i) * 100);
			_logger.LogWarning(
				"Attempt {AttemptNumber} failed, retrying after {DelayMs}ms",
				i,
				delay.TotalMilliseconds);
			await Task.Delay(delay, cancellationToken);
		}
	}
}
```

## Code Review Checklist

When reviewing code, ensure:

- ✅ Follows vertical slice architecture
- ✅ DI registration is clean and organized
- ✅ Logging includes correlation IDs
- ✅ Error handling is specific (not catch-all)
- ✅ Tests cover happy path and error cases
- ✅ Documentation is updated
- ✅ Configuration is validated on startup
- ✅ Timeouts are implemented for I/O operations
- ✅ External resources are properly disposed
- ✅ No hardcoded credentials or sensitive data

## Common Patterns

### The Handler Pattern

```csharp
public class MyHandler
{
	// Inject logger and dependencies
	public MyHandler(ILogger<MyHandler> logger, IDependency dep)
	{
		// Store in readonly fields
	}

	// Single public method
	public async Task<MyResult> HandleAsync(MyCommand cmd, CancellationToken ct)
	{
		// Log entry
		// Validate input
		// Call dependency
		// Log result
		// Return result or throw
	}
}
```

### The Feature Registration Pattern

```csharp
public static class MyFeatureExtensions
{
	public static IServiceCollection AddMyFeature(
		this IServiceCollection services,
		IConfiguration config)
	{
		// Configure options
		services.Configure<MyOptions>(config.GetSection("My"));

		// Register services (scope/singleton as needed)
		services.AddScoped<MyHandler>();

		return services;
	}
}
```

## Debugging Tips

1. **Enable Debug Logging**: Set `LogLevel.Default` to `Debug` in `appsettings.Development.json`
2. **Attach Debugger**: Set breakpoints in handlers
3. **Check Correlation ID**: Trace all logs related to a specific request
4. **Inspect Azure Resources**: Use Azure Portal or CLI to verify messages and blobs
5. **Run Tests**: Use xUnit's `[Theory]` for parametrized testing

## Next Steps

- Review existing features under `Features/`
- Study the test files to understand expected behavior
- Create a new feature following the "Creating a New Feature" guide above
- Submit PRs with tests and documentation updates
