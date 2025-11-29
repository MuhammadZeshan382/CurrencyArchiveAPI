using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Models;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for common validation operations across controllers.
/// Provides reusable validation methods for dates, currencies, and query parameters.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates and parses a date string in YYYY-MM-DD format.
    /// </summary>
    /// <param name="dateString">Date string to validate.</param>
    /// <param name="parameterName">Name of the parameter for error messages.</param>
    /// <param name="parsedDate">The parsed DateOnly value if successful.</param>
    /// <param name="errorResponse">Error response if validation fails.</param>
    /// <returns>True if validation succeeds, false otherwise.</returns>
    public static bool TryValidateDate(
        string? dateString,
        string parameterName,
        out DateOnly parsedDate,
        out ApiResponse<object>? errorResponse)
    {
        parsedDate = default;
        errorResponse = null;

        if (string.IsNullOrWhiteSpace(dateString))
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                "Missing required parameter",
                new[] { $"Parameter '{parameterName}' is required" }
            );
            return false;
        }

        if (!DateOnly.TryParseExact(dateString, AppConstants.DateFormats.StandardDateFormat, out parsedDate))
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                AppConstants.ErrorMessages.InvalidDateFormat,
                new[] { string.Format(AppConstants.ValidationMessages.DateFormatInvalid, parameterName) }
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates and parses a date string with an optional parameter.
    /// </summary>
    public static bool TryValidateDateOptional(
        string? dateString,
        string parameterName,
        out DateOnly? parsedDate,
        out ApiResponse<object>? errorResponse)
    {
        parsedDate = null;
        errorResponse = null;

        if (string.IsNullOrWhiteSpace(dateString))
        {
            return true; // Optional parameter
        }

        if (!DateOnly.TryParseExact(dateString, AppConstants.DateFormats.StandardDateFormat, out var date))
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                AppConstants.ErrorMessages.InvalidDateFormat,
                new[] { string.Format(AppConstants.ValidationMessages.DateFormatInvalid, parameterName) }
            );
            return false;
        }

        parsedDate = date;
        return true;
    }

    /// <summary>
    /// Validates a date range ensuring end date is not before start date.
    /// </summary>
    public static bool ValidateDateRange(
        DateOnly startDate,
        DateOnly endDate,
        out ApiResponse<object>? errorResponse)
    {
        errorResponse = null;

        if (endDate < startDate)
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                "Invalid date range",
                new[] { "end_date must be greater than or equal to start_date" }
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates and normalizes a currency code.
    /// </summary>
    public static bool TryValidateCurrencyCode(
        string? currencyCode,
        string parameterName,
        out string normalizedCode,
        out ApiResponse<object>? errorResponse)
    {
        normalizedCode = string.Empty;
        errorResponse = null;

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                "Missing required parameter",
                new[] { $"Parameter '{parameterName}' is required" }
            );
            return false;
        }

        normalizedCode = currencyCode.Trim().ToUpperInvariant();
        return true;
    }

    /// <summary>
    /// Validates an amount ensuring it's greater than zero.
    /// </summary>
    public static bool ValidateAmount(
        decimal amount,
        out ApiResponse<object>? errorResponse)
    {
        errorResponse = null;

        if (amount <= 0)
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                "Invalid amount",
                new[] { "Amount must be greater than 0" }
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates window size for rolling averages.
    /// </summary>
    public static bool ValidateWindowSize(
        int windowSize,
        int totalDays,
        out ApiResponse<object>? errorResponse)
    {
        errorResponse = null;

        if (windowSize < 1)
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                "Invalid window_size",
                new[] { "window_size must be at least 1 day" }
            );
            return false;
        }

        if (windowSize > totalDays)
        {
            errorResponse = ApiResponse<object>.FailureResponse(
                "Invalid window_size",
                new[] { $"window_size ({windowSize} days) cannot exceed date range ({totalDays} days)" }
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parses and normalizes a comma-separated list of currency symbols.
    /// Returns null if the input is empty or results in no valid symbols.
    /// </summary>
    public static IEnumerable<string>? ParseSymbols(string? symbols)
    {
        if (string.IsNullOrWhiteSpace(symbols))
        {
            return null;
        }

        var symbolList = symbols
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        return symbolList.Count > 0 ? symbolList : null;
    }

    /// <summary>
    /// Normalizes a base currency parameter, defaulting to EUR if not provided.
    /// </summary>
    public static string NormalizeBaseCurrency(string? baseCurrency)
    {
        return string.IsNullOrWhiteSpace(baseCurrency)
            ? "EUR"
            : baseCurrency.Trim().ToUpperInvariant();
    }
}
