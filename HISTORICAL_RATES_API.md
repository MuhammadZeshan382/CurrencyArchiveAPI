# Historical Rates Endpoint - Usage Examples

## Endpoint
```
GET /api/{YYYY-MM-DD}?base={BASE}&symbols={SYMBOLS}
```

## Parameters

| Parameter | Required | Description | Example |
|-----------|----------|-------------|---------|
| `YYYY-MM-DD` | Yes | Date in ISO format | `2013-12-24` |
| `base` | No | Base currency code (default: EUR) | `GBP` |
| `symbols` | No | Comma-separated currency codes | `USD,CAD,EUR` |

---

## Example Requests

### 1. Basic Request (EUR base, all currencies)
```http
GET https://localhost:7268/api/2013-12-24
```

**Response:**
```json
{
  "success": true,
  "message": "Historical rates retrieved successfully for 2013-12-24",
  "data": {
    "date": "2013-12-24",
    "base": "EUR",
    "rates": {
      "USD": 1.3791,
      "GBP": 0.8337,
      "CAD": 1.4671,
      "JPY": 144.72,
      // ... 170+ currencies
    },
    "timestamp": "2025-11-23T10:30:00.000Z"
  },
  "errors": []
}
```

### 2. Custom Base Currency (GBP)
```http
GET https://localhost:7268/api/2013-12-24?base=GBP
```

Converts all rates from EUR-base to GBP-base.

**Response:**
```json
{
  "success": true,
  "message": "Historical rates retrieved successfully for 2013-12-24",
  "data": {
    "date": "2013-12-24",
    "base": "GBP",
    "rates": {
      "GBP": 1.0,
      "USD": 1.6540,
      "EUR": 1.1997,
      "CAD": 1.7599,
      // ... all currencies relative to GBP
    },
    "timestamp": "2025-11-23T10:30:00.000Z"
  },
  "errors": []
}
```

### 3. Custom Base with Symbol Filter
```http
GET https://localhost:7268/api/2013-12-24?base=GBP&symbols=USD,CAD,EUR
```

**Response:**
```json
{
  "success": true,
  "message": "Historical rates retrieved successfully for 2013-12-24",
  "data": {
    "date": "2013-12-24",
    "base": "GBP",
    "rates": {
      "GBP": 1.0,
      "USD": 1.6540,
      "CAD": 1.7599,
      "EUR": 1.1997
    },
    "timestamp": "2025-11-23T10:30:00.000Z"
  },
  "errors": []
}
```

### 4. EUR Base with Symbol Filter
```http
GET https://localhost:7268/api/2024-01-01?base=EUR&symbols=USD,GBP,JPY
```

**Response:**
```json
{
  "success": true,
  "message": "Historical rates retrieved successfully for 2024-01-01",
  "data": {
    "date": "2024-01-01",
    "base": "EUR",
    "rates": {
      "USD": 1.103769,
      "GBP": 0.866892,
      "JPY": 156.33
    },
    "timestamp": "2025-11-23T10:30:00.000Z"
  },
  "errors": []
}
```

---

## How It Works

### Base Currency Conversion Logic

All data is stored with EUR as the base. When you request a different base:

**Example: Converting USD rate when base=GBP**

1. **Stored data (EUR base):**
   - EUR → USD = 1.3791
   - EUR → GBP = 0.8337

2. **Conversion formula:**
   ```
   GBP → USD = (EUR → USD) / (EUR → GBP)
   GBP → USD = 1.3791 / 0.8337
   GBP → USD = 1.6540
   ```

3. **Result:**
   - 1 GBP = 1.6540 USD

This is implemented using **decimal arithmetic** for precision:
```csharp
decimal gbpToUsd = eurToUsd / eurToGbp;
```

### Symbol Filtering

When `symbols` parameter is provided:
1. Parse comma-separated list
2. Convert to uppercase
3. Remove duplicates
4. Filter rates to include only requested currencies
5. Always include base currency (rate = 1.0)

---

## Error Responses

### Invalid Date Format
```http
GET https://localhost:7268/api/24-12-2013
```
```json
{
  "success": false,
  "message": "Invalid date format",
  "data": null,
  "errors": ["Date must be in YYYY-MM-DD format (e.g., 2013-12-24)"]
}
```

### Date Not Found
```http
GET https://localhost:7268/api/1990-01-01
```
```json
{
  "success": false,
  "message": "No rates found",
  "data": null,
  "errors": ["No exchange rates available for 1990-01-01"]
}
```

### Invalid Base Currency
```http
GET https://localhost:7268/api/2024-01-01?base=XYZ
```
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Base currency XYZ not found for 2024-01-01",
  "instance": "/api/2024-01-01"
}
```

---

## Performance Notes

- **All data is in-memory**: ~9,500 days × 170 currencies = 25MB
- **Parallel loading**: Data loads using all CPU cores
- **O(1) lookups**: FrozenDictionary for instant access
- **Decimal precision**: No floating-point errors
- **Thread-safe**: Singleton service with immutable data

---

## Date Range Available

- **From:** 1999-01-04 (first available date)
- **To:** 2025-11-23 (current data)
- **Total:** ~9,500 trading days
- **Currencies:** 170+ including crypto (BTC, ETH, etc.)
