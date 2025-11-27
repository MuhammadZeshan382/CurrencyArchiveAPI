using CurrencyArchiveAPI.Helpers;
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

        // Register helper services as singleton (stateless helpers, safe to share)
        services.AddSingleton<DataLoaderHelper>();
        services.AddSingleton<IRateConversionHelper, RateConversionHelper>();
        services.AddSingleton<TimeseriesDataHelper>();
        services.AddSingleton<FluctuationCalculatorHelper>();
        services.AddSingleton<RateCollectionHelper>();
        services.AddSingleton<RollingWindowHelper>();
        services.AddSingleton<CurrencyMetricsCalculatorHelper>();
        services.AddSingleton<CorrelationHelper>();

        // Register domain-specific services as scoped
        services.AddScoped<ICurrencyConverterService, CurrencyConverterService>();
        services.AddScoped<IHistoricalRatesService, HistoricalRatesService>();
        services.AddScoped<IFinancialAnalyticsService, FinancialAnalyticsService>();

        return services;
    }
}
