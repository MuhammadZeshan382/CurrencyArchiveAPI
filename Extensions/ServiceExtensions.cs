using CurrencyArchiveAPI.Services;

namespace CurrencyArchiveAPI.Extensions;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registers currency-related services with dependency injection.
    /// </summary>
    public static IServiceCollection AddCurrencyServices(this IServiceCollection services)
    {
        // Register currency data service as singleton (data is loaded once and shared)
        services.AddSingleton<ICurrencyDataService, CurrencyDataService>();
        
        // Register conversion service as scoped
        services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();

        return services;
    }
}
