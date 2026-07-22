namespace ScreenToImageConverter.Worker.Infrastructure.Storage;

/// <summary>
/// Represents a blob URL with metadata.
/// </summary>
public class BlobUrlInfo
{
    /// <summary>
    /// The full blob URI.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The SAS URL with authentication token for time-limited access.
    /// </summary>
    public string? SasUrl { get; set; }

    /// <summary>
    /// Expiration time for the SAS URL (UTC).
    /// </summary>
    public DateTime? SasUrlExpiresAt { get; set; }
}

/// <summary>
/// Interface for uploading and managing files in Azure Blob Storage.
/// Implementations handle blob operations, SAS URL generation, and lifecycle management.
/// </summary>
public interface IBlobStorageProvider : IAsyncDisposable
{
    /// <summary>
    /// Uploads a screenshot file to blob storage.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name of the blob file.</param>
    /// <param name="data">Byte array containing the file data.</param>
    /// <param name="contentType">MIME type of the file (e.g., "image/png").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>BlobUrlInfo containing the blob URI and SAS URL.</returns>
    Task<BlobUrlInfo> UploadAsync(
        string containerName,
        string blobName,
        byte[] data,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates a SAS URL for time-limited access to a blob.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name of the blob file.</param>
    /// <param name="expirationMinutes">Number of minutes before the URL expires. Default: 60.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>BlobUrlInfo with SAS URL and expiration time.</returns>
    Task<BlobUrlInfo> GenerateSasUrlAsync(
        string containerName,
        string blobName,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name of the blob file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a blob exists in storage.
    /// </summary>
    /// <param name="containerName">Name of the blob container.</param>
    /// <param name="blobName">Name of the blob file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob exists; false otherwise.</returns>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the blob storage provider is connected and accessible.
    /// </summary>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken);
}
