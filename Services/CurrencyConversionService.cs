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

        var result = new System.Collections.Concurrent.ConcurrentDictionary<string, Dictionary<string, decimal>>();
        var dates = new List<DateOnly>();
        var currentDate = startDate;

        // Pre-generate all dates
        while (currentDate <= endDate)
        {
            dates.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }

        // Process dates in parallel
        Parallel.ForEach(dates, date =>
        {
            try
            {
                var ratesForDate = GetHistoricalRates(date, baseCurrency, symbols);
                if (ratesForDate.Count > 0)
                {
                    result[date.ToString("yyyy-MM-dd")] = ratesForDate;
                }
            }
            catch (KeyNotFoundException)
            {
                // Skip dates with no data (weekends, holidays)
            }
        });

        return new Dictionary<string, Dictionary<string, decimal>>(result.OrderBy(x => x.Key));
    }

    public Dictionary<string, Models.CurrencyFluctuation> GetFluctuation(DateOnly startDate, DateOnly endDate, string baseCurrency = "EUR", IEnumerable<string>? symbols = null)
    {
        // Validate date range
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to start date");
        }

        // Get rates for start and end dates
        Dictionary<string, decimal> startRates;
        Dictionary<string, decimal> endRates;

        try
        {
            startRates = GetHistoricalRates(startDate, baseCurrency, symbols);
        }
        catch (KeyNotFoundException ex)
        {
            throw new KeyNotFoundException($"No exchange rates available for start date {startDate:yyyy-MM-dd}", ex);
        }

        try
        {
            endRates = GetHistoricalRates(endDate, baseCurrency, symbols);
        }
        catch (KeyNotFoundException ex)
        {
            throw new KeyNotFoundException($"No exchange rates available for end date {endDate:yyyy-MM-dd}", ex);
        }

        var result = new Dictionary<string, Models.CurrencyFluctuation>();

        // Get all currencies present in both start and end dates
        var allCurrencies = startRates.Keys.Union(endRates.Keys).ToHashSet();

        foreach (var currency in allCurrencies)
        {
            // Skip if currency missing from either date
            if (!startRates.ContainsKey(currency) || !endRates.ContainsKey(currency))
            {
                continue;
            }

            var startRate = startRates[currency];
            var endRate = endRates[currency];
            var change = endRate - startRate;
            var changePct = startRate != 0 ? (change / startRate) * 100 : 0;

            result[currency] = new Models.CurrencyFluctuation
            {
                StartRate = Math.Round(startRate, 6),
                EndRate = Math.Round(endRate, 6),
                Change = Math.Round(change, 4),
                ChangePct = Math.Round(changePct, 4)
            };
        }

        return result;
    }

    public List<Models.RollingWindow> GetRollingAverage(DateOnly startDate, DateOnly endDate, int windowSize, string baseCurrency = "EUR", IEnumerable<string>? symbols = null)
    {
        // Validate inputs
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to start date");
        }

        if (windowSize < 1)
        {
            throw new ArgumentException("Window size must be at least 1 day");
        }

        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        if (windowSize > totalDays)
        {
            throw new ArgumentException($"Window size ({windowSize} days) cannot exceed date range ({totalDays} days)");
        }

        // Get all available dates in the range with data (pre-fetch)
        var allDatesWithData = new System.Collections.Concurrent.ConcurrentBag<DateOnly>();
        var dates = new List<DateOnly>();
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            dates.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }

        // Parallel fetch of available dates
        Parallel.ForEach(dates, date =>
        {
            try
            {
                var rates = GetHistoricalRates(date, baseCurrency, symbols);
                if (rates.Count > 0)
                {
                    allDatesWithData.Add(date);
                }
            }
            catch (KeyNotFoundException) { }
        });

        var sortedDates = allDatesWithData.OrderBy(d => d).ToList();

        if (sortedDates.Count < windowSize)
        {
            throw new InvalidOperationException($"Insufficient data points. Found {sortedDates.Count}, need at least {windowSize}");
        }

        // Calculate rolling windows using sliding window technique (parallel)
        var windowCount = sortedDates.Count - windowSize + 1;
        var windows = new Models.RollingWindow[windowCount];

        Parallel.For(0, windowCount, i =>
        {
            var windowDates = sortedDates.Skip(i).Take(windowSize).ToList();
            var windowStart = windowDates.First();
            var windowEnd = windowDates.Last();

            // Collect rates for all dates in this window
            var windowRatesByDate = new Dictionary<DateOnly, Dictionary<string, decimal>>();
            
            foreach (var date in windowDates)
            {
                try
                {
                    var ratesForDate = GetHistoricalRates(date, baseCurrency, symbols);
                    windowRatesByDate[date] = ratesForDate;
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }
            }

            if (windowRatesByDate.Count == 0)
            {
                continue;
            }

            // Get all currencies present in this window
            var allCurrencies = windowRatesByDate.Values
                .SelectMany(r => r.Keys)
                .Distinct()
                .ToHashSet();

            var windowRates = new Dictionary<string, Models.RollingAverageData>();

            foreach (var currency in allCurrencies)
            {
                // Collect all rates for this currency in the window
                var currencyRates = new List<decimal>();
                
                foreach (var (_, rates) in windowRatesByDate)
                {
                    if (rates.ContainsKey(currency))
                    {
                        currencyRates.Add(rates[currency]);
                    }
                }

                if (currencyRates.Count == 0)
                {
                    continue;
                }

                // Calculate statistical measures
                var average = currencyRates.Average();
                var min = currencyRates.Min();
                var max = currencyRates.Max();
                
                // Calculate variance and standard deviation
                var variance = currencyRates.Count > 1 
                    ? currencyRates.Sum(r => (double)Math.Pow((double)(r - average), 2)) / currencyRates.Count
                    : 0;
                var stdDev = (decimal)Math.Sqrt(variance);

                windowRates[currency] = new Models.RollingAverageData
                {
                    Average = Math.Round(average, 6),
                    Min = Math.Round(min, 6),
                    Max = Math.Round(max, 6),
                    StdDev = Math.Round(stdDev, 6),
                    Variance = Math.Round((decimal)variance, 8)
                };
            }

            result.Add(new Models.RollingWindow
            {
                WindowStart = windowStart.ToString("yyyy-MM-dd"),
                WindowEnd = windowEnd.ToString("yyyy-MM-dd"),
                DataPoints = windowRatesByDate.Count,
                Rates = windowRates
            });
        }

        return result;
    }
}
