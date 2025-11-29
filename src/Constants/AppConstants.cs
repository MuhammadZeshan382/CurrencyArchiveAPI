namespace CurrencyArchiveAPI.Constants;

/// <summary>
/// Application-wide constants for CurrencyArchiveAPI.
/// Centralizes magic strings and numbers for maintainability.
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Date and time format constants.
    /// </summary>
    public static class DateFormats
    {
        public const string StandardDateFormat = "yyyy-MM-dd";
    }

    /// <summary>
    /// Financial calculation constants.
    /// </summary>
    public static class Financial
    {
        /// <summary>
        /// Number of trading days in a year (252 days).
        /// </summary>
        public const int TradingDaysPerYear = 252;

        /// <summary>
        /// Number of calendar days in a year (365 days).
        /// </summary>
        public const int CalendarDaysPerYear = 365;

        /// <summary>
        /// Risk-free rate used for Sharpe ratio calculations (4%).
        /// </summary>
        public const decimal RiskFreeRate = 0.04m;

        /// <summary>
        /// Z-score for 95% confidence interval (1.65).
        /// </summary>
        public const double ZScore95Confidence = 1.65;

        /// <summary>
        /// Approximate trading days in 3 months (63 days).
        /// </summary>
        public const int TradingDays3Months = 63;

        /// <summary>
        /// Approximate trading days in 12 months (252 days).
        /// </summary>
        public const int TradingDays12Months = 252;

        /// <summary>
        /// Period for 50-day Simple Moving Average.
        /// </summary>
        public const int SMA50Period = 50;

        /// <summary>
        /// Period for 200-day Simple Moving Average.
        /// </summary>
        public const int SMA200Period = 200;

        /// <summary>
        /// Rolling window period: 30 days.
        /// </summary>
        public const int RollingWindow30Days = 30;

        /// <summary>
        /// Rolling window period: 60 days.
        /// </summary>
        public const int RollingWindow60Days = 60;

        /// <summary>
        /// Rolling window period: 90 days.
        /// </summary>
        public const int RollingWindow90Days = 90;

        /// <summary>
        /// Rolling window period: 180 days.
        /// </summary>
        public const int RollingWindow180Days = 180;

        /// <summary>
        /// Minimum data points required for VaR calculation.
        /// </summary>
        public const int MinDataPointsForVaR = 20;

        /// <summary>
        /// Percentile for historical VaR (5%).
        /// </summary>
        public const double HistoricalVaRPercentile = 0.05;

        /// <summary>
        /// Percentage multiplier (100).
        /// </summary>
        public const decimal PercentageMultiplier = 100m;
    }

    /// <summary>
    /// Rounding precision constants.
    /// </summary>
    public static class Precision
    {
        public const int ExchangeRatePrecision = 6;
        public const int ChangePrecision = 4;
        public const int StandardDeviationPrecision = 8;
        public const int VariancePrecision = 10;
    }

    /// <summary>
    /// Currency-related constants.
    /// </summary>
    public static class Currency
    {
        /// <summary>
        /// Base currency for all exchange rates in the system.
        /// </summary>
        public const string BaseCurrency = "EUR";

        /// <summary>
        /// Default base currency when none is specified.
        /// </summary>
        public const string DefaultBaseCurrency = "EUR";
    }

    /// <summary>
    /// Error messages.
    /// </summary>
    public static class ErrorMessages
    {
        public const string MissingRequiredParameter = "Missing required parameter";
        public const string InvalidDateFormat = "Invalid date format";
        public const string InvalidDateRange = "Invalid date range";
        public const string InvalidAmount = "Invalid amount";
        public const string InvalidWindowSize = "Invalid window_size";
        public const string ExchangeRateNotFound = "Exchange rate not found";
        public const string NoCurrenciesFound = "No currencies found";
        public const string NoRatesFound = "No rates found";
        public const string NoDataFound = "No data found";
        public const string InvalidParameters = "Invalid parameters";
        public const string InsufficientData = "Insufficient data";
        public const string DataNotFound = "Data not found";
        public const string ServiceNotReady = "Service is not ready";
        public const string InvalidConversionRequest = "Invalid conversion request";
    }

    /// <summary>
    /// Success messages.
    /// </summary>
    public static class SuccessMessages
    {
        public const string RequestSuccessful = "Request successful";
        public const string ServiceHealthy = "Service is healthy and operational";
        public const string ConversionSuccessful = "Currency conversion completed successfully";
        public const string HistoricalRatesRetrieved = "Historical rates retrieved successfully";
        public const string TimeseriesRetrieved = "Timeseries data retrieved successfully";
        public const string FluctuationRetrieved = "Fluctuation data retrieved successfully";
        public const string RollingAverageCalculated = "Rolling average calculated successfully";
        public const string MetricsAnalysisCompleted = "Financial metrics analysis completed";
    }

    /// <summary>
    /// Validation messages.
    /// </summary>
    public static class ValidationMessages
    {
        public const string ParameterRequired = "Parameter '{0}' is required";
        public const string DateFormatInvalid = "{0} must be in YYYY-MM-DD format (e.g., 2024-01-01)";
        public const string AmountMustBePositive = "Amount must be greater than 0";
        public const string WindowSizeMustBePositive = "window_size must be at least 1 day";
        public const string WindowSizeExceedsRange = "window_size ({0} days) cannot exceed date range ({1} days)";
        public const string EndDateMustBeAfterStart = "end_date must be greater than or equal to start_date";
        public const string StartDateMustBeBeforeEnd = "Start date must be before or equal to end date";
        public const string EndDateMustBeAfterStartDate = "End date must be greater than or equal to start date";
        public const string AmountCannotBeNegative = "Amount cannot be negative";
        public const string WindowSizeMustBeAtLeastOne = "Window size must be at least 1 day";
    }

    /// <summary>
    /// Configuration keys.
    /// </summary>
    public static class Configuration
    {
        public const string CurrencyDataPath = "CurrencyDataPath";
        public const string DefaultDataPath = "Data";
    }

    /// <summary>
    /// File and path constants.
    /// </summary>
    public static class FilePaths
    {
        public const string JsonFilePattern = "*.json";
        public const string DateFileNameFormat = "dd-MM-yyyy";
    }
}
