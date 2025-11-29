namespace CurrencyArchiveAPI.Models;

/// <summary>
/// Request parameters for historical rates endpoint.
/// </summary>
public record HistoricalRatesRequest
{
    /// <summary>
    /// Date for which historical rates are requested (YYYY-MM-DD).
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Base currency code (default: EUR).
    /// </summary>
    public string Base { get; init; } = "EUR";

    /// <summary>
    /// Comma-separated list of currency codes to include in response.
    /// If null or empty, all available currencies are returned.
    /// </summary>
    public string? Symbols { get; init; }
}
