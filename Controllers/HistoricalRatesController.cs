using Asp.Versioning;
using CurrencyArchiveAPI.Helpers;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Services;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyArchiveAPI.Controllers;

/// <summary>
/// Controller for historical exchange rates and timeseries data.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class HistoricalRatesController : ControllerBase
{
    private readonly IHistoricalRatesService _historicalRatesService;
    private readonly ILogger<HistoricalRatesController> _logger;

    public HistoricalRatesController(
        IHistoricalRatesService historicalRatesService,
        ILogger<HistoricalRatesController> logger)
    {
        _historicalRatesService = historicalRatesService;
        _logger = logger;
    }

    /// <summary>
    /// Gets historical exchange rates for a specific date with custom base currency.
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Historical rates response with all requested currency rates</returns>
    [HttpGet("historical")]
    public IActionResult GetHistoricalRates(
        [FromQuery] string date,
        [FromQuery(Name = "base")] string? baseParam = null,
        [FromQuery] string? symbols = null)
    {
        if (!ValidationHelper.TryValidateDate(date, "date", out var parsedDate, out var error))
        {
            return BadRequest(error);
        }

        var baseCurrency = ValidationHelper.NormalizeBaseCurrency(baseParam);
        var symbolList = ValidationHelper.ParseSymbols(symbols)?.ToList();

        _logger.LogInformation(
            "Historical rates requested: Date={Date}, Base={Base}, Symbols={Symbols}",
            parsedDate,
            baseCurrency,
            symbols ?? "all"
        );

        var response = _historicalRatesService.GetHistoricalRates(parsedDate, baseCurrency, symbolList);

        if (response.Rates.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No rates found",
                new[] { $"No exchange rates available for {date}" }
            ));
        }

        // Order currency rates by code (ascending) for deterministic responses
        var orderedRates = response.Rates
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var orderedResponse = new HistoricalRatesResponse
        {
            Date = response.Date,
            Base = response.Base,
            Rates = orderedRates,
            Timestamp = response.Timestamp
        };

        return Ok(ApiResponse<HistoricalRatesResponse>.SuccessResponse(
            orderedResponse,
            $"Historical rates retrieved successfully for {date}"
        ));
    }

    /// <summary>
    /// Gets daily historical exchange rates between two dates.
    /// </summary>
    /// <param name="start_date">Start date in YYYY-MM-DD format (required)</param>
    /// <param name="end_date">End date in YYYY-MM-DD format (required)</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Timeseries response with daily rates for the date range</returns>
    [HttpGet("timeseries")]
    public IActionResult GetTimeseries(
        [FromQuery] string start_date,
        [FromQuery] string end_date,
        [FromQuery(Name = "base")] string? baseParam = null,
        [FromQuery] string? symbols = null)
    {
        if (!ValidationHelper.TryValidateDate(start_date, "start_date", out var startDate, out var error))
        {
            return BadRequest(error);
        }

        if (!ValidationHelper.TryValidateDate(end_date, "end_date", out var endDate, out error))
        {
            return BadRequest(error);
        }

        if (!ValidationHelper.ValidateDateRange(startDate, endDate, out error))
        {
            return BadRequest(error);
        }

        var baseCurrency = ValidationHelper.NormalizeBaseCurrency(baseParam);
        var symbolList = ValidationHelper.ParseSymbols(symbols)?.ToList();

        _logger.LogInformation(
            "Timeseries requested: StartDate={StartDate}, EndDate={EndDate}, Base={Base}, Symbols={Symbols}",
            start_date,
            end_date,
            baseCurrency,
            symbols ?? "all"
        );

        var response = _historicalRatesService.GetTimeseries(baseCurrency, symbolList ?? new List<string>(), startDate, endDate);

        if (response.Rates.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { $"No exchange rate data available for the date range {start_date} to {end_date}" }
            ));
        }

        // Order inner currency dictionaries by currency code for each date
        var orderedRatesByDate = response.Rates
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.OrderBy(inner => inner.Key).ToDictionary(inner => inner.Key, inner => inner.Value)
            );

        var orderedTimeseries = new TimeseriesResponse
        {
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            Base = response.Base,
            Rates = orderedRatesByDate
        };

        return Ok(ApiResponse<TimeseriesResponse>.SuccessResponse(
            orderedTimeseries,
            $"Timeseries data retrieved: {orderedTimeseries.Rates.Count} dates from {start_date} to {end_date}"
        ));
    }

    /// <summary>
    /// Gets currency fluctuation data between two dates.
    /// Shows start rate, end rate, absolute change, and percentage change.
    /// </summary>
    /// <param name="start_date">Start date in YYYY-MM-DD format (required)</param>
    /// <param name="end_date">End date in YYYY-MM-DD format (required)</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Fluctuation response with change data for each currency</returns>
    [HttpGet("fluctuation")]
    public IActionResult GetFluctuation(
        [FromQuery] string start_date,
        [FromQuery] string end_date,
        [FromQuery(Name = "base")] string? baseParam = null,
        [FromQuery] string? symbols = null)
    {
        if (!ValidationHelper.TryValidateDate(start_date, "start_date", out var startDate, out var error))
        {
            return BadRequest(error);
        }

        if (!ValidationHelper.TryValidateDate(end_date, "end_date", out var endDate, out error))
        {
            return BadRequest(error);
        }

        if (!ValidationHelper.ValidateDateRange(startDate, endDate, out error))
        {
            return BadRequest(error);
        }

        var baseCurrency = ValidationHelper.NormalizeBaseCurrency(baseParam);
        var symbolList = ValidationHelper.ParseSymbols(symbols)?.ToList();

        _logger.LogInformation(
            "Fluctuation requested: StartDate={StartDate}, EndDate={EndDate}, Base={Base}, Symbols={Symbols}",
            start_date,
            end_date,
            baseCurrency,
            symbols ?? "all"
        );

        FluctuationResponse response;

        try
        {
            response = _historicalRatesService.GetFluctuation(baseCurrency, symbolList ?? new List<string>(), startDate, endDate);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { ex.Message }
            ));
        }

        if (response.Rates.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { $"No common currency data available for the date range {start_date} to {end_date}" }
            ));
        }

        // Order fluctuation rates by currency code for deterministic responses
        var orderedFluctuationRates = response.Rates
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var orderedFluctuation = new FluctuationResponse
        {
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            Base = response.Base,
            Rates = orderedFluctuationRates
        };

        return Ok(ApiResponse<FluctuationResponse>.SuccessResponse(
            orderedFluctuation,
            $"Fluctuation data retrieved: {orderedFluctuation.Rates.Count} currencies from {start_date} to {end_date}"
        ));
    }
}
