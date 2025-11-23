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
    /// List of currency codes.
    /// </summary>
    public List<string> Currencies { get; init; } = new();
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
