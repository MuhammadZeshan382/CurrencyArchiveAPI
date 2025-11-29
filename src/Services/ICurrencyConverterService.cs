namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Service interface for basic currency conversion operations.
/// Handles single and bulk currency conversions.
/// </summary>
public interface ICurrencyConverterService
{
    /// <summary>
    /// Converts an amount from one currency to another on a specific date.
    /// </summary>
    /// <param name="fromCurrency">Source currency code (e.g., USD, GBP).</param>
    /// <param name="toCurrency">Target currency code (e.g., EUR, JPY).</param>
    /// <param name="date">Date for the conversion rate.</param>
    /// <param name="amount">Amount to convert (must be greater than 0).</param>
    /// <returns>Converted amount in the target currency.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when currency or date not found.</exception>
    decimal Convert(string fromCurrency, string toCurrency, DateOnly date, decimal amount);

    /// <summary>
    /// Gets all available currency codes for a specific date.
    /// </summary>
    /// <param name="date">Optional date to check availability. If null, returns all currencies ever available.</param>
    /// <returns>Collection of available currency codes.</returns>
    IEnumerable<string> GetAvailableCurrencies(DateOnly? date = null);

    /// <summary>
    /// Checks if a specific currency is available on a given date.
    /// </summary>
    /// <param name="currencyCode">Currency code to check.</param>
    /// <param name="date">Date to check availability for.</param>
    /// <returns>True if currency is available, false otherwise.</returns>
    bool IsCurrencyAvailable(string currencyCode, DateOnly date);

    /// <summary>
    /// Gets the exchange rate for a currency pair on a specific date.
    /// </summary>
    /// <param name="fromCurrency">Source currency code.</param>
    /// <param name="toCurrency">Target currency code.</param>
    /// <param name="date">Date for the exchange rate.</param>
    /// <returns>Exchange rate (1 unit of fromCurrency = X units of toCurrency).</returns>
    decimal GetExchangeRate(string fromCurrency, string toCurrency, DateOnly date);
}
