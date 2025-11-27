
using CurrencyArchiveAPI.Constants;
using CurrencyArchiveAPI.Models;
using CurrencyArchiveAPI.Utilities;
using System.Collections.Concurrent;

namespace CurrencyArchiveAPI.Helpers;

/// <summary>
/// Helper class for calculating comprehensive currency metrics.
/// Handles price statistics, returns, volatility, risk metrics, and technical indicators.
/// 
/// Key fixes:
/// - Standardize internal units: all return and volatility metrics are kept in decimal (e.g., 0.0028 = 0.28%).
/// - Compute cumulative/annualized returns from the returns series (geometric when appropriate).
/// - Compute volatilities from return series and then annualize using sqrt(TradingDaysPerYear).
/// - Compute Sharpe ratio using annualized return and annualized volatility (no percent/decimal mixing).
/// - Ensure VaR functions receive daily-decimal units and outputs are converted to percent only at the DTO layer.
/// </summary>
public class CurrencyMetricsCalculatorHelper
{
    /// <summary>
    /// Calculates comprehensive metrics for a single currency.
    /// </summary>
    public CurrencyVolatilityMetrics? CalculateCurrencyMetrics(
        string currency,
        Dictionary<string, Dictionary<string, decimal>> timeseriesData,
        ConcurrentDictionary<string, double[]> currencyReturnsDict,
        RateCollectionHelper rateCollectionHelper)
    {
        var ratesList = rateCollectionHelper.CollectRatesForCurrency(currency, timeseriesData);

        if (ratesList.Count < 2) return null;

        var rates = ratesList.ToArray();
        var n = rates.Length;

        var priceStats = CalculatePriceStatistics(rates);

        // Calculate returns (decimal daily returns) and log returns
        var dailyReturns = ReturnsCalculator.CalculateDailyReturns(rates); // double[] decimal returns like 0.001
        currencyReturnsDict[currency] = dailyReturns;

        var logReturns = ReturnsCalculator.CalculateLogReturns(rates);

        // Compute return metrics using the returns series (geometric annualization)
        var returnMetrics = CalculateReturnMetrics(dailyReturns);

        // Compute volatility metrics from logReturns (daily decimal) and annualize
        var volatilityMetrics = CalculateVolatilityMetrics(logReturns);

        // Compute risk metrics (need daily values)
        var riskMetrics = CalculateRiskMetrics(rates, dailyReturns, returnMetrics.avgDailyReturn, volatilityMetrics.dailyVolatility);

        // Technical indicators (z-score uses price-level std dev computed from full rates array)
        var technicalIndicators = CalculateTechnicalIndicators(rates, priceStats);

        var rollingMetrics = RollingWindowCalculator.CalculateRollingMetrics(rates, dailyReturns);

        // Compute price variance/stddev from full rates for DTO fields
        var priceVariance = StatisticsCalculator.CalculateVariance(rates, priceStats.average);
        var priceStdDev = (decimal)Math.Sqrt((double)priceVariance);

        return BuildVolatilityMetrics(priceStats, returnMetrics, volatilityMetrics, riskMetrics, technicalIndicators, rollingMetrics, n, priceVariance, priceStdDev);
    }

    /// <summary>
    /// Calculates basic price statistics (min, max, average, open, close, change).
    /// </summary>
    public (decimal min, decimal max, decimal average, decimal openRate, decimal closeRate, decimal change, decimal changePct) CalculatePriceStatistics(decimal[] rates)
    {
        var n = rates.Length;
        var min = decimal.MaxValue;
        var max = decimal.MinValue;
        var sum = 0m;

        for (int i = 0; i < n; i++)
        {
            if (rates[i] < min) min = rates[i];
            if (rates[i] > max) max = rates[i];
            sum += rates[i];
        }

        var average = sum / n;
        var openRate = rates[0];
        var closeRate = rates[n - 1];
        var change = closeRate - openRate;
        var changePct = openRate != 0
            ? (change / openRate) * AppConstants.Financial.PercentageMultiplier
            : 0;

        return (min, max, average, openRate, closeRate, change, changePct);
    }

    /// <summary>
    /// Calculates return metrics (daily mean, cumulative and annualized) from the daily returns series.
    /// Uses geometric compounding for cumulative return: Product(1 + DailyReturn[i]) - 1
    /// Annualized return: (1 + CumulativeReturn)^(252 / totalDays) - 1
    /// All return metrics are kept as **decimal** in daily/annual decimals (e.g., 0.01 == 1%).
    /// </summary>
    public (decimal avgDailyReturn, decimal cumulativeReturn, decimal annualizedReturn) CalculateReturnMetrics(double[] dailyReturns)
    {
        var totalDays = dailyReturns.Length;
        if (totalDays == 0) return (0m, 0m, 0m);

        // Average daily return (arithmetic mean) - used for display purposes
        var avgDailyReturn = (decimal)StatisticsCalculator.CalculateMean(dailyReturns);

        // Cumulative return using geometric compounding
        // Formula: CumulativeReturn = Product(1 + r[i]) - 1
        double cumulativeProduct = 1.0;
        foreach (var r in dailyReturns)
        {
            cumulativeProduct *= (1.0 + r);
        }
        var cumulativeReturn = (decimal)(cumulativeProduct - 1.0);

        // Annualized return using geometric method
        // Formula: AnnualizedReturn = (1 + CumulativeReturn)^(252 / totalDays) - 1
        var annualizedReturn = (decimal)(Math.Pow(
            (double)(1.0m + cumulativeReturn),
            (double)AppConstants.Financial.TradingDaysPerYear / totalDays
        ) - 1.0);

        return (avgDailyReturn, cumulativeReturn, annualizedReturn);
    }

    /// <summary>
    /// Calculates volatility metrics from log returns.
    /// Returns daily volatility (decimal) and annualized volatility (decimal).
    /// </summary>
    public (decimal dailyVolatility, decimal annualizedVolatility) CalculateVolatilityMetrics(double[] logReturns)
    {
        // logReturns should be in decimal (e.g., 0.0012 for 0.12%)
        var dailyVolatility = (decimal)StatisticsCalculator.CalculateStdDev(logReturns); // decimal daily
        var annualizedVolatility = dailyVolatility * (decimal)Math.Sqrt(AppConstants.Financial.TradingDaysPerYear);

        return (dailyVolatility, annualizedVolatility);
    }

    /// <summary>
    /// Calculates risk metrics (max drawdown, VaR).
    /// All VaR outputs are **daily decimals** (e.g., -0.0075 == -0.75% daily loss).
    /// Sharpe ratio will be computed later after annualized values are known.
    /// </summary>
    public (decimal maxDrawdown, decimal sharpeRatio, decimal historicalVaR95, decimal parametricVaR95) CalculateRiskMetrics(
        decimal[] rates,
        double[] dailyReturns,
        decimal avgDailyReturn,
        decimal dailyVolatility)
    {
        var (maxDrawdown, _) = RiskCalculator.CalculateDrawdown(rates);

        // RiskCalculator.CalculateVaR expected inputs: daily returns (double[]), mean (double), std (double)
        var (historicalVaR95, parametricVaR95) = RiskCalculator.CalculateVaR(
            dailyReturns,
            (double)avgDailyReturn,
            (double)dailyVolatility);

        var sharpeRatio = 0m; // placeholder; computed at DTO level where annualized values are present

        return (maxDrawdown, sharpeRatio, historicalVaR95, parametricVaR95);
    }

    /// <summary>
    /// Calculates technical indicators (momentum, SMA, z-score).
    /// Z-score uses price-level stddev computed from full price series variance (not returns).
    /// </summary>
    public (decimal? momentum3M, decimal? momentum12M, decimal? sma50, decimal? sma200, decimal zScore) CalculateTechnicalIndicators(
        decimal[] rates,
        (decimal min, decimal max, decimal average, decimal openRate, decimal closeRate, decimal change, decimal changePct) priceStats)
    {
        var momentum3M = MomentumCalculator.CalculateMomentum(rates, AppConstants.Financial.TradingDays3Months);
        var momentum12M = MomentumCalculator.CalculateMomentum(rates, AppConstants.Financial.TradingDays12Months);
        var sma50 = MomentumCalculator.CalculateSMA(rates, AppConstants.Financial.SMA50Period);
        var sma200 = MomentumCalculator.CalculateSMA(rates, AppConstants.Financial.SMA200Period);

        var variance = StatisticsCalculator.CalculateVariance(rates, priceStats.average);
        var stdDev = (decimal)Math.Sqrt((double)variance);
        var zScore = MomentumCalculator.CalculateZScore(priceStats.closeRate, priceStats.average, stdDev);

        return (momentum3M, momentum12M, sma50, sma200, zScore);
    }

    /// <summary>
    /// Builds the complete CurrencyVolatilityMetrics object from calculated components.
    /// All returned numeric values in the DTO are presented as percentages where appropriate:
    /// - Rates (Min, Max, Average, OpenRate, CloseRate, SMA*) remain as raw exchange rates.
    /// - Returns/Volatility/VaR/Drawdown/Momentum are converted to percentage using PercentageMultiplier for presentation.
    /// - Sharpe ratio is unitless and computed using annual decimals.
    /// </summary>
    public CurrencyVolatilityMetrics BuildVolatilityMetrics(
        (decimal min, decimal max, decimal average, decimal openRate, decimal closeRate, decimal change, decimal changePct) priceStats,
        (decimal avgDailyReturn, decimal cumulativeReturn, decimal annualizedReturn) returnMetrics,
        (decimal dailyVolatility, decimal annualizedVolatility) volatilityMetrics,
        (decimal maxDrawdown, decimal sharpeRatio, decimal historicalVaR95, decimal parametricVaR95) riskMetrics,
        (decimal? momentum3M, decimal? momentum12M, decimal? sma50, decimal? sma200, decimal zScore) technicalIndicators,
        RollingMetrics? rollingMetrics,
        int dataPoints,
        decimal priceVariance,
        decimal priceStdDev)
    {
        // Coefficient of Variation: CV = (StdDev / Average) × 100
        // Calculated as ratio then converted to percentage
        var coefficientOfVariation = priceStats.average != 0
            ? (priceStdDev / priceStats.average) * AppConstants.Financial.PercentageMultiplier
            : 0m;

        var rangePct = priceStats.average != 0
            ? ((priceStats.max - priceStats.min) / priceStats.average) * AppConstants.Financial.PercentageMultiplier
            : 0m;

        // Compute Sharpe ratio: (AnnualizedReturn - RiskFreeRate) / AnnualizedVolatility
        // All values must be in decimal form (e.g., 0.08 for 8%, 0.04 for 4%, 0.12 for 12%)
        // Formula: SharpeRatio = (AR - RF) / AV where all are decimals
        decimal sharpe = 0m;
        if (volatilityMetrics.annualizedVolatility != 0)
        {
            sharpe = (returnMetrics.annualizedReturn - AppConstants.Financial.RiskFreeRate) / volatilityMetrics.annualizedVolatility;
        }

        return new CurrencyVolatilityMetrics
        {
            Min = Math.Round(priceStats.min, AppConstants.Precision.ExchangeRatePrecision),
            Max = Math.Round(priceStats.max, AppConstants.Precision.ExchangeRatePrecision),
            Average = Math.Round(priceStats.average, AppConstants.Precision.ExchangeRatePrecision),
            OpenRate = Math.Round(priceStats.openRate, AppConstants.Precision.ExchangeRatePrecision),
            CloseRate = Math.Round(priceStats.closeRate, AppConstants.Precision.ExchangeRatePrecision),
            Change = Math.Round(priceStats.change, AppConstants.Precision.ExchangeRatePrecision),
            ChangePct = Math.Round(priceStats.changePct, AppConstants.Precision.ChangePrecision),

            // Price-level dispersion
            StdDev = Math.Round(priceStdDev, AppConstants.Precision.StandardDeviationPrecision),
            Variance = Math.Round(priceVariance, AppConstants.Precision.VariancePrecision),
            CoefficientOfVariation = Math.Round(coefficientOfVariation, AppConstants.Precision.ChangePrecision),

            // Volatility and returns presented as percentages for UI/JSON
            AnnualizedVolatility = Math.Round(volatilityMetrics.annualizedVolatility * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),
            RangePct = Math.Round(rangePct, AppConstants.Precision.ChangePrecision),
            DataPoints = dataPoints,

            AvgDailyReturn = Math.Round(returnMetrics.avgDailyReturn * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),
            CumulativeReturn = Math.Round(returnMetrics.cumulativeReturn * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),
            AnnualizedReturn = Math.Round(returnMetrics.annualizedReturn * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),

            DailyVolatility = Math.Round(volatilityMetrics.dailyVolatility * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),
            MaxDrawdown = Math.Round(riskMetrics.maxDrawdown * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),

            SharpeRatio = Math.Round(sharpe, AppConstants.Precision.ChangePrecision),
            RiskFreeRate = AppConstants.Financial.RiskFreeRate,

            HistoricalVaR95 = Math.Round(riskMetrics.historicalVaR95 * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),
            ParametricVaR95 = Math.Round(riskMetrics.parametricVaR95 * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision),

            ZScore = Math.Round(technicalIndicators.zScore, AppConstants.Precision.ChangePrecision),

            Momentum3M = technicalIndicators.momentum3M.HasValue
                ? Math.Round(technicalIndicators.momentum3M.Value * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision)
                : null,
            Momentum12M = technicalIndicators.momentum12M.HasValue
                ? Math.Round(technicalIndicators.momentum12M.Value * AppConstants.Financial.PercentageMultiplier, AppConstants.Precision.ChangePrecision)
                : null,
            SMA50 = technicalIndicators.sma50.HasValue
                ? Math.Round(technicalIndicators.sma50.Value, AppConstants.Precision.ExchangeRatePrecision)
                : null,
            SMA200 = technicalIndicators.sma200.HasValue
                ? Math.Round(technicalIndicators.sma200.Value, AppConstants.Precision.ExchangeRatePrecision)
                : null,

            Rolling = rollingMetrics
        };
    }

    /// <summary>
    /// Calculates comprehensive financial metrics for currency time series with fully corrected formulas.
    /// All calculations follow forex/financial industry standards with proper decimal handling.
    /// </summary>
    /// <param name="dailyReturns">Array of daily returns as decimals (e.g., 0.0028 = 0.28%)</param>
    /// <param name="logReturns">Array of log returns for volatility calculation</param>
    /// <param name="riskFreeRate">Risk-free rate as decimal (e.g., 0.04 = 4%)</param>
    /// <param name="meanPrice">Average price for CV calculation</param>
    /// <param name="stdDevPrice">Price standard deviation for CV calculation</param>
    /// <returns>Tuple of (avgDailyReturn, cumulativeReturn, annualizedReturn, sharpeRatio, coefficientOfVariation, dailyVolatility, annualizedVolatility)</returns>
    public (decimal avgDailyReturn, decimal cumulativeReturn, decimal annualizedReturn, decimal sharpeRatio,
            decimal coefficientOfVariation, decimal dailyVolatility, decimal annualizedVolatility)
        CalculateFinancialMetrics(
            double[] dailyReturns,
            double[] logReturns,
            decimal riskFreeRate,
            decimal meanPrice,
            decimal stdDevPrice)
    {
        var totalDays = dailyReturns.Length;

        // Validate inputs
        if (totalDays == 0 || logReturns.Length == 0)
        {
            return (0m, 0m, 0m, 0m, 0m, 0m, 0m);
        }

        // 1. Average daily return (arithmetic mean of daily returns)
        var avgDailyReturn = (decimal)StatisticsCalculator.CalculateMean(dailyReturns);

        // 2. Cumulative return using geometric compounding
        // Formula: CumulativeReturn = Product(1 + DailyReturn[i]) - 1
        double cumulativeProduct = 1.0;
        foreach (var r in dailyReturns)
        {
            cumulativeProduct *= (1.0 + r);
        }
        var cumulativeReturn = (decimal)(cumulativeProduct - 1.0);

        // 3. Annualized return using geometric method
        // Formula: AnnualizedReturn = (1 + CumulativeReturn)^(252 / totalDays) - 1
        var annualizedReturn = (decimal)(Math.Pow(
            (double)(1.0m + cumulativeReturn),
            (double)AppConstants.Financial.TradingDaysPerYear / totalDays
        ) - 1.0);

        // 4. Daily volatility from log returns (standard deviation)
        var dailyVolatility = (decimal)StatisticsCalculator.CalculateStdDev(logReturns);

        // 5. Annualized volatility
        // Formula: AnnualizedVolatility = DailyVolatility × √252
        var annualizedVolatility = dailyVolatility * (decimal)Math.Sqrt(AppConstants.Financial.TradingDaysPerYear);

        // 6. Sharpe ratio (risk-adjusted return)
        // Formula: SharpeRatio = (AnnualizedReturn - RiskFreeRate) / AnnualizedVolatility
        // All values must be in decimal form (no percentage conversion)
        decimal sharpeRatio = 0m;
        if (annualizedVolatility != 0)
        {
            sharpeRatio = (annualizedReturn - riskFreeRate) / annualizedVolatility;
        }

        // 7. Coefficient of Variation (CV)
        // Formula: CV = (StdDev / Average) × 100
        // Returns as percentage for display
        decimal coefficientOfVariation = 0m;
        if (meanPrice != 0)
        {
            coefficientOfVariation = (stdDevPrice / meanPrice) * AppConstants.Financial.PercentageMultiplier;
        }

        return (
            avgDailyReturn,           // Decimal (e.g., 0.0005 = 0.05% daily)
            cumulativeReturn,         // Decimal (e.g., 0.1250 = 12.50% total)
            annualizedReturn,         // Decimal (e.g., 0.0875 = 8.75% annual)
            sharpeRatio,              // Unitless ratio (e.g., 0.33)
            coefficientOfVariation,   // Percentage (e.g., 5.0 = 5.0%)
            dailyVolatility,          // Decimal (e.g., 0.0045 = 0.45% daily)
            annualizedVolatility      // Decimal (e.g., 0.0714 = 7.14% annual)
        );
    }
}
