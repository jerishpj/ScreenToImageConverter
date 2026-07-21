# Quick Reference

One-page cheat sheet for common tasks.

## Project Structure

```
src/ScreenToImageConverter.Shared/       Contracts & Interfaces
  Configuration/                         PlaywrightOptions, ServiceBusOptions, BlobStorageOptions
  Interfaces/                            IScreenshotProvider, IBlobStorageProvider, IMessageConsumer, IMessagePublisher
  Messages/                              HtmlScreenshotRequest, ScreenshotCompletedEvent
  Exceptions/                            ScreenshotProcessingException
  Results/                               OperationResult<T>

src/ScreenToImageConverter.Worker/       .NET 9 Worker Service
  Features/
	ScreenshotCapture/                   Playwright integration
	BlobStorageUpload/                   Azure Blob Storage upload
	ServiceBusMessaging/                 Service Bus consumer/publisher

tests/ScreenToImageConverter.Tests/      Unit & Integration Tests
```

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run --project src/ScreenToImageConverter.Worker

# Test
dotnet test

# Release build
dotnet build -c Release
```

## Dependency Injection

```csharp
// Feature registration
services.AddScreenshotCaptureFeature(configuration);
services.AddBlobStorageUploadFeature(configuration);
services.AddServiceBusMessagingFeature(configuration);

// Inject into service
public MyService(CaptureScreenshotHandler handler) { }
```

## Screenshot Capture

```csharp
var command = new CaptureScreenshotCommand
{
	Url = "https://example.com",
	ViewportWidth = 1920,
	ViewportHeight = 1080,
	TimeoutMs = 30000,
	CorrelationId = Guid.NewGuid().ToString()
};

var result = await handler.HandleAsync(command, cancellationToken);
// result.ImageData – PNG bytes
// result.ImageSizeBytes – File size
// result.CapturedAt – Timestamp
```

## Configuration

```json
{
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1920,
	"DefaultViewportHeight": 1080,
	"DefaultTimeoutMs": 30000,
	"MaxRetryAttempts": 2,
	"DisableSandbox": false
  },
  "AzureServiceBus": {
	"ConnectionString": "Endpoint=sb://...;",
	"QueueName": "screenshot-requests"
  },
  "AzureBlobStorage": {
	"ConnectionString": "DefaultEndpointsProtocol=https;...",
	"ContainerName": "screenshots"
  }
}
```

Override with environment variables:
```bash
export Playwright__DefaultTimeoutMs=45000
export AzureServiceBus__QueueName=my-queue
```

## Service Bus Message

```json
{
  "url": "https://example.com",
  "viewportWidth": 1920,
  "viewportHeight": 1080,
  "timeoutMs": 30000,
  "correlationId": "unique-id"
}
```

## Logging

```csharp
_logger.LogInformation("Processing {CorrelationId}", correlationId);
_logger.LogError(ex, "Failed to process {CorrelationId}", correlationId);
```

Set log level:
```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning"
	}
  }
}
```

## Testing

```csharp
// Mock provider
var mockProvider = new Mock<IScreenshotProvider>();
mockProvider
	.Setup(x => x.CaptureAsync(It.IsAny<string>(), ...))
	.ReturnsAsync(new ScreenshotResult { ... });

// Create handler with mock
var handler = new CaptureScreenshotHandler(
	mockProvider.Object,
	loggerMock.Object);

// Test
var result = await handler.HandleAsync(command);
Assert.NotNull(result);
```

## Create a New Feature

1. Create folder: `Features/YourFeatureName/`
2. Add subdirectories: `Commands/`, `Handlers/`, `Models/`
3. Implement command and handler
4. Create `YourFeatureExtensions.cs` for DI registration:
   ```csharp
   public static class YourFeatureExtensions
   {
	   public static IServiceCollection AddYourFeature(
		   this IServiceCollection services, IConfiguration config)
	   {
		   services.Configure<YourOptions>(config.GetSection("Your"));
		   services.AddScoped<YourHandler>();
		   return services;
	   }
   }
   ```
5. Register in `Program.cs`: `services.AddYourFeature(configuration);`
6. Add tests in `tests/ScreenToImageConverter.Tests/Features/YourFeatureName/`

## Health Checks

```csharp
// All health checks are registered in Program.cs
// Endpoint: http://localhost:5000/health
```

## Common Issues

| Issue | Solution |
|-------|----------|
| Browser not launching | Ensure Playwright is installed: `playwright install` |
| Connection timeout | Check connection string, firewall, Service Bus connectivity |
| Blob upload fails | Verify container exists, check storage account credentials |
| Tests timing out | Increase `DefaultTimeoutMs` in configuration |
| "Sandbox" errors in Docker | Set `DisableSandbox: true` in production config |

## Useful Commands

```bash
# Restore NuGet packages
dotnet restore

# Clean build
dotnet clean
dotnet build

# Run tests with output
dotnet test --logger:console --verbosity:detailed

# Run specific test
dotnet test --filter "MethodName"

# Format code
dotnet format

# List NuGet packages
dotnet list package

# Update packages
dotnet add package <PackageName> --version <Version>

# Publish for deployment
dotnet publish -c Release -o ./publish

# Install Playwright browsers
playwright install
```

## Azure CLI Commands

```bash
# Create Service Bus queue
az servicebus queue create \
  --namespace-name my-ns \
  --name screenshot-requests \
  --resource-group my-rg

# Create Storage container
az storage container create \
  --account-name myaccount \
  --name screenshots

# Get connection string
az servicebus namespace authorization-rule keys list \
  --namespace-name my-ns \
  --name RootManageSharedAccessKey

az storage account show-connection-string \
  --name myaccount \
  --resource-group my-rg

# Assign Managed Identity role
az role assignment create \
  --role "Azure Service Bus Data Owner" \
  --assignee-object-id <principal-id> \
  --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.ServiceBus/namespaces/<ns>
```

## Documentation Map

| Document | When to Read |
|----------|--------------|
| [README.md](../README.md) | Project overview, features, quick start |
| [GETTING_STARTED.md](./GETTING_STARTED.md) | Setup and first run |
| [DEVELOPMENT.md](./DEVELOPMENT.md) | Architecture, extending features |
| [CONFIGURATION.md](./CONFIGURATION.md) | All configuration options |
| [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) | Cheat sheet (this file) |

## Key Patterns

### Handler Pattern
```csharp
public class MyHandler
{
	public async Task<MyResult> HandleAsync(MyCommand cmd, CancellationToken ct)
	{
		// Process cmd, return result
	}
}
```

### Feature Registration Pattern
```csharp
services.AddScoped<MyHandler>();
services.Configure<MyOptions>(config.GetSection("My"));
```

### Error Handling Pattern
```csharp
try
{
	// operation
}
catch (Exception ex)
{
	_logger.LogError(ex, "Error");
	throw new DomainException("Message", ex);
}
```

## Vertical Slice Architecture Benefits

✅ Self-contained features – each feature has all code it needs  
✅ Independent teams – no conflicts between slices  
✅ Easy to test – mock dependencies at feature level  
✅ Easy to extend – add new feature without touching others  
✅ Clear boundaries – contracts define feature interface  

## Performance Tips

- Browser instance is singleton (reused across requests)
- Use appropriate timeout values (don't set too high)
- Implement retry logic for transient failures
- Monitor resource usage in production
- Use Managed Identity instead of connection strings

## Production Checklist

- ✅ Configuration validated on startup
- ✅ Logging configured for production
- ✅ Connection strings in Key Vault or Managed Identity
- ✅ Health checks passing
- ✅ Tests all passing
- ✅ Build successful with no warnings
- ✅ Application Insights configured
- ✅ Firewall rules configured
- ✅ Monitoring and alerts set up
- ✅ Backup strategy in place

## Debugging

```bash
# Enable debug logging
# Set LogLevel.Default to "Debug" in appsettings.Development.json

# Attach debugger
# Set breakpoint and run: dotnet run

# View correlation ID in logs
# grep "CorrelationId" application.log

# Check Message
# az servicebus queue peek --namespace-name ... --name ...
```

## Links

- [Microsoft Playwright .NET](https://playwright.dev/dotnet/)
- [Azure Service Bus](https://docs.microsoft.com/en-us/azure/service-bus-messaging/)
- [Azure Storage](https://docs.microsoft.com/en-us/azure/storage/)
- [.NET 9](https://docs.microsoft.com/en-us/dotnet/)
- [GitHub Repository](https://github.com/jerishpj/ScreenToImageConverter)
