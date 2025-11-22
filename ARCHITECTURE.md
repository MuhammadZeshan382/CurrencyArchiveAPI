# CurrencyArchiveAPI - Project Structure

## Overview
Lean architecture ASP.NET Core 8 API for historical currency exchange rates (1999-2025).  
All data is loaded into memory at startup using parallel processing for optimal performance.

---

## Folder Structure

```
CurrencyArchiveAPI/
├── Controllers/              # HTTP request handlers (no business logic)
│   ├── CurrencyController.cs # Currency operations endpoints (stubs)
│   └── HealthController.cs   # Service health monitoring
├── Services/                 # Business logic layer
│   ├── ICurrencyDataService.cs           # Data access interface
│   ├── CurrencyDataService.cs            # In-memory data management
│   ├── ICurrencyConversionService.cs     # Conversion interface
│   └── CurrencyConversionService.cs      # EUR-based conversion logic
├── Models/                   # Data transfer objects
│   ├── ApiResponse.cs        # Unified success response wrapper
│   └── ProblemDetailsResponse.cs # RFC 7807 error response
├── Middleware/               # Cross-cutting concerns
│   ├── CurrencyDataLoaderMiddleware.cs   # Lazy data loading
│   └── GlobalExceptionMiddleware.cs      # Centralized error handling
├── Extensions/               # Service registration
│   └── ServiceExtensions.cs  # DI configuration
├── Data/                     # JSON files (1999-2025)
│   └── {Year}/{Month}/DD-MM-YYYY.json
├── Program.cs                # Application bootstrap
└── Convert.cs                # ⚠️ LEGACY - Do not use
```

---

## Key Components

### 1. **CurrencyDataService** (Singleton)
- Loads ~9,500 JSON files at startup using `Parallel.ForEach`
- Stores data in `FrozenDictionary<DateOnly, FrozenDictionary<string, decimal>>`
- Provides O(1) lookups for any date/currency combination
- ~25MB memory footprint for entire 26-year dataset

### 2. **CurrencyConversionService** (Scoped)
- Implements EUR-based cross-rate conversion: `(amount / fromRate) * toRate`
- Returns `decimal` (never `double` or `float`)
- Throws domain exceptions for invalid currencies/dates

### 3. **CurrencyDataLoaderMiddleware**
- Lazy initialization on first HTTP request
- Thread-safe singleton pattern with `SemaphoreSlim`
- Returns 503 if data not loaded

### 4. **GlobalExceptionMiddleware**
- Converts all exceptions to RFC 7807 Problem Details
- Maps domain exceptions to appropriate HTTP status codes
- Never exposes stack traces in production

### 5. **ApiResponse<T>**
- Unified response format: `{ success, message, data, errors }`
- Used by all successful endpoints

---

## Data Format

### File Structure
```
Data/2024/January/01-01-2024.json
```

### Content Example
```json
{
  "EUR": 1,
  "USD": 1.103769,
  "GBP": 0.866892,
  "BTC": 0.000024995148,
  ...170+ currencies
}
```

---

## Startup Flow

1. **Program.cs** registers services via `AddCurrencyServices()`
2. **Middleware pipeline** configured:
   - Global exception handler
   - Currency data loader
3. **First HTTP request** triggers data loading:
   - `Parallel.ForEach` processes all JSON files
   - Results stored in frozen dictionaries
   - Logs summary: file count, date range, duration
4. **Subsequent requests** use in-memory cache

---

## Design Principles Followed

✅ **Lean Architecture** - No repository pattern, no unnecessary abstraction  
✅ **Decimal Precision** - All rates use `decimal` type  
✅ **Immutable Data** - `FrozenDictionary` for read-only post-load  
✅ **Parallel Loading** - Multi-threaded file processing  
✅ **Global Error Handling** - RFC 7807 Problem Details  
✅ **Dependency Injection** - All services registered in DI container  
✅ **Structured Logging** - `ILogger<T>` throughout  
✅ **Unified Responses** - `ApiResponse<T>` wrapper  

---

## Current Status

### ✅ Implemented
- Complete service layer with interfaces
- Parallel data loading middleware
- Global exception handling
- Unified response models
- Health check endpoint
- DI configuration
- Swagger documentation

### 📝 TODO (Not Implemented Yet)
- Controller endpoint implementations (stubs only)
- API versioning package
- Input validation (FluentValidation)
- CORS configuration
- Rate limiting
- Unit tests
- Integration tests

---

## API Endpoints (Stubs)

All endpoints return placeholder responses:

```
GET /api/v1/currency/rate/{currencyCode}?date=2024-01-01
GET /api/v1/currency/currencies?date=2024-01-01
GET /api/v1/currency/rates?date=2024-01-01
GET /api/v1/currency/info
GET /api/health
```

---

## Next Steps

1. Implement actual endpoint logic in `CurrencyController`
2. Add FluentValidation for request validation
3. Install and configure API versioning
4. Add unit tests for conversion logic
5. Configure CORS and rate limiting
6. Add integration tests
7. Document API with XML comments for Swagger

---

## Testing Locally

```bash
# Build
dotnet build

# Run
dotnet run

# Access Swagger
https://localhost:7268/swagger
```

The data will be loaded on the first API call (lazy loading).
