namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Service interface for accessing in-memory currency exchange rate data.
/// </summary>
public interface ICurrencyDataService
{
    /// <summary>
    /// Gets the exchange rate for a specific currency on a specific date.
    /// </summary>
    /// <param name="date">The date for the exchange rate.</param>
    /// <param name="currencyCode">The currency code (e.g., USD, GBP).</param>
    /// <returns>The exchange rate relative to EUR, or null if not found.</returns>
    decimal? GetRate(DateOnly date, string currencyCode);

    /// <summary>
    /// Gets all available currencies for a specific date.
    /// </summary>
    /// <param name="date">The date to query.</param>
    /// <returns>Collection of currency codes available on that date.</returns>
    IEnumerable<string> GetAvailableCurrencies(DateOnly date);

    /// <summary>
    /// Gets all exchange rates for a specific date.
    /// </summary>
    /// <param name="date">The date to query.</param>
    /// <returns>Dictionary of currency codes to exchange rates, or null if date not found.</returns>
    IReadOnlyDictionary<string, decimal>? GetRatesForDate(DateOnly date);

    /// <summary>
    /// Checks if data is loaded and available.
    /// </summary>
    bool IsDataLoaded { get; }

    /// <summary>
    /// Gets the total number of dates loaded.
    /// </summary>
    int TotalDatesLoaded { get; }

    /// <summary>
    /// Gets the date range of available data.
    /// </summary>
    (DateOnly MinDate, DateOnly MaxDate) GetDateRange();
}
