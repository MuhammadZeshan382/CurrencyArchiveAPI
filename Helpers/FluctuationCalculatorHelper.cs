using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Models;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for calculating currency fluctuations between two time periods.
/// </summary>
public class FluctuationCalculatorHelper
{
    /// <summary>
    /// Calculates fluctuations (change and change percentage) for currencies
    /// between start and end periods.
    /// </summary>
    public Dictionary<string, CurrencyFluctuation> CalculateFluctuations(
        Dictionary<string, decimal> startRates,
        Dictionary<string, decimal> endRates)
    {
        var result = new Dictionary<string, CurrencyFluctuation>();
        var allCurrencies = startRates.Keys.Union(endRates.Keys).ToHashSet();

        foreach (var currency in allCurrencies)
        {
            if (!startRates.TryGetValue(currency, out var startRate) ||
                !endRates.TryGetValue(currency, out var endRate))
            {
                continue;
            }

            var change = endRate - startRate;
            var changePct = startRate != 0 ? (change / startRate) * AppConstants.Financial.PercentageMultiplier : 0;

            result[currency] = new CurrencyFluctuation
            {
                StartRate = Math.Round(startRate, AppConstants.Precision.ExchangeRatePrecision),
                EndRate = Math.Round(endRate, AppConstants.Precision.ExchangeRatePrecision),
                Change = Math.Round(change, AppConstants.Precision.ChangePrecision),
                ChangePct = Math.Round(changePct, AppConstants.Precision.ChangePrecision)
            };
        }

        return result;
    }
}
