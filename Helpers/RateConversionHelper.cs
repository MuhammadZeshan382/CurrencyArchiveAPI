using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Services;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Implementation of rate conversion helper service.
/// Provides shared logic for converting rates between different base currencies.
/// </summary>
public class RateConversionHelper : IRateConversionHelper
{
    private readonly ICurrencyDataService _dataService;

    public RateConversionHelper(ICurrencyDataService dataService)
    {
        _dataService = dataService;
    }

    /// <inheritdoc/>
    public Dictionary<string, decimal> GetHistoricalRatesForDate(
        DateOnly date,
        string baseCurrency,
        List<string> symbols)
    {
        var baseCode = baseCurrency.ToUpperInvariant();
        var eurRates = _dataService.GetRatesForDate(date)
            ?? throw new KeyNotFoundException(
                $"{AppConstants.ErrorMessages.NoRatesFound} for {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");

        if (baseCode == AppConstants.Currency.BaseCurrency)
        {
            return FilterRates(eurRates, symbols);
        }

        var baseRate = _dataService.GetRate(date, baseCode)
            ?? throw new KeyNotFoundException(
                $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {baseCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");

        return ConvertRatesToBase(eurRates, baseCode, baseRate, symbols);
    }

    /// <inheritdoc/>
    public Dictionary<string, decimal> FilterRates(
        IReadOnlyDictionary<string, decimal> rates,
        List<string> symbols)
    {
        var result = new Dictionary<string, decimal>();

        if (symbols == null || symbols.Count == 0)
        {
            foreach (var (currency, rate) in rates)
            {
                result[currency] = Math.Round(rate, AppConstants.Precision.ExchangeRatePrecision);
            }
            return result;
        }

        var currenciesToInclude = symbols.Select(s => s.ToUpperInvariant()).ToHashSet();

        foreach (var (currency, rate) in rates)
        {
            if (currenciesToInclude.Contains(currency))
            {
                result[currency] = Math.Round(rate, AppConstants.Precision.ExchangeRatePrecision);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public Dictionary<string, decimal> ConvertRatesToBase(
        IReadOnlyDictionary<string, decimal> eurRates,
        string baseCode,
        decimal baseRate,
        List<string> symbols)
    {
        var result = new Dictionary<string, decimal>();
        var currencyCodes = symbols?.Select(s => s.ToUpperInvariant()).ToHashSet();

        if (currencyCodes == null || currencyCodes.Contains(baseCode))
        {
            result[baseCode] = 1.0m;
        }

        foreach (var (currency, eurRate) in eurRates)
        {
            if (currency == baseCode) continue;
            if (currencyCodes != null && !currencyCodes.Contains(currency)) continue;

            var convertedRate = eurRate / baseRate;
            result[currency] = Math.Round(convertedRate, AppConstants.Precision.ExchangeRatePrecision);
        }

        return result;
    }
}
