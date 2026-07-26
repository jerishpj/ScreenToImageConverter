# ScreenToImageConverter - Technical Reference

Complete technical reference covering architecture, features, API contracts, and system design.

---

## Table of Contents

1. [Architecture & Design](#architecture--design)
2. [Features & Functional Flows](#features--functional-flows)
3. [API Reference](#api-reference)
4. [Security Architecture](#security-architecture)

---

# Architecture & Design

## 🏗️ Architectural Pattern: Vertical Slice

ScreenToImageConverter uses **Vertical Slice Architecture**, organizing code by feature rather than technical layers.

### Traditional Layered vs Vertical Slice

```
TRADITIONAL LAYERED          VERTICAL SLICE
(by Technical Concern)        (by Feature)

Controllers/                  Feature: ConvertHtmlToImage
  Screenshot                    ├─ Command
  Blob                          ├─ Handler
  ServiceBus                    ├─ Validator
								├─ Response
Services/                       ├─ Interfaces
  Screenshot                    └─ Exceptions
  Blob
  ServiceBus                Infrastructure (Shared):
							  ├─ Notifications
Data/                         ├─ Screenshots
  Screenshot                  ├─ Storage
  Blob                        └─ Resilience
  ServiceBus
```

### Benefits

✅ Self-contained – All feature code in one place
✅ Easy to understand – Less jumping between files
✅ Isolated changes – Modify feature without touching others
✅ Minimal coupling – Shared infrastructure only
✅ Team scalability – Different teams can own features
✅ Testing – Feature can be tested independently

## 📁 Project Structure

```
ScreenToImageConverter/
│
├─ src/ScreenToImageConverter.Worker/
│  │
│  ├─ Features/ConvertHtmlToImage/
│  │  ├─ ConvertHtmlToImageCommand.cs
│  │  ├─ ConvertHtmlToImageHandler.cs
│  │  ├─ HtmlRequestValidator.cs
│  │  ├─ ImageMetadataResponse.cs
│  │  ├─ OperationResult.cs
│  │  └─ ScreenshotProcessingException.cs
│  │
│  ├─ Infrastructure/
│  │  ├─ Notifications/
│  │  │  ├─ IMessageConsumer.cs
│  │  │  ├─ IMessagePublisher.cs
│  │  │  ├─ ServiceBusConsumer.cs
│  │  │  ├─ RabbitMqConsumer.cs
│  │  │  ├─ HtmlScreenshotRequest.cs
│  │  │  ├─ ScreenshotCompletedEvent.cs
│  │  │  ├─ RabbitMqOptions.cs
│  │  │  └─ ServiceBusOptions.cs
│  │  │
│  │  ├─ Screenshots/
│  │  │  ├─ IScreenshotProvider.cs
│  │  │  ├─ PlaywrightScreenshotProvider.cs
│  │  │  ├─ PlaywrightOptions.cs
│  │  │  └─ PlaywrightInstaller.cs
│  │  │
│  │  ├─ Storage/
│  │  │  ├─ IBlobStorageService.cs
│  │  │  ├─ BlobStorageService.cs
│  │  │  ├─ StorageSettings.cs
│  │  │  └─ BlobStorageProvider.cs
│  │  │
│  │  ├─ Resilience/
│  │  │  ├─ RabbitMqConnectionPolicy.cs
│  │  │  └─ StartupDiagnostics.cs
│  │  │
│  │  └─ Exceptions/
│  │     └─ ScreenshotProcessingExceptions.cs
│  │
│  ├─ AppSettings/
│  │  ├─ PlaywrightOptions.cs
│  │  ├─ RabbitMqOptions.cs
│  │  ├─ ServiceBusOptions.cs
│  │  └─ StorageSettings.cs
│  │
│  ├─ Extensions/
│  │  ├─ ServiceCollectionExtensions.cs
│  │  └─ HealthCheckExtensions.cs
│  │
│  ├─ Program.cs
│  ├─ Worker.cs
│  ├─ appsettings.json
│  └─ appsettings.Development.json
│
├─ tests/ScreenToImageConverter.Tests/
│  ├─ Builders/
│  │  └─ HtmlScreenshotRequestBuilder.cs
│  ├─ Factories/
│  │  └─ TestDataFactory.cs
│  ├─ Fixtures/
│  │  ├─ MockScreenshotProvider.cs
│  │  ├─ MockMessageConsumer.cs
│  │  └─ MockBlobStorageProvider.cs
│  └─ Unit|Integration/
│
└─ Docs/
```

## 🔄 Message Flow Sequence

```
Service Bus Topic                Worker Service              Response Topic
	│                              │                            │
	├─ HtmlScreenshotRequest      │                            │
	├────────────────────────────→ ServiceBusConsumer          │
	│                              │                            │
	│                              ├─ Deserialize              │
	│                              ├─ Route to handler         │
	│                              │                            │
	│                              └─ ProcessMessageAsync()    │
	│                                   │                       │
	│                                   ├─ Validate request    │
	│                                   ├─ Capture screenshot  │
	│                                   ├─ Upload to blob      │
	│                                   └─ Publish event       │
	│                              │                            │
	│                              └──────────────────────────→ ScreenshotCompletedEvent
```

## 🔌 Interfaces & Abstractions

### IMessageConsumer
Receives messages from broker:

```csharp
public interface IMessageConsumer
{
	void RegisterMessageHandler(Func<HtmlScreenshotRequest, string, CancellationToken, Task> handler);
	Task StartAsync(CancellationToken cancellationToken);
	Task StopAsync(CancellationToken cancellationToken);
	bool IsConnected { get; }
}
```

### IMessagePublisher
Publishes completion events:

```csharp
public interface IMessagePublisher
{
	Task PublishAsync<T>(T message, string correlationId, CancellationToken cancellationToken) where T : class;
}
```

### IScreenshotProvider
Captures screenshots:

```csharp
public interface IScreenshotProvider
{
	Task InitializeAsync(CancellationToken cancellationToken);
	Task<byte[]> CaptureScreenshotAsync(
		string url, 
		int? viewportWidth, 
		int? viewportHeight, 
		int? timeoutMs, 
		CancellationToken cancellationToken);
	Task ShutdownAsync();
}
```

### IBlobStorageService
Manages blob storage:

```csharp
public interface IBlobStorageService
{
	Task<BlobUploadResult> UploadAsync(
		string containerName, 
		string blobName, 
		byte[] data, 
		string contentType, 
		CancellationToken cancellationToken);

	Task<string> GenerateSasUrlAsync(
		string containerName, 
		string blobName, 
		int expirationMinutes, 
		CancellationToken cancellationToken);
}
```

## 🧩 Key Design Decisions

| Decision | Rationale | Trade-off |
|----------|-----------|-----------|
| Vertical Slice | Better cohesion, easier to understand | Duplicate interfaces possible |
| BackgroundService | Built-in lifecycle management | Limited to background tasks |
| Options Pattern | Strong typing, validation | Requires restart for changes |
| Factory Pattern | Testability, loose coupling | More abstractions |
| Async/Await | Better scalability | More complex error handling |
| Fire-and-Forget Publishing | Reduced blocking time | Events may not be published if crash |
| Message Handler Pattern | Loose coupling | Indirection layer |

## 🔄 Dependency Injection Setup

Configured in `ServiceCollectionExtensions.cs`:

```csharp
public static void AddApplicationConfiguration(IServiceCollection services, IConfiguration config)
{
	// Configuration
	services.Configure<PlaywrightOptions>(config.GetSection("Playwright"));
	services.Configure<ServiceBusOptions>(config.GetSection("ServiceBus"));
	services.Configure<StorageSettings>(config.GetSection("BlobStorage"));

	// Infrastructure
	services.AddSingleton<IScreenshotProvider, PlaywrightScreenshotProvider>();
	services.AddSingleton<IBlobStorageService, BlobStorageService>();

	// Messaging (Service Bus by default, RabbitMQ in development)
	if (env.IsDevelopment())
	{
		services.AddScoped<IMessageConsumer, RabbitMqConsumer>();
	}
	else
	{
		services.AddScoped<IMessageConsumer, ServiceBusConsumer>();
	}

	// Feature handler
	services.AddScoped<ConvertHtmlToImageHandler>();

	// Worker
	services.AddHostedService<Worker>();
}
```

## 🔒 Concurrency Model

**Message Processing:**
- Single-threaded per subscription (configurable)
- Independent message processing
- No shared state between messages
- Thread-safe by design

**Browser Context:**
- One browser instance per service
- Multiple contexts for concurrent requests
- Each context isolated
- Browser is singleton (expensive to create)

**Blob Storage:**
- Async operations with Task-based concurrency
- Multiple concurrent uploads supported
- Storage service handles batching

## 🚀 Scalability Approach

**Horizontal Scaling:**
- Deploy multiple instances
- Each subscribes to same Service Bus subscription
- Service Bus distributes round-robin
- Linear throughput increase

**Vertical Scaling:**
- Increase `MaxConcurrentCalls`
- More browser contexts
- More concurrent requests per instance

**Optimization:**
- Resource pooling (browser reuse)
- Async I/O (non-blocking)
- Message batching
- Caching (SAS URL generation)

## 🏥 Health Check Architecture

Three health checks provided:

| Check | Purpose | Validates |
|-------|---------|-----------|
| PlaywrightHealthCheck | Browser readiness | Browser running, can create context |
| BlobStorageHealthCheck | Storage readiness | Connection, container access, SAS generation |
| ConfigurationHealthCheck | Configuration validity | All required settings present, valid values |

## 🔄 Error Handling Architecture

**Three-layer error handling:**

1. **Input Validation Layer** (Fast fail)
   - Validate before processing
   - Prevent poison pills
   - Return immediate feedback

2. **Processing Layer** (Retry logic)
   - Capture failures → retry
   - Storage failures → message requeue
   - Timeout → graceful failure

3. **Service Bus Layer** (Dead letter)
   - Max retries exceeded → dead letter
   - Poison pill → dead letter
   - Corrupted message → dead letter

---

# Features & Functional Flows

## 🎯 Feature: HTML to Image Conversion

Convert web pages (HTML URLs) to PNG screenshot images.

## 📋 Request Flow

### Step 1: Message Arrival

Message arrives on Service Bus topic: `html-screenshot-requests`

```json
{
  "requestId": "req-123",
  "url": "https://www.example.com",
  "viewportWidth": 1920,
  "viewportHeight": 1080,
  "timeoutMs": 30000,
  "waitForPageLoad": true
}
```

### Step 2: Consumer Deserialization

`ServiceBusConsumer` receives and deserializes:
- Validates JSON structure
- Converts to `HtmlScreenshotRequest` object
- Routes to registered message handler

### Step 3: Handler Processing

`ConvertHtmlToImageHandler` processes:

```
┌─────────────────────────────────────┐
│ Validation Phase                    │
│ ├─ URL format valid?                │
│ ├─ Viewport dimensions valid?       │
│ └─ Timeout reasonable?              │
└─────────────────────────────────────┘
				 │
				 ▼
┌─────────────────────────────────────┐
│ Screenshot Capture Phase            │
│ ├─ Launch browser context           │
│ ├─ Navigate to URL                  │
│ ├─ Wait for page load               │
│ ├─ Capture screenshot (PNG)         │
│ └─ Close context                    │
└─────────────────────────────────────┘
				 │
				 ▼
┌─────────────────────────────────────┐
│ Blob Upload Phase                   │
│ ├─ Upload PNG to blob storage       │
│ ├─ Generate SAS URL (60 min expiry) │
│ └─ Extract metadata                 │
└─────────────────────────────────────┘
				 │
				 ▼
┌─────────────────────────────────────┐
│ Event Publishing Phase              │
│ ├─ Create completion event          │
│ ├─ Publish to response topic        │
│ └─ Acknowledge original message     │
└─────────────────────────────────────┘
```

### Step 4: Event Publishing

`ScreenshotCompletedEvent` published to: `screenshot-completed-events`

```json
{
  "requestId": "req-123",
  "status": "Success",
  "blobUri": "https://account.blob.core.windows.net/screenshots/2024/01/15/req-123_143022_456.png",
  "blobSasUrl": "https://account.blob.core.windows.net/screenshots/2024/01/15/req-123_143022_456.png?sv=...",
  "processingDurationMs": 2450,
  "screenshotWidth": 1920,
  "screenshotHeight": 2080,
  "screenshotSizeBytes": 256000,
  "errorMessage": null,
  "timestamp": "2024-01-15T14:30:25Z"
}
```

## 📊 Data Models

### HtmlScreenshotRequest (Inbound)

```csharp
public class HtmlScreenshotRequest
{
	public string RequestId { get; set; }           // Unique request ID
	public string Url { get; set; }                 // Target URL
	public int? ViewportWidth { get; set; }         // Viewport width (default: 1920)
	public int? ViewportHeight { get; set; }        // Viewport height (default: 1080)
	public int? TimeoutMs { get; set; }             // Timeout in ms (default: 30000)
	public bool WaitForPageLoad { get; set; }       // Wait for page load?
}
```

### ScreenshotCompletedEvent (Outbound)

```csharp
public class ScreenshotCompletedEvent
{
	public string RequestId { get; set; }           // Original request ID
	public string Status { get; set; }              // Success or Error
	public string? BlobUri { get; set; }            // Full blob path
	public string? BlobSasUrl { get; set; }         // Time-limited download URL
	public int ProcessingDurationMs { get; set; }   // Processing time
	public int? ScreenshotWidth { get; set; }       // Actual width
	public int? ScreenshotHeight { get; set; }      // Actual height
	public long? ScreenshotSizeBytes { get; set; }  // File size
	public string? ErrorMessage { get; set; }       // If failed
	public DateTime Timestamp { get; set; }         // When completed
}
```

## ❌ Error Handling

### Error Categories

| Category | Cause | Recovery |
|----------|-------|----------|
| InvalidRequest | Bad URL, invalid viewport | Reject, dead letter |
| TimeoutError | Page load too slow | Retry (3 attempts) |
| ScreenshotError | Browser context error | Retry (3 attempts) |
| StorageError | Upload failed | Retry + requeue message |
| UnexpectedError | Unhandled exception | Log, requeue message |

### Retry Strategy

```
Attempt 1 (immediate)
	├─ Failed? → Delay 1s, Attempt 2
	│
Attempt 2 (1s delay)
	├─ Failed? → Delay 2s, Attempt 3
	│
Attempt 3 (2s delay)
	├─ Failed? → Dead letter message
```

### Dead Letter Queue

Messages moved to dead-letter when:
- Max retries exceeded
- Validation fails (poison pill)
- Corrupted message format

## 🔌 Integration Points

### Inbound (Receive)
- **Service Bus Topic**: `html-screenshot-requests`
- **Subscription**: `screenshot-worker-subscription`
- **Format**: JSON (HtmlScreenshotRequest)

### Outbound (Send)
- **Service Bus Topic**: `screenshot-completed-events`
- **Format**: JSON (ScreenshotCompletedEvent)
- **Subscribers**: Any external system can listen

### Blob Storage
- **Container**: `screenshots`
- **Path**: `screenshots/YYYY/MM/DD/{RequestId}_{timestamp}_{millis}.png`
- **Access**: SAS URL (read-only, 60 min expiry)

---

# API Reference

Complete message contracts and integration examples.

## 📨 Message Schemas

### Request Message: HtmlScreenshotRequest

**Topic**: `html-screenshot-requests`
**Format**: JSON
**Partition Key**: RequestId (for ordered processing)

**Full Schema:**

```json
{
  "requestId": "string (required, unique identifier, max 256 chars)",
  "url": "string (required, valid HTTP/HTTPS URL, max 2048 chars)",
  "viewportWidth": "integer (optional, 320-3840, default 1920)",
  "viewportHeight": "integer (optional, 240-2160, default 1080)",
  "timeoutMs": "integer (optional, 5000-120000, default 30000)",
  "waitForPageLoad": "boolean (optional, default true)"
}
```

**Validation Rules:**
- `requestId`: Non-empty, unique within 24 hours
- `url`: Valid HTTP/HTTPS URL
- `viewportWidth`: Between 320 and 3840 pixels
- `viewportHeight`: Between 240 and 2160 pixels
- `timeoutMs`: Between 5 and 120 seconds
- `waitForPageLoad`: If true, waits for network idle

**Example:**

```json
{
  "requestId": "order-123-screenshot",
  "url": "https://www.example.com/product/SKU-456",
  "viewportWidth": 1920,
  "viewportHeight": 1080,
  "timeoutMs": 30000,
  "waitForPageLoad": true
}
```

### Response Message: ScreenshotCompletedEvent

**Topic**: `screenshot-completed-events`
**Format**: JSON
**Correlation ID**: RequestId (from original request)

**Full Schema:**

```json
{
  "requestId": "string (same as request)",
  "status": "string (Success or Error)",
  "blobUri": "string (full path to screenshot blob)",
  "blobSasUrl": "string (time-limited download URL, 60 min expiry)",
  "processingDurationMs": "integer (milliseconds)",
  "screenshotWidth": "integer (actual width in pixels)",
  "screenshotHeight": "integer (actual height in pixels)",
  "screenshotSizeBytes": "integer (file size in bytes)",
  "errorMessage": "string (if status is Error)",
  "timestamp": "string (ISO 8601 timestamp)"
}
```

**Status Values:**
- `Success` – Screenshot captured and uploaded
- `Error` – Failed after 3 retries

**Example Success:**

```json
{
  "requestId": "order-123-screenshot",
  "status": "Success",
  "blobUri": "https://storage.blob.core.windows.net/screenshots/2024/01/15/order-123-screenshot_143022_456.png",
  "blobSasUrl": "https://storage.blob.core.windows.net/screenshots/2024/01/15/order-123-screenshot_143022_456.png?sv=2021-06-08&sig=...",
  "processingDurationMs": 2450,
  "screenshotWidth": 1920,
  "screenshotHeight": 2080,
  "screenshotSizeBytes": 256000,
  "errorMessage": null,
  "timestamp": "2024-01-15T14:30:25Z"
}
```

**Example Error:**

```json
{
  "requestId": "order-123-screenshot",
  "status": "Error",
  "blobUri": null,
  "blobSasUrl": null,
  "processingDurationMs": 35000,
  "screenshotWidth": null,
  "screenshotHeight": null,
  "screenshotSizeBytes": null,
  "errorMessage": "Timeout waiting for page load after 30000ms",
  "timestamp": "2024-01-15T14:30:55Z"
}
```

## 💻 SDK Integration Examples

### C# (.NET)

**Send Request:**
```csharp
using Azure.Messaging.ServiceBus;
using System.Text.Json;

var client = new ServiceBusClient("your-namespace.servicebus.windows.net");
var sender = client.CreateSender("html-screenshot-requests");

var request = new
{
	requestId = "my-request-001",
	url = "https://www.example.com",
	viewportWidth = 1920,
	viewportHeight = 1080,
	timeoutMs = 30000,
	waitForPageLoad = true
};

var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(request));
message.CorrelationId = request.requestId;
await sender.SendMessageAsync(message);
```

**Receive Response:**
```csharp
var receiver = client.CreateReceiver("screenshot-completed-events", 
	new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

var message = await receiver.ReceiveMessageAsync();
if (message != null)
{
	var response = JsonSerializer.Deserialize<dynamic>(message.Body);
	Console.WriteLine($"Status: {response.status}");
	Console.WriteLine($"Blob URL: {response.blobSasUrl}");
	await receiver.CompleteMessageAsync(message);
}
```

### Python

**Send Request:**
```python
from azure.servicebus import ServiceBusClient, ServiceBusMessage
import json

client = ServiceBusClient.from_connection_string("<connection_string>")
sender = client.get_topic_sender("html-screenshot-requests")

request = {
	"requestId": "my-request-001",
	"url": "https://www.example.com",
	"viewportWidth": 1920,
	"viewportHeight": 1080,
	"timeoutMs": 30000,
	"waitForPageLoad": True
}

message = ServiceBusMessage(
	body=json.dumps(request).encode('utf-8'),
	correlation_id=request['requestId']
)
sender.send_messages(message)
```

**Receive Response:**
```python
receiver = client.get_subscription_receiver("screenshot-completed-events", 
											"my-subscription")

for message in receiver:
	response = json.loads(str(message))
	print(f"Status: {response['status']}")
	print(f"Blob URL: {response['blobSasUrl']}")
	receiver.complete_message(message)
```

### Node.js

**Send Request:**
```javascript
const { ServiceBusClient } = require("@azure/service-bus");

const client = new ServiceBusClient("<connection-string>");
const sender = client.createSender("html-screenshot-requests");

const request = {
	requestId: "my-request-001",
	url: "https://www.example.com",
	viewportWidth: 1920,
	viewportHeight: 1080,
	timeoutMs: 30000,
	waitForPageLoad: true
};

const message = {
	body: JSON.stringify(request),
	correlationId: request.requestId
};

await sender.sendMessages(message);
```

**Receive Response:**
```javascript
const receiver = client.createReceiver("screenshot-completed-events", 
									   "my-subscription");

const messages = await receiver.receiveMessages(1);
for (const message of messages) {
	const response = JSON.parse(message.body);
	console.log(`Status: ${response.status}`);
	console.log(`Blob URL: ${response.blobSasUrl}`);
	await receiver.completeMessage(message);
}
```

### Bash (Azure CLI)

**Send Request:**
```bash
# Create request JSON
cat > request.json <<EOF
{
  "requestId": "my-request-001",
  "url": "https://www.example.com",
  "viewportWidth": 1920,
  "viewportHeight": 1080,
  "timeoutMs": 30000,
  "waitForPageLoad": true
}
EOF

# Send via Service Bus
az servicebus topic send \
  --namespace-name your-namespace \
  --topic-name html-screenshot-requests \
  --body "@request.json"
```

## 📊 Blob Storage Details

### Path Structure

```
screenshots/YYYY/MM/DD/{RequestId}_{HHmmss}_{millis}.png
```

**Example:**
```
screenshots/2024/01/15/order-123_143022_456.png
```

**Components:**
- `YYYY` – Year (2024)
- `MM` – Month (01-12)
- `DD` – Day (01-31)
- `RequestId` – Original request ID
- `HHmmss` – Hour, minute, second
- `millis` – Milliseconds for uniqueness

### SAS URL Format

```
https://account.blob.core.windows.net/container/path/blob.png?sv=2021-06-08&ss=b&srt=sco&sp=rwdlac&se=2024-01-15T15:30:25Z&st=2024-01-15T14:30:25Z&spr=https&sig=...
```

**Parameters:**
- `sv` – Service version
- `sp` – Permissions (Read, Write, Delete, etc.)
- `se` – Signature expiration (60 minutes from creation)
- `st` – Signature start time
- `sig` – HMAC-SHA256 signature

### Blob Metadata

Metadata stored in blob properties:

```
x-ms-meta-RequestId: order-123
x-ms-meta-CaptureTimestamp: 2024-01-15T14:30:22Z
x-ms-meta-ViewportWidth: 1920
x-ms-meta-ViewportHeight: 1080
```

---

# Security Architecture

## 🔐 Authentication

### Managed Identity (Production - Recommended)

No credentials needed. Azure handles authentication:

```csharp
// Automatically uses system-assigned managed identity
var credential = new DefaultAzureCredential();
var blobClient = new BlobContainerClient(new Uri(containerUri), credential);
var serviceBusClient = new ServiceBusClient("ns.servicebus.windows.net", credential);
```

### Connection String (Development Only)

```csharp
var blobClient = new BlobContainerClient(connectionString, containerName);
var serviceBusClient = new ServiceBusClient(connectionString);
```

## 👤 Authorization (RBAC)

### Required Roles

**For Blob Storage:**
- `Storage Blob Data Contributor` – Upload/read blobs

**For Service Bus:**
- `Azure Service Bus Data Owner` – Send/receive messages

### Role Assignment

```bash
# For managed identity
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee-object-id <identity-object-id> \
  --scope /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Storage/storageAccounts/{account}

az role assignment create \
  --role "Azure Service Bus Data Owner" \
  --assignee-object-id <identity-object-id> \
  --scope /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.ServiceBus/namespaces/{namespace}
```

## 🔗 Data Protection

### SAS URLs

Screenshot URLs are read-only, time-limited:

```
Expiration: 60 minutes from generation
Permissions: Read only (no delete/modify)
Protocol: HTTPS only
```

### Blob Storage Access

```json
{
  "BlobStorage": {
	"ContainerName": "screenshots",
	"UseManagedIdentity": true,
	"SasUrlExpirationMinutes": 60,
	"AutoCreateContainer": true
  }
}
```

### Message Encryption

Service Bus messages encrypted in transit:
- HTTPS for metadata
- Transport Layer Security (TLS)
- Encrypted at rest

## 🚫 Sensitive Data Handling

### In Logs

Sensitive data is **never** logged:
- Connection strings ❌
- Storage account keys ❌
- Request URLs (unless explicitly enabled) ⚠️
- SAS URLs ❌

### Safe Logging Example

```csharp
_logger.LogInformation("Processing request {RequestId}", requestId);  // ✅ Safe
_logger.LogInformation("URL: {Url}", url);                           // ⚠️ Check if safe
_logger.LogError("Connection string: {CS}", connectionString);      // ❌ Unsafe
```

## 🛡️ Security Best Practices

1. **Use Managed Identity** – Never hardcode credentials
2. **Enable diagnostic logging** – Audit all access
3. **Restrict container access** – Set to Private
4. **SAS URL expiration** – Keep short-lived (60 minutes)
5. **Network security** – Use service endpoints/firewall
6. **Key rotation** – Rotate keys regularly
7. **Audit logs** – Monitor access patterns
8. **HTTPS only** – Never use HTTP

## 🔍 Audit Logging

### Azure Monitor Diagnostic Logs

```bash
# Enable diagnostic logging
az monitor diagnostic-settings create \
  --resource /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Storage/storageAccounts/{account} \
  --name blob-diagnostics \
  --workspace {workspace-id} \
  --logs '[{"category": "StorageRead", "enabled": true}]'
```

### Key Events to Monitor

- ✅ Blob uploads
- ✅ SAS URL generation
- ✅ Failed authentication
- ✅ Unauthorized access attempts
- ✅ Key/credential access

---

## 📚 Additional Resources

- [Azure Service Bus Documentation](https://learn.microsoft.com/en-us/azure/service-bus-messaging/)
- [Azure Blob Storage Documentation](https://learn.microsoft.com/en-us/azure/storage/blobs/)
- [Azure Identity Library](https://learn.microsoft.com/en-us/azure/developer/python/sdk/authentication-azure-hosted-environments)
- [Playwright Documentation](https://playwright.dev/)

---

**Last Updated**: January 2024
**Version**: 1.0
