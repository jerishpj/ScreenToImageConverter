# Configuration Reference

This document describes all configuration options for ScreenToImageConverter.

## Overview

Configuration is stored in JSON files following .NET conventions:
- `appsettings.json` – Base configuration (shared across all environments)
- `appsettings.Development.json` – Development overrides
- `appsettings.Production.json` – Production overrides

The active environment is controlled by `ASPNETCORE_ENVIRONMENT`:
```bash
export ASPNETCORE_ENVIRONMENT=Production
```

## Playwright Configuration

### Section: `Playwright`

Controls browser automation behavior.

```json
{
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1920,
	"DefaultViewportHeight": 1080,
	"DefaultTimeoutMs": 30000,
	"MaxRetryAttempts": 2,
	"DisableSandbox": false,
	"LaunchArgumentsJson": "[]"
  }
}
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| **BrowserType** | string | `chromium` | Browser engine to use: `chromium`, `firefox`, or `webkit` |
| **DefaultViewportWidth** | int | `1920` | Default screenshot width in pixels |
| **DefaultViewportHeight** | int | `1080` | Default screenshot height in pixels |
| **DefaultTimeoutMs** | int | `30000` | Default timeout in milliseconds (30 seconds) |
| **MaxRetryAttempts** | int | `2` | Number of retry attempts on transient failures |
| **DisableSandbox** | bool | `false` | Disable browser sandbox (required for Docker/Kubernetes) |
| **LaunchArgumentsJson** | string | `"[]"` | Additional browser launch arguments as JSON array |

### Development Example

```json
{
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1280,
	"DefaultViewportHeight": 720,
	"DefaultTimeoutMs": 60000,
	"MaxRetryAttempts": 3,
	"DisableSandbox": false
  }
}
```

### Production Example

```json
{
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1920,
	"DefaultViewportHeight": 1080,
	"DefaultTimeoutMs": 30000,
	"MaxRetryAttempts": 2,
	"DisableSandbox": true
  }
}
```

### Browser Type Selection

| Type | Use Case | Performance | Memory |
|------|----------|-------------|--------|
| **chromium** | Most compatible, recommended for production | Fast | Moderate |
| **firefox** | Alternative engine, better compatibility with some sites | Fast | Moderate |
| **webkit** | Lightweight, Safari-compatible | Fastest | Lowest |

### Launch Arguments

For advanced browser control, pass custom launch arguments:

```json
{
  "Playwright": {
	"LaunchArgumentsJson": "[\"--disable-dev-shm-usage\", \"--disable-gpu\"]"
  }
}
```

Common arguments:
- `--disable-dev-shm-usage` – Fix for limited shared memory (Docker)
- `--disable-gpu` – Disable GPU acceleration
- `--no-sandbox` – Disable sandbox (for unprivileged containers)

## Azure Service Bus Configuration

### Section: `AzureServiceBus`

Configures message consumption and publishing.

```json
{
  "AzureServiceBus": {
	"ConnectionString": "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY",
	"QueueName": "screenshot-requests",
	"TopicName": "screenshot-events",
	"SubscriptionName": "all-events",
	"MaxConcurrentCalls": 10,
	"PrefetchCount": 10,
	"MessageLockDurationMinutes": 5,
	"DeadLetterQueueEnabled": true
  }
}
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| **ConnectionString** | string | Required | Service Bus namespace connection string |
| **QueueName** | string | `screenshot-requests` | Queue name for receiving screenshot requests |
| **TopicName** | string | `screenshot-events` | Topic name for publishing completion events |
| **SubscriptionName** | string | `all-events` | Subscription name for topic subscriptions |
| **MaxConcurrentCalls** | int | `10` | Maximum concurrent message handlers |
| **PrefetchCount** | int | `10` | Number of messages to prefetch |
| **MessageLockDurationMinutes** | int | `5` | Lock duration for message processing |
| **DeadLetterQueueEnabled** | bool | `true` | Enable dead-letter queue for failed messages |

### Using Managed Identity (Production)

For Azure App Service, omit connection string and use Managed Identity:

```json
{
  "AzureServiceBus": {
	"NamespaceName": "screentoimageconverter-ns.servicebus.windows.net",
	"QueueName": "screenshot-requests",
	"TopicName": "screenshot-events",
	"SubscriptionName": "all-events",
	"UseManagedIdentity": true
  }
}
```

Then assign role in Azure:
```bash
az role assignment create \
  --role "Azure Service Bus Data Owner" \
  --assignee-object-id <app-service-identity-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<group>/providers/Microsoft.ServiceBus/namespaces/<namespace>
```

### Connection String Format

```
Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<key-name>;SharedAccessKey=<key-value>
```

Get from Azure CLI:
```bash
az servicebus namespace authorization-rule keys list \
  --namespace-name screentoimageconverter-ns \
  --name RootManageSharedAccessKey
```

## Azure Blob Storage Configuration

### Section: `AzureBlobStorage`

Configures screenshot upload and retrieval.

```json
{
  "AzureBlobStorage": {
	"ConnectionString": "DefaultEndpointsProtocol=https;AccountName=screentoimageconverter;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net",
	"ContainerName": "screenshots",
	"BlobNamePrefix": "screenshots/",
	"CreateContainerIfNotExists": true,
	"PublicAccess": "None",
	"DefaultContentType": "image/png"
  }
}
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| **ConnectionString** | string | Required | Storage account connection string |
| **ContainerName** | string | `screenshots` | Container name for storing images |
| **BlobNamePrefix** | string | `screenshots/` | Prefix for blob names (for organization) |
| **CreateContainerIfNotExists** | bool | `true` | Auto-create container on startup |
| **PublicAccess** | string | `None` | Access level: `None`, `Blob`, or `Container` |
| **DefaultContentType** | string | `image/png` | MIME type for uploaded blobs |

### Using Managed Identity (Production)

```json
{
  "AzureBlobStorage": {
	"StorageAccountName": "screentoimageconverter",
	"ContainerName": "screenshots",
	"UseManagedIdentity": true
  }
}
```

Then assign role in Azure:
```bash
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee-object-id <app-service-identity-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<group>/providers/Microsoft.Storage/storageAccounts/<account>
```

### Connection String Format

```
DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net
```

Get from Azure CLI:
```bash
az storage account show-connection-string \
  --name screentoimageconverter \
  --resource-group my-resource-group
```

## Application Logging Configuration

### Section: `Logging`

Controls log output verbosity.

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning",
	  "System": "Warning",
	  "ScreenToImageConverter": "Information"
	}
  }
}
```

### Log Levels

| Level | Usage |
|-------|-------|
| `Trace` | Very detailed diagnostic info (rarely needed) |
| `Debug` | Detailed diagnostic info (development only) |
| `Information` | General informational messages (default) |
| `Warning` | Warning messages (potential issues) |
| `Error` | Error messages (failures, exceptions) |
| `Critical` | Critical errors (system failure) |

### Development Configuration

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Debug",
	  "Microsoft": "Information",
	  "System": "Information"
	}
  }
}
```

### Production Configuration

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning",
	  "System": "Warning"
	}
  }
}
```

## Application Insights Configuration

### Section: `ApplicationInsights`

Enables monitoring and telemetry.

```json
{
  "ApplicationInsights": {
	"InstrumentationKey": "your-instrumentation-key"
  }
}
```

Or use environment variable:
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=YOUR_KEY;..."
```

## Complete Example Configurations

### Development (appsettings.Development.json)

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Debug",
	  "Microsoft": "Information"
	}
  },
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1280,
	"DefaultViewportHeight": 720,
	"DefaultTimeoutMs": 60000,
	"MaxRetryAttempts": 3,
	"DisableSandbox": false
  },
  "AzureServiceBus": {
	"ConnectionString": "Endpoint=sb://local-ns.servicebus.windows.net/;...",
	"QueueName": "screenshot-requests",
	"TopicName": "screenshot-events",
	"MaxConcurrentCalls": 5
  },
  "AzureBlobStorage": {
	"ConnectionString": "UseDevelopmentStorage=true",
	"ContainerName": "screenshots",
	"CreateContainerIfNotExists": true
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning"
	}
  },
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1920,
	"DefaultViewportHeight": 1080,
	"DefaultTimeoutMs": 30000,
	"MaxRetryAttempts": 2,
	"DisableSandbox": true
  },
  "AzureServiceBus": {
	"NamespaceName": "screentoimageconverter-ns.servicebus.windows.net",
	"QueueName": "screenshot-requests",
	"TopicName": "screenshot-events",
	"SubscriptionName": "all-events",
	"UseManagedIdentity": true,
	"MaxConcurrentCalls": 20,
	"DeadLetterQueueEnabled": true
  },
  "AzureBlobStorage": {
	"StorageAccountName": "screentoimageconverter",
	"ContainerName": "screenshots",
	"UseManagedIdentity": true,
	"PublicAccess": "None"
  },
  "ApplicationInsights": {
	"InstrumentationKey": "${APPINSIGHTS_KEY}"
  }
}
```

## Configuration Validation

Configuration is validated on application startup. If invalid settings are detected, the application will fail with clear error messages.

Example error:
```
FATAL: Configuration validation failed: PlaywrightOptions.DefaultTimeoutMs must be greater than 0
```

## Environment Variables

Override any configuration section using environment variables with the pattern:
```
SECTION__SUBSECTION__KEY=value
```

Examples:
```bash
export Playwright__DefaultTimeoutMs=45000
export AzureServiceBus__QueueName=my-custom-queue
export Logging__LogLevel__Default=Debug
```

## Key Rotation

### Connection String Rotation

To rotate Service Bus or Storage connection strings without downtime:

1. Generate a new connection string in Azure
2. Update `appsettings.json`
3. Restart the application
4. Verify the application connects successfully
5. Revoke the old connection string in Azure

### Using Azure Key Vault

For production, store connection strings in Azure Key Vault:

```bash
# Store in Key Vault
az keyvault secret set \
  --vault-name my-keyvault \
  --name servicebus-connection-string \
  --value "Endpoint=sb://..."

# Reference in configuration
export ServiceBus__ConnectionString="@Microsoft.KeyVault(SecretUri=https://my-keyvault.vault.azure.net/secrets/servicebus-connection-string/)"
```

Then configure your App Service to use Managed Identity to access Key Vault.

## Docker Configuration

For Docker deployments, set environment variables:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0

ENV ASPNETCORE_ENVIRONMENT=Production
ENV Playwright__DisableSandbox=true
ENV Playwright__LaunchArgumentsJson='["--disable-dev-shm-usage"]'
ENV AzureServiceBus__UseManagedIdentity=true
ENV AzureBlobStorage__UseManagedIdentity=true

COPY ./src/ScreenToImageConverter.Worker/bin/Release/net9.0/publish/ .

ENTRYPOINT ["dotnet", "ScreenToImageConverter.Worker.dll"]
```

## Troubleshooting Configuration

### Issue: "Connection refused"
- Verify connection string is correct
- Check firewall rules in Azure
- Ensure resource exists and is accessible

### Issue: "Playwright browser not found"
- Ensure Playwright dependencies are installed: `playwright install`
- Check available disk space
- For Docker, set `DisableSandbox: true`

### Issue: "Service Bus message not processed"
- Verify `QueueName` matches the actual queue name
- Check `MaxConcurrentCalls` is > 0
- Review logs for error messages

### Issue: "Blob storage upload failed"
- Verify `ContainerName` exists
- Check storage account access credentials
- Ensure managed identity has correct role assignment

## Configuration Best Practices

1. ✅ Never commit secrets to version control
2. ✅ Use `appsettings.json` for defaults only
3. ✅ Use environment-specific `appsettings.{Environment}.json` for overrides
4. ✅ Use environment variables for sensitive data
5. ✅ Use Azure Key Vault for production secrets
6. ✅ Document all required configuration
7. ✅ Validate configuration on startup
8. ✅ Use managed identity instead of connection strings in Azure
9. ✅ Keep timeouts reasonable for your use case
10. ✅ Monitor configuration changes in production

## Next Steps

- See [GETTING_STARTED.md](./GETTING_STARTED.md) for setup instructions
- See [DEVELOPMENT.md](./DEVELOPMENT.md) for configuration validation code
- Review your `appsettings.json` against this reference
