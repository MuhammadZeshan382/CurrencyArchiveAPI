namespace CurrencyArchiveAPI.Models;

/// <summary>
/// RFC 7807 Problem Details response for error handling.
/// </summary>
public record ProblemDetailsResponse
{
    /// <summary>
    /// A URI reference that identifies the problem type.
    /// </summary>
    public string Type { get; init; } = "about:blank";

    /// <summary>
    /// A short, human-readable summary of the problem type.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem.
    /// </summary>
    public string Instance { get; init; } = string.Empty;

    /// <summary>
    /// Additional validation errors if applicable.
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; init; }
}
