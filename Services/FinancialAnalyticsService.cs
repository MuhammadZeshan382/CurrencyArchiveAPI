using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Helpers;
using CurrencyArchiveAPI.Models;
using System.Collections.Concurrent;

namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Implementation of advanced financial analytics and metrics.
/// Handles volatility, risk metrics, momentum indicators, and rolling window analysis.
/// </summary>
public class FinancialAnalyticsService : IFinancialAnalyticsService
{
    private readonly TimeseriesDataHelper _timeseriesDataHelper;
    private readonly RateCollectionHelper _rateCollectionHelper;
    private readonly RollingWindowHelper _rollingWindowHelper;
    private readonly CurrencyMetricsCalculatorHelper _metricsCalculatorHelper;
    private readonly CorrelationHelper _correlationHelper;

    public FinancialAnalyticsService(
        TimeseriesDataHelper timeseriesDataHelper,
        RateCollectionHelper rateCollectionHelper,
        RollingWindowHelper rollingWindowHelper,
        CurrencyMetricsCalculatorHelper metricsCalculatorHelper,
        CorrelationHelper correlationHelper)
    {
        _timeseriesDataHelper = timeseriesDataHelper;
        _rateCollectionHelper = rateCollectionHelper;
        _rollingWindowHelper = rollingWindowHelper;
        _metricsCalculatorHelper = metricsCalculatorHelper;
        _correlationHelper = correlationHelper;
    }

    /// <inheritdoc/>
    public RollingMetricsResponse GetRollingMetrics(
        string baseCurrency,
        List<string> targetCurrency,
        DateOnly startDate,
        DateOnly endDate,
        int windowSize)
    {
        _rollingWindowHelper.ValidateWindowParameters(startDate, endDate, windowSize);

        var baseCode = baseCurrency.ToUpperInvariant();

        var uppervarianttargetcurrnecy = targetCurrency.Select(x => x.ToUpperInvariant()).ToList();
        var datesWithData = _rateCollectionHelper.CollectDatesWithData(startDate, endDate, baseCode, uppervarianttargetcurrnecy);

        if (datesWithData.Count < windowSize)
        {
            throw new InvalidOperationException(
                $"{AppConstants.ErrorMessages.InsufficientData}. Found {datesWithData.Count}, need at least {windowSize}");
        }

        var windows = _rollingWindowHelper.CalculateRollingWindows(datesWithData, windowSize, baseCode, uppervarianttargetcurrnecy);

        return new RollingMetricsResponse
        {
            StartDate = startDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            EndDate = endDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            Base = baseCode,
            WindowSize = windowSize,
            Windows = windows
        };
    }

    /// <inheritdoc/>
    public FinancialMetricsResponse GetFinancialMetrics(
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
        var timeseriesData = _timeseriesDataHelper.GetTimeseriesData(startDate, endDate, baseCode, symbols);

        if (timeseriesData.Count == 0)
        {
            throw new InvalidOperationException(
                $"{AppConstants.ErrorMessages.NoDataFound} between {startDate.ToString(AppConstants.DateFormats.StandardDateFormat)} and {endDate.ToString(AppConstants.DateFormats.StandardDateFormat)}");
        }

        var allCurrencies = _correlationHelper.ExtractUniqueCurrencies(timeseriesData);
        var currencyReturnsDict = new ConcurrentDictionary<string, double[]>();
        var result = new ConcurrentDictionary<string, CurrencyVolatilityMetrics>();

        // Process each currency in parallel
        Parallel.ForEach(allCurrencies, currency =>
        {
            var metrics = _metricsCalculatorHelper.CalculateCurrencyMetrics(currency, timeseriesData, currencyReturnsDict, _rateCollectionHelper);
            if (metrics != null)
            {
                result[currency] = metrics;
            }
        });

        // Add correlations after all currencies processed
        _correlationHelper.AddCorrelations(result, currencyReturnsDict, allCurrencies);

        return new FinancialMetricsResponse
        {
            StartDate = startDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            EndDate = endDate.ToString(AppConstants.DateFormats.StandardDateFormat),
            Base = baseCode,
            Metrics = new Dictionary<string, CurrencyVolatilityMetrics>(result.OrderBy(x => x.Key))
        };
    }
}
