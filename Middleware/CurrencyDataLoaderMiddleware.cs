using CurrencyArchiveAPI.Services;

namespace CurrencyArchiveAPI.Middleware;

/// <summary>
/// Middleware that ensures currency data is loaded before processing any requests.
/// </summary>
public class CurrencyDataLoaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CurrencyDataLoaderMiddleware> _logger;
    private static bool _isInitialized = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public CurrencyDataLoaderMiddleware(RequestDelegate next, ILogger<CurrencyDataLoaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrencyDataService currencyDataService)
    {
        // Ensure data is loaded (only once)
        if (!_isInitialized)
        {
            await _initLock.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    _logger.LogInformation("Initializing currency data on first request...");
                    
                    if (currencyDataService is CurrencyDataService service)
                    {
                        await service.LoadDataAsync();
                        _isInitialized = true;
                        _logger.LogInformation("Currency data initialization completed");
                    }
                    else
                    {
                        _logger.LogError("CurrencyDataService not properly registered");
                        throw new InvalidOperationException("Currency data service not available");
                    }
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        // Verify data is loaded before processing request
        if (!currencyDataService.IsDataLoaded)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Service Unavailable",
                message = "Currency data is not loaded yet. Please try again."
            });
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register the currency data loader middleware.
/// </summary>
public static class CurrencyDataLoaderMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrencyDataLoader(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CurrencyDataLoaderMiddleware>();
    }
}
