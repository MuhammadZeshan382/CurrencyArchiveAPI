# CurrencyArchiveAPI – GitHub Copilot Project Instructions

This file defines how GitHub Copilot should generate code for the CurrencyArchiveAPI project.  
The API loads 20 years of historical exchange rates into memory and exposes a few fast conversion endpoints.  
Architecture must remain lightweight, high-performance, and easy to maintain.

---

# 1. Project Philosophy
- Keep the entire codebase simple, readable, and fast.
- Avoid over-architecting or heavy layering.
- Use in-memory data structures for instant currency conversion.
- Emphasize correctness, performance, and clarity.
- All design decisions must support maintainability and ease of onboarding.

---

# 2. Current Project Structure & State

## 2.1 Actual Folder Structure (as-is)
```
CurrencyArchiveAPI/
├── Program.cs                    # Minimal ASP.NET Core 8 bootstrap
├── Convert.cs                    # Legacy utility class (namespace: CurrencyLibrary.Utility)
├── Controllers/                  # Empty - needs implementation
├── Data/                         # 20 years of daily rates (1999-2025)
│   ├── {Year}/                   # e.g., 2024/
│   │   ├── {Month}/              # e.g., January/
│   │   │   ├── DD-MM-YYYY.json   # e.g., 01-01-2024.json
```

## 2.2 Data Format Discovery
- **Location**: `Data/{Year}/{Month}/DD-MM-YYYY.json`
- **Structure**: Flat JSON object with currency codes as keys (e.g., `"EUR": 1`, `"USD": 1.103769`)
- **Base Currency**: EUR is always 1.0 (all rates are EUR-based)
- **Coverage**: Daily files from 1999-01-04 through 2025 (26+ years, ~9,500 files)
- **Currencies**: 170+ currencies per file (including crypto like BTC)

## 2.3 Target Architecture (to be implemented)

### Controllers/
- Handle HTTP requests
- Map requests to services
- Contain zero business logic
- Use `[ApiVersion("1.0")]` attribute

### Services/
- **ICurrencyConversionService**: Core service interface
- Load all JSON files at startup into optimized `Dictionary<DateOnly, Dictionary<string, decimal>>`
- Perform currency conversions using decimal arithmetic
- Expose: Convert(), GetAvailableCurrencies(), GetRateHistory()

### Models/
- Request/Response DTOs as `record` types
- Unified API response wrapper with success/message/data/errors
- Immutable rate models

### Extensions/
- Service registration (`AddCurrencyServices()`)
- Exception middleware registration
- CORS/rate limiting configuration

---

# 3. Data Loading Implementation Pattern

## 3.1 Startup Loading Strategy
```csharp
// Use IHostedService or configure in Program.cs before app.Run()
// Load all files from Data/ recursively into:
Dictionary<DateOnly, Dictionary<string, decimal>> _rates;

// Key format: new DateOnly(2024, 1, 1)
// Value: { "USD" => 1.103769m, "GBP" => 0.866892m, ... }
```

## 3.2 File Path Convention
- Parse directory structure: `Data/{yyyy}/{MonthName}/DD-MM-yyyy.json`
- Example: `Data/2024/January/01-01-2024.json` → DateOnly(2024, 1, 1)
- Handle month name → number mapping (e.g., "January" → 1)

## 3.3 Decimal Precision Rules
- **Always use `decimal`** for rates (never `double` or `float`)
- Parse JSON numbers as `decimal` to avoid precision loss
- The existing `Convert.cs` uses `double` - this is legacy and should NOT be used as reference

## 3.4 Memory Optimization
- Pre-size dictionaries if possible: `new Dictionary<string, decimal>(170)`
- Consider `FrozenDictionary<TKey, TValue>` from .NET 8 for immutable post-load data
- Estimated memory: ~170 currencies × 9,500 days × 16 bytes ≈ 25MB (acceptable for in-memory)

---

# 4. Currency Conversion Logic

## 4.1 EUR-Based Cross-Rate Conversion
All rates in the dataset are EUR-based. To convert from CurrencyA → CurrencyB:

```csharp
// Example: USD to GBP on 2024-01-01
// rates["USD"] = 1.103769  (1 EUR = 1.103769 USD)
// rates["GBP"] = 0.866892  (1 EUR = 0.866892 GBP)

// Convert 100 USD to GBP:
// Step 1: USD → EUR: 100 / 1.103769 = 90.60 EUR
// Step 2: EUR → GBP: 90.60 * 0.866892 = 78.54 GBP

decimal result = (amount / rateFrom) * rateTo;
```

## 4.2 Legacy Code Warning
The file `Convert.cs` (namespace `CurrencyLibrary.Utility`) contains a `double`-based conversion method.  
**Do NOT use or reference this code** - it violates the decimal-only rule and uses incorrect namespacing.

## 4.3 Service Interface Design
```csharp
public interface ICurrencyConversionService
{
    decimal Convert(string fromCurrency, string toCurrency, DateOnly date, decimal amount);
    IEnumerable<string> GetAvailableCurrencies(DateOnly? date = null);
    Dictionary<DateOnly, decimal> GetRateHistory(string currency, DateOnly startDate, DateOnly endDate);
}
```

---

# 5. Web API Guidelines

## 5.1 REST Standards
- Use nouns, not verbs.
- Prefer plural resource names when appropriate.
- Return:
  - `200 OK` for success  
  - `400 Bad Request` for validation failure  
  - `404 Not Found` for invalid currency/date  
  - `500 Internal Server Error` for unexpected failures  

## 5.2 Versioning
- Use ASP.NET API versioning package.
- Version via URL:
  - `/api/v1/...`
  - Future: `/api/v2/...`
- Controllers should include `[ApiVersion("1.0")]`.


## 5.3 Response Pattern Requirements

- Every endpoint should return a unified API response structure.
- Responses must include:
  - success indicator  
  - message field  
  - data (may be null)  
  - errors collection (for failures)  
- Avoid returning raw objects directly from controllers.


# 6. Documentation Practices

## 6.1 In-Code Documentation
- Use XML documentation on public classes and methods.
- Provide summary, parameters, and returns.

## 6.2 API Documentation
- Use Swagger/OpenAPI 3.
- Document:
  - Request models
  - Response models
  - Error formats

## 6.3 Readme Documentation
Include:
- Dataset description  
- Supported endpoints  
- Usage examples  
- Startup instructions  

---

# 7. Cross-Cutting Concerns

## 7.1 Exception Handling
- Use global exception middleware.
- Provide consistent error response structure:


## 7.2 Logging
- Use `ILogger<T>`.
- Log:
  - Startup dataset load summary
  - Warnings for invalid inputs
  - Unexpected service exceptions

## 7.3 CORS
- Allow configurable origins.
- Default: limit to specific client domains.

## 7.4 Rate Limiting (optional)
- Use built-in ASP.NET rate limiting middleware.
- Protect public APIs from misuse.

## 7.5 Health Check
- `/health` endpoint with in-memory status.

---

# 8. Currency Conversion Service Rules
- A single central service: `ICurrencyConversionService`.
- Supports:
  - Convert(currencyA, currencyB, date)
  - List available currencies
  - Fetch rate history ranges
- Use decimal arithmetic only.
- No floating-point operations.
- Return friendly error messages for missing currencies/dates.

---

# 9. C# Language Guidelines
- Enable nullable reference types.
- Use `record` for DTOs and settings.
- Keep methods short and single-responsibility.
- Prefer explicit types for clarity.
- Use expression-bodied members only when helpful.
- Avoid static state and global variables.
- Prefer dependency injection everywhere.

---

# 10. Performance Best Practices
- Optimize data structures for lookup speed.
- Pre-calculate frequently accessed values when useful.
- Consider caching conversion results.
- Avoid unnecessary LINQ overhead in hot paths.
- Keep startup fast by streaming JSON instead of loading large objects at once.
- Cache results of:
  - Repeated conversions
  - Frequently accessed dates

---

# 11. Security Guidelines
- Use HTTPS redirection and HSTS.
- Sanitize currency codes and dates.
- Do not trust client input.
- Store secrets using environment variables.
- Validate all query parameters and body inputs.

---


## 12. Error Handling Rules

- All errors must follow a consistent format inspired by ISO/RFC-7807.
- Error responses must include:
  - type (error category URI)  
  - title  
  - status  
  - detail  
  - instance (request path)  
  - errors (validation messages if any)  
- Validation errors must always use HTTP 400.
- Unhandled exceptions should be mapped to a consistent internal error response.
- Never expose stack traces or technical internals.

---

## 13. Exception-Handling Expectations

- Assume a global exception middleware is present.
- Do not generate try/catch blocks inside controllers unless truly necessary.
- Let the middleware convert exceptions into standardized API error responses.
- Ensure any thrown exceptions are meaningful and domain-focused.

---

## 14. Validation Behavior

- Validation should be handled through attributes or FluentValidation.
- Validation messages should always be mapped into the unified error format.
- Copilot must avoid returning exceptions directly to callers.

# 15. Git & DevOps Practices
- Use conventional commits:
  - `feat:`
  - `fix:`
  - `perf:`
  - `refactor:`
- PRs must include:
  - Summary of change
  - Reason
  - Tests if applicable
- GitHub Actions pipeline:
  - Build → test → publish artifacts → deploy
- Keep dependencies minimal.

---

# 16. Testing Guidelines
- Unit test:
  - Conversion service
  - Rate lookup logic
  - Date validations
- Use in-memory dataset for testing.
- Avoid controller tests unless necessary.
- Use xUnit with Moq/NSubstitute.

---

# 17. How GitHub Copilot Should Behave
- Follow this architecture strictly.
- Prefer lightweight and readable implementations.
- Generate code using the folder structure above.
- Avoid repository patterns or unnecessary abstractions.
- Use modern C# features responsibly.
- Produce code with:
  - clear naming
  - short methods
  - deterministic behavior
- For example snippets, always place files in proper folders.

---

## 18. Cross-Cutting Concerns

- Use structured logging for errors and request lifecycle events.
- Maintain consistent correlation IDs for logs and responses.
- Assume currency rates are loaded once at startup and cached in memory.
- Keep request processing lightweight and fast.
