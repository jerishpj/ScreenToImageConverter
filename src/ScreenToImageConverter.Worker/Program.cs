using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using ScreenToImageConverter.Worker;
using ScreenToImageConverter.Worker.Extensions;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog for structured logging
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "HtmlToImageWorker")
        .WriteTo.Console()
        .CreateLogger();

    builder.Services.AddSerilog();

    // Add Application Insights telemetry
    builder.Services.AddApplicationInsightsTelemetryWorkerService();

    // Register application configuration with validation
    builder.Services.AddApplicationConfiguration(builder.Configuration);

    // Register the main ConvertHtmlToImage feature (vertical slice)
    builder.Services.AddConvertHtmlToImageFeature();

    // Add health checks
    builder.Services.AddApplicationHealthChecks();

    // Register hosted services
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();

    // Log application startup
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🚀 HtmlToImageWorker Service starting...");
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
