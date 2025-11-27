using Asp.Versioning;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyArchiveAPI.Controllers;

/// <summary>
/// Controller for service health and monitoring endpoints.
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("api")]
public class HealthController : ControllerBase
{
    private readonly ICurrencyDataService _dataService;

    public HealthController(ICurrencyDataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>
    /// Basic health check endpoint for service monitoring.
    /// </summary>
    /// <returns>Health status of the service including data load state</returns>
    [HttpGet("health")]
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
}
