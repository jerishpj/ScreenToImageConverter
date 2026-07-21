# Step 6: Service Bus Consumer & Orchestration Implementation Guide

## Overview

Step 6 implements the core orchestration logic that ties together all the infrastructure components built in previous steps. This is where the screenshot processing pipeline comes to life.

## Architecture

```
Service Bus Topic (html-screenshot-requests)
		   ↓
Service Bus Subscription (screenshot-worker-subscription)
		   ↓
IMessageConsumer (NEW)
		   ↓
HtmlScreenshotRequest (Deserialized)
		   ↓
Request Validation
		   ↓
PlaywrightScreenshotProvider
	   ↓
	 PNG Screenshot
		   ↓
BlobStorageProvider
	   ↓
	 Blob URI + SAS URL
		   ↓
ScreenshotCompletedEvent (Factory)
		   ↓
IMessagePublisher (NEW)
		   ↓
Service Bus Topic (screenshot-completed-events)
		   ↓
Downstream Consumers (Notification, PDF, etc.)
```

## Components to Implement

### 1. IMessageConsumer Implementation
**File**: `src/ScreenToImageConverter.Infrastructure/Consumers/ServiceBusMessageConsumer.cs`

**Responsibilities**:
- Connect to Service Bus subscription
- Listen for incoming HtmlScreenshotRequest messages
- Deserialize JSON messages
- Invoke message handler callback
- Handle message completion/abandonment
- Manage dead-letter queue for failed messages
- Support graceful shutdown with message drain

**Key Methods**:
- `StartAsync(CancellationToken)` - Start listening
- `StopAsync(CancellationToken)` - Stop gracefully
- `IsConnected` property - Connectivity status

**Configuration**:
- Uses ServiceBusOptions from Shared
- Supports both connection strings and managed identity
- Configurable max concurrent calls
- Configurable prefetch count

### 2. IMessagePublisher Implementation
**File**: `src/ScreenToImageConverter.Infrastructure/Publishers/ServiceBusMessagePublisher.cs`

**Responsibilities**:
- Connect to Service Bus namespace
- Serialize messages to JSON
- Publish ScreenshotCompletedEvent messages
- Support correlation ID propagation
- Handle transient failures with retries
- Support graceful shutdown

**Key Methods**:
- `PublishAsync<T>(T message, CancellationToken)` - Publish message
- `PublishAsync<T>(T message, string correlationId, CancellationToken)` - With correlation ID
- `IsConnected` property - Connectivity status

**Configuration**:
- Uses ServiceBusOptions from Shared
- Message serialization strategy (JSON)
- Retry policies with exponential backoff

### 3. Screenshot Processing Orchestrator
**File**: `src/ScreenToImageConverter.Worker/Orchestration/ScreenshotProcessingOrchestrator.cs`

**Responsibilities**:
- Coordinate the end-to-end screenshot workflow
- Validate incoming requests
- Call PlaywrightScreenshotProvider
- Call BlobStorageProvider for upload
- Generate completion events
- Handle errors and retries
- Measure performance metrics

**Workflow**:
```
1. Validate HtmlScreenshotRequest
   ├─ Check URL format
   ├─ Check required fields
   └─ Validate viewport/timeout values

2. Capture Screenshot
   ├─ Apply request viewport settings
   ├─ Apply request timeout settings
   ├─ Handle capture failures with retries
   └─ Measure capture duration

3. Upload to Blob Storage
   ├─ Generate blob filename (requestId_timestamp.png)
   ├─ Upload PNG data
   ├─ Generate SAS URL
   └─ Handle storage failures

4. Create Completion Event
   ├─ On success: CreateSuccess(...) factory
   ├─ On failure: CreateFailure(...) factory
   ├─ Include metrics (duration, retries)
   └─ Preserve correlation context

5. Publish Event
   ├─ Serialize to JSON
   ├─ Set correlation ID
   ├─ Publish to Service Bus
   └─ Handle publication failures
```

**Error Handling**:
- Retry logic with exponential backoff
- Circuit breaker pattern for cascading failures
- Detailed error logging with context
- Dead-letter support for unrecoverable failures

### 4. DI Registration & Extensions
**File**: `src/ScreenToImageConverter.Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs` (Update)

**Add Methods**:
- `AddServiceBusMessageConsumer()` - Register IMessageConsumer
- `AddServiceBusMessagePublisher()` - Register IMessagePublisher
- `AddScreenshotOrchestrator()` - Register orchestrator

### 5. Worker Service Integration
**File**: `src/ScreenToImageConverter.Worker/Worker.cs` (Update)

**Modifications**:
- Inject IMessageConsumer and orchestrator
- Implement message handler callback
- Start consumer in ExecuteAsync
- Handle StopAsync for graceful shutdown
- Implement proper cancellation token propagation

## Implementation Sequence

### Phase 1: Message Publisher
1. Create ServiceBusMessagePublisher class
2. Implement PublishAsync methods
3. Add JSON serialization strategy
4. Register in DI
5. Test message publishing

### Phase 2: Message Consumer
1. Create ServiceBusMessageConsumer class
2. Implement message handler callback mechanism
3. Implement graceful shutdown
4. Add error handling and dead-letter support
5. Register in DI
6. Test message consumption

### Phase 3: Orchestrator
1. Create ScreenshotProcessingOrchestrator class
2. Implement validation logic
3. Implement screenshot capture workflow
4. Implement blob upload workflow
5. Implement event creation and publishing
6. Add error handling and retries
7. Register in DI
8. Test end-to-end workflow

### Phase 4: Worker Integration
1. Update Worker.cs to use consumer
2. Update Worker.cs to use orchestrator
3. Implement message handler callback
4. Test integration

### Phase 5: Testing & Validation
1. Unit tests for orchestrator logic
2. Integration tests with Service Bus
3. End-to-end testing with sample messages
4. Performance testing
5. Error scenario testing

## Code Templates

### ServiceBusMessageConsumer Structure
```csharp
public class ServiceBusMessageConsumer : IMessageConsumer
{
	private readonly ServiceBusClient _serviceBusClient;
	private readonly ServiceBusProcessor _processor;
	private readonly ILogger<ServiceBusMessageConsumer> _logger;
	private Func<ProcessMessageEventArgs, Task>? _messageHandler;
	private Func<ProcessErrorEventArgs, Task>? _errorHandler;

	public ServiceBusMessageConsumer(
		IOptionsSnapshot<ServiceBusOptions> options,
		ILogger<ServiceBusMessageConsumer> logger)
	{
		// Initialize Service Bus client (managed identity or connection string)
		// Initialize processor for subscription
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		// Setup message and error handlers
		// Start processor
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		// Stop processor gracefully
		// Drain in-flight messages if needed
	}

	public bool IsConnected => _processor?.IsProcessing ?? false;

	public async ValueTask DisposeAsync()
	{
		// Cleanup resources
	}
}
```

### ScreenshotProcessingOrchestrator Structure
```csharp
public class ScreenshotProcessingOrchestrator
{
	private readonly IScreenshotProvider _screenshotProvider;
	private readonly IBlobStorageProvider _blobStorageProvider;
	private readonly IMessagePublisher _messagePublisher;
	private readonly BlobStorageOptions _blobStorageOptions;
	private readonly ILogger<ScreenshotProcessingOrchestrator> _logger;

	public async Task<ScreenshotCompletedEvent> ProcessAsync(
		HtmlScreenshotRequest request,
		CancellationToken cancellationToken)
	{
		try
		{
			// 1. Validate request
			var errors = request.Validate();
			if (errors.Count > 0)
				return ScreenshotCompletedEvent.CreateFailure(...);

			// 2. Capture screenshot
			var screenshotData = await CaptureScreenshotAsync(request, cancellationToken);

			// 3. Upload to blob storage
			var blobInfo = await UploadToBlobStorageAsync(request, screenshotData, cancellationToken);

			// 4. Create success event
			var completionEvent = ScreenshotCompletedEvent.CreateSuccess(
				request.RequestId,
				request.Url,
				blobInfo.FileName,
				blobInfo.ContainerName,
				blobInfo.Uri,
				blobInfo.SasUrl,
				blobInfo.SasUrlExpiresAt,
				screenshotData.Length,
				request.CorrelationId,
				request.SourceId
			);

			// 5. Publish completion event
			await _messagePublisher.PublishAsync(completionEvent, request.CorrelationId, cancellationToken);

			return completionEvent;
		}
		catch (Exception ex)
		{
			// Error handling and event creation
			return ScreenshotCompletedEvent.CreateFailure(
				request.RequestId,
				request.Url,
				ex.Message,
				request.CorrelationId,
				request.SourceId
			);
		}
	}
}
```

## Testing Strategy

### Unit Tests
- Request validation logic
- Error handling scenarios
- Event creation logic
- Configuration validation

### Integration Tests
- Service Bus message consumption
- Service Bus message publishing
- Blob Storage upload and retrieval
- End-to-end workflow

### Performance Tests
- Screenshot capture throughput
- Blob upload throughput
- Message processing latency
- Memory usage under load

## Monitoring & Observability

### Metrics to Log
- Request ID and correlation ID
- Screenshot capture duration
- Blob upload duration
- Total processing duration
- Retry attempts
- Error categories

### Health Check Integration
- Consumer connectivity
- Publisher connectivity
- Service Bus topic/subscription accessibility

## Configuration Changes

Update `appsettings.json` if needed:
- Service Bus topic/subscription names (already configured)
- Max concurrent message processing
- Message prefetch count
- Retry policies
- Timeouts

## Deployment Considerations

1. **Azure Service Bus Setup**
   - Create topic: `html-screenshot-requests`
   - Create subscription: `screenshot-worker-subscription`
   - Create topic: `screenshot-completed-events`

2. **Managed Identity**
   - Assign necessary Service Bus roles to worker identity
   - Assign necessary Blob Storage roles to worker identity

3. **Monitoring**
   - Configure Application Insights alerts
   - Set up log aggregation
   - Monitor queue depth
   - Monitor processing latency

4. **Scaling**
   - Set max concurrent calls based on Playwright resources
   - Consider multiple worker instances
   - Monitor Blob Storage throughput

---

**Ready to implement Step 6!**
