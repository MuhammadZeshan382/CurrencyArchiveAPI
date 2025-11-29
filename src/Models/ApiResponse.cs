namespace CurrencyArchiveAPI.Models;

/// <summary>
/// Unified API response wrapper for all endpoints.
/// </summary>
/// <typeparam name="T">The type of data being returned.</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// A descriptive message about the response.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The actual data payload (null if unsuccessful).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Collection of error messages (empty if successful).
    /// </summary>
    public IEnumerable<string> Errors { get; init; } = Enumerable.Empty<string>();

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Request successful")
        => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

    /// <summary>
    /// Creates a failure response with errors.
    /// </summary>
    public static ApiResponse<T> FailureResponse(string message, IEnumerable<string>? errors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? Enumerable.Empty<string>()
        };
}
