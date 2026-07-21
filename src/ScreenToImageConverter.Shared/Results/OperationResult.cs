namespace ScreenToImageConverter.Shared.Results;

/// <summary>
/// Generic result wrapper for operations that may succeed or fail.
/// Implements the Result pattern for functional-style error handling.
/// </summary>
/// <typeparam name="T">Type of the successful result value.</typeparam>
public class OperationResult<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// The result value if successful; null if failed.
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// Error message if the operation failed; null if successful.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; private set; }

    private OperationResult() { }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OperationResult<T> Success(T data)
    {
        return new OperationResult<T>
        {
            IsSuccess = true,
            Data = data,
            ErrorMessage = null,
            Exception = null
        };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static OperationResult<T> Failure(string errorMessage)
    {
        return new OperationResult<T>
        {
            IsSuccess = false,
            Data = default,
            ErrorMessage = errorMessage,
            Exception = null
        };
    }

    /// <summary>
    /// Creates a failed result with an exception.
    /// </summary>
    public static OperationResult<T> Failure(Exception exception)
    {
        return new OperationResult<T>
        {
            IsSuccess = false,
            Data = default,
            ErrorMessage = exception.Message,
            Exception = exception
        };
    }

    /// <summary>
    /// Creates a failed result with an error message and exception.
    /// </summary>
    public static OperationResult<T> Failure(string errorMessage, Exception exception)
    {
        return new OperationResult<T>
        {
            IsSuccess = false,
            Data = default,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Non-generic result wrapper for operations that don't return a value.
/// </summary>
public class OperationResult
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Error message if the operation failed; null if successful.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// The exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; private set; }

    private OperationResult() { }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OperationResult Success()
    {
        return new OperationResult
        {
            IsSuccess = true,
            ErrorMessage = null,
            Exception = null
        };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static OperationResult Failure(string errorMessage)
    {
        return new OperationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = null
        };
    }

    /// <summary>
    /// Creates a failed result with an exception.
    /// </summary>
    public static OperationResult Failure(Exception exception)
    {
        return new OperationResult
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            Exception = exception
        };
    }

    /// <summary>
    /// Creates a failed result with an error message and exception.
    /// </summary>
    public static OperationResult Failure(string errorMessage, Exception exception)
    {
        return new OperationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
