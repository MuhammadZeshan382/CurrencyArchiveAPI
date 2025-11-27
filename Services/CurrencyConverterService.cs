using CurrencyArchiveAPI.Constants;

namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Implementation of basic currency conversion operations.
/// Handles single and bulk currency conversions using EUR as base currency.
/// </summary>
public class CurrencyConverterService : ICurrencyConverterService
{
    private readonly ICurrencyDataService _dataService;
    private readonly ILogger<CurrencyConverterService> _logger;

    public CurrencyConverterService(
        ICurrencyDataService dataService,
        ILogger<CurrencyConverterService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public decimal Convert(string fromCurrency, string toCurrency, DateOnly date, decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException(AppConstants.ValidationMessages.AmountCannotBeNegative, nameof(amount));
        }

        var fromCode = fromCurrency.ToUpperInvariant();
        var toCode = toCurrency.ToUpperInvariant();

        // Special case: same currency
        if (fromCode == toCode)
        {
            return amount;
        }

        // Special case: EUR is base currency
        if (fromCode == AppConstants.Currency.BaseCurrency)
        {
            var toRate = _dataService.GetRate(date, toCode)
                ?? throw new KeyNotFoundException(
                    $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {toCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");
            return amount * toRate;
        }

        if (toCode == AppConstants.Currency.BaseCurrency)
        {
            var fromRate = _dataService.GetRate(date, fromCode)
                ?? throw new KeyNotFoundException(
                    $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {fromCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");
            return amount / fromRate;
        }

        // Cross-rate conversion: from -> EUR -> to
        var rateFrom = _dataService.GetRate(date, fromCode)
            ?? throw new KeyNotFoundException(
                $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {fromCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");

        var rateTo = _dataService.GetRate(date, toCode)
            ?? throw new KeyNotFoundException(
                $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {toCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");

        return (amount / rateFrom) * rateTo;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableCurrencies(DateOnly? date = null)
    {
        if (date.HasValue)
        {
            return _dataService.GetAvailableCurrencies(date.Value);
        }

        // Return all unique currencies across sample dates
        var (minDate, maxDate) = _dataService.GetDateRange();
        var allCurrencies = new HashSet<string>();

        var sampleDates = new[] { minDate, maxDate };
        foreach (var sampleDate in sampleDates)
        {
            var currencies = _dataService.GetAvailableCurrencies(sampleDate);
            foreach (var currency in currencies)
            {
                allCurrencies.Add(currency);
            }
        }

        return allCurrencies.OrderBy(c => c);
    }

    /// <inheritdoc/>
    public bool IsCurrencyAvailable(string currencyCode, DateOnly date)
    {
        var code = currencyCode.ToUpperInvariant();
        var rate = _dataService.GetRate(date, code);
        return rate.HasValue;
    }

    /// <inheritdoc/>
    public decimal GetExchangeRate(string fromCurrency, string toCurrency, DateOnly date)
    {
        var fromCode = fromCurrency.ToUpperInvariant();
        var toCode = toCurrency.ToUpperInvariant();

        // Special case: same currency
        if (fromCode == toCode)
        {
            return 1.0m;
        }

        // Special case: EUR is base currency
        if (fromCode == AppConstants.Currency.BaseCurrency)
        {
            var toRate = _dataService.GetRate(date, toCode)
                ?? throw new KeyNotFoundException(
                    $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {toCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");
            return toRate;
        }

        if (toCode == AppConstants.Currency.BaseCurrency)
        {
            var fromRate = _dataService.GetRate(date, fromCode)
                ?? throw new KeyNotFoundException(
                    $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {fromCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");
            return 1.0m / fromRate;
        }

        // Cross-rate: from -> EUR -> to
        var rateFrom = _dataService.GetRate(date, fromCode)
            ?? throw new KeyNotFoundException(
                $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {fromCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");

        var rateTo = _dataService.GetRate(date, toCode)
            ?? throw new KeyNotFoundException(
                $"{AppConstants.ErrorMessages.ExchangeRateNotFound} for {toCode} on {date.ToString(AppConstants.DateFormats.StandardDateFormat)}");

        return rateTo / rateFrom;
    }
}
