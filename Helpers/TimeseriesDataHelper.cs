using CurrencyArchiveAPI.Constants;
using System.Collections.Concurrent;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for collecting timeseries data across date ranges.
/// Used by both HistoricalRatesService and FinancialAnalyticsService.
/// </summary>
public class TimeseriesDataHelper
{
    private readonly IRateConversionHelper _rateHelper;

    public TimeseriesDataHelper(IRateConversionHelper rateHelper)
    {
        _rateHelper = rateHelper;
    }

    /// <summary>
    /// Collects timeseries data for the specified date range and currencies.
    /// Returns a dictionary with date strings as keys and rate dictionaries as values.
    /// </summary>
    public Dictionary<string, Dictionary<string, decimal>> GetTimeseriesData(
        DateOnly startDate,
        DateOnly endDate,
        string baseCurrency,
        List<string> symbols)
    {
        var result = new ConcurrentDictionary<string, Dictionary<string, decimal>>();
        var dates = DateRangeHelper.GenerateDateRange(startDate, endDate);

        Parallel.ForEach(dates, date =>
        {
            try
            {
                var ratesForDate = _rateHelper.GetHistoricalRatesForDate(date, baseCurrency, symbols);
                if (ratesForDate.Count > 0)
                {
                    result[date.ToString(AppConstants.DateFormats.StandardDateFormat)] = ratesForDate;
                }
            }
            catch (KeyNotFoundException)
            {
                // Skip dates with no data
            }
        });

        return new Dictionary<string, Dictionary<string, decimal>>(result.OrderBy(x => x.Key));
    }
}
