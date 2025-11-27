using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Utilities;
using System.Collections.Concurrent;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for calculating rolling window statistics.
/// Handles window validation, calculation, and aggregation.
/// </summary>
public class RollingWindowHelper
{
    private readonly IRateConversionHelper _rateHelper;

    public RollingWindowHelper(IRateConversionHelper rateHelper)
    {
        _rateHelper = rateHelper;
    }

    /// <summary>
    /// Validates rolling window parameters to ensure they are logically correct.
    /// </summary>
    public void ValidateWindowParameters(DateOnly startDate, DateOnly endDate, int windowSize)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(AppConstants.ValidationMessages.EndDateMustBeAfterStartDate);
        }

        if (windowSize < 1)
        {
            throw new ArgumentException(AppConstants.ValidationMessages.WindowSizeMustBeAtLeastOne);
        }

        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        if (windowSize > totalDays)
        {
            throw new ArgumentException(
                string.Format(AppConstants.ValidationMessages.WindowSizeExceedsRange, windowSize, totalDays));
        }
    }

    /// <summary>
    /// Calculates rolling windows for the specified dates and currencies.
    /// Processes windows in parallel for performance.
    /// </summary>
    public List<RollingWindow> CalculateRollingWindows(
        List<DateOnly> sortedDates,
        int windowSize,
        string baseCurrency,
        List<string> symbols)
    {
        var windowCount = sortedDates.Count - windowSize + 1;
        var windows = new RollingWindow[windowCount];

        Parallel.For(0, windowCount, i =>
        {
            var window = CalculateSingleWindow(sortedDates, i, windowSize, baseCurrency, symbols);
            if (window != null)
            {
                windows[i] = window;
            }
        });

        return windows.Where(w => w != null).ToList();
    }

    /// <summary>
    /// Calculates statistics for a single rolling window.
    /// </summary>
    public RollingWindow? CalculateSingleWindow(
        List<DateOnly> sortedDates,
        int windowIndex,
        int windowSize,
        string baseCurrency,
        List<string> symbols)
    {
        var windowDates = sortedDates.Skip(windowIndex).Take(windowSize).ToList();
        var windowStart = windowDates.First();
        var windowEnd = windowDates.Last();

        var windowRatesByDate = CollectWindowRates(windowDates, baseCurrency, symbols);

        if (windowRatesByDate.Count == 0) return null;

        var allCurrencies = windowRatesByDate.Values
            .SelectMany(r => r.Keys)
            .Distinct()
            .ToHashSet();

        var windowRates = new ConcurrentDictionary<string, RollingAverageData>();

        Parallel.ForEach(allCurrencies, currency =>
        {
            var currencyRates = CollectCurrencyRatesFromWindow(currency, windowRatesByDate);
            if (currencyRates.Count > 0)
            {
                windowRates[currency] = CalculateWindowStatistics(currencyRates);
            }
        });

        return new RollingWindow
        {
            WindowStart = windowStart.ToString(AppConstants.DateFormats.StandardDateFormat),
            WindowEnd = windowEnd.ToString(AppConstants.DateFormats.StandardDateFormat),
            DataPoints = windowRatesByDate.Count,
            Rates = new Dictionary<string, RollingAverageData>(windowRates)
        };
    }

    /// <summary>
    /// Collects rates for all specified dates within a window.
    /// </summary>
    public Dictionary<DateOnly, Dictionary<string, decimal>> CollectWindowRates(
        List<DateOnly> windowDates,
        string baseCurrency,
        List<string> symbols)
    {
        var windowRatesByDate = new Dictionary<DateOnly, Dictionary<string, decimal>>();

        foreach (var date in windowDates)
        {
            try
            {
                var ratesForDate = _rateHelper.GetHistoricalRatesForDate(date, baseCurrency, symbols);
                windowRatesByDate[date] = ratesForDate;
            }
            catch (KeyNotFoundException) { }
        }

        return windowRatesByDate;
    }

    /// <summary>
    /// Collects rates for a specific currency from window data.
    /// </summary>
    public List<decimal> CollectCurrencyRatesFromWindow(
        string currency,
        Dictionary<DateOnly, Dictionary<string, decimal>> windowRatesByDate)
    {
        var currencyRates = new List<decimal>();

        foreach (var (_, rates) in windowRatesByDate)
        {
            if (rates.TryGetValue(currency, out var rate))
            {
                currencyRates.Add(rate);
            }
        }

        return currencyRates;
    }

    /// <summary>
    /// Calculates statistical metrics for a window of rates.
    /// </summary>
    public RollingAverageData CalculateWindowStatistics(List<decimal> currencyRates)
    {
        var average = currencyRates.Average();
        var min = currencyRates.Min();
        var max = currencyRates.Max();
        var variance = StatisticsCalculator.CalculateVariance(currencyRates.ToArray(), average);
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return new RollingAverageData
        {
            Average = Math.Round(average, AppConstants.Precision.ExchangeRatePrecision),
            Min = Math.Round(min, AppConstants.Precision.ExchangeRatePrecision),
            Max = Math.Round(max, AppConstants.Precision.ExchangeRatePrecision),
            StdDev = Math.Round(stdDev, AppConstants.Precision.ExchangeRatePrecision),
            Variance = Math.Round(variance, AppConstants.Precision.StandardDeviationPrecision)
        };
    }
}
