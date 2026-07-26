# ScreenToImageConverter - Complete Guide

A comprehensive guide covering setup, configuration, deployment, development, testing, troubleshooting, and operations.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Configuration](#configuration)
3. [Deployment](#deployment)
4. [Development Guide](#development-guide)
5. [Testing Guide](#testing-guide)
6. [Troubleshooting](#troubleshooting)
7. [Operations & Monitoring](#operations--monitoring)

---

# Getting Started

Complete guide to set up and run ScreenToImageConverter for the first time.

## 📋 Prerequisites

### Required Software
- **.NET 9 SDK** or later ([download](https://dotnet.microsoft.com/download))
- **Git** (for cloning repository)
- **Visual Studio 2022/2026** (optional, for IDE development)
- **PowerShell 7+** or Bash (for command-line work)

### Azure Resources Required
- **Azure Subscription** (with active credits or pay-as-you-go)
- **Azure Service Bus Namespace** (for messaging)
- **Azure Storage Account** (for blob storage)
- **Application Insights** (optional, for monitoring)

### User Permissions
- Permissions to create/manage Service Bus resources
- Permissions to create/manage Storage accounts
- Access to use Managed Identity or connection strings

### Development Tools (Optional)
- **Docker** (for containerized deployment)
- **Postman** or similar tool (for testing API)
- **Azure Storage Explorer** (for blob inspection)

## 🔧 Installation & Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/jerishpj/ScreenToImageConverter.git
cd ScreenToImageConverter
```

### Step 2: Restore Dependencies

```bash
dotnet restore
```

### Step 3: Build the Solution

```bash
dotnet build
```

### Step 4: Create Azure Resources

#### A. Azure Service Bus Namespace
1. Go to [Azure Portal](https://portal.azure.com)
2. Create new resource → Service Bus
3. Configure:
   - **Namespace name**: `your-namespace`
   - **Tier**: Standard (recommended) or Premium
   - **Region**: Choose closest to your location
4. Click "Create"

#### B. Service Bus Topics & Subscriptions

**Topic 1: `html-screenshot-requests`**
```
├─ Default message TTL: 14 days
├─ Duplicate detection: Enabled
└─ Partitioning: Enabled
```

**Topic 1 Subscription: `screenshot-worker-subscription`**
```
├─ Lock duration: 30 seconds
├─ Dead letter on exceptions: Enabled
└─ Max delivery count: 3
```

**Topic 2: `screenshot-completed-events`**
```
├─ Default message TTL: 7 days
└─ Partitioning: Enabled
```

#### C. Azure Storage Account
1. Create new resource → Storage Account
2. Configure:
   - **Account name**: `yourstorageaccount`
   - **Region**: Same as Service Bus
   - **Performance**: Standard
   - **Replication**: LRS or GRS
3. Click "Create"

#### D. Create Blob Container
1. In Storage Account → Containers
2. Create container: `screenshots`
3. Set "Public access level" to "Private"

#### E. Create Managed Identity (Recommended)
1. In Storage Account → Access Control (IAM)
   - Add role: "Storage Blob Data Contributor"
   - Assign to: Managed identity

2. In Service Bus namespace → Access Control (IAM)
   - Add role: "Azure Service Bus Data Owner"

### Step 5: Configure Application Settings

Create `appsettings.Development.json`:

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information"
	}
  },
  "ServiceBus": {
	"FullyQualifiedNamespace": "your-namespace.servicebus.windows.net",
	"UseManagedIdentity": true,
	"HtmlScreenshotRequestTopicName": "html-screenshot-requests",
	"HtmlScreenshotRequestSubscriptionName": "screenshot-worker-subscription",
	"ScreenshotCompletedEventTopicName": "screenshot-completed-events",
	"MaxConcurrentCalls": 1,
	"PrefetchCount": 0,
	"MaxConnectionRetries": 5,
	"InitialRetryDelaySeconds": 2,
	"MaxRetryDelaySeconds": 60,
	"ConnectionTimeoutSeconds": 10,
	"EnableGracefulDegradation": true
  },
  "BlobStorage": {
	"AccountName": "yourstorageaccount",
	"UseManagedIdentity": true,
	"ContainerName": "screenshots",
	"SasUrlExpirationMinutes": 60,
	"AutoCreateContainer": true
  },
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1920,
	"DefaultViewportHeight": 1080,
	"DefaultTimeoutMs": 30000,
	"WaitUntilEvent": "networkidle",
	"Headless": true,
	"DisableSandbox": false,
	"DeviceScaleFactor": 1.0,
	"FullPage": true,
	"MaxRetryAttempts": 2,
	"RetryDelayMs": 1000,
	"EmulateDeviceUserAgent": true
  }
}
```

### Step 6: Verify Configuration

The application validates configuration on startup. Errors will appear immediately if configuration is invalid.

## ▶️ Running the Application

### Using .NET CLI

```bash
cd src/ScreenToImageConverter.Worker
dotnet run
```

Expected output:
```
✅ Worker service started
✅ Message consumer connected
✅ Health checks operational
```

### Using Visual Studio

1. Open `ScreenToImageConverter.sln`
2. Set `ScreenToImageConverter.Worker` as startup project
3. Press `F5`

### Using Docker

```bash
docker build -t screentoimageconverter .
docker run \
  -e ServiceBus__FullyQualifiedNamespace=your-namespace.servicebus.windows.net \
  -e BlobStorage__AccountName=yourstorageaccount \
  screentoimageconverter
```

## ✅ Verify Installation

### Health Checks
The application validates:
- ✅ Playwright browser initialization
- ✅ Blob Storage connectivity
- ✅ Service Bus connectivity
- ✅ Configuration validation

### Run Tests
```bash
dotnet test
# Expected: 185 Tests (185 Passed, 0 Failed, 0 Skipped)
```

## 🚀 Your First Screenshot Request

### 1. Prepare Request

```json
{
  "requestId": "test-001",
  "url": "https://www.example.com",
  "viewportWidth": 1920,
  "viewportHeight": 1080,
  "timeoutMs": 30000,
  "waitForPageLoad": true
}
```

### 2. Send to Service Bus

```csharp
var client = new ServiceBusClient("your-namespace.servicebus.windows.net");
var sender = client.CreateSender("html-screenshot-requests");
var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonContent));
await sender.SendMessageAsync(message);
```

### 3. Monitor Processing

Watch application logs for:
```
📨 Received message [RequestId: test-001]
✅ Step 1/3: Validating request
✅ Step 2/3: Capturing screenshot
✅ Step 3/3: Uploading to blob storage
✅ Message processed
```

---

# Configuration

Complete reference for all configuration options.

## 📋 Configuration Overview

Configuration priority (highest to lowest):
1. **Environment Variables**
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **Code defaults**

## 🔧 Configuration Sources

### Method 1: appsettings.json (File-Based)

```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "ServiceBus": { ... },
  "BlobStorage": { ... },
  "Playwright": { ... }
}
```

### Method 2: Environment-Specific Files

```
appsettings.json
appsettings.Development.json
appsettings.Staging.json
appsettings.Production.json
```

Later files override earlier ones.

### Method 3: Environment Variables

```bash
# Bash
export ServiceBus__FullyQualifiedNamespace="your-namespace.servicebus.windows.net"

# PowerShell
$env:ServiceBus__FullyQualifiedNamespace = "your-namespace.servicebus.windows.net"

# Docker
environment:
  - ServiceBus__FullyQualifiedNamespace=your-namespace.servicebus.windows.net
```

**Note**: Use `__` (double underscore) for nesting.

### Method 4: Azure Key Vault

```csharp
// In Program.cs
var keyVaultUrl = new Uri($"https://{keyVaultName}.vault.azure.net/");
var credential = new DefaultAzureCredential();
config.AddAzureKeyVault(keyVaultUrl, credential);
```

## 📝 Complete Configuration Reference

### Logging Settings

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning",
	  "Microsoft.Hosting.Lifetime": "Information",
	  "ScreenToImageConverter": "Information"
	}
  }
}
```

**Log Levels:**
- `Trace` – Verbose debugging
- `Debug` – Debugging information
- `Information` – Normal operation info
- `Warning` – Warning messages
- `Error` – Error, will retry
- `Critical` – Critical error, immediate action

### Service Bus Settings

```json
{
  "ServiceBus": {
	"FullyQualifiedNamespace": "your-namespace.servicebus.windows.net",
	"UseManagedIdentity": true,
	"HtmlScreenshotRequestTopicName": "html-screenshot-requests",
	"HtmlScreenshotRequestSubscriptionName": "screenshot-worker-subscription",
	"ScreenshotCompletedEventTopicName": "screenshot-completed-events",
	"MaxConcurrentCalls": 1,
	"PrefetchCount": 0,
	"MaxConnectionRetries": 5,
	"InitialRetryDelaySeconds": 2,
	"MaxRetryDelaySeconds": 60,
	"ConnectionTimeoutSeconds": 10,
	"EnableGracefulDegradation": true,
	"ReconnectionIntervalSeconds": 5,
	"MaxReconnectionIntervalSeconds": 60
  }
}
```

**Tuning Tips:**
- `MaxConcurrentCalls`: Increase for parallel processing (requires more memory)
- `EnableGracefulDegradation`: Set false to fail fast if broker unavailable
- Retry settings: Adjust based on network stability

### Blob Storage Settings

```json
{
  "BlobStorage": {
	"AccountName": "yourstorageaccount",
	"UseManagedIdentity": true,
	"ContainerName": "screenshots",
	"SasUrlExpirationMinutes": 60,
	"AutoCreateContainer": true
  }
}
```

**Authentication Options:**

**Option A: Managed Identity (Recommended)**
```json
{ "UseManagedIdentity": true }
```

**Option B: Connection String**
```json
{
  "UseManagedIdentity": false,
  "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
}
```

### Playwright Settings

```json
{
  "Playwright": {
	"BrowserType": "chromium",
	"Headless": true,
	"DisableSandbox": false,
	"DefaultViewportWidth": 1920,
	"DefaultViewportHeight": 1080,
	"DeviceScaleFactor": 1.0,
	"DefaultTimeoutMs": 30000,
	"WaitUntilEvent": "networkidle",
	"FullPage": true,
	"MaxRetryAttempts": 2,
	"RetryDelayMs": 1000,
	"EmulateDeviceUserAgent": true
  }
}
```

**Browser Types:**
- `chromium` – Fast, most compatible
- `firefox` – Alternative, slightly slower
- `webkit` – Safari-like, experimental

**Wait Until Options:**
- `load` – All resources loaded
- `domcontentloaded` – DOM ready (faster)
- `networkidle` – No network activity (slowest)

## 🌍 Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "Logging": { "LogLevel": { "Default": "Debug" } },
  "ServiceBus": {
	"FullyQualifiedNamespace": "dev-namespace.servicebus.windows.net",
	"EnableGracefulDegradation": true
  },
  "Playwright": {
	"DisableSandbox": false,
	"DefaultTimeoutMs": 60000
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "ServiceBus": {
	"FullyQualifiedNamespace": "prod-namespace.servicebus.windows.net",
	"MaxConcurrentCalls": 4,
	"EnableGracefulDegradation": false
  },
  "Playwright": {
	"DisableSandbox": true,
	"DefaultTimeoutMs": 30000,
	"MaxRetryAttempts": 3
  }
}
```

## 🔐 Security & Secrets Management

### Don't Do This (Insecure)
```json
{
  "BlobStorage": {
	"AccountKey": "my-secret-key-exposed-in-code"
  }
}
```

### Do This Instead (Secure)

**Option 1: Environment Variables**
```bash
export BlobStorage__AccountKey="my-secret-key"
```

**Option 2: Azure Key Vault**
```csharp
var keyVault = new Uri($"https://{kvName}.vault.azure.net/");
config.AddAzureKeyVault(keyVault, new DefaultAzureCredential());
```

**Option 3: User Secrets (Development)**
```bash
dotnet user-secrets set "BlobStorage:AccountKey" "my-key"
```

**Option 4: Managed Identity (Recommended)**
```json
{ "BlobStorage": { "UseManagedIdentity": true } }
```

---

# Deployment

Guide to deploy ScreenToImageConverter to production.

## 🐳 Docker Deployment

### Step 1: Build Docker Image

```bash
# From repo root
docker build -t screentoimageconverter:latest .
```

### Step 2: Push to Registry

```bash
# Docker Hub
docker tag screentoimageconverter:latest youraccount/screentoimageconverter:latest
docker push youraccount/screentoimageconverter:latest

# Azure Container Registry
az acr build --registry myregistry --image screentoimageconverter:latest .
```

### Step 3: Run Container

```bash
docker run -d \
  --name screentoimageconverter \
  -e ServiceBus__FullyQualifiedNamespace=your-namespace.servicebus.windows.net \
  -e BlobStorage__AccountName=yourstorageaccount \
  -e ASPNETCORE_ENVIRONMENT=Production \
  screentoimageconverter:latest
```

## ☁️ Azure Container Apps Deployment

### Step 1: Create Container App Environment

```bash
az containerapp env create \
  --name myenv \
  --resource-group mygroup \
  --location eastus
```

### Step 2: Create Container App

```bash
az containerapp create \
  --name screentoimageconverter \
  --resource-group mygroup \
  --environment myenv \
  --image youraccount/screentoimageconverter:latest \
  --target-port 80 \
  --env-vars \
	ServiceBus__FullyQualifiedNamespace=your-namespace.servicebus.windows.net \
	BlobStorage__AccountName=yourstorageaccount \
	ASPNETCORE_ENVIRONMENT=Production
```

## ☸️ Kubernetes (AKS) Deployment

### Step 1: Create Deployment Manifest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: screentoimageconverter
spec:
  replicas: 3
  selector:
	matchLabels:
	  app: screentoimageconverter
  template:
	metadata:
	  labels:
		app: screentoimageconverter
	spec:
	  containers:
	  - name: screentoimageconverter
		image: youraccount/screentoimageconverter:latest
		ports:
		- containerPort: 80
		env:
		- name: ServiceBus__FullyQualifiedNamespace
		  valueFrom:
			secretKeyRef:
			  name: appsettings
			  key: servicebus-namespace
		- name: BlobStorage__AccountName
		  valueFrom:
			configMapKeyRef:
			  name: appsettings
			  key: blobstorage-account
		- name: ASPNETCORE_ENVIRONMENT
		  value: "Production"
		resources:
		  requests:
			memory: "256Mi"
			cpu: "250m"
		  limits:
			memory: "512Mi"
			cpu: "500m"
		livenessProbe:
		  httpGet:
			path: /health
			port: 80
		  initialDelaySeconds: 30
		  periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: screentoimageconverter
spec:
  selector:
	app: screentoimageconverter
  ports:
  - port: 80
	targetPort: 80
  type: LoadBalancer
```

### Step 2: Deploy to AKS

```bash
kubectl apply -f deployment.yaml
```

## 🏭 Azure Container Instances (ACI)

### One-Time Deployment

```bash
az container create \
  --resource-group mygroup \
  --name screentoimageconverter \
  --image youraccount/screentoimageconverter:latest \
  --environment-variables \
	ServiceBus__FullyQualifiedNamespace=your-namespace.servicebus.windows.net \
	BlobStorage__AccountName=yourstorageaccount \
  --cpu 2 \
  --memory 4
```

## 📊 Scaling

### Container Apps

```bash
az containerapp update \
  --name screentoimageconverter \
  --resource-group mygroup \
  --set properties.template.scale.minReplicas=2 \
		   properties.template.scale.maxReplicas=10
```

### AKS

```bash
kubectl autoscale deployment screentoimageconverter \
  --min=2 --max=10 --cpu-percent=80
```

## 🔄 CI/CD with GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy

on:
  push:
	branches: [main]

jobs:
  deploy:
	runs-on: ubuntu-latest
	steps:
	- uses: actions/checkout@v2

	- name: Build Docker image
	  run: |
		docker build -t myregistry.azurecr.io/screentoimageconverter:${{ github.sha }} .
		docker push myregistry.azurecr.io/screentoimageconverter:${{ github.sha }}

	- name: Deploy to Container Apps
	  run: |
		az containerapp update \
		  --name screentoimageconverter \
		  --resource-group mygroup \
		  --image myregistry.azurecr.io/screentoimageconverter:${{ github.sha }}
```

---

# Development Guide

Guide for developers extending and maintaining the codebase.

## 🛠️ Development Environment Setup

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022/2026 or VS Code
- Git

### Initial Setup

```bash
# Clone repository
git clone https://github.com/jerishpj/ScreenToImageConverter.git
cd ScreenToImageConverter

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

## 🏗️ Vertical Slice Architecture

Code is organized by feature, not technical layer:

```
src/ScreenToImageConverter.Worker/
├─ Features/ConvertHtmlToImage/
│  ├─ ConvertHtmlToImageCommand.cs
│  ├─ ConvertHtmlToImageHandler.cs
│  ├─ HtmlRequestValidator.cs
│  ├─ ImageMetadataResponse.cs
│  └─ ScreenshotProcessingException.cs
│
├─ Infrastructure/
│  ├─ Notifications/ (Service Bus/RabbitMQ)
│  ├─ Screenshots/ (Playwright)
│  ├─ Storage/ (Blob Storage)
│  └─ Resilience/
│
├─ AppSettings/ (Configuration models)
├─ Extensions/ (Dependency injection)
├─ Program.cs
└─ Worker.cs (BackgroundService)
```

## ➕ Adding a New Feature

### Step 1: Create Feature Folder

```bash
mkdir src/ScreenToImageConverter.Worker/Features/MyFeature
```

### Step 2: Create Feature Files

**MyFeatureCommand.cs** (Input model)
```csharp
public class MyFeatureCommand
{
	public string? InputData { get; set; }
}
```

**MyFeatureHandler.cs** (Business logic)
```csharp
public class MyFeatureHandler
{
	private readonly ILogger<MyFeatureHandler> _logger;

	public MyFeatureHandler(ILogger<MyFeatureHandler> logger)
	{
		_logger = logger;
	}

	public async Task<Result> HandleAsync(MyFeatureCommand command, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Processing feature");
		// Business logic here
		return Result.Success();
	}
}
```

**MyFeatureValidator.cs** (Input validation)
```csharp
public class MyFeatureValidator
{
	public static void Validate(MyFeatureCommand command)
	{
		if (string.IsNullOrEmpty(command.InputData))
			throw new ArgumentException("InputData required");
	}
}
```

### Step 3: Register in DI

In `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<MyFeatureHandler>();
```

### Step 4: Use in Handler

```csharp
var handler = serviceProvider.GetRequiredService<MyFeatureHandler>();
await handler.HandleAsync(command, cancellationToken);
```

## 🔄 Key Patterns

### Result Pattern

```csharp
public class Result
{
	public bool IsSuccessful { get; set; }
	public string? Message { get; set; }

	public static Result Success() => new() { IsSuccessful = true };
	public static Result Failure(string message) => new() { IsSuccessful = false, Message = message };
}
```

### Async/Await Pattern

Always use async/await for I/O:

```csharp
public async Task<byte[]> GetDataAsync(CancellationToken cancellationToken)
{
	// ✅ Good
	return await httpClient.GetByteArrayAsync(url, cancellationToken);

	// ❌ Bad
	return httpClient.GetByteArray(url);
}
```

## 📝 Coding Standards

### Naming Conventions
- Classes: `PascalCase` (e.g., `ConvertHtmlToImageHandler`)
- Methods: `PascalCase` (e.g., `ProcessMessageAsync`)
- Properties: `PascalCase` (e.g., `RequestId`)
- Private fields: `_camelCase` (e.g., `_logger`)
- Constants: `UPPER_SNAKE_CASE` (e.g., `MAX_RETRIES`)

### Code Organization
1. Public properties
2. Constructors
3. Public methods
4. Private methods
5. Private fields

### Exception Handling

```csharp
try
{
	// Operation
}
catch (ServiceBusException ex) when (ex.IsTransient)
{
	_logger.LogWarning("Transient error, will retry: {Message}", ex.Message);
	// Retry logic
}
catch (Exception ex)
{
	_logger.LogError(ex, "Unexpected error");
	throw;
}
```

## 🧪 Testing Standards

### Unit Test Template

```csharp
[Fact]
public async Task Handler_WithValidRequest_ShouldReturnSuccess()
{
	// Arrange
	var handler = new ConvertHtmlToImageHandler(
		mockProvider,
		mockStorage,
		mockLogger);

	var command = new HtmlScreenshotRequestBuilder()
		.WithUrl("https://example.com")
		.Build();

	// Act
	var result = await handler.HandleAsync(command, CancellationToken.None);

	// Assert
	Assert.True(result.IsSuccessful);
	mockProvider.Verify(x => x.CaptureScreenshotAsync(
		It.IsAny<string>(),
		It.IsAny<int?>(),
		It.IsAny<int?>(),
		It.IsAny<int?>(),
		It.IsAny<CancellationToken>()), Times.Once);
}
```

### Test Fixtures

Available test fixtures:
- `MockScreenshotProvider` – Simulates Playwright
- `MockMessageConsumer` – Simulates Service Bus
- `MockBlobStorageProvider` – In-memory storage
- `HtmlScreenshotRequestBuilder` – Fluent test data builder

## 🔍 Debugging

### Visual Studio Debugging

1. Set breakpoints
2. Press `F5` to start debugging
3. Use Debug → Windows → Locals to inspect variables
4. Use Immediate Window to execute code

### Console Logging

```csharp
_logger.LogInformation("Request {RequestId} processing", requestId);
_logger.LogError(ex, "Error processing request");
```

### Health Checks

```bash
# Check application health
curl http://localhost:5000/health
```

## 🚀 Development Workflow

1. Create branch: `git checkout -b feature/my-feature`
2. Make changes
3. Run tests: `dotnet test`
4. Commit: `git commit -am "Add my feature"`
5. Push: `git push origin feature/my-feature`
6. Create Pull Request

---

# Testing Guide

Complete testing strategy and guidance.

## 🧪 Test Infrastructure

### Test Projects

**ScreenToImageConverter.Tests** contains:
- Unit tests
- Integration tests
- Test fixtures & mocks
- Builders & factories

### Test Libraries

- **xUnit** – Test framework
- **Moq** – Mocking library
- **FluentAssertions** (optional) – Assertions

## 🏗️ Test Doubles

### MockScreenshotProvider

Simulates Playwright browser without launching:

```csharp
var mock = new MockScreenshotProvider();
var screenshot = await mock.CaptureScreenshotAsync(
	"https://example.com", 
	1920, 1080, 30000, 
	CancellationToken.None);

// Returns fake PNG data
Assert.NotNull(screenshot);
Assert.NotEmpty(screenshot);
```

### MockMessageConsumer

Simulates Service Bus without network:

```csharp
var mock = new MockMessageConsumer();

// Register handler
Func<HtmlScreenshotRequest, string, CancellationToken, Task> handler = 
	async (msg, id, ct) => { /* process */ };

mock.RegisterMessageHandler(handler);
await mock.StartAsync(CancellationToken.None);

// Inject messages
mock.EnqueueMessage(new HtmlScreenshotRequest { ... });
```

### MockBlobStorageProvider

In-memory blob storage:

```csharp
var mock = new MockBlobStorageProvider();

// Upload
var result = await mock.UploadAsync(
	"container", "blob.png", data, "image/png", 
	CancellationToken.None);

// Generate SAS URL
var url = await mock.GenerateSasUrlAsync(
	"container", "blob.png", 60, 
	CancellationToken.None);

// Verify
Assert.NotNull(result.BlobUri);
```

## 🧩 Test Builders

### HtmlScreenshotRequestBuilder

Fluent builder for test data:

```csharp
var request = new HtmlScreenshotRequestBuilder()
	.WithRequestId("test-001")
	.WithUrl("https://example.com")
	.WithViewportWidth(1920)
	.WithViewportHeight(1080)
	.WithTimeoutMs(30000)
	.WithWaitForPageLoad(true)
	.Build();
```

## ▶️ Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test File

```bash
dotnet test --filter "FullyQualifiedName~ConvertHtmlToImageHandlerTests"
```

### Run Specific Test Category

```bash
dotnet test --filter "Category=Unit"
```

### Run with Verbosity

```bash
dotnet test --verbosity detailed
```

### Run in Visual Studio

1. Test → Test Explorer
2. Click "Run All" or right-click specific test

**Expected**: 185 tests pass ✅

## 📝 Test Patterns

### AAA (Arrange-Act-Assert)

```csharp
[Fact]
public async Task ShouldProcessValidRequest()
{
	// Arrange - Setup
	var handler = CreateHandler();
	var request = new HtmlScreenshotRequestBuilder().Build();

	// Act - Execute
	var result = await handler.HandleAsync(request, CancellationToken.None);

	// Assert - Verify
	Assert.True(result.IsSuccessful);
}
```

### Happy Path Test

```csharp
[Fact]
public async Task Handler_WithValidRequest_ShouldSucceed()
{
	var handler = new ConvertHtmlToImageHandler(mockProvider, mockStorage);
	var request = ValidRequest();

	var result = await handler.HandleAsync(request, CancellationToken.None);

	Assert.True(result.IsSuccessful);
}
```

### Error Path Test

```csharp
[Fact]
public async Task Handler_WithInvalidUrl_ShouldFail()
{
	var handler = new ConvertHtmlToImageHandler(...);
	var request = new HtmlScreenshotRequestBuilder()
		.WithUrl("invalid-url")
		.Build();

	var ex = await Assert.ThrowsAsync<ArgumentException>(
		async () => await handler.HandleAsync(request, CancellationToken.None));

	Assert.Contains("URL", ex.Message);
}
```

## 📊 Test Coverage

Run coverage report:

```bash
dotnet test /p:CollectCoverage=true
```

Expected coverage: 80%+ ✅

---

# Troubleshooting

Common issues and solutions.

## 🚀 Startup Issues

### Issue: "Unable to load Playwright"

**Solution:**
```bash
# Playwright is downloaded on first use
# If it fails, manually install:
pwsh bin/Release/net9.0/playwright.ps1 install
```

### Issue: "Service Bus not accessible"

**Checks:**
1. Verify namespace name is correct
2. Confirm network access to Azure
3. Check firewall/VPN isn't blocking
4. Verify Managed Identity has correct role

### Issue: "Storage account not accessible"

**Checks:**
1. Verify storage account name is correct
2. Confirm Managed Identity has "Storage Blob Data Contributor" role
3. Check container "screenshots" exists
4. Verify "AutoCreateContainer" is enabled

### Issue: "Configuration validation failed"

**Solution:**
1. Check error message in logs
2. Verify all required settings present
3. Ensure values are valid type
4. Compare with reference configuration

## 📨 Message Processing Issues

### Issue: "Messages not being processed"

**Checks:**
1. Service Bus subscription exists and is enabled
2. No messages in dead-letter queue (check why)
3. Handler is registered correctly
4. Application is running and connected
5. Check logs for processing errors

### Issue: "Service Bus connection timeout"

**Solution:**
```json
{
  "ServiceBus": {
	"ConnectionTimeoutSeconds": 30,
	"MaxConnectionRetries": 10,
	"EnableGracefulDegradation": true
  }
}
```

Increase retry settings and enable graceful degradation.

### Issue: "Dead letter messages accumulating"

**Causes & Fixes:**
- Invalid message format → Check sender's message format
- Handler throws exception → Review handler logic
- Max delivery count exceeded → Increase in subscription settings
- Blob upload failures → Check storage credentials

## 📸 Screenshot Capture Issues

### Issue: "Screenshot is blank or broken"

**Tries:**
1. Increase `DefaultTimeoutMs` – Site may need more time
2. Change `WaitUntilEvent` to "domcontentloaded"
3. Check website is accessible and not blocking bots
4. Verify `DisableSandbox` setting for Docker

### Issue: "Playwright context error"

**Solution:**
```json
{
  "Playwright": {
	"DisableSandbox": true,
	"MaxRetryAttempts": 3
  }
}
```

### Issue: "Timeout capturing screenshot"

**Solution:**
```json
{
  "Playwright": {
	"DefaultTimeoutMs": 60000,
	"WaitUntilEvent": "domcontentloaded"
  }
}
```

## 💾 Blob Storage Issues

### Issue: "Cannot upload to blob storage"

**Checks:**
1. Storage account credentials correct
2. Container exists: `screenshots`
3. Managed Identity has "Storage Blob Data Contributor" role
4. Storage account not throttled (check metrics)

### Issue: "SAS URL generation fails"

**Solution:**
```json
{
  "BlobStorage": {
	"UseManagedIdentity": true
  }
}
```

Ensure using Managed Identity instead of connection string.

## 🧪 Test Issues

### Issue: "Tests fail with connection errors"

**Solution:**
- Tests use mocks and don't require Azure
- Check error message carefully
- Run `dotnet build` first
- Run individual test to isolate issue

### Issue: "Test timeout"

**Solution:**
```csharp
[Fact(Timeout = 5000)]  // 5 second timeout
public async Task MyTest() { ... }
```

## 🔍 Diagnostics

### Get Application Health

```bash
curl http://localhost:5000/health
```

### Check Service Bus Topics

```bash
az servicebus topic list --namespace-name your-namespace
az servicebus topic subscription list --namespace-name your-namespace --topic-name html-screenshot-requests
```

### Check Blob Container

```bash
az storage container list --account-name yourstorageaccount
az storage blob list --account-name yourstorageaccount --container-name screenshots
```

### View Application Logs

```bash
# Docker logs
docker logs screentoimageconverter

# Kubernetes logs
kubectl logs deployment/screentoimageconverter
```

---

# Operations & Monitoring

Guide for operating and monitoring in production.

## 🏥 Health Checks

Three health checks provided:

**PlaywrightHealthCheck**
- Browser initialized?
- Browser process running?
- Can create context?

**BlobStorageHealthCheck**
- Can connect?
- Can access container?
- Can generate SAS URL?

**ConfigurationHealthCheck**
- All settings present?
- All values valid?

### Check Health

```bash
curl http://localhost:5000/health
```

## 📊 Monitoring Setup

### Application Insights

```csharp
// In Program.cs
builder.Services.AddApplicationInsights();
```

Monitor:
- Request duration
- Exception rates
- Custom metrics
- Dependency tracking

### Azure Monitor Alerts

```bash
az monitor metrics alert create \
  --name "High Error Rate" \
  --resource-group mygroup \
  --scopes /subscriptions/{subscriptionId}/resourceGroups/mygroup/providers/microsoft.insights/components/myapp \
  --condition "avg Exceptions/Server > 10"
```

## 📈 Performance Tuning

### Increase Throughput

```json
{
  "ServiceBus": {
	"MaxConcurrentCalls": 4
  },
  "Playwright": {
	"MaxRetryAttempts": 3
  }
}
```

### Reduce Resource Usage

```json
{
  "ServiceBus": {
	"MaxConcurrentCalls": 1,
	"PrefetchCount": 0
  },
  "Playwright": {
	"DefaultTimeoutMs": 20000,
	"MaxRetryAttempts": 1
  }
}
```

## 🔄 Scaling Strategy

### Horizontal Scaling
- Deploy multiple instances
- Each processes messages independently
- Linear throughput increase

### Vertical Scaling
- Increase `MaxConcurrentCalls`
- More memory/CPU per instance

## 🔐 Security Best Practices

1. Use Managed Identity, never store credentials
2. Keep SAS URLs short-lived (60 minutes)
3. Monitor failed authentication attempts
4. Use private blob storage (not public)
5. Enable diagnostic logging
6. Regular security updates

## 📋 Maintenance Tasks

### Daily
- Check health endpoint
- Monitor error rates
- Check dead-letter queue

### Weekly
- Review performance metrics
- Check for stuck messages
- Verify backups

### Monthly
- Review configuration
- Update dependencies
- Security audit

---

## 📚 Additional Resources

- [API Reference](./REFERENCE.md) - Message formats and technical specs
- [Architecture & Design](./REFERENCE.md) - System design and patterns
- [GitHub Issues](https://github.com/jerishpj/ScreenToImageConverter/issues) - Report bugs
- [GitHub Discussions](https://github.com/jerishpj/ScreenToImageConverter/discussions) - Ask questions

---

**Last Updated**: January 2024
**Version**: 1.0
