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
                catch (KeyNotFoundException) { }
            }

            if (windowRatesByDate.Count == 0)
            {
                return;
            }

            // Get all currencies present in this window
            var allCurrencies = windowRatesByDate.Values
                .SelectMany(r => r.Keys)
                .Distinct()
                .ToHashSet();

            var windowRates = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.RollingAverageData>();

            // Parallel calculation of statistics per currency
            Parallel.ForEach(allCurrencies, currency =>
            {
                var currencyRates = new List<decimal>();
                
                foreach (var (_, rates) in windowRatesByDate)
                {
                    if (rates.TryGetValue(currency, out var rate))
                    {
                        currencyRates.Add(rate);
                    }
                }

                if (currencyRates.Count == 0) return;

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
            });

            windows[i] = new Models.RollingWindow
            {
                WindowStart = windowStart.ToString("yyyy-MM-dd"),
                WindowEnd = windowEnd.ToString("yyyy-MM-dd"),
                DataPoints = windowRatesByDate.Count,
                Rates = new Dictionary<string, Models.RollingAverageData>(windowRates)
            };
        });

        return windows.Where(w => w != null).ToList();
    }

    public Dictionary<string, Models.CurrencyVolatilityMetrics> GetFinancialMetrics(DateOnly startDate, DateOnly endDate, string baseCurrency = "EUR", IEnumerable<string>? symbols = null)
    {
        // Validate date range
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to start date");
        }

        // Get timeseries data (reuse existing method for consistency)
        var timeseriesData = GetTimeseries(startDate, endDate, baseCurrency, symbols);

        if (timeseriesData.Count == 0)
        {
            throw new InvalidOperationException($"No data available between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}");
        }

        // Extract all unique currencies across all dates
        var allCurrencies = timeseriesData.Values
            .SelectMany(rates => rates.Keys)
            .Distinct()
            .ToHashSet();

        if (allCurrencies.Count == 0)
        {
            throw new InvalidOperationException("No currency data found for the specified parameters");
        }

        // Build currency returns dictionary for correlation calculations
        var currencyReturnsDict = new System.Collections.Concurrent.ConcurrentDictionary<string, double[]>();

        var result = new System.Collections.Concurrent.ConcurrentDictionary<string, Models.CurrencyVolatilityMetrics>();

        // Process each currency in parallel
        Parallel.ForEach(allCurrencies, currency =>
        {
            // Collect all rates for this currency across the date range
            var ratesList = new List<decimal>();
            var sortedDates = timeseriesData.Keys.OrderBy(d => d).ToList();

            foreach (var dateStr in sortedDates)
            {
                if (timeseriesData[dateStr].TryGetValue(currency, out var rate))
                {
                    ratesList.Add(rate);
                }
            }

            // Need at least 2 data points for meaningful volatility analysis
            if (ratesList.Count < 2)
            {
                return;
            }

            // Convert to array for efficient indexed access
            var rates = ratesList.ToArray();
            var n = rates.Length;

            // Calculate basic statistics (on raw prices)
            var min = decimal.MaxValue;
            var max = decimal.MinValue;
            var sum = 0m;
            
            for (int i = 0; i < n; i++)
            {
                if (rates[i] < min) min = rates[i];
                if (rates[i] > max) max = rates[i];
                sum += rates[i];
            }
            
            var average = sum / n;
            var openRate = rates[0];
            var closeRate = rates[n - 1];
            var change = closeRate - openRate;
            var changePct = openRate != 0 ? (change / openRate) * 100 : 0;

            // Calculate daily returns (simple returns for most metrics)
            var dailyReturns = new double[n - 1];
            for (int i = 1; i < n; i++)
            {
                dailyReturns[i - 1] = rates[i - 1] != 0 
                    ? (double)((rates[i] - rates[i - 1]) / rates[i - 1])
                    : 0;
            }

            // Store returns for correlation analysis
            currencyReturnsDict[currency] = dailyReturns;

            // Calculate log returns for volatility
            var logReturns = new double[n - 1];
            for (int i = 1; i < n; i++)
            {
                logReturns[i - 1] = rates[i - 1] != 0 
                    ? Math.Log((double)(rates[i] / rates[i - 1]))
                    : 0;
            }

            // 1. Average Daily Return
            var avgDailyReturn = CalculateMean(dailyReturns);

            // 2. Cumulative Return
            var cumulativeReturn = openRate != 0 ? (closeRate / openRate) - 1 : 0;

            // 3. Annualized Return
            var totalDays = n - 1;
            var annualizedReturn = totalDays > 0 
                ? (decimal)(Math.Pow((double)(1 + cumulativeReturn), 365.0 / totalDays) - 1)
                : 0;

            // 4. Daily Volatility & Annualized Volatility
            var dailyVolatility = CalculateStdDev(logReturns);
            var annualizedVolatility = (decimal)(dailyVolatility * Math.Sqrt(252) * 100);

            // 5 & 6. Drawdown & Maximum Drawdown
            var (maxDrawdown, drawdowns) = CalculateDrawdown(rates);

            // 7. Sharpe Ratio
            const decimal riskFreeRate = 0.04m;
            var sharpeRatio = annualizedVolatility != 0 
                ? ((annualizedReturn - riskFreeRate) / (annualizedVolatility / 100))
                : 0;

            // Calculate variance and standard deviation of raw prices (for legacy metrics)
            var priceVariance = CalculateVariance(rates, average);
            var priceStdDev = (decimal)Math.Sqrt((double)priceVariance);

            // Coefficient of Variation
            var coefficientOfVariation = average != 0 ? (priceStdDev / average) : 0;

            // Range as percentage of mean
            var rangePct = average != 0 ? ((max - min) / average) * 100 : 0;

            // 9. Value at Risk (VaR)
            var (historicalVaR95, parametricVaR95) = CalculateVaR(dailyReturns, avgDailyReturn, dailyVolatility);

            // 13. Z-Score
            var zScore = priceStdDev != 0 ? (closeRate - average) / priceStdDev : 0;

            // 12. Momentum Indicators
            var momentum3M = CalculateMomentum(rates, 63); // ~3 months = 63 trading days
            var momentum12M = CalculateMomentum(rates, 252); // ~12 months = 252 trading days

            // Simple Moving Averages
            var sma50 = CalculateSMA(rates, 50);
            var sma200 = CalculateSMA(rates, 200);

            // 8. Rolling Metrics (30, 60, 90, 180 days)
            var rollingMetrics = CalculateRollingMetrics(rates, dailyReturns);

            result[currency] = new Models.CurrencyVolatilityMetrics
            {
                // Existing metrics
                Min = Math.Round(min, 6),
                Max = Math.Round(max, 6),
                Average = Math.Round(average, 6),
                OpenRate = Math.Round(openRate, 6),
                CloseRate = Math.Round(closeRate, 6),
                Change = Math.Round(change, 6),
                ChangePct = Math.Round(changePct, 4),
                StdDev = Math.Round(priceStdDev, 8),
                Variance = Math.Round(priceVariance, 10),
                CoefficientOfVariation = Math.Round(coefficientOfVariation, 4),
                AnnualizedVolatility = Math.Round(annualizedVolatility, 4),
                RangePct = Math.Round(rangePct, 4),
                DataPoints = n,

                // New professional metrics
                AvgDailyReturn = Math.Round((decimal)avgDailyReturn * 100, 4),
                CumulativeReturn = Math.Round(cumulativeReturn * 100, 4),
                AnnualizedReturn = Math.Round(annualizedReturn * 100, 4),
                DailyVolatility = Math.Round((decimal)dailyVolatility * 100, 4),
                MaxDrawdown = Math.Round(maxDrawdown * 100, 4),
                SharpeRatio = Math.Round(sharpeRatio, 4),
                RiskFreeRate = riskFreeRate,
                HistoricalVaR95 = Math.Round(historicalVaR95 * 100, 4),
                ParametricVaR95 = Math.Round(parametricVaR95 * 100, 4),
                ZScore = Math.Round(zScore, 4),
                Momentum3M = momentum3M.HasValue ? Math.Round(momentum3M.Value * 100, 4) : null,
                Momentum12M = momentum12M.HasValue ? Math.Round(momentum12M.Value * 100, 4) : null,
                SMA50 = sma50.HasValue ? Math.Round(sma50.Value, 6) : null,
                SMA200 = sma200.HasValue ? Math.Round(sma200.Value, 6) : null,
                Rolling = rollingMetrics
            };
        });

        // 10. Calculate correlations after all currencies processed
        if (allCurrencies.Count > 1)
        {
            foreach (var currency in allCurrencies)
            {
                if (!result.TryGetValue(currency, out var metrics))
                    continue;

                var correlations = new Dictionary<string, decimal>();
                
                if (currencyReturnsDict.TryGetValue(currency, out var returns1))
                {
                    foreach (var otherCurrency in allCurrencies)
                    {
                        if (currency == otherCurrency) continue;
                        
                        if (currencyReturnsDict.TryGetValue(otherCurrency, out var returns2))
                        {
                            var correlation = CalculateCorrelation(returns1, returns2);
                            if (!double.IsNaN(correlation))
                            {
                                correlations[otherCurrency] = Math.Round((decimal)correlation, 4);
                            }
                        }
                    }
                }

                // Update with correlations
                if (correlations.Count > 0)
                {
                    result[currency] = metrics with { Correlations = correlations };
                }
            }
        }

        return new Dictionary<string, Models.CurrencyVolatilityMetrics>(result.OrderBy(x => x.Key));
    }

    // ===== Helper Methods for Financial Calculations =====

    private static double CalculateMean(double[] values)
    {
        if (values.Length == 0) return 0;
        
        var sum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }
        return sum / values.Length;
    }

    private static double CalculateStdDev(double[] values)
    {
        if (values.Length < 2) return 0;
        
        var mean = CalculateMean(values);
        var sumSquaredDiff = 0.0;
        
        for (int i = 0; i < values.Length; i++)
        {
            var diff = values[i] - mean;
            sumSquaredDiff += diff * diff;
        }
        
        return Math.Sqrt(sumSquaredDiff / values.Length);
    }

    private static decimal CalculateVariance(decimal[] values, decimal mean)
    {
        if (values.Length < 2) return 0;
        
        var sumSquaredDiff = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            var diff = (double)(values[i] - mean);
            sumSquaredDiff += diff * diff;
        }
        
        return (decimal)(sumSquaredDiff / values.Length);
    }

    private static (decimal maxDrawdown, decimal[] drawdowns) CalculateDrawdown(decimal[] prices)
    {
        if (prices.Length == 0) return (0, Array.Empty<decimal>());
        
        var drawdowns = new decimal[prices.Length];
        var peak = prices[0];
        var maxDD = 0m;
        
        for (int i = 0; i < prices.Length; i++)
        {
            if (prices[i] > peak)
            {
                peak = prices[i];
            }
            
            drawdowns[i] = peak != 0 ? (prices[i] - peak) / peak : 0;
            
            if (drawdowns[i] < maxDD)
            {
                maxDD = drawdowns[i];
            }
        }
        
        return (maxDD, drawdowns);
    }

    private static (decimal historicalVaR, decimal parametricVaR) CalculateVaR(
        double[] returns, 
        double meanReturn, 
        double stdDev)
    {
        if (returns.Length < 20) return (0, 0);
        
        // Historical VaR 95%: 5th percentile of returns
        var sortedReturns = new double[returns.Length];
        Array.Copy(returns, sortedReturns, returns.Length);
        Array.Sort(sortedReturns);
        
        var index95 = (int)(returns.Length * 0.05);
        var historicalVaR = (decimal)sortedReturns[index95];
        
        // Parametric VaR 95%: mean - 1.65 * std
        var parametricVaR = (decimal)(meanReturn - 1.65 * stdDev);
        
        return (historicalVaR, parametricVaR);
    }

    private static decimal? CalculateMomentum(decimal[] prices, int period)
    {
        if (prices.Length < period + 1) return null;
        
        var currentPrice = prices[^1];
        var pastPrice = prices[^(period + 1)];
        
        return pastPrice != 0 ? (currentPrice - pastPrice) / pastPrice : null;
    }

    private static decimal? CalculateSMA(decimal[] prices, int period)
    {
        if (prices.Length < period) return null;
        
        var sum = 0m;
        for (int i = prices.Length - period; i < prices.Length; i++)
        {
            sum += prices[i];
        }
        
        return sum / period;
    }

    private static Models.RollingMetrics? CalculateRollingMetrics(
        decimal[] prices, 
        double[] returns)
    {
        var window30 = CalculateRollingWindow(prices, returns, 30);
        var window60 = CalculateRollingWindow(prices, returns, 60);
        var window90 = CalculateRollingWindow(prices, returns, 90);
        var window180 = CalculateRollingWindow(prices, returns, 180);
        
        return new Models.RollingMetrics
        {
            Window30D = window30,
            Window60D = window60,
            Window90D = window90,
            Window180D = window180
        };
    }

    private static Models.RollingPeriodMetrics? CalculateRollingWindow(
        decimal[] prices, 
        double[] returns, 
        int windowSize)
    {
        if (prices.Length < windowSize) return null;
        
        // Get last N prices
        var startIdx = prices.Length - windowSize;
        var sum = 0m;
        
        for (int i = startIdx; i < prices.Length; i++)
        {
            sum += prices[i];
        }
        
        var mean = sum / windowSize;
        
        // Calculate std dev for window
        var sumSquaredDiff = 0.0;
        for (int i = startIdx; i < prices.Length; i++)
        {
            var diff = (double)(prices[i] - mean);
            sumSquaredDiff += diff * diff;
        }
        var stdDev = (decimal)Math.Sqrt(sumSquaredDiff / windowSize);
        
        // Calculate return for window
        var windowReturn = prices[startIdx] != 0 
            ? (prices[^1] - prices[startIdx]) / prices[startIdx] 
            : 0;
        
        // Calculate volatility from returns
        var returnStartIdx = Math.Max(0, returns.Length - windowSize + 1);
        var returnCount = Math.Min(windowSize - 1, returns.Length - returnStartIdx);
        
        if (returnCount < 2)
            return null;
        
        var returnSum = 0.0;
        for (int i = returnStartIdx; i < returnStartIdx + returnCount; i++)
        {
            returnSum += returns[i];
        }
        var returnMean = returnSum / returnCount;
        
        var returnSumSquaredDiff = 0.0;
        for (int i = returnStartIdx; i < returnStartIdx + returnCount; i++)
        {
            var diff = returns[i] - returnMean;
            returnSumSquaredDiff += diff * diff;
        }
        
        var returnStdDev = Math.Sqrt(returnSumSquaredDiff / returnCount);
        var volatility = (decimal)(returnStdDev * Math.Sqrt(252) * 100);
        
        return new Models.RollingPeriodMetrics
        {
            Mean = Math.Round(mean, 6),
            StdDev = Math.Round(stdDev, 8),
            Return = Math.Round(windowReturn * 100, 4),
            Volatility = Math.Round(volatility, 4)
        };
    }

    private static double CalculateCorrelation(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2) return double.NaN;
        
        var n = x.Length;
        var meanX = CalculateMean(x);
        var meanY = CalculateMean(y);
        
        var sumXY = 0.0;
        var sumX2 = 0.0;
        var sumY2 = 0.0;
        
        for (int i = 0; i < n; i++)
        {
            var dx = x[i] - meanX;
            var dy = y[i] - meanY;
            sumXY += dx * dy;
            sumX2 += dx * dx;
            sumY2 += dy * dy;
        }
        
        var denominator = Math.Sqrt(sumX2 * sumY2);
        return denominator != 0 ? sumXY / denominator : double.NaN;
    }

}
