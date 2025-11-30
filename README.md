# CurrencyArchiveAPI

[![CI/CD Pipeline](https://github.com/MuhammadZeshan382/CurrencyArchiveAPI/actions/workflows/ci-cd-pipeline.yml/badge.svg)](https://github.com/MuhammadZeshan382/CurrencyArchiveAPI/actions/workflows/ci-cd-pipeline.yml)
[![Version](https://img.shields.io/badge/Version-1.0.0.56-blue.svg?logo=github&logoColor=white)](https://github.com/MuhammadZeshan382/CurrencyArchiveAPI/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Docker Version](https://img.shields.io/docker/v/mzeshawn/currencyarchiveapi?sort=semver&logo=docker&logoColor=white&label=Docker&color=2496ED)](https://hub.docker.com/r/mzeshawn/currencyarchiveapi)
[![Swagger](https://img.shields.io/badge/API-Swagger-85EA2D?logo=swagger&logoColor=white)](https://currencyapi.vitalmedx.com)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?logo=opensourceinitiative&logoColor=white)](LICENSE)


## Overview

A fast, free currency exchange API with 25+ years of historical data. Load 9,500+ days of rates for 160+ currencies into memory (only ~25 MB) and get instant conversions. Built with .NET 8.

**[Try the live API](https://currencyapi.vitalmedx.com)**

### Why I Built This

I was learning time series analysis and forecasting, but I couldn't find enough data to practice with. Most datasets were either incomplete or locked behind paid APIs with monthly subscriptions. That didn't make sense for someone just trying to learn.

So I started scraping currency data from reliable sources and organizing it properly. I did this years ago and kept the data with me for a long time. At first, I thought about just uploading the JSON files to GitHub. But then I realized people would still need to download files, parse them, and write their own conversion logic. That's a lot of work.

Instead, I built this API on top of the data. Now anyone can make a simple HTTP request and get instant results. No downloads, no parsing, no setup.

**The main problems this solves:**

Most currency APIs charge $20-$30 per month based on the number of requests you make. Free alternatives only offer 20-30 currencies with incomplete data.

- **Paid APIs are expensive**: Monthly costs add up when you're charged per request
- **Free APIs are limited**: Usually only 20-30 currencies with gaps in historical data
- **Poor documentation**: Hard to understand and use
- **Complex setup**: Requires databases and external dependencies
- **Slow responses**: Takes seconds for simple conversions

This API loads 25 years of forex data into memory at startup. No database needed, and responses come back in milliseconds.

**Who can use this?**
- Financial analysts doing trend analysis and research
- Frontend developers learning to build apps without API costs
- ML engineers building and training prediction models
- Students learning time series analysis and forecasting
- Anyone needing historical currency data without monthly fees

### What You Get

- **Complete data**: Forex rates from 1999 to now with proper calculations
- **No database needed**: JSON files load into memory at startup
- **Accurate conversions**: EUR-based cross-rate math for any currency pair
- **Wide coverage**: 160+ currencies across 9,500+ trading days
- **Fast performance**: 25 years of data in just 25 MB of RAM
- **Easy to maintain**: Clean folder structure organized by year, month, and day

---

## Features

- Convert between any of 160+ currencies
- Pull historical rates from any date back to 1999
- Get time-series data across custom date ranges
- Financial analytics: volatility, returns, momentum indicators
- Rolling window stats for trend analysis
- Switch base currency on the fly
- Filter results by currency symbols
- Versioned API (v1.0, ready for future versions)
- Full Swagger docs for testing
- Docker support out of the box
- Proper error handling that actually tells you what went wrong
- Health check endpoint

---

## Tech Stack

- .NET 8 (ASP.NET Core)
- Asp.Versioning.Mvc 8.1.0
- Swagger/OpenAPI (Swashbuckle 6.4.0)
- In-memory dictionaries (JSON files loaded at startup)
- Docker
- Standard dependency injection, nothing fancy

---

## Project Structure

```
CurrencyArchiveAPI/
├── src/
│   ├── Controllers/           # API endpoint controllers
│   │   ├── ConversionController.cs
│   │   ├── HistoricalRatesController.cs
│   │   ├── AnalyticsController.cs
│   │   └── HealthController.cs
│   ├── Services/              # Business logic layer
│   │   ├── CurrencyDataService.cs
│   │   ├── CurrencyConverterService.cs
│   │   ├── HistoricalRatesService.cs
│   │   └── FinancialAnalyticsService.cs
│   ├── Models/                # Request/Response DTOs
│   │   ├── ApiResponse.cs
│   │   ├── CurrencyResponseModels.cs
│   │   └── ProblemDetailsResponse.cs
│   ├── Middleware/            # Cross-cutting concerns
│   │   ├── CurrencyDataLoaderMiddleware.cs
│   │   └── GlobalExceptionMiddleware.cs
│   ├── Helpers/               # Utility and calculation helpers
│   ├── Extensions/            # Service registration extensions
│   ├── Constants/             # Application constants
│   ├── Documentation/         # API documentation resources
│   │   ├── ApiDocumentation.json
│   │   └── ApiDocumentationFilter.cs
│   ├── Data/                  # Historical rate files
│   │   └── EUR/
│   │       ├── 1999/
│   │       │   ├── January/
│   │       │   │   ├── 04-01-1999.json
│   │       │   │   └── ...
│   │       │   ├── February/
│   │       │   └── ...
│   │       ├── 2000/
│   │       └── ... (through 2025)
│   ├── Program.cs             # Application entry point
│   └── Dockerfile             # Container configuration
├── CurrencyArchiveAPI.sln     # Solution file
└── README.md                  # This file
```

---

## Getting Started

### What You Need

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Optional) [Docker](https://www.docker.com/get-started) if you want to containerize it

### Quick Start

**Want to try it first?** Check out the [live deployment](https://currencyapi.vitalmedx.com).

**Run it locally:**

```bash
git clone https://github.com/MuhammadZeshan382/CurrencyArchiveAPI.git
cd CurrencyArchiveAPI/src
dotnet restore
dotnet run
```

Hit `http://localhost:5000` and you're good to go.

### Docker Route

If you prefer containers:

```bash
docker build -t currencyarchiveapi:latest -f src/Dockerfile .
docker run -d -p 8080:8080 --name currencyapi currencyarchiveapi:latest
```

Now it's running at `http://localhost:8080`

---

## API Docs

Check out the interactive Swagger UI:

| Environment | Swagger Documentation |
|-------------|-----------------------|
| **Live** | [Open Swagger UI](https://currencyapi.vitalmedx.com) |
| **Local** | `http://localhost:5000` or `http://localhost:5000/swagger` |
| **Docker** | `http://localhost:8080` or `http://localhost:8080/swagger` |

Play around with the endpoints on the [live deployment](https://currencyapi.vitalmedx.com), see examples, test queries—it's all there.

---

## How It Works

### The Cross-Rate Math

All the data uses EUR as the base (that's what I could scrape consistently). To convert USD → GBP, we do:

`Result = (Amount / FromRate) × ToRate`

Real example: 100 USD to GBP on 2024-01-15
- 1 EUR = 1.103769 USD
- 1 EUR = 0.866892 GBP
- (100 / 1.103769) × 0.866892 = 78.54 GBP

Simple cross-rate calculation, works for any currency pair.

### Startup Process

When the app starts:
1. Scans `Data/EUR/` folders (Year/Month/Day structure)
2. Parses every JSON file
3. Dumps it all into `Dictionary<DateOnly, Dictionary<string, decimal>>`
4. Uses ~25 MB of RAM total
5. Lookups happen in O(1) time

---

## What's Next

Got some ideas for where to take this. Feel free to grab any of these and run with it:

- **Unit tests** - Yeah, I know. They're coming. All methods need proper test coverage.
- **USD data source** - Adding another base currency dataset for better accuracy and redundancy
- **Auto-updates** - GitHub Action to automatically fetch and update historical data daily
- **Performance tweaks** - Always room to make endpoints faster. Caching, optimization, the works.
- **ML forecasting** - Currency predictions using ML.NET. Could be interesting.
- **Better docs** - Dedicated documentation site with examples and guides
- **Live rates** - Real-time currency data integration for current market prices
- **Demo client app** - A simple web or desktop app to show the API in action with visual examples

Want to implement one? Cool:

1. Check [Issues](https://github.com/MuhammadZeshan382/CurrencyArchiveAPI/issues) to see if someone's already working on it
2. Fork it, build it, test it
3. Send a PR

No bureaucracy, just good code.

---

## Contributing

Pull requests welcome! Just:

1. Fork it and make a branch
2. Keep the code style consistent
3. Use sensible commit messages (feat:, fix:, docs:, etc.)
4. Update docs if you change how something works
5. Open a PR

### Code Rules

- Name things clearly
- Stick to C# conventions
- One method = one job
- Add XML docs for public APIs
- Always use `decimal` for money math (never float/double)

---

## License

MIT License - do whatever you want with it.

---

## Star It?

If this saved you some time (or money on a paid API):

-  Star the repo
-  Share it with someone who needs it
-  Report bugs or suggest features in [Issues](https://github.com/MuhammadZeshan382/CurrencyArchiveAPI/issues)
-  Send a PR if you build something cool

---

Made with ❤️❤️❤️.