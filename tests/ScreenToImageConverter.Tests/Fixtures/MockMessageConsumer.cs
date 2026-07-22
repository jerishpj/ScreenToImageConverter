using Microsoft.Extensions.Logging;
using ScreenToImageConverter.Worker.Infrastructure.Notifications;

namespace ScreenToImageConverter.Tests.Fixtures;

/// <summary>
/// Mock implementation of IMessageConsumer for testing purposes.
/// Simulates Azure Service Bus message consumption without actual broker connectivity.
/// </summary>
public class MockMessageConsumer : IMessageConsumer
{
    private readonly ILogger<MockMessageConsumer> _logger;
    private bool _isConnected;
    private bool _disposed;

    public bool IsConnected => _isConnected;

    public MockMessageConsumer(ILogger<MockMessageConsumer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockMessageConsumer starting");
        _isConnected = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MockMessageConsumer stopping");
        _isConnected = false;
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("MockMessageConsumer disposing");
        await StopAsync();
        _disposed = true;
    }
}
