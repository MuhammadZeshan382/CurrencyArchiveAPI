namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Service interface for currency conversion operations.
/// </summary>
public interface ICurrencyConversionService
{
    /// <summary>
    /// Converts an amount from one currency to another on a specific date.
    /// </summary>
    /// <param name="fromCurrency">Source currency code (e.g., USD).</param>
    /// <param name="toCurrency">Target currency code (e.g., GBP).</param>
    /// <param name="date">Date for the conversion.</param>
    /// <param name="amount">Amount to convert.</param>
    /// <returns>Converted amount in target currency.</returns>
    decimal Convert(string fromCurrency, string toCurrency, DateOnly date, decimal amount);

    /// <summary>
    /// Gets all available currencies for a specific date.
    /// </summary>
    /// <param name="date">Optional date to check. If null, returns all currencies ever available.</param>
    /// <returns>Collection of currency codes.</returns>
    IEnumerable<string> GetAvailableCurrencies(DateOnly? date = null);

    /// <summary>
    /// Gets the rate history for a specific currency over a date range.
    /// </summary>
    /// <param name="currency">Currency code to query.</param>
    /// <param name="startDate">Start date of the range.</param>
    /// <param name="endDate">End date of the range.</param>
    /// <returns>Dictionary mapping dates to exchange rates.</returns>
    Dictionary<DateOnly, decimal> GetRateHistory(string currency, DateOnly startDate, DateOnly endDate);
}
