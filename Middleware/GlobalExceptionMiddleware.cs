using CurrencyArchiveAPI.Models;
using System.Net;
using System.Text.Json;

namespace CurrencyArchiveAPI.Middleware;

/// <summary>
/// Global exception handling middleware that converts exceptions into RFC 7807 Problem Details.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var problemDetails = exception switch
        {
            ArgumentException argEx => CreateProblemDetails(
                context,
                HttpStatusCode.BadRequest,
                "Bad Request",
                argEx.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            
            KeyNotFoundException notFoundEx => CreateProblemDetails(
                context,
                HttpStatusCode.NotFound,
                "Not Found",
                notFoundEx.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            ),
            
            InvalidOperationException invalidOpEx => CreateProblemDetails(
                context,
                HttpStatusCode.BadRequest,
                "Invalid Operation",
                invalidOpEx.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            
            _ => CreateProblemDetails(
                context,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An unexpected error occurred. Please try again later.",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            )
        };

        context.Response.StatusCode = problemDetails.Status;
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private ProblemDetailsResponse CreateProblemDetails(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail,
        string type)
    {
        return new ProblemDetailsResponse
        {
            Type = type,
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
    }
}

/// <summary>
/// Extension method to register the global exception middleware.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
