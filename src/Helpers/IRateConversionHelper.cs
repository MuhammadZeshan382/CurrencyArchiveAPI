namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper service for currency rate conversion operations.
/// Provides shared logic for converting rates between different base currencies.
/// </summary>
public interface IRateConversionHelper
{
    /// <summary>
    /// Gets historical rates for a specific date with optional base currency conversion.
    /// </summary>
    /// <param name="date">Date to get rates for.</param>
    /// <param name="baseCurrency">Base currency code.</param>
    /// <param name="symbols">Target currency symbols (empty for all).</param>
    /// <returns>Dictionary of currency codes and their rates.</returns>
    Dictionary<string, decimal> GetHistoricalRatesForDate(
        DateOnly date,
        string baseCurrency,
        List<string> symbols);

    /// <summary>
    /// Filters rates to only include specified symbols.
    /// </summary>
    /// <param name="rates">Source rates dictionary.</param>
    /// <param name="symbols">Currency symbols to include (empty for all).</param>
    /// <returns>Filtered rates dictionary.</returns>
    Dictionary<string, decimal> FilterRates(
        IReadOnlyDictionary<string, decimal> rates,
        List<string> symbols);

    /// <summary>
    /// Converts EUR-based rates to a different base currency.
    /// </summary>
    /// <param name="eurRates">EUR-based rates.</param>
    /// <param name="baseCode">New base currency code.</param>
    /// <param name="baseRate">Rate of base currency in EUR.</param>
    /// <param name="symbols">Target currency symbols to include.</param>
    /// <returns>Converted rates dictionary.</returns>
    Dictionary<string, decimal> ConvertRatesToBase(
        IReadOnlyDictionary<string, decimal> eurRates,
        string baseCode,
        decimal baseRate,
        List<string> symbols);
}
