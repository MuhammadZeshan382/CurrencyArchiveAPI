namespace CurrencyArchiveAPI.Utilities;

/// <summary>
/// Helper class for calculating financial returns from rate data.
/// </summary>
public static class ReturnsCalculator
{
    /// <summary>
    /// Calculates daily returns from rate data.
    /// Returns[i] = (Rate[i] - Rate[i-1]) / Rate[i-1]
    /// </summary>
    /// <param name="rates">Array of exchange rates.</param>
    /// <returns>Array of daily returns.</returns>
    public static double[] CalculateDailyReturns(decimal[] rates)
    {
        if (rates.Length < 2)
        {
            return Array.Empty<double>();
        }

        var returns = new double[rates.Length - 1];
        for (int i = 1; i < rates.Length; i++)
        {
            if (rates[i - 1] != 0)
            {
                returns[i - 1] = (double)((rates[i] - rates[i - 1]) / rates[i - 1]);
            }
        }

        return returns;
    }

    /// <summary>
    /// Calculates logarithmic returns from rate data.
    /// LogReturn[i] = ln(Rate[i] / Rate[i-1])
    /// </summary>
    /// <param name="rates">Array of exchange rates.</param>
    /// <returns>Array of log returns.</returns>
    public static double[] CalculateLogReturns(decimal[] rates)
    {
        if (rates.Length < 2)
        {
            return Array.Empty<double>();
        }

        var logReturns = new double[rates.Length - 1];
        for (int i = 1; i < rates.Length; i++)
        {
            if (rates[i - 1] > 0 && rates[i] > 0)
            {
                logReturns[i - 1] = Math.Log((double)(rates[i] / rates[i - 1]));
            }
        }

        return logReturns;
    }
}
