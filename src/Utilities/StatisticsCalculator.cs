namespace CurrencyArchiveAPI.Utilities;

/// <summary>
/// Provides statistical calculation methods for financial analysis.
/// Handles mean, standard deviation, variance, and correlation calculations.
/// </summary>
public static class StatisticsCalculator
{
    /// <summary>
    /// Calculates the mean (average) of a set of values.
    /// </summary>
    public static double CalculateMean(double[] values)
    {
        if (values.Length == 0) return 0;

        var sum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }
        return sum / values.Length;
    }

    /// <summary>
    /// Calculates the standard deviation of a set of values.
    /// </summary>
    public static double CalculateStdDev(double[] values)
    {
        if (values.Length < 2) return 0;

        var mean = CalculateMean(values);
        var sumSquaredDiff = 0.0;

        for (int i = 0; i < values.Length; i++)
        {
            var diff = values[i] - mean;
            sumSquaredDiff += diff * diff;
        }

        return Math.Sqrt(sumSquaredDiff / values.Length);
    }

    /// <summary>
    /// Calculates the variance of decimal prices.
    /// </summary>
    public static decimal CalculateVariance(decimal[] values, decimal mean)
    {
        if (values.Length < 2) return 0;

        var sumSquaredDiff = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            var diff = (double)(values[i] - mean);
            sumSquaredDiff += diff * diff;
        }

        return (decimal)(sumSquaredDiff / values.Length);
    }

    /// <summary>
    /// Calculates the correlation coefficient between two sets of returns.
    /// </summary>
    public static double CalculateCorrelation(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2) return double.NaN;

        var n = x.Length;
        var meanX = CalculateMean(x);
        var meanY = CalculateMean(y);

        var sumXY = 0.0;
        var sumX2 = 0.0;
        var sumY2 = 0.0;

        for (int i = 0; i < n; i++)
        {
            var dx = x[i] - meanX;
            var dy = y[i] - meanY;
            sumXY += dx * dy;
            sumX2 += dx * dx;
            sumY2 += dy * dy;
        }

        var denominator = Math.Sqrt(sumX2 * sumY2);
        return denominator != 0 ? sumXY / denominator : double.NaN;
    }
}
