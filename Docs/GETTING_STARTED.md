# Getting Started with ScreenToImageConverter

## Prerequisites

- .NET 9 SDK or later
- Visual Studio 2022/2026 (optional, but recommended)
- Azure Storage Account
- Azure Service Bus Namespace
- Git (to clone the repository)

## Installation

### 1. Clone the Repository
```bash
git clone https://github.com/jerishpj/ScreenToImageConverter.git
cd ScreenToImageConverter
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Build the Solution
```bash
dotnet build
```

Verify: All projects should compile with zero warnings and errors.

## Configuration

### Development Environment Setup

1. **Update `appsettings.Development.json`:**

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
	"ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY",
	"QueueName": "screenshot-requests"
  },
  "AzureBlobStorage": {
	"ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
	"ContainerName": "screenshots"
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning"
	}
  }
}
```

2. **Create Azure Resources** (if not already done):

#### Azure Storage Account
```bash
az storage account create \
  --name screentoimageconverter \
  --resource-group my-resource-group \
  --location eastus
```

#### Azure Service Bus
```bash
az servicebus namespace create \
  --name screentoimageconverter-ns \
  --resource-group my-resource-group \
  --location eastus

# Create queue for receiving requests
az servicebus queue create \
  --namespace-name screentoimageconverter-ns \
  --name screenshot-requests \
  --resource-group my-resource-group

# Create topic for publishing results
az servicebus topic create \
  --namespace-name screentoimageconverter-ns \
  --name screenshot-events \
  --resource-group my-resource-group
```

### Production Configuration

For production, use `appsettings.Production.json`:
- Use **Managed Identity** instead of connection strings
- Set `DisableSandbox: true` for Docker
- Increase retry attempts to 3
- Set timeouts appropriately for your URLs
- Use Application Insights for monitoring

Example with Managed Identity:
```json
{
  "Playwright": {
	"BrowserType": "chromium",
	"DisableSandbox": true,
	"MaxRetryAttempts": 3
  }
}
```

Configure App Service Managed Identity to access:
- Storage Account (Storage Blob Data Contributor)
- Service Bus Namespace (Azure Service Bus Data Owner)

## Running the Application

### Development Mode
```bash
dotnet run --project src/ScreenToImageConverter.Worker
```

The worker will start and listen for messages on your configured Service Bus queue.

### With Environment Variable
```bash
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project src/ScreenToImageConverter.Worker
```

### In Visual Studio
1. Set `ScreenToImageConverter.Worker` as startup project
2. Press `F5` or click the Run button
3. Watch the console output for startup logs

## First Screenshot Capture

### Option 1: Send a Service Bus Message

```bash
# Using Azure CLI to peek at the queue
az servicebus queue show \
  --namespace-name screentoimageconverter-ns \
  --name screenshot-requests \
  --resource-group my-resource-group
```

### Option 2: Direct Code Example

From another .NET application or integration test:

```csharp
using ScreenToImageConverter.Shared.Messages;
using ScreenToImageConverter.Shared.Interfaces;
using Azure.Messaging.ServiceBus;

// Publisher (sends request)
var client = new ServiceBusClient("your-connection-string");
var sender = client.CreateSender("screenshot-requests");

var request = new HtmlScreenshotRequest
{
	Url = "https://www.example.com",
	ViewportWidth = 1920,
	ViewportHeight = 1080,
	TimeoutMs = 30000,
	CorrelationId = Guid.NewGuid().ToString()
};

var message = new ServiceBusMessage(JsonSerializer.Serialize(request));
await sender.SendMessageAsync(message);

Console.WriteLine($"Message sent with CorrelationId: {request.CorrelationId}");
```

### Option 3: Use Test Project

```bash
dotnet test --project tests/ScreenToImageConverter.Tests
```

Run the handler tests to see screenshot capture in action:
```bash
dotnet test --project tests/ScreenToImageConverter.Tests --filter "CaptureScreenshotHandlerTests"
```

## Verifying Setup

### 1. Check Connectivity

```bash
# Test Service Bus connection
az servicebus queue show \
  --namespace-name screentoimageconverter-ns \
  --name screenshot-requests

# Test Storage connection
az storage account show \
  --name screentoimageconverter \
  --resource-group my-resource-group
```

### 2. Check Logs

When running the application, you should see:
```
[INF] ScreenToImageConverter Worker starting...
[INF] PlaywrightScreenshotProvider initialized
[INF] Worker listening for messages on queue: screenshot-requests
[INF] Health check endpoint ready
```

### 3. Run Tests

```bash
dotnet test
```

Expected output:
- 52 tests passing
- 5 known failures in ServiceBusMessageConsumer/EventPublisher tests (to be addressed)

## Troubleshooting

### Issue: "Connection refused" to Service Bus

**Solution:**
- Verify connection string in `appsettings.json`
- Ensure Service Bus namespace exists and is accessible
- Check firewall rules if using IP restrictions

### Issue: "Storage account not found"

**Solution:**
- Verify storage account name and connection string
- Ensure the container `screenshots` exists

### Issue: Playwright browser not launching

**Solution:**
- Ensure .NET dependencies are installed (Playwright will auto-download on first run)
- For Docker deployments, set `DisableSandbox: true`
- Check available disk space

### Issue: Tests timing out

**Solution:**
- Increase `DefaultTimeoutMs` in configuration
- Check network connectivity
- Ensure `https://www.example.com` (or your test URL) is accessible

## Next Steps

1. ✅ Review [ARCHITECTURE.md](../ARCHITECTURE.md) to understand the design
2. ✅ Explore [FEATURES.md](./FEATURES.md) for detailed feature documentation
3. ✅ Read [CONFIGURATION.md](./CONFIGURATION.md) for all configuration options
4. ✅ Review the test files to understand expected behavior
5. ✅ Deploy to staging environment

## Additional Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [Azure Service Bus Documentation](https://docs.microsoft.com/en-us/azure/service-bus-messaging/)
- [Azure Storage Documentation](https://docs.microsoft.com/en-us/azure/storage/)
- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/)

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review logs in the console output
3. Search existing GitHub issues
4. Create a new issue with logs and configuration details
