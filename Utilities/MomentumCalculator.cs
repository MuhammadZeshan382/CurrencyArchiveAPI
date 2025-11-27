namespace CurrencyArchiveAPI.Utilities;

/// <summary>
/// Provides momentum and technical indicator calculations.
/// Handles momentum, SMA (Simple Moving Average), and related metrics.
/// </summary>
public static class MomentumCalculator
{
    /// <summary>
    /// Calculates momentum over a specific period.
    /// </summary>
    /// <param name="prices">Array of prices.</param>
    /// <param name="period">Number of periods to look back.</param>
    /// <returns>Momentum as a decimal, or null if insufficient data.</returns>
    public static decimal? CalculateMomentum(decimal[] prices, int period)
    {
        if (prices.Length < period + 1) return null;

        var currentPrice = prices[^1];
        var pastPrice = prices[^(period + 1)];

        return pastPrice != 0 ? (currentPrice - pastPrice) / pastPrice : null;
    }

    /// <summary>
    /// Calculates Simple Moving Average (SMA) over a specific period.
    /// </summary>
    /// <param name="prices">Array of prices.</param>
    /// <param name="period">Number of periods for the average.</param>
    /// <returns>SMA value, or null if insufficient data.</returns>
    public static decimal? CalculateSMA(decimal[] prices, int period)
    {
        if (prices.Length < period) return null;

        var sum = 0m;
        for (int i = prices.Length - period; i < prices.Length; i++)
        {
            sum += prices[i];
        }

        return sum / period;
    }

    /// <summary>
    /// Calculates Z-score for the current price relative to the mean and standard deviation.
    /// </summary>
    /// <param name="currentPrice">Current price value.</param>
    /// <param name="mean">Mean of prices.</param>
    /// <param name="stdDev">Standard deviation of prices.</param>
    /// <returns>Z-score value.</returns>
    public static decimal CalculateZScore(decimal currentPrice, decimal mean, decimal stdDev)
    {
        return stdDev != 0 ? (currentPrice - mean) / stdDev : 0;
    }
}
