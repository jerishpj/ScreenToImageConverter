# Test Project Documentation

## Overview

The `ScreenToImageConverter.Tests` project provides comprehensive testing utilities and fixtures for the ScreenToImageConverter Worker Service. It uses XUnit as the test framework with Moq for mocking dependencies.

## Project Structure

```
tests/ScreenToImageConverter.Tests/
├── Builders/
│   └── HtmlScreenshotRequestBuilder.cs          # Fluent builder for test requests
├── Factories/
│   └── TestDataFactory.cs                       # Factory for creating consistent test data
├── Fixtures/
│   ├── MockScreenshotProvider.cs                # Mock IScreenshotProvider implementation
│   ├── MockMessageConsumer.cs                   # Mock IMessageConsumer implementation
│   └── MockBlobStorageProvider.cs               # Mock IBlobStorageProvider implementation
├── Integration/
│   └── ConvertHtmlToImageHandlerTests.cs        # Integration tests demonstrating usage
├── GlobalUsings.cs                              # Global namespace declarations
└── ScreenToImageConverter.Tests.csproj          # Project file
```

## Key Components

### 1. Test Fixtures

#### MockScreenshotProvider
- Simulates screenshot capture without launching actual browsers
- Generates valid PNG data with configurable behavior
- Supports failure simulation for testing error paths

**Usage:**
```csharp
var mockProvider = new MockScreenshotProvider(logger);
await mockProvider.InitializeAsync(CancellationToken.None);
var screenshotData = await mockProvider.CaptureScreenshotAsync("https://example.com");
```

#### MockMessageConsumer
- Simulates Azure Service Bus message consumption
- Does not require actual broker connectivity
- Implements IMessageConsumer interface

**Usage:**
```csharp
var mockConsumer = new MockMessageConsumer(logger);
await mockConsumer.StartAsync(CancellationToken.None);
// ... test code ...
await mockConsumer.StopAsync(CancellationToken.None);
```

#### MockBlobStorageProvider
- Stores blobs in-memory for testing
- Tracks all operations for verification
- Supports SAS URL generation and blob lifecycle management

**Usage:**
```csharp
var mockStorage = new MockBlobStorageProvider(logger);
var result = await mockStorage.UploadAsync("container", "blob.png", data, "image/png");
var exists = await mockStorage.ExistsAsync("container", "blob.png");
```

### 2. Builders

#### HtmlScreenshotRequestBuilder
Fluent API for building test instances of `HtmlScreenshotRequest` with sensible defaults.

**Usage:**
```csharp
var request = new HtmlScreenshotRequestBuilder()
	.WithUrl("https://example.com")
	.WithViewport(1920, 1080)
	.WithTimeout(30000)
	.Build();
```

### 3. Factories

#### TestDataFactory
Provides static factory methods for creating consistent test data across all tests.

**Methods:**
- `CreateValidHtmlScreenshotRequest()` - Valid request with defaults
- `CreateInvalidHtmlScreenshotRequest_MissingUrl()` - Invalid: missing URL
- `CreateInvalidHtmlScreenshotRequest_InvalidViewport()` - Invalid: zero viewport
- `CreateSuccessfulScreenshotCompletedEvent()` - Success event with blob info
- `CreateFailedScreenshotCompletedEvent()` - Failure event with error message

## Dependencies

The test project references:
- **XUnit**: Test framework for writing and running tests
- **Moq**: Mocking library for creating test doubles
- **Microsoft.Extensions.DependencyInjection**: Dependency injection container
- **Microsoft.Extensions.Logging**: Logging abstractions

All test dependencies are configured in `ScreenToImageConverter.Tests.csproj`.

## Running Tests

### Run All Tests
```powershell
dotnet test
```

### Run Specific Test Class
```powershell
dotnet test --filter "FullyQualifiedName~ConvertHtmlToImageHandlerTests"
```

### Run Tests with Verbosity
```powershell
dotnet test -v d
```

### Debug Tests in Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Right-click on a test and select "Debug"

## Test Organization

All tests follow the AAA (Arrange-Act-Assert) pattern:

```csharp
[Fact]
public async Task TestName_Scenario_ExpectedOutcome()
{
	// Arrange
	var dependency = new MockSomething();

	// Act
	var result = await dependency.DoSomethingAsync();

	// Assert
	Assert.NotNull(result);
}
```

## Best Practices for Test Development

1. **Use Builders for Complex Objects**
   ```csharp
   var request = new HtmlScreenshotRequestBuilder()
	   .WithUrl("https://example.com")
	   .Build();
   ```

2. **Use Factories for Consistent Test Data**
   ```csharp
   var validRequest = TestDataFactory.CreateValidHtmlScreenshotRequest();
   var invalidRequest = TestDataFactory.CreateInvalidHtmlScreenshotRequest_MissingUrl();
   ```

3. **Test Both Success and Failure Paths**
   ```csharp
   [Fact]
   public async Task Feature_ValidInput_ShouldSucceed() { }

   [Fact]
   public async Task Feature_InvalidInput_ShouldFail() { }
   ```

4. **Use Descriptive Test Names**
   - Format: `MethodUnderTest_Scenario_ExpectedOutcome`
   - Example: `ValidateRequest_WithMissingUrl_ShouldThrow`

5. **Keep Tests Independent**
   - Each test should be runnable in any order
   - Use fixtures/factories to set up consistent state
   - Clean up after tests if needed

## Adding New Tests

1. Create a new test class in the appropriate folder (e.g., `Integration/` for integration tests)
2. Inherit from test utilities as needed
3. Use Global Usings for common namespaces (already configured)
4. Follow AAA pattern and naming conventions
5. Run `dotnet test` to verify new tests pass

## Debugging Tests

### Enable Verbose Logging
```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
	builder.AddConsole();
	builder.SetMinimumLevel(LogLevel.Debug);
});
```

### Set Breakpoints
- In Visual Studio, click on the line number to set a breakpoint
- Run test with debugger attached
- Use Debug → Windows → Immediate to inspect variables

### Inspect Mock State
```csharp
var publishedMessages = mockPublisher.PublishedMessages;
var uploadedBlobs = mockStorage.Blobs;
```

## Project Configuration

The test project is configured with:
- **Target Framework**: .NET 9.0
- **Nullable**: Enabled
- **Implicit Usings**: Enabled
- **Test Framework**: XUnit with VSTest adapter

Global usings in `GlobalUsings.cs` automatically include:
- `Xunit`
- `Moq`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.DependencyInjection`

## CI/CD Integration

Tests are automatically run as part of the build pipeline. Ensure:
1. All tests pass locally before committing
2. New features include corresponding tests
3. Test names clearly describe what is being tested
4. Maintain >80% code coverage for new code

## References

- [XUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Microsoft.Extensions.Logging](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging)
