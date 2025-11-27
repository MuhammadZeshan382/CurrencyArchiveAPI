namespace CurrencyArchiveAPI.Utilities;

/// <summary>
/// Provides volatility and risk calculation methods for financial analysis.
/// Handles drawdown, VaR (Value at Risk), and related risk metrics.
/// </summary>
public static class RiskCalculator
{
    /// <summary>
    /// Calculates maximum drawdown and drawdown series for a price array.
    /// </summary>
    /// <param name="prices">Array of prices.</param>
    /// <returns>Tuple of (maxDrawdown, drawdownArray).</returns>
    public static (decimal maxDrawdown, decimal[] drawdowns) CalculateDrawdown(decimal[] prices)
    {
        if (prices.Length == 0) return (0, Array.Empty<decimal>());

        var drawdowns = new decimal[prices.Length];
        var peak = prices[0];
        var maxDD = 0m;

        for (int i = 0; i < prices.Length; i++)
        {
            if (prices[i] > peak)
            {
                peak = prices[i];
            }

            drawdowns[i] = peak != 0 ? (prices[i] - peak) / peak : 0;

            if (drawdowns[i] < maxDD)
            {
                maxDD = drawdowns[i];
            }
        }

        return (maxDD, drawdowns);
    }

    /// <summary>
    /// Calculates Historical VaR (Value at Risk) and Parametric VaR at 95% confidence level.
    /// </summary>
    /// <param name="returns">Array of returns.</param>
    /// <param name="meanReturn">Mean of returns.</param>
    /// <param name="stdDev">Standard deviation of returns.</param>
    /// <returns>Tuple of (historicalVaR, parametricVaR).</returns>
    public static (decimal historicalVaR, decimal parametricVaR) CalculateVaR(
        double[] returns,
        double meanReturn,
        double stdDev)
    {
        if (returns.Length < 20) return (0, 0);

        // Historical VaR 95%: 5th percentile of returns
        var sortedReturns = new double[returns.Length];
        Array.Copy(returns, sortedReturns, returns.Length);
        Array.Sort(sortedReturns);

        var index95 = (int)(returns.Length * 0.05);
        var historicalVaR = (decimal)sortedReturns[index95];

        // Parametric VaR 95%: mean - 1.65 * std
        var parametricVaR = (decimal)(meanReturn - 1.65 * stdDev);

        return (historicalVaR, parametricVaR);
    }

    /// <summary>
    /// Calculates the Sharpe ratio for annualized returns and volatility.
    /// Formula: (annualizedReturn - riskFreeRate) / annualizedVolatility
    /// All parameters should be in decimal form (e.g., 0.08 for 8%, 0.15 for 15%).
    /// </summary>
    /// <param name="annualizedReturn">Annualized return as a decimal (e.g., 0.08 for 8%).</param>
    /// <param name="annualizedVolatility">Annualized volatility as a decimal (e.g., 0.15 for 15%).</param>
    /// <param name="riskFreeRate">Risk-free rate as a decimal (default 0.04 for 4%).</param>
    /// <returns>Sharpe ratio (unitless).</returns>
    public static decimal CalculateSharpeRatio(
        decimal annualizedReturn,
        decimal annualizedVolatility,
        decimal riskFreeRate = 0.04m)
    {
        return annualizedVolatility != 0
            ? (annualizedReturn - riskFreeRate) / annualizedVolatility
            : 0;
    }
}
