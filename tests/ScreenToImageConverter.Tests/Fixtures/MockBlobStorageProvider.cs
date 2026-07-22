using Microsoft.Extensions.Logging;
using ScreenToImageConverter.Worker.Infrastructure.Storage;

namespace ScreenToImageConverter.Tests.Fixtures;

/// <summary>
/// Mock implementation of IBlobStorageProvider for testing purposes.
/// Stores blobs in memory without actual Azure Blob Storage connectivity.
/// </summary>
public class MockBlobStorageProvider : IBlobStorageProvider
{
    private readonly ILogger<MockBlobStorageProvider> _logger;
    private bool _isConnected;
    private bool _disposed;
    private readonly Dictionary<string, byte[]> _blobs = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConnected => _isConnected;

    /// <summary>
    /// In-memory blob storage for testing.
    /// </summary>
    public IReadOnlyDictionary<string, byte[]> Blobs => _blobs;

    public MockBlobStorageProvider(ILogger<MockBlobStorageProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isConnected = true;
    }

    /// <summary>
    /// Clears all blobs from memory storage.
    /// </summary>
    public void ClearBlobs()
    {
        _blobs.Clear();
    }

    public async Task<BlobUrlInfo> UploadAsync(
        string containerName,
        string blobName,
        byte[] data,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("MockBlobStorageProvider is not connected");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        }

        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        }

        var key = $"{containerName}/{blobName}";
        _blobs[key] = data;

        _logger.LogInformation("💾 MockBlobStorageProvider uploaded blob: {BlobName} ({SizeKb} KB)", 
            blobName, data.Length / 1024);

        var blobUri = $"https://mock.blob.core.windows.net/{containerName}/{blobName}";
        var sasUrl = $"https://mock.blob.core.windows.net/{containerName}/{blobName}?sv=2021-06-08&sig=mock";

        return new BlobUrlInfo
        {
            Uri = blobUri,
            SasUrl = sasUrl,
            SasUrlExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }

    public async Task<BlobUrlInfo> GenerateSasUrlAsync(
        string containerName,
        string blobName,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("MockBlobStorageProvider is not connected");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        }

        var key = $"{containerName}/{blobName}";
        if (!_blobs.ContainsKey(key))
        {
            throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'");
        }

        var blobUri = $"https://mock.blob.core.windows.net/{containerName}/{blobName}";
        var sasUrl = $"https://mock.blob.core.windows.net/{containerName}/{blobName}?sv=2021-06-08&sig=mock";

        _logger.LogInformation("🔗 MockBlobStorageProvider generated SAS URL for: {BlobName}", blobName);

        return new BlobUrlInfo
        {
            Uri = blobUri,
            SasUrl = sasUrl,
            SasUrlExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("MockBlobStorageProvider is not connected");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        }

        var key = $"{containerName}/{blobName}";
        if (_blobs.Remove(key))
        {
            _logger.LogInformation("🗑️  MockBlobStorageProvider deleted blob: {BlobName}", blobName);
        }
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("MockBlobStorageProvider is not connected");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        }

        var key = $"{containerName}/{blobName}";
        return _blobs.ContainsKey(key);
    }

    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return _isConnected;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("MockBlobStorageProvider disposing");
        _isConnected = false;
        _blobs.Clear();
        _disposed = true;
    }
}
