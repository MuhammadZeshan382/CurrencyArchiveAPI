using CurrencyArchiveAPI.Services;
using System.Collections.Concurrent;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for collecting currency rates across date ranges.
/// Provides reusable methods for gathering rate data from the data service.
/// </summary>
public class RateCollectionHelper
{
    private readonly ICurrencyDataService _dataService;
    private readonly IRateConversionHelper _rateHelper;

    public RateCollectionHelper(
        ICurrencyDataService dataService,
        IRateConversionHelper rateHelper)
    {
        _dataService = dataService;
        _rateHelper = rateHelper;
    }

    /// <summary>
    /// Collects rates for a single currency across a date range.
    /// Returns an array of rates in chronological order.
    /// </summary>
    public decimal[] GetRatesArray(string currency, DateOnly startDate, DateOnly endDate)
    {
        var currencyCode = currency.ToUpperInvariant();
        var ratesList = new List<decimal>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var rate = _dataService.GetRate(date, currencyCode);
            if (rate.HasValue)
            {
                ratesList.Add(rate.Value);
            }
        }

        return ratesList.ToArray();
    }

    /// <summary>
    /// Collects rates for a specific currency from timeseries data.
    /// Returns rates in chronological order based on sorted date keys.
    /// </summary>
    public List<decimal> CollectRatesForCurrency(
        string currency,
        Dictionary<string, Dictionary<string, decimal>> timeseriesData)
    {
        var ratesList = new List<decimal>();
        var sortedDates = timeseriesData.Keys.OrderBy(d => d).ToList();

        foreach (var dateStr in sortedDates)
        {
            if (timeseriesData[dateStr].TryGetValue(currency, out var rate))
            {
                ratesList.Add(rate);
            }
        }

        return ratesList;
    }

    /// <summary>
    /// Collects all dates within a range that have data for the specified currencies.
    /// Returns dates sorted chronologically.
    /// </summary>
    public List<DateOnly> CollectDatesWithData(
        DateOnly startDate,
        DateOnly endDate,
        string baseCurrency,
        List<string> symbols)
    {
        var allDatesWithData = new ConcurrentBag<DateOnly>();
        var dates = DateRangeHelper.GenerateDateRange(startDate, endDate);

        Parallel.ForEach(dates, date =>
        {
            try
            {
                var rates = _rateHelper.GetHistoricalRatesForDate(date, baseCurrency, symbols);
                if (rates.Count > 0)
                {
                    allDatesWithData.Add(date);
                }
            }
            catch (KeyNotFoundException) { }
        });

        return allDatesWithData.OrderBy(d => d).ToList();
    }
}
