using System.Globalization;
using System.Text.Json;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for loading currency data from JSON files.
/// Handles file path parsing and JSON deserialization.
/// </summary>
public class DataLoaderHelper
{
    private readonly ILogger<DataLoaderHelper> _logger;

    public DataLoaderHelper(ILogger<DataLoaderHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses date from file path format: Data/{Year}/{Month}/DD-MM-YYYY.json
    /// </summary>
    public DateOnly? ParseDateFromPath(string filePath)
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
    public Dictionary<string, decimal>? LoadRatesFromFile(string filePath)
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
}
