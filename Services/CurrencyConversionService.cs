namespace CurrencyArchiveAPI.Services;

/// <summary>
/// Implementation of currency conversion service using in-memory rate data.
/// </summary>
public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly ICurrencyDataService _dataService;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(
        ICurrencyDataService dataService,
        ILogger<CurrencyConversionService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public decimal Convert(string fromCurrency, string toCurrency, DateOnly date, decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        var fromCode = fromCurrency.ToUpperInvariant();
        var toCode = toCurrency.ToUpperInvariant();

        // Special case: same currency
        if (fromCode == toCode)
        {
            return amount;
        }

        // Special case: EUR is base currency
        if (fromCode == "EUR")
        {
            var toRate = _dataService.GetRate(date, toCode)
                ?? throw new KeyNotFoundException($"Exchange rate not found for {toCode} on {date:yyyy-MM-dd}");
            return amount * toRate;
        }

        if (toCode == "EUR")
        {
            var fromRate = _dataService.GetRate(date, fromCode)
                ?? throw new KeyNotFoundException($"Exchange rate not found for {fromCode} on {date:yyyy-MM-dd}");
            return amount / fromRate;
        }

        // Cross-rate conversion: from -> EUR -> to
        var rateFrom = _dataService.GetRate(date, fromCode)
            ?? throw new KeyNotFoundException($"Exchange rate not found for {fromCode} on {date:yyyy-MM-dd}");
        
        var rateTo = _dataService.GetRate(date, toCode)
            ?? throw new KeyNotFoundException($"Exchange rate not found for {toCode} on {date:yyyy-MM-dd}");

        // Convert: (amount / rateFrom) * rateTo
        return (amount / rateFrom) * rateTo;
    }

    public IEnumerable<string> GetAvailableCurrencies(DateOnly? date = null)
    {
        if (date.HasValue)
        {
            return _dataService.GetAvailableCurrencies(date.Value);
        }

        // Return all unique currencies across all dates
        var (minDate, maxDate) = _dataService.GetDateRange();
        var allCurrencies = new HashSet<string>();

        // Sample a few dates to get comprehensive currency list
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

    public Dictionary<DateOnly, decimal> GetRateHistory(string currency, DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
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
