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

    /// <summary>
    /// Gets historical exchange rates for a specific date with custom base currency.
    /// Converts all rates from EUR-base to the specified base currency.
    /// </summary>
    /// <param name="date">Date for historical rates.</param>
    /// <param name="baseCurrency">Base currency code (default: EUR).</param>
    /// <param name="symbols">Optional list of currency codes to filter results.</param>
    /// <returns>Dictionary of currency codes to exchange rates relative to base currency.</returns>
    Dictionary<string, decimal> GetHistoricalRates(DateOnly date, string baseCurrency = "EUR", IEnumerable<string>? symbols = null);

    /// <summary>
    /// Gets timeseries data (daily rates) between two dates with optional base currency and symbol filtering.
    /// </summary>
    /// <param name="startDate">Start date of the timeseries.</param>
    /// <param name="endDate">End date of the timeseries.</param>
    /// <param name="baseCurrency">Base currency code (default: EUR).</param>
    /// <param name="symbols">Optional list of currency codes to filter results.</param>
    /// <returns>Dictionary mapping date strings (YYYY-MM-DD) to currency rate dictionaries.</returns>
    Dictionary<string, Dictionary<string, decimal>> GetTimeseries(DateOnly startDate, DateOnly endDate, string baseCurrency = "EUR", IEnumerable<string>? symbols = null);

    /// <summary>
    /// Gets fluctuation data showing how currencies changed between two dates.
    /// </summary>
    /// <param name="startDate">Start date of the fluctuation period.</param>
    /// <param name="endDate">End date of the fluctuation period.</param>
    /// <param name="baseCurrency">Base currency code (default: EUR).</param>
    /// <param name="symbols">Optional list of currency codes to filter results.</param>
    /// <returns>Dictionary mapping currency codes to fluctuation data (start_rate, end_rate, change, change_pct).</returns>
    Dictionary<string, Models.CurrencyFluctuation> GetFluctuation(DateOnly startDate, DateOnly endDate, string baseCurrency = "EUR", IEnumerable<string>? symbols = null);

    /// <summary>
    /// Calculates rolling averages (Simple Moving Average) for currencies over a date range.
    /// Uses sliding window technique to compute statistical measures for each window period.
    /// </summary>
    /// <param name="startDate">Start date of the analysis period.</param>
    /// <param name="endDate">End date of the analysis period.</param>
    /// <param name="windowSize">Window size in days for rolling average calculation.</param>
    /// <param name="baseCurrency">Base currency code (default: EUR).</param>
    /// <param name="symbols">Optional list of currency codes to filter results.</param>
    /// <returns>List of rolling window data with statistical measures (average, min, max, stddev, variance).</returns>
    List<Models.RollingWindow> GetRollingAverage(DateOnly startDate, DateOnly endDate, int windowSize, string baseCurrency = "EUR", IEnumerable<string>? symbols = null);

    /// <summary>
    /// Analyzes comprehensive financial metrics and risk indicators for currencies over a date range.
    /// Calculates returns, volatility, drawdown, Sharpe ratio, VaR, momentum, correlations, and rolling statistics.
    /// </summary>
    /// <param name="startDate">Start date of the analysis period.</param>
    /// <param name="endDate">End date of the analysis period.</param>
    /// <param name="baseCurrency">Base currency code (default: EUR).</param>
    /// <param name="symbols">Optional list of currency codes to filter results.</param>
    /// <returns>Dictionary mapping currency codes to comprehensive financial metrics.</returns>
    Dictionary<string, Models.CurrencyVolatilityMetrics> GetFinancialMetrics(DateOnly startDate, DateOnly endDate, string baseCurrency = "EUR", IEnumerable<string>? symbols = null);
}
