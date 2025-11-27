using CurrencyArchiveAPI.Models;

namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Service interface for advanced financial analytics and metrics.
/// Handles volatility, risk metrics, momentum indicators, and rolling window analysis.
/// </summary>
public interface IFinancialAnalyticsService
{
    /// <summary>
    /// Calculates rolling average for a currency pair over a date range.
    /// </summary>
    /// <param name="baseCurrency">Base currency code.</param>
    /// <param name="targetCurrency">Target currency code.</param>
    /// <param name="startDate">Start date of the analysis period.</param>
    /// <param name="endDate">End date of the analysis period.</param>
    /// <param name="windowSize">Size of the rolling window in days.</param>
    /// <returns>Rolling average response with calculated metrics.</returns>
    RollingMetricsResponse GetRollingMetrics(
        string baseCurrency,
        string targetCurrency,
        DateOnly startDate,
        DateOnly endDate,
        int windowSize);

    /// <summary>
    /// Calculates comprehensive financial metrics for currency pairs.
    /// Includes volatility, risk metrics, momentum indicators, and correlation analysis.
    /// </summary>
    /// <param name="baseCurrency">Base currency code.</param>
    /// <param name="symbols">Target currency symbols to analyze.</param>
    /// <param name="startDate">Start date of the analysis period.</param>
    /// <param name="endDate">End date of the analysis period.</param>
    /// <returns>Financial metrics response with detailed analytics.</returns>
    FinancialMetricsResponse GetFinancialMetrics(
        string baseCurrency,
        List<string> symbols,
        DateOnly startDate,
        DateOnly endDate);
}
