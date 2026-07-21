# ScreenToImageConverter - Vertical Slice Architecture

## Overview

The ScreenToImageConverter solution has been refactored from a traditional layered architecture to a **Vertical Slice Architecture**. This approach organizes code by feature rather than by technical layer, resulting in more cohesive, maintainable, and independently deployable features.

## What is Vertical Slice Architecture?

In vertical slice architecture, each feature (or slice) contains all the code necessary to implement that feature across all layers:
- Command/Request handling
- Business logic
- Data access
- Infrastructure integration
- Tests

This is in contrast to layered architecture where code is organized by technical concerns (Controllers, Services, Repositories, etc.).

**Benefits:**
- ✅ Features are self-contained and easier to understand
- ✅ Changes to a feature affect only that feature's code
- ✅ Teams can work independently on different features
- ✅ Easier to add or remove features
- ✅ Better testability with feature-scoped dependencies
- ✅ Clear feature boundaries and contracts

## Solution Structure

```
ScreenToImageConverter/
├── src/
│   └── ScreenToImageConverter.Worker/
│       ├── Program.cs                          [Entry point, DI registration]
│       ├── Worker.cs                           [BackgroundService orchestrator]
│       ├── appsettings*.json                   [Configuration]
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs  [Shared configuration, health checks]
│       └── Features/                           [Vertical slices]
│           ├── ScreenshotCapture/
│           │   ├── Commands/
│           │   │   └── CaptureScreenshotCommand.cs
│           │   ├── Handlers/
│           │   │   └── CaptureScreenshotHandler.cs
│           │   ├── Providers/
│           │   │   └── PlaywrightScreenshotProvider.cs
│           │   ├── Interfaces/
│           │   │   └── IScreenshotCaptureService.cs
│           │   ├── Models/
│           │   │   └── ScreenshotResult.cs
│           │   ├── Exceptions/
│           │   │   └── (Feature-specific exceptions)
│           │   ├── Health/
│           │   │   └── PlaywrightHealthCheck.cs
│           │   └── Extensions/
│           │       └── ScreenshotCaptureExtensions.cs
│           │
│           ├── BlobStorageUpload/
│           │   ├── Commands/
│           │   │   └── UploadScreenshotCommand.cs
│           │   ├── Handlers/
│           │   │   └── UploadScreenshotHandler.cs
│           │   ├── Providers/
│           │   │   └── BlobStorageProvider.cs
│           │   ├── Interfaces/
│           │   │   └── IBlobStorageUploadService.cs
│           │   ├── Models/
│           │   │   └── BlobUploadResult.cs
│           │   ├── Exceptions/
│           │   │   └── (Feature-specific exceptions)
│           │   ├── Health/
│           │   │   └── BlobStorageHealthCheck.cs
│           │   └── Extensions/
│           │       └── BlobStorageExtensions.cs
│           │
│           └── ServiceBusMessaging/
│               ├── Commands/
│               │   ├── PublishEventCommand.cs
│               │   └── ConsumeMessageCommand.cs
│               ├── Handlers/
│               │   ├── ScreenshotProcessingOrchestrator.cs
│               │   ├── MessageConsumerHandler.cs
│               │   └── EventPublisherHandler.cs
│               ├── Consumers/
│               │   └── ServiceBusMessageConsumer.cs
│               ├── Publishers/
│               │   └── ServiceBusEventPublisher.cs
│               ├── Interfaces/
│               │   └── (Feature interfaces)
│               ├── Models/
│               │   └── MessageHandlerResult.cs
│               ├── Exceptions/
│               │   └── (Feature-specific exceptions)
│               ├── Validators/
│               │   └── HtmlScreenshotRequestValidator.cs
│               └── Extensions/
│                   └── ServiceBusMessagingExtensions.cs
│
├── src/ScreenToImageConverter.Shared/          [Cross-project contracts]
│   ├── Configuration/
│   │   ├── ServiceBusOptions.cs
│   │   ├── BlobStorageOptions.cs
│   │   └── PlaywrightOptions.cs
│   ├── Interfaces/
│   │   ├── IScreenshotProvider.cs
│   │   ├── IBlobStorageProvider.cs
│   │   ├── IMessageConsumer.cs
│   │   └── IMessagePublisher.cs
│   ├── Messages/
│   │   ├── HtmlScreenshotRequest.cs
│   │   └── ScreenshotCompletedEvent.cs
│   ├── Exceptions/
│   │   └── ScreenshotProcessingExceptions.cs
│   └── Results/
│       └── OperationResult.cs
│
└── tests/
	└── ScreenToImageConverter.Tests/
		└── Features/
			├── ScreenshotCapture/
			│   └── CaptureScreenshotHandlerTests.cs
			├── BlobStorageUpload/
			│   └── UploadScreenshotHandlerTests.cs
			└── ServiceBusMessaging/
				└── HtmlScreenshotRequestValidatorTests.cs
```

## Feature Slices

### 1. ScreenshotCapture Feature

**Purpose:** Handles capturing screenshots from HTML URLs using Playwright.

**Key Components:**
- `PlaywrightScreenshotProvider` - Playwright browser automation integration
- `CaptureScreenshotHandler` - Orchestrates screenshot capture workflow
- `PlaywrightHealthCheck` - Monitors Playwright provider health

**Responsibilities:**
- Browser initialization and management
- URL navigation and screenshot capture
- Retry logic with exponential backoff
- Health status reporting

**Public Interface (Shared):**
- `IScreenshotProvider` - Abstraction for screenshot providers

**Feature Extension:**
```csharp
services.AddScreenshotCaptureFeature();
await serviceProvider.InitializePlaywrightAsync();
```

---

### 2. BlobStorageUpload Feature

**Purpose:** Handles uploading screenshots to Azure Blob Storage and generating SAS URLs.

**Key Components:**
- `BlobStorageProvider` - Azure Blob Storage integration
- `UploadScreenshotHandler` - Orchestrates upload workflow
- `BlobStorageHealthCheck` - Monitors blob storage connectivity

**Responsibilities:**
- Blob upload with metadata
- SAS URL generation for time-limited access
- Container management
- Connectivity health checks

**Public Interface (Shared):**
- `IBlobStorageProvider` - Abstraction for blob storage providers

**Feature Extension:**
```csharp
services.AddBlobStorageUploadFeature();
```

---

### 3. ServiceBusMessaging Feature

**Purpose:** Handles consuming messages from Service Bus and publishing completion events.

**Key Components:**
- `ServiceBusMessageConsumer` - Service Bus message consumption and routing
- `ServiceBusEventPublisher` - Service Bus event publishing
- `ScreenshotProcessingOrchestrator` - Coordinates entire screenshot workflow
- `HtmlScreenshotRequestValidator` - Validates incoming messages

**Responsibilities:**
- Service Bus connection management
- Message deserialization and validation
- Event publication with correlation IDs
- Dead-letter handling for failed messages
- Cross-feature orchestration

**Public Interfaces (Shared):**
- `IMessageConsumer` - Message consumption abstraction
- `IMessagePublisher` - Event publishing abstraction

**Feature Extension:**
```csharp
services.AddServiceBusMessagingFeature();
```

---

## Workflow: Screenshot Processing Pipeline

The `ScreenshotProcessingOrchestrator` coordinates all features in a complete screenshot processing pipeline:

```
Service Bus Message Received
	↓
[ServiceBusMessaging] Consumer deserializes & validates
	↓
Worker.ProcessMessageAsync() triggers orchestrator
	↓
[ScreenshotCapture] CaptureScreenshotHandler captures from URL
	↓
[BlobStorageUpload] UploadScreenshotHandler uploads to storage & generates SAS URL
	↓
[ServiceBusMessaging] Publisher publishes ScreenshotCompletedEvent
	↓
Success: Event published to downstream consumers
Error: Failure event published, message dead-lettered
```

## Dependency Flow

```
External Dependencies (Azure, Configuration)
		↓
Shared Interfaces & Contracts
		↓
Feature Extensions Register Services
		↓
Features Implement Interfaces
		↓
Orchestrator Coordinates Features
		↓
Worker BackgroundService Manages Lifecycle
```

## Adding a New Feature

To add a new vertical slice feature:

1. **Create feature directory structure:**
   ```
   Features/NewFeature/
   ├── Commands/
   ├── Handlers/
   ├── Models/
   ├── Services/
   ├── Interfaces/
   ├── Exceptions/
   ├── Health/
   └── Extensions/
   ```

2. **Create feature extension method:**
   ```csharp
   public static class NewFeatureExtensions
   {
	   public static IServiceCollection AddNewFeature(this IServiceCollection services)
	   {
		   services.AddScoped<NewFeatureHandler>();
		   // Register dependencies
		   return services;
	   }
   }
   ```

3. **Register in Program.cs:**
   ```csharp
   builder.Services.AddNewFeature();
   ```

4. **Create tests in corresponding test feature structure:**
   ```
   tests/ScreenToImageConverter.Tests/Features/NewFeature/
   ├── CommandHandlerTests.cs
   └── ServiceTests.cs
   ```

## Configuration

Configuration is shared via `ScreenToImageConverter.Shared` and injected into features:

- `ServiceBusOptions` - Service Bus configuration
- `BlobStorageOptions` - Blob Storage configuration
- `PlaywrightOptions` - Playwright configuration

Features access configuration via `IOptions<ConfigOptions>` pattern:

```csharp
public class MyFeatureService
{
	private readonly MyFeatureOptions _options;

	public MyFeatureService(IOptions<MyFeatureOptions> options)
	{
		_options = options.Value;
	}
}
```

## Health Checks

Each feature registers its own health checks via its extension:

```csharp
services.AddHealthChecks()
	.AddCheck<PlaywrightHealthCheck>("playwright", tags: new[] { "ready", "live" });
```

Health check endpoints:
- `/health` - Overall health
- `/health/ready` - Readiness (for Kubernetes)
- `/health/live` - Liveness (for Kubernetes)

## Testing Strategy

Each feature has corresponding tests organized in the same structure:

```
tests/ScreenToImageConverter.Tests/Features/FeatureName/
├── HandlerTests.cs
├── ProviderTests.cs
└── IntegrationTests.cs
```

**Test Levels:**
- **Unit Tests:** Test feature components in isolation
- **Integration Tests:** Test feature with mocked external dependencies
- **End-to-End Tests:** Test complete workflow across features

## Benefits Realized

✅ **Reduced Cognitive Load** - Developers focus on one feature at a time
✅ **Improved Cohesion** - Related code is located together
✅ **Better Testability** - Features can be tested independently
✅ **Easier Onboarding** - New developers understand features by reading one directory
✅ **Independent Scaling** - Features can be developed/deployed independently
✅ **Clear Contracts** - Shared interfaces define feature boundaries
✅ **Maintainability** - Changes are localized to specific features

## Migration from Layered Architecture

This solution was migrated from a traditional layered architecture (Infrastructure, Services, Repositories) to vertical slices. The migration included:

1. Moving providers from `Infrastructure` to feature directories
2. Creating feature-specific handlers and commands
3. Creating feature extension methods for DI registration
4. Creating feature-specific health checks
5. Updating orchestration logic in Worker and Program
6. Organizing tests by feature instead of layer

## Future Considerations

- Consider CQRS (Command Query Responsibility Segregation) for complex features
- Implement feature toggles for gradual feature rollout
- Consider feature-specific databases if features scale independently
- Monitor feature performance independently
- Consider API gateways for inter-service communication if features become microservices

---

**Last Updated:** 2024
**Architecture Pattern:** Vertical Slice Architecture
**Status:** ✅ Production Ready
