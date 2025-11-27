using CurrencyArchiveAPI.Models;

namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Service interface for historical exchange rate operations.
/// Handles timeseries data, fluctuations, and historical rate queries.
/// </summary>
public interface IHistoricalRatesService
{
    /// <summary>
    /// Gets historical exchange rates for a specific date.
    /// </summary>
    /// <param name="date">The date to get rates for.</param>
    /// <param name="baseCurrency">Base currency code (optional, defaults to EUR).</param>
    /// <param name="symbols">Target currency symbols to retrieve rates for.</param>
    /// <returns>Historical rates response with currency rates.</returns>
    HistoricalRatesResponse GetHistoricalRates(
        DateOnly date,
        string? baseCurrency = null,
        List<string>? symbols = null);

    /// <summary>
    /// Gets timeseries data showing day-by-day exchange rates.
    /// </summary>
    /// <param name="baseCurrency">Base currency code.</param>
    /// <param name="symbols">Target currency symbols.</param>
    /// <param name="startDate">Start date of the timeseries.</param>
    /// <param name="endDate">End date of the timeseries.</param>
    /// <returns>Timeseries response with daily rates and changes.</returns>
    TimeseriesResponse GetTimeseries(
        string baseCurrency,
        List<string> symbols,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// Gets fluctuation analysis showing rate changes over a period.
    /// </summary>
    /// <param name="baseCurrency">Base currency code.</param>
    /// <param name="symbols">Target currency symbols.</param>
    /// <param name="startDate">Start date for fluctuation analysis.</param>
    /// <param name="endDate">End date for fluctuation analysis.</param>
    /// <returns>Fluctuation response with high, low, change data.</returns>
    FluctuationResponse GetFluctuation(
        string baseCurrency,
        List<string> symbols,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// Gets the rate history for a specific currency over a date range.
    /// </summary>
    /// <param name="currency">Currency code to retrieve history for.</param>
    /// <param name="startDate">Start date of the history.</param>
    /// <param name="endDate">End date of the history.</param>
    /// <returns>Dictionary mapping dates to exchange rates.</returns>
    Dictionary<DateOnly, decimal> GetRateHistory(
        string currency,
        DateOnly startDate,
        DateOnly endDate);
}
