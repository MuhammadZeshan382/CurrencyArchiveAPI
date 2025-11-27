using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Utilities;
using System.Collections.Concurrent;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for calculating correlations between currencies.
/// </summary>
public class CorrelationHelper
{
    /// <summary>
    /// Extracts unique currency codes from timeseries data.
    /// </summary>
    public HashSet<string> ExtractUniqueCurrencies(Dictionary<string, Dictionary<string, decimal>> timeseriesData)
    {
        return timeseriesData.Values
            .SelectMany(rates => rates.Keys)
            .Distinct()
            .ToHashSet();
    }

    /// <summary>
    /// Adds correlation data to currency metrics for all currency pairs.
    /// </summary>
    public void AddCorrelations(
        ConcurrentDictionary<string, CurrencyVolatilityMetrics> result,
        ConcurrentDictionary<string, double[]> currencyReturnsDict,
        HashSet<string> allCurrencies)
    {
        if (allCurrencies.Count <= 1) return;

        foreach (var currency in allCurrencies)
        {
            if (!result.TryGetValue(currency, out var metrics)) continue;
            if (!currencyReturnsDict.TryGetValue(currency, out var returns1)) continue;

            var correlations = new Dictionary<string, decimal>();

            foreach (var otherCurrency in allCurrencies)
            {
                if (currency == otherCurrency) continue;

                if (currencyReturnsDict.TryGetValue(otherCurrency, out var returns2))
                {
                    var correlation = StatisticsCalculator.CalculateCorrelation(returns1, returns2);
                    if (!double.IsNaN(correlation))
                    {
                        correlations[otherCurrency] = Math.Round((decimal)correlation, AppConstants.Precision.ChangePrecision);
                    }
                }
            }

            if (correlations.Count > 0)
            {
                result[currency] = metrics with { Correlations = correlations };
            }
        }
    }
}
