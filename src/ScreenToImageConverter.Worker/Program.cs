using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using ScreenToImageConverter.Worker;
using ScreenToImageConverter.Worker.Extensions;
using ScreenToImageConverter.Worker.Features.BlobStorageUpload.Extensions;
using ScreenToImageConverter.Worker.Features.ScreenshotCapture.Extensions;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Extensions;
using ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Handlers;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog for structured logging
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "ScreenToImageConverter.Worker")
        .WriteTo.Console()
        .CreateLogger();

    builder.Services.AddSerilog();

    // Add Application Insights telemetry
    builder.Services.AddApplicationInsightsTelemetryWorkerService();

    // Register application configuration with validation
    builder.Services.AddApplicationConfiguration(builder.Configuration);

    // Register vertical slice features
    builder.Services.AddScreenshotCaptureFeature();
    builder.Services.AddBlobStorageUploadFeature();
    builder.Services.AddServiceBusMessagingFeature();

    // Register orchestrator
    builder.Services.AddScoped<ScreenshotProcessingOrchestrator>();

    // Add health checks
    builder.Services.AddApplicationHealthChecks();

    // Register hosted services
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();

    // Log application startup
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🚀 ScreenToImageConverter Worker Service starting...");
    logger.LogInformation("Environment: {Environment}", host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);

    // Initialize Playwright provider
    logger.LogInformation("Initializing Playwright screenshot provider...");
    await host.Services.InitializePlaywrightAsync(CancellationToken.None);

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
