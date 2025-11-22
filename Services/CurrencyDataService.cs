using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;

namespace CurrencyArchiveAPI.Services;

/// <summary>
/// In-memory currency data service that loads and caches all historical exchange rates.
/// </summary>
public class CurrencyDataService : ICurrencyDataService
{
    private FrozenDictionary<DateOnly, FrozenDictionary<string, decimal>>? _rates;
    private readonly ILogger<CurrencyDataService> _logger;
    private readonly string _dataPath;

    public bool IsDataLoaded => _rates != null;
    public int TotalDatesLoaded => _rates?.Count ?? 0;

    public CurrencyDataService(ILogger<CurrencyDataService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _dataPath = configuration["CurrencyDataPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Data");
    }

    /// <summary>
    /// Loads all currency data from JSON files into memory using parallel processing.
    /// </summary>
    public async Task LoadDataAsync()
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting to load currency data from {DataPath}", _dataPath);

        if (!Directory.Exists(_dataPath))
        {
            _logger.LogError("Data directory not found at {DataPath}", _dataPath);
            throw new DirectoryNotFoundException($"Data directory not found at {_dataPath}");
        }

        // Find all JSON files
        var jsonFiles = Directory.GetFiles(_dataPath, "*.json", SearchOption.AllDirectories);
        _logger.LogInformation("Found {FileCount} JSON files to process", jsonFiles.Length);

        if (jsonFiles.Length == 0)
        {
            _logger.LogWarning("No JSON files found in data directory");
            _rates = FrozenDictionary<DateOnly, FrozenDictionary<string, decimal>>.Empty;
            return;
        }

        // Use ConcurrentDictionary for thread-safe parallel loading
        var tempRates = new ConcurrentDictionary<DateOnly, Dictionary<string, decimal>>();
        var successCount = 0;
        var errorCount = 0;

        // Parallel processing for performance
        Parallel.ForEach(jsonFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
        {
            try
            {
                var date = ParseDateFromPath(file);
                if (date.HasValue)
                {
                    var rates = LoadRatesFromFile(file);
                    if (rates != null && rates.Count > 0)
                    {
                        tempRates.TryAdd(date.Value, rates);
                        Interlocked.Increment(ref successCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load file: {FilePath}", file);
                Interlocked.Increment(ref errorCount);
            }
        });

        // Convert to frozen dictionaries for optimal read performance
        var frozenRates = tempRates.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToFrozenDictionary()
        );

        _rates = frozenRates;

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Currency data loaded successfully. Files: {SuccessCount} succeeded, {ErrorCount} failed. " +
            "Total dates: {DateCount}. Duration: {Duration:F2}s",
            successCount, errorCount, _rates.Count, duration.TotalSeconds
        );

        if (_rates.Count > 0)
        {
            var (minDate, maxDate) = GetDateRange();
            var sampleDate = _rates.Keys.First();
            var currencyCount = _rates[sampleDate].Count;
            
            _logger.LogInformation(
                "Date range: {MinDate:yyyy-MM-dd} to {MaxDate:yyyy-MM-dd}. " +
                "Currencies per day: ~{CurrencyCount}",
                minDate, maxDate, currencyCount
            );
        }
    }

    /// <summary>
    /// Parses date from file path format: Data/{Year}/{Month}/DD-MM-YYYY.json
    /// </summary>
    private DateOnly? ParseDateFromPath(string filePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Try parsing DD-MM-YYYY format
            if (DateOnly.TryParseExact(fileName, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            // Fallback: try other common formats
            if (DateOnly.TryParse(fileName, out date))
            {
                return date;
            }

            _logger.LogWarning("Unable to parse date from filename: {FileName}", fileName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing date from path: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Loads exchange rates from a JSON file.
    /// </summary>
    private Dictionary<string, decimal>? LoadRatesFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(json);
            return rates;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in file: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            return null;
        }
    }

    public decimal? GetRate(DateOnly date, string currencyCode)
    {
        if (_rates == null)
        {
            throw new InvalidOperationException("Currency data not loaded");
        }

        if (!_rates.TryGetValue(date, out var ratesForDate))
        {
            return null;
        }

        return ratesForDate.TryGetValue(currencyCode.ToUpperInvariant(), out var rate) ? rate : null;
    }

    public IEnumerable<string> GetAvailableCurrencies(DateOnly date)
    {
        if (_rates == null)
        {
            throw new InvalidOperationException("Currency data not loaded");
        }

        return _rates.TryGetValue(date, out var ratesForDate) 
            ? ratesForDate.Keys 
            : Enumerable.Empty<string>();
    }

    public IReadOnlyDictionary<string, decimal>? GetRatesForDate(DateOnly date)
    {
        if (_rates == null)
        {
            throw new InvalidOperationException("Currency data not loaded");
        }

        return _rates.TryGetValue(date, out var ratesForDate) ? ratesForDate : null;
    }

    public (DateOnly MinDate, DateOnly MaxDate) GetDateRange()
    {
        if (_rates == null || _rates.Count == 0)
        {
            throw new InvalidOperationException("Currency data not loaded or empty");
        }

        var dates = _rates.Keys;
        return (dates.Min(), dates.Max());
    }
}
