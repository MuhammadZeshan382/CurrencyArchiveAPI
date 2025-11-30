using Asp.Versioning;
using CurrencyArchiveAPI.Helpers;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Services;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyArchiveAPI.Controllers;

/// <summary>
/// Controller for financial analytics and advanced currency metrics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class AnalyticsController : ControllerBase
{
    private readonly IFinancialAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IFinancialAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>Calculate rolling window statistics</summary>
    [HttpGet("rolling-metrics")]
    [ProducesResponseType(typeof(ApiResponse<RollingMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetRollingMetrics(
        [FromQuery] string start_date,
        [FromQuery] string end_date,
        [FromQuery] int window_size,
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

        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        if (!ValidationHelper.ValidateWindowSize(window_size, totalDays, out error))
        {
            return BadRequest(error);
        }

        var baseCurrency = ValidationHelper.NormalizeBaseCurrency(baseParam);
        var symbolList = ValidationHelper.ParseSymbols(symbols)?.ToList() ?? new List<string>() { "USD" };



        _logger.LogInformation(
            "Rolling average requested: StartDate={StartDate}, EndDate={EndDate}, WindowSize={WindowSize}, Base={Base}, Target={Target}",
            start_date,
            end_date,
            window_size,
            baseCurrency,
            symbolList
        );

        RollingMetricsResponse response;

        try
        {
            response = _analyticsService.GetRollingMetrics(baseCurrency, symbolList, startDate, endDate, window_size);
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

        if (response.Windows.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { $"No sufficient data available for rolling average calculation in the date range {start_date} to {end_date}" }
            ));
        }

        // Order currency dictionaries within each window by currenclecy code
        var orderedWindows = response.Windows
            .Select(w => new RollingWindow
            {
                WindowStart = w.WindowStart,
                WindowEnd = w.WindowEnd,
                DataPoints = w.DataPoints,
                Rates = w.Rates.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value)
            })
            .ToList();

        var orderedRollingResponse = new RollingMetricsResponse
        {
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            Base = response.Base,
            WindowSize = response.WindowSize,
            Windows = orderedWindows
        };

        return Ok(ApiResponse<RollingMetricsResponse>.SuccessResponse(
            orderedRollingResponse,
            $"Rolling average calculated: {orderedRollingResponse.Windows.Count} windows with {window_size}-day periods"
        ));
    }

    /// <summary>Perform comprehensive financial analysis</summary>
    [HttpGet("financial-metrics")]
    [ProducesResponseType(typeof(ApiResponse<FinancialAnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetFinancialMetrics(
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
        var symbolList = ValidationHelper.ParseSymbols(symbols)?.ToList() ?? new List<string>() { "USD" };

        _logger.LogInformation(
            "Financial metrics requested: Start={StartDate}, End={EndDate}, Base={Base}, Symbols={Symbols}",
            start_date,
            end_date,
            baseCurrency,
            symbols ?? "all"
        );

        FinancialMetricsResponse response;

        try
        {
            response = _analyticsService.GetFinancialMetrics(baseCurrency, symbolList, startDate, endDate);
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "Data not found",
                new[] { ex.Message }
            ));
        }

        if (response.Metrics.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No data found",
                new[] { $"No currency data available for financial analysis in the date range {start_date} to {end_date}" }
            ));
        }

        // Calculate total data points from first currency (they should all have same count)
        var dataPoints = response.Metrics.Values.FirstOrDefault()?.DataPoints ?? 0;

        // Order metrics by currency code for deterministic responses
        var orderedMetrics = response.Metrics
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var compatibilityResponse = new FinancialAnalysisResponse
        {
            StartDate = start_date,
            EndDate = end_date,
            Base = baseCurrency,
            DataPoints = dataPoints,
            Currencies = orderedMetrics
        };

        return Ok(ApiResponse<FinancialAnalysisResponse>.SuccessResponse(
            compatibilityResponse,
            $"Financial metrics analysis completed: {compatibilityResponse.Currencies.Count} currencies analyzed over {dataPoints} trading days"
        ));
    }
}
