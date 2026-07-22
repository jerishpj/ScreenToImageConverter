namespace ScreenToImageConverter.Worker.Infrastructure.Exceptions;

/// <summary>
/// Base exception for the screenshot processing system.
/// </summary>
public class ScreenshotProcessingException : Exception
{
    public ScreenshotProcessingException(string message) : base(message) { }

    public ScreenshotProcessingException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when screenshot capture fails.
/// </summary>
public class ScreenshotCapturException : ScreenshotProcessingException
{
    public ScreenshotCapturException(string message) : base(message) { }

    public ScreenshotCapturException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when blob storage operation fails.
/// </summary>
public class BlobStorageException : ScreenshotProcessingException
{
    public BlobStorageException(string message) : base(message) { }

    public BlobStorageException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when Service Bus operation fails.
/// </summary>
public class ServiceBusException : ScreenshotProcessingException
{
    public ServiceBusException(string message) : base(message) { }

    public ServiceBusException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public class ConfigurationException : ScreenshotProcessingException
{
    public ConfigurationException(string message) : base(message) { }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a message is invalid or malformed.
/// </summary>
public class InvalidMessageException : ScreenshotProcessingException
{
    public InvalidMessageException(string message) : base(message) { }

    public InvalidMessageException(string message, Exception innerException)
        : base(message, innerException) { }
}
