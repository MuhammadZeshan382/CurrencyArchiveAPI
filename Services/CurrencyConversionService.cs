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

    public Dictionary<string, decimal> GetHistoricalRates(DateOnly date, string baseCurrency = "EUR", IEnumerable<string>? symbols = null)
    {
        var baseCode = baseCurrency.ToUpperInvariant();
        
        // Get all rates for the date (EUR-based from data source)
        var eurRates = _dataService.GetRatesForDate(date);
        if (eurRates == null)
        {
            throw new KeyNotFoundException($"No exchange rates found for {date:yyyy-MM-dd}");
        }

        var result = new Dictionary<string, decimal>();

        // If base is EUR, return rates directly (with optional filtering)
        if (baseCode == "EUR")
        {
            var currenciesToInclude = symbols?.Select(s => s.ToUpperInvariant()).ToHashSet();

            foreach (var (currency, rate) in eurRates)
            {
                if (currenciesToInclude == null || currenciesToInclude.Contains(currency))
                {
                    result[currency] = rate;
                }
            }

            return result;
        }

        // For non-EUR base, convert all rates
        // Formula: targetRate / baseRate
        // Example: If base=GBP, to get USD rate:
        // EUR->USD = 1.10, EUR->GBP = 0.87
        // GBP->USD = 1.10 / 0.87 = 1.264

        var baseRate = _dataService.GetRate(date, baseCode);
        if (!baseRate.HasValue)
        {
            throw new KeyNotFoundException($"Base currency {baseCode} not found for {date:yyyy-MM-dd}");
        }

        var currencyCodes = symbols?.Select(s => s.ToUpperInvariant()).ToHashSet();

        // Add the base currency itself (always 1.0)
        if (currencyCodes == null || currencyCodes.Contains(baseCode))
        {
            result[baseCode] = 1.0m;
        }

        // Convert all other currencies relative to the new base
        foreach (var (currency, eurRate) in eurRates)
        {
            // Skip the base currency (already added)
            if (currency == baseCode)
            {
                continue;
            }

            // Apply symbol filter if provided
            if (currencyCodes != null && !currencyCodes.Contains(currency))
            {
                continue;
            }

            // Convert: (eurRate / baseRate) gives rate from base to target
            result[currency] = eurRate / baseRate.Value;
        }

        return result;
    }

    public Dictionary<string, Dictionary<string, decimal>> GetTimeseries(DateOnly startDate, DateOnly endDate, string baseCurrency = "EUR", IEnumerable<string>? symbols = null)
    {
        // Validate date range
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to start date");
        }

        var daysDifference = endDate.DayNumber - startDate.DayNumber;
        if (daysDifference > 365)
        {
            throw new ArgumentException("Maximum timeframe is 365 days");
        }

        var result = new Dictionary<string, Dictionary<string, decimal>>();
        var currentDate = startDate;

        // Iterate through each day in the range
        while (currentDate <= endDate)
        {
            try
            {
                var ratesForDate = GetHistoricalRates(currentDate, baseCurrency, symbols);
                if (ratesForDate.Count > 0)
                {
                    result[currentDate.ToString("yyyy-MM-dd")] = ratesForDate;
                }
            }
            catch (KeyNotFoundException)
            {
                // Skip dates with no data (weekends, holidays)
                _logger.LogDebug("No data available for {Date}", currentDate);
            }

            currentDate = currentDate.AddDays(1);
        }

        return result;
    }
}
