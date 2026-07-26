using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using ScreenToImageConverter.Worker;
using ScreenToImageConverter.Worker.Extensions;
using ScreenToImageConverter.Worker.Infrastructure.Resilience;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog for structured logging with enhanced diagnostics
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

    // Register resilience and diagnostics services
    builder.Services.AddApplicationResilience();

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
    logger.LogInformation("Environment: {Environment}", 
        host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);

    // Run startup diagnostics
    logger.LogInformation("🔍 Running startup diagnostics...");
    try
    {
        // Create a scope to resolve scoped services
        using var scope = host.Services.CreateScope();
        var diagnostics = scope.ServiceProvider.GetRequiredService<IStartupDiagnostics>();
        var diagnosticResult = await diagnostics.ValidateAsync(CancellationToken.None);

        if (!diagnosticResult.AllCriticalDependenciesAvailable)
        {
            logger.LogWarning(
                "⚠️ Startup diagnostics detected unavailable dependencies:\n{DiagnosticSummary}",
                diagnosticResult.GetSummary());
        }
        else
        {
            logger.LogInformation("✅ All critical dependencies are available");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ Startup diagnostics encountered an issue, but continuing startup");
    }

    // Initialize Playwright provider
    logger.LogInformation("💾 Initializing Playwright screenshot provider...");
    await host.Services.InitializePlaywrightAsync(CancellationToken.None);
    logger.LogInformation("✅ Playwright screenshot provider initialized");

    logger.LogInformation("📊 Starting worker service...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Application terminated unexpectedly. Error: {ErrorMessage}", ex.Message);
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
