using Asp.Versioning;
using CurrencyArchiveAPI.Helpers;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyArchiveAPI.Controllers;

/// <summary>
/// Controller for currency conversion operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class ConversionController : ControllerBase
{
    private readonly ICurrencyDataService _dataService;
    private readonly ICurrencyConverterService _converterService;
    private readonly ILogger<ConversionController> _logger;

    public ConversionController(
        ICurrencyDataService dataService,
        ICurrencyConverterService converterService,
        ILogger<ConversionController> logger)
    {
        _dataService = dataService;
        _converterService = converterService;
        _logger = logger;
    }

    /// <summary>
    /// Converts an amount from one currency to another using exchange rates.
    /// </summary>
    /// <param name="from">Source currency code (e.g., GBP)</param>
    /// <param name="to">Target currency code (e.g., JPY)</param>
    /// <param name="amount">Amount to convert</param>
    /// <param name="date">Optional date in YYYY-MM-DD format. If not specified, uses most recent available data.</param>
    /// <returns>Conversion result with converted amount and exchange rate</returns>
    [HttpGet("convert")]
    public IActionResult ConvertCurrency(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount,
        [FromQuery] string? date = null)
    {
        // Validate source currency
        if (!ValidationHelper.TryValidateCurrencyCode(from, "from", out var fromCurrency, out var error))
        {
            return BadRequest(error);
        }

        // Validate target currency
        if (!ValidationHelper.TryValidateCurrencyCode(to, "to", out var toCurrency, out error))
        {
            return BadRequest(error);
        }

        // Validate amount
        if (!ValidationHelper.ValidateAmount(amount, out error))
        {
            return BadRequest(error);
        }

        // Determine the date to use
        DateOnly conversionDate;
        if (string.IsNullOrWhiteSpace(date))
        {
            // Use most recent date
            var (_, maxDate) = _dataService.GetDateRange();
            conversionDate = maxDate;
        }
        else
        {
            if (!ValidationHelper.TryValidateDate(date, "date", out conversionDate, out error))
            {
                return BadRequest(error);
            }
        }

        _logger.LogInformation(
            "Conversion requested: {Amount} {From} to {To} on {Date}",
            amount,
            fromCurrency,
            toCurrency,
            conversionDate);

        try
        {
            // Perform the conversion
            var convertedAmount = _converterService.Convert(fromCurrency, toCurrency, conversionDate, amount);

            // Calculate the exchange rate (result / amount)
            var exchangeRate = amount != 0 ? convertedAmount / amount : 0;

            var response = new ConvertResponse
            {
                Date = conversionDate.ToString("yyyy-MM-dd"),
                From = fromCurrency,
                To = toCurrency,
                Amount = amount,
                Result = Math.Round(convertedAmount, 6),
                Rate = Math.Round(exchangeRate, 6)
            };

            return Ok(ApiResponse<ConvertResponse>.SuccessResponse(
                response,
                $"Converted {amount} {fromCurrency} to {convertedAmount:F6} {toCurrency}"
            ));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "Exchange rate not found",
                new[] { ex.Message }
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid conversion request",
                new[] { ex.Message }
            ));
        }
    }

    /// <summary>
    /// Gets all available currency codes for a specific date.
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format (e.g., 2024-01-01)</param>
    /// <returns>List of all currency codes available on the specified date</returns>
    [HttpGet("currencies")]
    public IActionResult GetAvailableCurrencies([FromQuery] string date)
    {
        if (!ValidationHelper.TryValidateDate(date, "date", out var parsedDate, out var error))
        {
            return BadRequest(error);
        }

        _logger.LogInformation("Available currencies requested for date: {Date}", date);

            var codes = _converterService.GetAvailableCurrencies(parsedDate)
                .OrderBy(c => c)
                .ToList();

        if (codes.Count == 0)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "No currencies found",
                new[] { $"No currency data available for {date}" }
            ));
        }

        // Enrich codes with friendly names using the CurrencySymbols dictionary
        var currencyInfos = codes
            .Select(code => new CurrencyInfo
            {
                Code = code,
                Name = CurrencySymbols.Symbols.TryGetValue(code, out var name) ? name : string.Empty
            })
            .ToList();

        var response = new AvailableCurrenciesResponse
        {
            Date = date,
            Count = currencyInfos.Count,
            Currencies = currencyInfos
        };

        return Ok(ApiResponse<AvailableCurrenciesResponse>.SuccessResponse(
            response,
            $"{response.Count} currencies available for {date}"
        ));
    }
}
