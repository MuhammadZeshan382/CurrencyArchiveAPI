namespace CurrencyArchiveAPI.Models;

/// <summary>
/// Response model for historical rates endpoint.
/// </summary>
public record HistoricalRatesResponse
{
    /// <summary>
    /// The date for which rates are provided.
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The base currency for the rates.
    /// </summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>
    /// Dictionary of currency codes to exchange rates.
    /// All rates are relative to the base currency.
    /// </summary>
    public Dictionary<string, decimal> Rates { get; init; } = new();

    /// <summary>
    /// Timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
