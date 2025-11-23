using Asp.Versioning;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyArchiveAPI.Controllers;

/// <summary>
/// API controller for all currency exchange rate operations.
/// Provides access to individual rates, historical data, timeseries, and monitoring.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyDataService _dataService;
    private readonly ICurrencyConversionService _conversionService;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(
        ICurrencyDataService dataService,
        ICurrencyConversionService conversionService,
        ILogger<CurrencyController> logger)
    {
        _dataService = dataService;
        _conversionService = conversionService;
        _logger = logger;
    }

    #region Currency Conversion Endpoint

    /// <summary>
    /// Converts an amount from one currency to another using exchange rates.
    /// </summary>
    /// <param name="from">Source currency code (e.g., GBP)</param>
    /// <param name="to">Target currency code (e.g., JPY)</param>
    /// <param name="amount">Amount to convert</param>
    /// <param name="date">Optional date in YYYY-MM-DD format. If not specified, uses most recent available data.</param>
    /// <returns>Conversion result with converted amount and exchange rate</returns>
    [HttpGet("api/v{version:apiVersion}/convert")]
    public IActionResult ConvertCurrency(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount,
        [FromQuery] string? date = null)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(from))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Missing required parameter",
                new[] { "Parameter 'from' is required" }
            ));
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Missing required parameter",
                new[] { "Parameter 'to' is required" }
            ));
        }

        if (amount <= 0)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid amount",
                new[] { "Amount must be greater than 0" }
            ));
        }

        // Determine the date to use
        DateOnly conversionDate;
        if (string.IsNullOrWhiteSpace(date))
        {
            // Use most recent date
            var (_, maxDate) = _dataService.GetDateRange();
            conversionDate = maxDate;
        }
        else
        {
            // Parse provided date
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out conversionDate))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Invalid date format",
                    new[] { "Date must be in YYYY-MM-DD format (e.g., 2024-01-01)" }
                ));
            }
        }

        var fromCurrency = from.Trim().ToUpperInvariant();
        var toCurrency = to.Trim().ToUpperInvariant();

        _logger.LogInformation(
            "Conversion requested: {Amount} {From} to {To} on {Date}",
            amount,
            fromCurrency,
            toCurrency,
            conversionDate);

        try
        {
            // Perform the conversion
            var convertedAmount = _conversionService.Convert(fromCurrency, toCurrency, conversionDate, amount);

            // Calculate the exchange rate (result / amount)
            var exchangeRate = amount != 0 ? convertedAmount / amount : 0;

            var response = new ConvertResponse
            {
                Date = conversionDate.ToString("yyyy-MM-dd"),
                From = fromCurrency,
                To = toCurrency,
                Amount = amount,
                Result = Math.Round(convertedAmount, 6),
                Rate = Math.Round(exchangeRate, 6)
            };

            return Ok(ApiResponse<ConvertResponse>.SuccessResponse(
                response,
                $"Converted {amount} {fromCurrency} to {convertedAmount:F6} {toCurrency}"
            ));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "Exchange rate not found",
                new[] { ex.Message }
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid conversion request",
                new[] { ex.Message }
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during currency conversion");
            return StatusCode(500, ApiResponse<object>.FailureResponse(
                "Internal server error",
                new[] { "An error occurred while processing the conversion" }
            ));
        }
    }

    #endregion

    #region Currency Query Endpoints


    /// <summary>
    /// Gets all available currency codes for a specific date.
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format (e.g., 2024-01-01)</param>
    /// <returns>List of all currency codes available on the specified date</returns>
    [HttpGet("api/v{version:apiVersion}/currencyavailable")]
    public IActionResult GetAvailableCurrencies([FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid date format",
                new[] { "Date must be in YYYY-MM-DD format (e.g., 2024-01-01)" }
            ));
        }
        
        _logger.LogInformation("Available currencies requested for date: {Date}", date);

        var currencies = _conversionService.GetAvailableCurrencies(parsedDate).ToList();

        if (currencies.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No currencies found",
                new[] { $"No currency data available for {date}" }
            ));
        }

        var response = new AvailableCurrenciesResponse
        {
            Date = date,
            Count = currencies.Count,
            Currencies = currencies
        };

        return Ok(ApiResponse<AvailableCurrenciesResponse>.SuccessResponse(
            response,
            $"{currencies.Count} currencies available for {date}"
        ));
    }

    #endregion

    #region Historical Rates Endpoint

    /// <summary>
    /// Gets historical exchange rates for a specific date with custom base currency.
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Historical rates response with all requested currency rates</returns>
    [HttpGet("api/v{version:apiVersion}/historical")]
    public IActionResult GetHistoricalRates(
        [FromQuery] string date,
        [FromQuery(Name = "base")] string? baseParam = null,
        [FromQuery] string? symbols = null)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid date format",
                new[] { "Date must be in YYYY-MM-DD format (e.g., 2013-12-24)" }
            ));
        }

        var baseCurrency = string.IsNullOrWhiteSpace(baseParam) ? "EUR" : baseParam.Trim().ToUpperInvariant();

        IEnumerable<string>? symbolList = null;
        if (!string.IsNullOrWhiteSpace(symbols))
        {
            symbolList = symbols
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (!symbolList.Any())
            {
                symbolList = null;
            }
        }

        _logger.LogInformation(
            "Historical rates requested: Date={Date}, Base={Base}, Symbols={Symbols}",
            parsedDate,
            baseCurrency,
            symbols ?? "all"
        );

        var rates = _conversionService.GetHistoricalRates(parsedDate, baseCurrency, symbolList);

        if (rates.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No rates found",
                new[] { $"No exchange rates available for {date}" }
            ));
        }

        var response = new HistoricalRatesResponse
        {
            Date = date,
            Base = baseCurrency,
            Rates = rates,
            Timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<HistoricalRatesResponse>.SuccessResponse(
            response,
            $"Historical rates retrieved successfully for {date}"
        ));
    }

    #endregion

    #region Timeseries Endpoint

    /// <summary>
    /// Gets daily historical exchange rates between two dates.
    /// </summary>
    /// <param name="start_date">Start date in YYYY-MM-DD format (required)</param>
    /// <param name="end_date">End date in YYYY-MM-DD format (required)</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Timeseries response with daily rates for the date range</returns>
    [HttpGet("api/v{version:apiVersion}/timeseries")]
    public IActionResult GetTimeseries(
        [FromQuery] string start_date,
        [FromQuery] string end_date,
        [FromQuery(Name = "base")] string? baseParam = null,
        [FromQuery] string? symbols = null)
    {
        if (!DateOnly.TryParseExact(start_date, "yyyy-MM-dd", out var startDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid start_date format",
                new[] { "start_date must be in YYYY-MM-DD format (e.g., 2012-05-01)" }
            ));
        }

        if (!DateOnly.TryParseExact(end_date, "yyyy-MM-dd", out var endDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid end_date format",
                new[] { "end_date must be in YYYY-MM-DD format (e.g., 2012-05-25)" }
            ));
        }

        if (endDate < startDate)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid date range",
                new[] { "end_date must be greater than or equal to start_date" }
            ));
        }

 
        var baseCurrency = string.IsNullOrWhiteSpace(baseParam) ? "EUR" : baseParam.Trim().ToUpperInvariant();

        IEnumerable<string>? symbolList = null;
        if (!string.IsNullOrWhiteSpace(symbols))
        {
            symbolList = symbols
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (!symbolList.Any())
            {
                symbolList = null;
            }
        }

        _logger.LogInformation(
            "Timeseries requested: StartDate={StartDate}, EndDate={EndDate}, Base={Base}, Symbols={Symbols}",
            start_date,
            end_date,
            baseCurrency,
            symbols ?? "all"
        );

        var timeseriesData = _conversionService.GetTimeseries(startDate, endDate, baseCurrency, symbolList);

        if (timeseriesData.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { $"No exchange rate data available for the date range {start_date} to {end_date}" }
            ));
        }

        var response = new TimeseriesResponse
        {
            StartDate = start_date,
            EndDate = end_date,
            Base = baseCurrency,
            Rates = timeseriesData
        };

        return Ok(ApiResponse<TimeseriesResponse>.SuccessResponse(
            response,
            $"Timeseries data retrieved: {timeseriesData.Count} dates from {start_date} to {end_date}"
        ));
    }

    #endregion

    #region Fluctuation Endpoint

    /// <summary>
    /// Gets currency fluctuation data between two dates.
    /// Shows start rate, end rate, absolute change, and percentage change.
    /// </summary>
    /// <param name="start_date">Start date in YYYY-MM-DD format (required)</param>
    /// <param name="end_date">End date in YYYY-MM-DD format (required)</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Fluctuation response with change data for each currency</returns>
    [HttpGet("api/v{version:apiVersion}/fluctuation")]
    public IActionResult GetFluctuation(
        [FromQuery] string start_date,
        [FromQuery] string end_date,
        [FromQuery(Name = "base")] string? baseParam = null,
        [FromQuery] string? symbols = null)
    {
        if (!DateOnly.TryParseExact(start_date, "yyyy-MM-dd", out var startDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid start_date format",
                new[] { "start_date must be in YYYY-MM-DD format (e.g., 2018-02-25)" }
            ));
        }

        if (!DateOnly.TryParseExact(end_date, "yyyy-MM-dd", out var endDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid end_date format",
                new[] { "end_date must be in YYYY-MM-DD format (e.g., 2018-02-26)" }
            ));
        }

        if (endDate < startDate)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid date range",
                new[] { "end_date must be greater than or equal to start_date" }
            ));
        }

        var baseCurrency = string.IsNullOrWhiteSpace(baseParam) ? "EUR" : baseParam.Trim().ToUpperInvariant();

        IEnumerable<string>? symbolList = null;
        if (!string.IsNullOrWhiteSpace(symbols))
        {
            symbolList = symbols
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (!symbolList.Any())
            {
                symbolList = null;
            }
        }

        _logger.LogInformation(
            "Fluctuation requested: StartDate={StartDate}, EndDate={EndDate}, Base={Base}, Symbols={Symbols}",
            start_date,
            end_date,
            baseCurrency,
            symbols ?? "all"
        );

        Dictionary<string, CurrencyFluctuation> fluctuationData;
        
        try
        {
            fluctuationData = _conversionService.GetFluctuation(startDate, endDate, baseCurrency, symbolList);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { ex.Message }
            ));
        }

        if (fluctuationData.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { $"No common currency data available for the date range {start_date} to {end_date}" }
            ));
        }

        var response = new FluctuationResponse
        {
            StartDate = start_date,
            EndDate = end_date,
            Base = baseCurrency,
            Rates = fluctuationData
        };

        return Ok(ApiResponse<FluctuationResponse>.SuccessResponse(
            response,
            $"Fluctuation data retrieved: {fluctuationData.Count} currencies from {start_date} to {end_date}"
        ));
    }

    #endregion

    #region Health Check Endpoint

    /// <summary>
    /// Basic health check endpoint for service monitoring.
    /// </summary>
    /// <returns>Health status of the service including data load state</returns>
    [HttpGet("api/health")]
    [ApiVersionNeutral]
    public IActionResult GetHealth()
    {
        var isHealthy = _dataService.IsDataLoaded && _dataService.TotalDatesLoaded > 0;

        if (!isHealthy)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.FailureResponse(
                    "Service is not ready",
                    new[] { "Currency data has not been loaded yet" }
                )
            );
        }

        var (minDate, maxDate) = _dataService.GetDateRange();

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                status = "healthy",
                dataLoaded = _dataService.IsDataLoaded,
                totalDates = _dataService.TotalDatesLoaded,
                dateRange = new
                {
                    from = minDate.ToString("yyyy-MM-dd"),
                    to = maxDate.ToString("yyyy-MM-dd")
                },
                timestamp = DateTime.UtcNow
            },
            "Service is healthy and operational"
        ));
    }

    #endregion

    #region Rolling Average Endpoint

    /// <summary>
    /// Calculates rolling averages (Simple Moving Average) for currencies over a date range.
    /// Uses sliding window technique to compute statistical measures including mean, min, max, standard deviation, and variance.
    /// </summary>
    /// <param name="start_date">Start date in YYYY-MM-DD format (required)</param>
    /// <param name="end_date">End date in YYYY-MM-DD format (required)</param>
    /// <param name="window_size">Window size in days for rolling average calculation (required, minimum 1)</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Rolling average response with statistical measures for each window</returns>
    [HttpGet("api/v{version:apiVersion}/rolling-average")]
    public IActionResult GetRollingAverage(
        [FromQuery] string start_date,
        [FromQuery] string end_date,
        [FromQuery] int window_size,
        [FromQuery(Name = "base")] string? baseParam = null,
        [FromQuery] string? symbols = null)
    {
        if (!DateOnly.TryParseExact(start_date, "yyyy-MM-dd", out var startDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid start_date format",
                new[] { "start_date must be in YYYY-MM-DD format (e.g., 2025-01-01)" }
            ));
        }

        if (!DateOnly.TryParseExact(end_date, "yyyy-MM-dd", out var endDate))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid end_date format",
                new[] { "end_date must be in YYYY-MM-DD format (e.g., 2025-01-30)" }
            ));
        }

        if (endDate < startDate)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid date range",
                new[] { "end_date must be greater than or equal to start_date" }
            ));
        }

        if (window_size < 1)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid window_size",
                new[] { "window_size must be at least 1 day" }
            ));
        }

        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        if (window_size > totalDays)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid window_size",
                new[] { $"window_size ({window_size} days) cannot exceed date range ({totalDays} days)" }
            ));
        }

        var baseCurrency = string.IsNullOrWhiteSpace(baseParam) ? "EUR" : baseParam.Trim().ToUpperInvariant();

        IEnumerable<string>? symbolList = null;
        if (!string.IsNullOrWhiteSpace(symbols))
        {
            symbolList = symbols
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            if (!symbolList.Any())
            {
                symbolList = null;
            }
        }

        _logger.LogInformation(
            "Rolling average requested: StartDate={StartDate}, EndDate={EndDate}, WindowSize={WindowSize}, Base={Base}, Symbols={Symbols}",
            start_date,
            end_date,
            window_size,
            baseCurrency,
            symbols ?? "all"
        );

        List<Models.RollingWindow> rollingWindows;

        try
        {
            rollingWindows = _conversionService.GetRollingAverage(startDate, endDate, window_size, baseCurrency, symbolList);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid parameters",
                new[] { ex.Message }
            ));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "Insufficient data",
                new[] { ex.Message }
            ));
        }

        if (rollingWindows.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { $"No sufficient data available for rolling average calculation in the date range {start_date} to {end_date}" }
            ));
        }

        var response = new Models.RollingAverageResponse
        {
            StartDate = start_date,
            EndDate = end_date,
            Base = baseCurrency,
            WindowSize = window_size,
            Windows = rollingWindows
        };

        return Ok(ApiResponse<Models.RollingAverageResponse>.SuccessResponse(
            response,
            $"Rolling average calculated: {rollingWindows.Count} windows with {window_size}-day periods"
        ));
    }

    #endregion
}
