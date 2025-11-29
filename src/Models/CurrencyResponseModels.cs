namespace CurrencyArchiveAPI.Models;

/// <summary>
/// Response model for single exchange rate query.
/// </summary>
public record ExchangeRateResponse
{
    /// <summary>
    /// The currency code for this rate.
    /// </summary>
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>
    /// The date for this exchange rate.
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The exchange rate value.
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    /// The base currency (EUR by default).
    /// </summary>
    public string BaseCurrency { get; init; } = string.Empty;
}

/// <summary>
/// Response model for currency conversion endpoint.
/// </summary>
public record ConvertResponse
{
    /// <summary>
    /// The date used for the conversion rate.
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The source currency code.
    /// </summary>
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// The target currency code.
    /// </summary>
    public string To { get; init; } = string.Empty;

    /// <summary>
    /// The original amount to convert.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// The converted amount in target currency.
    /// </summary>
    public decimal Result { get; init; }

    /// <summary>
    /// The exchange rate used for conversion.
    /// </summary>
    public decimal Rate { get; init; }
}

/// <summary>
/// Response model for available currencies query.
/// </summary>
public record AvailableCurrenciesResponse
{
    /// <summary>
    /// The date for which currencies are listed.
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// Total number of available currencies.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// List of currencies with code and friendly name.
    /// </summary>
    public List<CurrencyInfo> Currencies { get; init; } = new();
}

/// <summary>
/// Lightweight currency information (code + name) used in responses.
/// </summary>
public record CurrencyInfo
{
    /// <summary>
    /// ISO currency code (e.g., USD).
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Friendly currency name (e.g., United States Dollar).
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Response model for bulk exchange rates query.
/// </summary>
public record BulkExchangeRatesResponse
{
    /// <summary>
    /// The date for these exchange rates.
    /// </summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// The base currency for all rates.
    /// </summary>
    public string BaseCurrency { get; init; } = string.Empty;

    /// <summary>
    /// Total number of rates included.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Dictionary of currency codes to exchange rates.
    /// </summary>
    public Dictionary<string, decimal> Rates { get; init; } = new();
}

/// <summary>
/// Response model for dataset information query.
/// </summary>
public record DatasetInfoResponse
{
    /// <summary>
    /// Indicates whether data is loaded.
    /// </summary>
    public bool IsLoaded { get; init; }

    /// <summary>
    /// Total number of dates loaded.
    /// </summary>
    public int TotalDates { get; init; }

    /// <summary>
    /// Date range of available data.
    /// </summary>
    public DateRangeInfo DateRange { get; init; } = new();
}

/// <summary>
/// Date range information.
/// </summary>
public record DateRangeInfo
{
    /// <summary>
    /// Start date of available data.
    /// </summary>
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// End date of available data.
    /// </summary>
    public string To { get; init; } = string.Empty;
}

/// <summary>
/// Response model for timeseries endpoint.
/// Returns daily exchange rates between two dates.
/// </summary>
public record TimeseriesResponse
{
    /// <summary>
    /// Start date of the timeseries (YYYY-MM-DD).
    /// </summary>
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// End date of the timeseries (YYYY-MM-DD).
    /// </summary>
    public string EndDate { get; init; } = string.Empty;

    /// <summary>
    /// Base currency for all rates.
    /// </summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>
    /// Dictionary of dates to currency rates.
    /// Key: Date (YYYY-MM-DD), Value: Dictionary of currency codes to rates.
    /// </summary>
    public Dictionary<string, Dictionary<string, decimal>> Rates { get; init; } = new();
}

/// <summary>
/// Response model for fluctuation endpoint.
/// Shows how currencies fluctuate between two dates.
/// </summary>
public record FluctuationResponse
{
    /// <summary>
    /// Start date of the fluctuation period (YYYY-MM-DD).
    /// </summary>
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// End date of the fluctuation period (YYYY-MM-DD).
    /// </summary>
    public string EndDate { get; init; } = string.Empty;

    /// <summary>
    /// Base currency for all rates.
    /// </summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>
    /// Dictionary of currency codes to their fluctuation data.
    /// </summary>
    public Dictionary<string, CurrencyFluctuation> Rates { get; init; } = new();
}

/// <summary>
/// Fluctuation data for a single currency.
/// </summary>
public record CurrencyFluctuation
{
    /// <summary>
    /// Exchange rate at the start date.
    /// </summary>
    public decimal StartRate { get; init; }

    /// <summary>
    /// Exchange rate at the end date.
    /// </summary>
    public decimal EndRate { get; init; }

    /// <summary>
    /// Absolute change in rate (end_rate - start_rate).
    /// </summary>
    public decimal Change { get; init; }

    /// <summary>
    /// Percentage change ((change / start_rate) * 100).
    /// </summary>
    public decimal ChangePct { get; init; }
}

/// <summary>
/// Response model for Financial analysis endpoint.
/// Provides comprehensive statistical analysis including min, max, and volatility metrics.
/// </summary>
public record FinancialAnalysisResponse
{
    /// <summary>
    /// Start date of the analysis period (YYYY-MM-DD).
    /// </summary>
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// End date of the analysis period (YYYY-MM-DD).
    /// </summary>
    public string EndDate { get; init; } = string.Empty;

    /// <summary>
    /// Base currency for all rates.
    /// </summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>
    /// Number of trading days analyzed.
    /// </summary>
    public int DataPoints { get; init; }

    /// <summary>
    /// Dictionary of currency volatility metrics.
    /// </summary>
    public Dictionary<string, CurrencyVolatilityMetrics> Currencies { get; init; } = new();
}

/// <summary>
/// Comprehensive volatility and statistical metrics for a single currency.
/// </summary>
public record CurrencyVolatilityMetrics
{
    /// <summary>
    /// Minimum exchange rate during the period.
    /// </summary>
    public decimal Min { get; init; }

    /// <summary>
    /// Maximum exchange rate during the period.
    /// </summary>
    public decimal Max { get; init; }

    /// <summary>
    /// Average (mean) exchange rate over the period.
    /// </summary>
    public decimal Average { get; init; }

    /// <summary>
    /// Opening rate (first available date in period).
    /// </summary>
    public decimal OpenRate { get; init; }

    /// <summary>
    /// Closing rate (last available date in period).
    /// </summary>
    public decimal CloseRate { get; init; }

    /// <summary>
    /// Absolute change from open to close.
    /// </summary>
    public decimal Change { get; init; }

    /// <summary>
    /// Percentage change from open to close.
    /// Expressed as percentage (e.g., 2.50 means 2.50%).
    /// </summary>
    public decimal ChangePct { get; init; }

    /// <summary>
    /// Standard deviation of daily rates (population).
    /// Measures the dispersion of rates around the mean.
    /// Expressed in exchange rate units (e.g., 0.0125 for EUR/USD).
    /// </summary>
    public decimal StdDev { get; init; }

    /// <summary>
    /// Variance of daily rates (population).
    /// Square of standard deviation.
    /// Expressed in exchange rate units squared.
    /// </summary>
    public decimal Variance { get; init; }

    /// <summary>
    /// Coefficient of Variation (CV = StdDev / Mean * 100).
    /// Normalized measure of volatility as a percentage of the mean.
    /// Higher CV indicates higher relative volatility.
    /// Expressed as percentage (e.g., 5.0 means 5.0%).
    /// </summary>
    public decimal CoefficientOfVariation { get; init; }

    /// <summary>
    /// Annualized volatility (StdDev(log returns) * sqrt(252)).
    /// Standard financial metric assuming 252 trading days per year.
    /// Expressed as percentage (e.g., 12.50 means 12.50% annual volatility).
    /// </summary>
    public decimal AnnualizedVolatility { get; init; }

    /// <summary>
    /// Range as percentage of mean ((Max - Min) / Mean * 100).
    /// Indicates price range relative to average price.
    /// Expressed as percentage (e.g., 15.0 means 15.0%).
    /// </summary>
    public decimal RangePct { get; init; }

    /// <summary>
    /// Number of data points used in calculations.
    /// </summary>
    public int DataPoints { get; init; }

    // ===== New Professional Financial Metrics =====

    /// <summary>
    /// Average daily return (arithmetic mean of daily returns).
    /// Expressed as percentage (e.g., 0.05 means 0.05% per day).
    /// </summary>
    public decimal AvgDailyReturn { get; init; }

    /// <summary>
    /// Cumulative return over the period ((price_end / price_start) - 1).
    /// Expressed as percentage (e.g., 12.50 means 12.50% total return).
    /// </summary>
    public decimal CumulativeReturn { get; init; }

    /// <summary>
    /// Annualized return ((1 + cumulative)^(252 / days) - 1).
    /// Expressed as percentage (e.g., 8.75 means 8.75% per year).
    /// </summary>
    public decimal AnnualizedReturn { get; init; }

    /// <summary>
    /// Daily volatility (standard deviation of daily returns).
    /// Expressed as percentage (e.g., 0.45 means 0.45% daily volatility).
    /// </summary>
    public decimal DailyVolatility { get; init; }

    /// <summary>
    /// Maximum drawdown (largest peak-to-trough decline).
    /// Expressed as negative percentage (e.g., -15.75 means -15.75% loss).
    /// </summary>
    public decimal MaxDrawdown { get; init; }

    /// <summary>
    /// Sharpe ratio (risk-adjusted return metric).
    /// Formula: (annualized_return - risk_free_rate) / annualized_volatility
    /// Unitless ratio. Higher values indicate better risk-adjusted returns.
    /// Typical values: greater than 1.0 (excellent), 0.5-1.0 (good), 0-0.5 (acceptable), less than 0 (poor)
    /// </summary>
    public decimal SharpeRatio { get; init; }

    /// <summary>
    /// Risk-free rate used in Sharpe ratio calculation.
    /// Expressed as decimal (e.g., 0.04 = 4% annual rate).
    /// </summary>
    public decimal RiskFreeRate { get; init; }

    /// <summary>
    /// Historical Value at Risk at 95% confidence level.
    /// Worst expected daily loss with 95% confidence.
    /// Expressed as negative percentage (e.g., -1.25 means -1.25% daily loss).
    /// </summary>
    public decimal HistoricalVaR95 { get; init; }

    /// <summary>
    /// Parametric Value at Risk at 95% confidence level.
    /// Formula: mean - 1.65 * std (assumes normal distribution)
    /// Expressed as negative percentage (e.g., -1.18 means -1.18% daily loss).
    /// </summary>
    public decimal ParametricVaR95 { get; init; }

    /// <summary>
    /// Z-score of current price ((current - mean) / std).
    /// Measures how many standard deviations the current price is from mean.
    /// Unitless (e.g., 1.75 means 1.75 standard deviations above mean).
    /// </summary>
    public decimal ZScore { get; init; }

    /// <summary>
    /// 3-month momentum return (63 trading days).
    /// Expressed as percentage (e.g., 5.25 means 5.25% gain over 3 months).
    /// </summary>
    public decimal? Momentum3M { get; init; }

    /// <summary>
    /// 12-month momentum return (252 trading days).
    /// Expressed as percentage (e.g., 8.50 means 8.50% gain over 12 months).
    /// </summary>
    public decimal? Momentum12M { get; init; }

    /// <summary>
    /// 50-day simple moving average.
    /// Expressed in exchange rate units (e.g., 1.0920 for EUR/USD).
    /// </summary>
    public decimal? SMA50 { get; init; }

    /// <summary>
    /// 200-day simple moving average.
    /// Expressed in exchange rate units (e.g., 1.0875 for EUR/USD).
    /// </summary>
    public decimal? SMA200 { get; init; }

    /// <summary>
    /// Rolling window metrics for 30, 60, 90, 180-day periods.
    /// </summary>
    public RollingMetrics? Rolling { get; init; }

    /// <summary>
    /// Correlation coefficients against other currencies (if multiple currencies analyzed).
    /// </summary>
    public Dictionary<string, decimal>? Correlations { get; init; }
}

/// <summary>
/// Rolling window metrics for multiple time periods.
/// </summary>
public record RollingMetrics
{
    public RollingPeriodMetrics? Window30D { get; init; }
    public RollingPeriodMetrics? Window60D { get; init; }
    public RollingPeriodMetrics? Window90D { get; init; }
    public RollingPeriodMetrics? Window180D { get; init; }
}

/// <summary>
/// Metrics for a specific rolling window period.
/// </summary>
public record RollingPeriodMetrics
{
    /// <summary>
    /// Rolling mean price.
    /// Expressed in exchange rate units (e.g., 1.0850 for EUR/USD).
    /// </summary>
    public decimal Mean { get; init; }

    /// <summary>
    /// Rolling standard deviation of prices.
    /// Expressed in exchange rate units (e.g., 0.0125 for EUR/USD).
    /// </summary>
    public decimal StdDev { get; init; }

    /// <summary>
    /// Rolling return over the window.
    /// Expressed as percentage (e.g., 3.50 means 3.50% return over the window).
    /// </summary>
    public decimal Return { get; init; }

    /// <summary>
    /// Rolling volatility (annualized from window's daily returns).
    /// Expressed as percentage (e.g., 10.25 means 10.25% annualized volatility).
    /// </summary>
    public decimal Volatility { get; init; }
}


/// <summary>
/// Response model for rolling average endpoint.
/// Provides moving averages for currencies over specified window periods.
/// </summary>
public record RollingMetricsResponse
{
    /// <summary>
    /// Start date of the analysis period (YYYY-MM-DD).
    /// </summary>
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// End date of the analysis period (YYYY-MM-DD).
    /// </summary>
    public string EndDate { get; init; } = string.Empty;

    /// <summary>
    /// Base currency for all rates.
    /// </summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>
    /// Window size in days for the rolling average calculation.
    /// </summary>
    public int WindowSize { get; init; }

    /// <summary>
    /// List of rolling average windows with their calculated values.
    /// </summary>
    public List<RollingWindow> Windows { get; init; } = new();
}

/// <summary>
/// Represents a single rolling average window period.
/// </summary>
public record RollingWindow
{
    /// <summary>
    /// Start date of this window (YYYY-MM-DD).
    /// </summary>
    public string WindowStart { get; init; } = string.Empty;

    /// <summary>
    /// End date of this window (YYYY-MM-DD).
    /// </summary>
    public string WindowEnd { get; init; } = string.Empty;

    /// <summary>
    /// Number of data points used in this window calculation.
    /// </summary>
    public int DataPoints { get; init; }

    /// <summary>
    /// Dictionary of currency codes to their rolling average rates.
    /// </summary>
    public Dictionary<string, RollingAverageData> Rates { get; init; } = new();
}

/// <summary>
/// Statistical data for a currency's rolling average.
/// </summary>
public record RollingAverageData
{
    /// <summary>
    /// Simple Moving Average (SMA) - arithmetic mean of rates in the window.
    /// </summary>
    public decimal Average { get; init; }

    /// <summary>
    /// Minimum rate in the window.
    /// </summary>
    public decimal Min { get; init; }

    /// <summary>
    /// Maximum rate in the window.
    /// </summary>
    public decimal Max { get; init; }

    /// <summary>
    /// Standard deviation of rates in the window (measure of volatility).
    /// </summary>
    public decimal StdDev { get; init; }

    /// <summary>
    /// Variance of rates in the window.
    /// </summary>
    public decimal Variance { get; init; }
}

/// <summary>
/// Response model for financial metrics endpoint.
/// Returns comprehensive financial analytics for currencies.
/// </summary>
public record FinancialMetricsResponse
{
    /// <summary>
    /// Start date of the analysis period (YYYY-MM-DD).
    /// </summary>
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// End date of the analysis period (YYYY-MM-DD).
    /// </summary>
    public string EndDate { get; init; } = string.Empty;

    /// <summary>
    /// Base currency for all metrics.
    /// </summary>
    public string Base { get; init; } = string.Empty;

    /// <summary>
    /// Dictionary of currency codes to their financial metrics.
    /// </summary>
    public Dictionary<string, CurrencyVolatilityMetrics> Metrics { get; init; } = new();
}
