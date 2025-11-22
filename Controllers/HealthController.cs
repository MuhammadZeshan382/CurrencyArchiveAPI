using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyArchiveAPI.Controllers;

/// <summary>
/// Health check controller for monitoring service status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ICurrencyDataService _dataService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ICurrencyDataService dataService, ILogger<HealthController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    /// <returns>Health status of the service.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetHealth()
    {
        var isHealthy = _dataService.IsDataLoaded && _dataService.TotalDatesLoaded > 0;

        if (!isHealthy)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.FailureResponse(
                    "Service is not ready",
                    new[] { "Currency data not loaded" }
                )
            );
        }

        var (minDate, maxDate) = _dataService.GetDateRange();

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                status = "Healthy",
                dataLoaded = _dataService.IsDataLoaded,
                totalDates = _dataService.TotalDatesLoaded,
                dateRange = new
                {
                    from = minDate.ToString("yyyy-MM-dd"),
                    to = maxDate.ToString("yyyy-MM-dd")
                },
                timestamp = DateTime.UtcNow
            },
            "Service is healthy"
        ));
    }
}
