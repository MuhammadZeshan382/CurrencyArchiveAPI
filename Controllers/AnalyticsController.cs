using Asp.Versioning;
using CurrencyArchiveAPI.Helpers;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Services;
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
    [HttpGet("rolling-metrics")]
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
        var symbolList = ValidationHelper.ParseSymbols(symbols)?.ToList();

        // For rolling average, use first symbol or default to USD if no symbols provided
        var targetCurrency = symbolList?.FirstOrDefault() ?? "USD";

        _logger.LogInformation(
            "Rolling average requested: StartDate={StartDate}, EndDate={EndDate}, WindowSize={WindowSize}, Base={Base}, Target={Target}",
            start_date,
            end_date,
            window_size,
            baseCurrency,
            targetCurrency
        );

        RollingMetricsResponse response;

        try
        {
            response = _analyticsService.GetRollingMetrics(baseCurrency, targetCurrency, startDate, endDate, window_size);
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

        return Ok(ApiResponse<RollingMetricsResponse>.SuccessResponse(
            response,
            $"Rolling average calculated: {response.Windows.Count} windows with {window_size}-day periods"
        ));
    }

    /// <summary>
    /// Analyzes comprehensive financial metrics and risk indicators for currencies over a date range.
    /// Provides returns, volatility, drawdown, Sharpe ratio, VaR, momentum, SMA, Z-score, correlations, and rolling statistics.
    /// </summary>
    /// <param name="start_date">Start date in YYYY-MM-DD format (required)</param>
    /// <param name="end_date">End date in YYYY-MM-DD format (required)</param>
    /// <param name="baseParam">Optional base currency (default: EUR)</param>
    /// <param name="symbols">Optional comma-separated list of currency codes to filter</param>
    /// <returns>Financial metrics response with comprehensive analytics for each currency</returns>
    [HttpGet("financial-metrics")]
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
        var symbolList = ValidationHelper.ParseSymbols(symbols)?.ToList();

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
            response = _analyticsService.GetFinancialMetrics(baseCurrency, symbolList ?? new List<string>(), startDate, endDate);
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

        var compatibilityResponse = new FinancialAnalysisResponse
        {
            StartDate = start_date,
            EndDate = end_date,
            Base = baseCurrency,
            DataPoints = dataPoints,
            Currencies = response.Metrics
        };

        return Ok(ApiResponse<FinancialAnalysisResponse>.SuccessResponse(
            compatibilityResponse,
            $"Financial metrics analysis completed: {response.Metrics.Count} currencies analyzed over {dataPoints} trading days"
        ));
    }
}
