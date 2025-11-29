using CurrencyArchiveAPI.Models;

namespace CurrencyArchiveAPI.Utilities;

/// <summary>
/// Provides rolling window calculations for financial time series analysis.
/// </summary>
public static class RollingWindowCalculator
{
    /// <summary>
    /// Calculates rolling metrics for multiple window sizes (30, 60, 90, 180 days).
    /// </summary>
    /// <param name="prices">Array of prices.</param>
    /// <param name="returns">Array of returns.</param>
    /// <returns>RollingMetrics object with all window calculations.</returns>
    public static RollingMetrics? CalculateRollingMetrics(decimal[] prices, double[] returns)
    {
        var window30 = CalculateRollingWindow(prices, returns, 30);
        var window60 = CalculateRollingWindow(prices, returns, 60);
        var window90 = CalculateRollingWindow(prices, returns, 90);
        var window180 = CalculateRollingWindow(prices, returns, 180);

        return new RollingMetrics
        {
            Window30D = window30,
            Window60D = window60,
            Window90D = window90,
            Window180D = window180
        };
    }

    /// <summary>
    /// Calculates rolling period metrics for a specific window size.
    /// </summary>
    /// <param name="prices">Array of prices.</param>
    /// <param name="returns">Array of returns.</param>
    /// <param name="windowSize">Size of the rolling window.</param>
    /// <returns>RollingPeriodMetrics for the window, or null if insufficient data.</returns>
    public static RollingPeriodMetrics? CalculateRollingWindow(
        decimal[] prices,
        double[] returns,
        int windowSize)
    {
        if (prices.Length < windowSize) return null;

        // Get last N prices
        var startIdx = prices.Length - windowSize;
        var sum = 0m;

        for (int i = startIdx; i < prices.Length; i++)
        {
            sum += prices[i];
        }

        var mean = sum / windowSize;

        // Calculate std dev for window
        var sumSquaredDiff = 0.0;
        for (int i = startIdx; i < prices.Length; i++)
        {
            var diff = (double)(prices[i] - mean);
            sumSquaredDiff += diff * diff;
        }
        var stdDev = (decimal)Math.Sqrt(sumSquaredDiff / windowSize);

        // Calculate return for window
        var windowReturn = prices[startIdx] != 0
            ? (prices[^1] - prices[startIdx]) / prices[startIdx]
            : 0;

        // Calculate volatility from returns
        var returnStartIdx = Math.Max(0, returns.Length - windowSize + 1);
        var returnCount = Math.Min(windowSize - 1, returns.Length - returnStartIdx);

        if (returnCount < 2)
            return null;

        var returnSum = 0.0;
        for (int i = returnStartIdx; i < returnStartIdx + returnCount; i++)
        {
            returnSum += returns[i];
        }
        var returnMean = returnSum / returnCount;

        var returnSumSquaredDiff = 0.0;
        for (int i = returnStartIdx; i < returnStartIdx + returnCount; i++)
        {
            var diff = returns[i] - returnMean;
            returnSumSquaredDiff += diff * diff;
        }

        var returnStdDev = Math.Sqrt(returnSumSquaredDiff / returnCount);
        var volatility = (decimal)(returnStdDev * Math.Sqrt(252) * 100);

        return new RollingPeriodMetrics
        {
            Mean = Math.Round(mean, 6),
            StdDev = Math.Round(stdDev, 8),
            Return = Math.Round(windowReturn * 100, 4),
            Volatility = Math.Round(volatility, 4)
        };
    }
}
