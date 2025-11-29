using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Helpers;
using CurrencyArchiveAPI.Models;

namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Implementation of historical exchange rate operations.
/// Handles timeseries data, fluctuations, and historical rate queries.
/// </summary>
public class HistoricalRatesService : IHistoricalRatesService
{
    private readonly ICurrencyDataService _dataService;
    private readonly IRateConversionHelper _rateHelper;
    private readonly TimeseriesDataHelper _timeseriesDataHelper;
    private readonly FluctuationCalculatorHelper _fluctuationCalculatorHelper;

    public HistoricalRatesService(
        ICurrencyDataService dataService,
        IRateConversionHelper rateHelper,
        TimeseriesDataHelper timeseriesDataHelper,
        FluctuationCalculatorHelper fluctuationCalculatorHelper)
    {
        _dataService = dataService;
        _rateHelper = rateHelper;
        _timeseriesDataHelper = timeseriesDataHelper;
        _fluctuationCalculatorHelper = fluctuationCalculatorHelper;
    }

    /// <inheritdoc/>
    public HistoricalRatesResponse GetHistoricalRates(
        DateOnly date,
        string? baseCurrency = null,
        List<string>? symbols = null)
    {
        var baseCode = (baseCurrency ?? AppConstants.Currency.DefaultBaseCurrency).ToUpperInvariant();
        var rates = _rateHelper.GetHistoricalRatesForDate(date, baseCode, symbols ?? new List<string>());

        return new HistoricalRatesResponse
        {
            Date = date.ToString(AppConstants.DateFormats.StandardDateFormat),
            Base = baseCode,
            Rates = rates
        };
    }

    /// <inheritdoc/>
    public TimeseriesResponse GetTimeseries(
        string baseCurrency,
        List<string> symbols,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(AppConstants.ValidationMessages.EndDateMustBeAfterStartDate);
        }

        var baseCode = baseCurrency.ToUpperInvariant();
        var timeseries = _timeseriesDataHelper.GetTimeseriesData(startDate, endDate, baseCode, symbols);

        return new TimeseriesResponse
        {
            StartDate = startDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            EndDate = endDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            Base = baseCode,
            Rates = timeseries
        };
    }

    /// <inheritdoc/>
    public FluctuationResponse GetFluctuation(
        string baseCurrency,
        List<string> symbols,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(AppConstants.ValidationMessages.EndDateMustBeAfterStartDate);
        }

        var baseCode = baseCurrency.ToUpperInvariant();
        var startRates = _rateHelper.GetHistoricalRatesForDate(startDate, baseCode, symbols);
        var endRates = _rateHelper.GetHistoricalRatesForDate(endDate, baseCode, symbols);

        var fluctuations = _fluctuationCalculatorHelper.CalculateFluctuations(startRates, endRates);

        return new FluctuationResponse
        {
            StartDate = startDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            EndDate = endDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            Base = baseCode,
            Rates = fluctuations
        };
    }

    /// <inheritdoc/>
    public Dictionary<DateOnly, decimal> GetRateHistory(
        string currency,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException(AppConstants.ValidationMessages.StartDateMustBeBeforeEnd);
        }

        var currencyCode = currency.ToUpperInvariant();
        var history = new Dictionary<DateOnly, decimal>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var rate = _dataService.GetRate(date, currencyCode);
            if (rate.HasValue)
            {
                history[date] = rate.Value;
            }
        }

        return history;
    }
}
